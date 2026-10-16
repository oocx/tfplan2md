using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Compact primitive collection rows for <see cref="ResourceChangeStage"/>.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md.
/// </summary>
internal sealed partial class ResourceChangeStage
{
    /// <summary>
    /// Builds the single logical row for one primitive collection.
    /// </summary>
    /// <param name="change">The Terraform change payload.</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="collection">The detected collection.</param>
    /// <param name="beforeSensitive">Flattened before-state sensitivity map.</param>
    /// <param name="afterSensitive">Flattened after-state sensitivity map.</param>
    /// <returns>The row, or <c>null</c> when the collection is unchanged and unchanged values are hidden.</returns>
    private AttributeChangeModel? BuildCollectionAttribute(
        Change change,
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        DetectedCollection collection,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        var path = collection.Path;
        var isSensitive = IsSensitiveAttribute(path, beforeSensitive, afterSensitive)
            || HasSensitiveElement(path, beforeSensitive, afterSensitive);
        var masked = isSensitive && !_showSensitive;

        var beforeState = CollectionValues.StateOf(collection.Before);
        var afterState = CollectionValues.StateOf(collection.After);
        var unknownNode = AfterUnknownHelper.TryGetNode(change.AfterUnknown, path);
        var wholeUnknown = AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, path)
            && afterState is CollectionState.Absent or CollectionState.Null;
        var unknownFlags = wholeUnknown || masked ? [] : ReadUnknownFlags(unknownNode);
        string? beforeDisplay;
        string? afterDisplay;
        CollectionChangeModel model;
        bool valuesEqual;

        if (masked)
        {
            beforeDisplay = SensitiveMask;
            afterDisplay = SensitiveMask;
            valuesEqual = string.Equals(RawKey(collection.Before), RawKey(collection.After), StringComparison.Ordinal);
            model = new CollectionChangeModel { BeforeState = CollectionState.Masked, AfterState = CollectionState.Masked };
        }
        else
        {
            beforeDisplay = CollectionValues.CanonicalText(collection.Before);
            afterDisplay = wholeUnknown ? null : CollectionValues.CanonicalText(collection.After);
            model = BuildCollectionModel(collection, beforeState, afterState, wholeUnknown, unknownFlags);
            valuesEqual = !IsCollectionChanged(model);
        }

        ApplyComputedKnownAfterApplyOverride(change, configurationReferences, path, isSensitive, ref beforeDisplay, ref afterDisplay, ref valuesEqual);
        afterDisplay ??= "-";

        if (!_showUnchangedValues && valuesEqual)
        {
            return null;
        }

        return new AttributeChangeModel
        {
            Name = path,
            Before = beforeDisplay,
            After = afterDisplay,
            IsSensitive = isSensitive,
            IsLarge = false,
            Collection = model
        };
    }

    /// <summary>
    /// Builds the element lists, statuses and states for an unmasked collection.
    /// </summary>
    private static CollectionChangeModel BuildCollectionModel(
        DetectedCollection collection,
        CollectionState beforeState,
        CollectionState afterState,
        bool wholeUnknown,
        List<bool> unknownFlags)
    {
        var beforeElements = CollectionValues.ReadElements(collection.Before);
        var afterElements = CollectionValues.ReadElements(collection.After);
        var isUnresolved = unknownFlags.Contains(true);

        if (wholeUnknown)
        {
            return new CollectionChangeModel
            {
                BeforeState = beforeState,
                AfterState = CollectionState.Unknown,
                Before = ToModels(beforeElements, Enumerable.Repeat(CollectionElementStatus.Unchanged, beforeElements.Count).ToList())
            };
        }

        if (isUnresolved)
        {
            var count = Math.Max(afterElements.Count, unknownFlags.Count);
            var afterModels = new List<CollectionElementModel>(count);
            for (var index = 0; index < count; index++)
            {
                var unknown = index < unknownFlags.Count && unknownFlags[index];
                if (unknown || index >= afterElements.Count)
                {
                    afterModels.Add(new CollectionElementModel(index, null, JsonValueKind.Null, unknown ? CollectionElementStatus.Unknown : CollectionElementStatus.Unchanged));
                }
                else
                {
                    afterModels.Add(new CollectionElementModel(index, afterElements[index].Value, afterElements[index].Key.Kind, CollectionElementStatus.Unchanged));
                }
            }

            return new CollectionChangeModel
            {
                BeforeState = beforeState,
                AfterState = afterModels.Count == 0 ? afterState : CollectionState.Elements,
                Before = ToModels(beforeElements, Enumerable.Repeat(CollectionElementStatus.Unchanged, beforeElements.Count).ToList()),
                After = afterModels,
                IsUnresolved = true
            };
        }

        var diff = CollectionElementDiffer.Diff(beforeElements.ConvertAll(e => e.Key), afterElements.ConvertAll(e => e.Key));
        return new CollectionChangeModel
        {
            BeforeState = beforeState,
            AfterState = afterState,
            Before = ToModels(beforeElements, diff.Before),
            After = ToModels(afterElements, diff.After),
            OrderChanged = diff.OrderChanged
        };
    }

    /// <summary>
    /// Zips element texts with their statuses into model records.
    /// </summary>
    private static List<CollectionElementModel> ToModels(
        List<(string? Value, CollectionElementKey Key)> elements,
        IReadOnlyList<CollectionElementStatus> statuses)
        => elements.Select((element, index) => new CollectionElementModel(index, element.Value, element.Key.Kind, statuses[index])).ToList();

    /// <summary>
    /// Determines whether a collection model describes a visible change.
    /// </summary>
    private static bool IsCollectionChanged(CollectionChangeModel model)
    {
        if (model.IsUnresolved || model.AfterState == CollectionState.Unknown || model.OrderChanged)
        {
            return true;
        }

        if (Normalize(model.BeforeState) != Normalize(model.AfterState))
        {
            return true;
        }

        return model.Before.Concat(model.After).Any(element => element.Status != CollectionElementStatus.Unchanged);
    }

    /// <summary>
    /// Treats a JSON null collection like an absent one, as the previous per-key rows did.
    /// </summary>
    private static CollectionState Normalize(CollectionState state)
        => state == CollectionState.Null ? CollectionState.Absent : state;

    /// <summary>
    /// Returns comparable text for a side without exposing it; null and absent are equivalent.
    /// </summary>
    private static string RawKey(JsonElement? side)
        => side is null or { ValueKind: JsonValueKind.Null } ? string.Empty : CollectionValues.Compact(side.Value);

    /// <summary>
    /// Determines whether any element beneath a collection path is marked sensitive.
    /// </summary>
    private static bool HasSensitiveElement(
        string path,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        var prefix = path + "[";
        return IsMarkedUnder(beforeSensitive, prefix) || IsMarkedUnder(afterSensitive, prefix);
    }

    private static bool IsMarkedUnder(Dictionary<string, string?> sensitivity, string prefix)
        => sensitivity.Any(entry => entry.Value == "true" && entry.Key.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>
    /// Reads per-element unknown flags from an <c>after_unknown</c> node; malformed shapes yield no flags.
    /// </summary>
    private static List<bool> ReadUnknownFlags(JsonElement? node)
        => node is { ValueKind: JsonValueKind.Array } array
            ? array.EnumerateArray().Select(flag => flag.ValueKind == JsonValueKind.True).ToList()
            : [];
}
