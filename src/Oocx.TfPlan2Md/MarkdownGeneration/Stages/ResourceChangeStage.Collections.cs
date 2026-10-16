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
    /// <remarks>
    /// Sensitive masking is decided here, once, before any element text or comparison key exists
    /// (docs/features/148-compact-collection-diffs/architecture.md, section 5a). A collection marked sensitive as a
    /// whole is one masked value without elements. Otherwise each entry is masked on each side where that side's
    /// sensitivity map marks it, and a masked entry is compared only with masked entries through opaque tokens.
    /// </remarks>
    /// <param name="change">The Terraform change payload.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <param name="resourceType">The Terraform resource type.</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="collection">The detected collection.</param>
    /// <param name="beforeSensitive">Flattened before-state sensitivity map.</param>
    /// <param name="afterSensitive">Flattened after-state sensitivity map.</param>
    /// <returns>The row, or <c>null</c> when the collection is unchanged and unchanged values are hidden.</returns>
    private AttributeChangeModel? BuildCollectionAttribute(
        Change change,
        string providerName,
        string resourceType,
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        DetectedCollection collection,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        var path = collection.Path;
        var sensitivity = ReadSensitivity(path, beforeSensitive, afterSensitive);
        var wholeSensitive = sensitivity.Whole;
        var isSensitive = sensitivity.Any;
        var wholeMasked = wholeSensitive && !_showSensitive;
        var maskEntries = !wholeSensitive && !_showSensitive;

        var beforeState = CollectionValues.StateOf(collection.Before);
        var afterState = CollectionValues.StateOf(collection.After);
        var unknownNode = AfterUnknownHelper.TryGetNode(change.AfterUnknown, path);
        var wholeUnknown = AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, path)
            && afterState is CollectionState.Absent or CollectionState.Null;
        var unknownFlags = wholeUnknown || wholeMasked ? [] : ReadUnknownFlags(unknownNode);
        string? beforeDisplay;
        string? afterDisplay;
        CollectionChangeModel model;
        bool valuesEqual;

        if (wholeMasked)
        {
            beforeDisplay = SensitiveMask;
            afterDisplay = SensitiveMask;
            valuesEqual = string.Equals(RawKey(collection.Before), RawKey(collection.After), StringComparison.Ordinal);
            model = new CollectionChangeModel { BeforeState = CollectionState.Masked, AfterState = CollectionState.Masked };
        }
        else
        {
            var beforeMarks = maskEntries ? sensitivity.BeforeMarks : [];
            var afterMarks = maskEntries ? sensitivity.AfterMarks : [];
            var (before, after) = PrepareElements(collection, beforeMarks, afterMarks);
            model = BuildCollectionModel(
                before,
                after,
                beforeState,
                afterState,
                wholeUnknown,
                unknownFlags,
                ResolveKnownAfterApplyLabel(configurationReferences, path),
                afterMarks);
            (beforeDisplay, afterDisplay) = CanonicalTexts(collection, model, maskEntries && isSensitive, wholeUnknown);
            valuesEqual = !IsCollectionChanged(model);
        }

        // Only whole-collection sensitivity takes part: with per-entry masking a wholly unknown after side must not
        // turn the visible entries of the before side into one masked value.
        ApplyComputedKnownAfterApplyOverride(change, configurationReferences, path, wholeSensitive, ref beforeDisplay, ref afterDisplay, ref valuesEqual);
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
            IsLarge = !wholeMasked && HasLargeElement(model, providerName, resourceType, path),
            Collection = model
        };
    }

    /// <summary>
    /// Determines whether any known, non-masked element needs the dedicated large-value section. Masked entries never
    /// take part, so a masked value's length cannot change how the row renders.
    /// </summary>
    private bool HasLargeElement(CollectionChangeModel model, string providerName, string resourceType, string path)
        => model.Before.Concat(model.After).Any(element =>
            element.Status != CollectionElementStatus.Unknown
            && !element.IsMasked
            && IsLargeUnresolvedValue(element.Value, providerName, resourceType, $"{path}[{element.Index}]"));

    /// <summary>
    /// The sensitivity of one collection: whether it is marked as a whole, whether anything in it is marked, and the
    /// entry indices each side marks.
    /// </summary>
    private sealed record CollectionSensitivity(bool Whole, bool Any, HashSet<int> BeforeMarks, HashSet<int> AfterMarks);

    /// <summary>
    /// Reads the sensitivity of a collection. A mark beneath the path that is not a plain entry index cannot be
    /// mapped to one entry, so the whole collection is then treated as sensitive rather than risking that a marked
    /// value is shown.
    /// </summary>
    private static CollectionSensitivity ReadSensitivity(
        string path,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        var beforeMarks = ReadEntryMarks(path, beforeSensitive);
        var afterMarks = ReadEntryMarks(path, afterSensitive);
        if (beforeMarks is null || afterMarks is null || IsSensitiveAttribute(path, beforeSensitive, afterSensitive))
        {
            return new CollectionSensitivity(true, true, [], []);
        }

        return new CollectionSensitivity(false, beforeMarks.Count > 0 || afterMarks.Count > 0, beforeMarks, afterMarks);
    }

    /// <summary>
    /// Returns the canonical before and after texts. With masked entries the texts are built from the model, never
    /// from the raw arrays, which still contain the masked values.
    /// </summary>
    private static (string Before, string? After) CanonicalTexts(
        DetectedCollection collection,
        CollectionChangeModel model,
        bool fromModel,
        bool wholeUnknown)
    {
        if (fromModel)
        {
            return (
                CollectionCanonicalText.FromModel(model.BeforeState, model.Before),
                wholeUnknown ? null : CollectionCanonicalText.FromModel(model.AfterState, model.After));
        }

        return (
            CollectionValues.CanonicalText(collection.Before),
            wholeUnknown ? null : CollectionValues.CanonicalText(collection.After));
    }

    /// <summary>
    /// Reads the entry indices that one side's sensitivity map marks as sensitive beneath a collection path.
    /// </summary>
    /// <param name="path">The collection path.</param>
    /// <param name="sensitivity">One side's flattened sensitivity map.</param>
    /// <returns>The marked indices, or <c>null</c> when a mark beneath the path is not a plain entry index.</returns>
    private static HashSet<int>? ReadEntryMarks(string path, Dictionary<string, string?> sensitivity)
    {
        var prefix = path + "[";
        var marks = new HashSet<int>();
        foreach (var entry in sensitivity)
        {
            if (entry.Value != "true" || !entry.Key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var rest = entry.Key.AsSpan(prefix.Length);
            if (rest.Length < 2
                || rest[^1] != ']'
                || !int.TryParse(rest[..^1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index))
            {
                return null;
            }

            marks.Add(index);
        }

        return marks;
    }

    /// <summary>
    /// One collection element ready for diffing. A masked element carries no value and an opaque key.
    /// </summary>
    private readonly record struct PreparedElement(string? Value, CollectionElementKey Key, bool IsMasked);

    /// <summary>
    /// Reads both sides and replaces each masked element by an opaque comparison token. Tokens number the distinct
    /// values of masked elements across both sides, so masked-to-masked equality across sides is value equality,
    /// while a masked element never equals a visible one. The token map lives only in this method.
    /// </summary>
    private static (List<PreparedElement> Before, List<PreparedElement> After) PrepareElements(
        DetectedCollection collection,
        HashSet<int> beforeMarks,
        HashSet<int> afterMarks)
    {
        var tokens = new Dictionary<CollectionElementKey, string>();
        List<PreparedElement> Prepare(JsonElement? side, HashSet<int> marks)
            => CollectionValues.ReadElements(side).Select((element, index) =>
            {
                if (!marks.Contains(index))
                {
                    return new PreparedElement(element.Value, element.Key, false);
                }

                if (!tokens.TryGetValue(element.Key, out var token))
                {
                    token = "#" + tokens.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    tokens[element.Key] = token;
                }

                return new PreparedElement(null, new CollectionElementKey(JsonValueKind.Undefined, token, Masked: true), true);
            }).ToList();

        var before = Prepare(collection.Before, beforeMarks);
        var after = Prepare(collection.After, afterMarks);
        return (before, after);
    }

    /// <summary>
    /// Builds the element lists, statuses and states for a collection that is not masked as a whole.
    /// </summary>
    private static CollectionChangeModel BuildCollectionModel(
        List<PreparedElement> beforeElements,
        List<PreparedElement> afterElements,
        CollectionState beforeState,
        CollectionState afterState,
        bool wholeUnknown,
        List<bool> unknownFlags,
        string unknownLabel,
        HashSet<int> afterMarks)
    {
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
                if (unknown)
                {
                    // An entry that is both unknown and sensitive uses the combined placeholder of sensitive computed values.
                    var label = afterMarks.Contains(index) ? $"🔒{unknownLabel}" : unknownLabel;
                    afterModels.Add(new CollectionElementModel(index, label, JsonValueKind.Null, CollectionElementStatus.Unknown));
                }
                else if (index >= afterElements.Count)
                {
                    afterModels.Add(new CollectionElementModel(index, null, JsonValueKind.Null, CollectionElementStatus.Unchanged));
                }
                else
                {
                    afterModels.Add(ToModel(afterElements[index], index, CollectionElementStatus.Unchanged));
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
    /// Zips prepared elements with their statuses into model records.
    /// </summary>
    private static List<CollectionElementModel> ToModels(
        List<PreparedElement> elements,
        IReadOnlyList<CollectionElementStatus> statuses)
        => elements.Select((element, index) => ToModel(element, index, statuses[index])).ToList();

    /// <summary>
    /// Creates the model record of one element. A masked element keeps only its index and status.
    /// </summary>
    private static CollectionElementModel ToModel(PreparedElement element, int index, CollectionElementStatus status)
        => element.IsMasked
            ? new CollectionElementModel(index, null, JsonValueKind.Undefined, status, IsMasked: true)
            : new CollectionElementModel(index, element.Value, element.Key.Kind, status);

    /// <summary>
    /// Determines whether a collection model describes a visible change.
    /// </summary>
    private static bool IsCollectionChanged(CollectionChangeModel model)
    {
        if (model.IsUnresolved || model.AfterState == CollectionState.Unknown || model.OrderChanged)
        {
            return true;
        }

        if (IsElementless(model.BeforeState) && IsElementless(model.AfterState))
        {
            return false;
        }

        if (model.BeforeState != model.AfterState)
        {
            return true;
        }

        return model.Before.Concat(model.After).Any(element => element.Status != CollectionElementStatus.Unchanged);
    }

    /// <summary>
    /// Determines whether a side holds no elements. Absent, <c>null</c> and <c>[]</c> are equivalent for change
    /// detection, as they were for the previous per-key rows: Terraform writes every unset nested block as <c>[]</c>,
    /// so treating them as different would add an empty row for each unset block on every create and delete.
    /// The states stay distinct in the model and are shown when the other side has elements.
    /// </summary>
    private static bool IsElementless(CollectionState state)
        => state is CollectionState.Absent or CollectionState.Null or CollectionState.Empty;

    /// <summary>
    /// Returns comparable text for a side without exposing it. Absent, <c>null</c> and <c>[]</c> are equivalent, matching
    /// <see cref="IsElementless"/>, so a masked collection follows the same change rule as an unmasked one.
    /// </summary>
    private static string RawKey(JsonElement? side)
        => side switch
        {
            null or { ValueKind: JsonValueKind.Null } => string.Empty,
            { ValueKind: JsonValueKind.Array } array when array.GetArrayLength() == 0 => string.Empty,
            _ => CollectionValues.Compact(side.Value),
        };

    /// <summary>
    /// Reads per-element unknown flags from an <c>after_unknown</c> node; malformed shapes yield no flags.
    /// </summary>
    private static List<bool> ReadUnknownFlags(JsonElement? node)
        => node is { ValueKind: JsonValueKind.Array } array
            ? array.EnumerateArray().Select(flag => flag.ValueKind == JsonValueKind.True).ToList()
            : [];
}
