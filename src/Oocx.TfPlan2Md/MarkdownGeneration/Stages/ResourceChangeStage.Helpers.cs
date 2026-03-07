using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Helper methods for <see cref="ResourceChangeStage"/>.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal sealed partial class ResourceChangeStage
{
    /// <summary>
    /// Resolves the display label for a computed known-after-apply attribute.
    /// </summary>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="flattenedKey">Flattened attribute key.</param>
    /// <returns>A formatted known-after-apply label with optional reference context.</returns>
    private static string ResolveKnownAfterApplyLabel(
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        string flattenedKey)
    {
        var topLevelAttribute = GetTopLevelAttributeName(flattenedKey);
        if (!string.IsNullOrWhiteSpace(topLevelAttribute)
            && configurationReferences.TryGetValue(topLevelAttribute, out var references)
            && references.Count > 0)
        {
            var selectedReference = ReferenceSelector.SelectBestReference(references);
            if (!string.IsNullOrWhiteSpace(selectedReference))
            {
                return $"(known after apply: {selectedReference})";
            }
        }

        return "(known after apply)";
    }

    /// <summary>
    /// Extracts the top-level attribute name from a flattened key.
    /// </summary>
    /// <param name="flattenedKey">Flattened key such as <c>tags.env</c> or <c>rules[0].priority</c>.</param>
    /// <returns>The top-level attribute name.</returns>
    private static string GetTopLevelAttributeName(string flattenedKey)
    {
        if (string.IsNullOrWhiteSpace(flattenedKey))
        {
            return string.Empty;
        }

        var dotIndex = flattenedKey.IndexOf('.');
        var bracketIndex = flattenedKey.IndexOf('[');

        if (dotIndex < 0 && bracketIndex < 0)
        {
            return flattenedKey;
        }

        if (dotIndex < 0)
        {
            return flattenedKey[..bracketIndex];
        }

        if (bracketIndex < 0)
        {
            return flattenedKey[..dotIndex];
        }

        var endIndex = Math.Min(dotIndex, bracketIndex);
        return flattenedKey[..endIndex];
    }

    /// <summary>
    /// Normalizes resource addresses for configuration lookups by removing instance keys.
    /// </summary>
    /// <param name="address">The resource address to normalize.</param>
    /// <returns>The normalized address without instance keys.</returns>
    private static string NormalizeResourceAddressForReferenceLookup(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        if (!address.EndsWith(']'))
        {
            return address;
        }

        var bracketIndex = address.LastIndexOf('[');
        return bracketIndex < 0 ? address : address[..bracketIndex];
    }

    /// <summary>
    /// Determines whether an attribute is marked as sensitive.
    /// </summary>
    /// <param name="key">The flattened attribute key.</param>
    /// <param name="beforeSensitive">Flattened before-state sensitivity map.</param>
    /// <param name="afterSensitive">Flattened after-state sensitivity map.</param>
    /// <returns><c>true</c> when the attribute is sensitive; otherwise <c>false</c>.</returns>
    private static bool IsSensitiveAttribute(
        string key,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        return SensitivityHelper.IsSensitiveAttribute(key, beforeSensitive, afterSensitive);
    }

    /// <summary>
    /// Flattens a JSON-like object into dotted attribute paths.
    /// </summary>
    /// <param name="obj">The source object.</param>
    /// <param name="prefix">Optional prefix for nested traversal.</param>
    /// <returns>The flattened attribute map.</returns>
    private static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "")
    {
        return JsonFlattener.ConvertToFlatDictionary(obj, prefix);
    }

    /// <summary>
    /// Determines the normalized action string from Terraform's action list.
    /// </summary>
    /// <param name="actions">List of Terraform actions.</param>
    /// <returns>The normalized action string for report generation.</returns>
    private static string DetermineAction(IReadOnlyList<string> actions)
    {
        if (actions.Count == 0)
        {
            return NoOpAction;
        }

        if (actions.Contains(CreateAction) && actions.Contains(DeleteAction))
        {
            return ReplaceAction;
        }

        if (actions.Contains(CreateAction) && actions.Contains(ForgetAction))
        {
            return ReplaceAction;
        }

        if (actions.Contains(CreateAction))
        {
            return CreateAction;
        }

        if (actions.Contains(DeleteAction))
        {
            return DeleteAction;
        }

        if (actions.Contains(UpdateAction))
        {
            return UpdateAction;
        }

        if (actions.Contains(ReadAction))
        {
            return ReadAction;
        }

        if (actions.Contains(OpenAction))
        {
            return OpenAction;
        }

        if (actions.Contains(NoOpAction))
        {
            return NoOpAction;
        }

        if (actions.Contains(ForgetAction))
        {
            return ForgetAction;
        }

        Console.Error.WriteLine(
            $"Warning: Encountered unknown Terraform action set: [{string.Join(", ", actions)}]; classifying as '{UnknownAction}'.");

        return UnknownAction;
    }

    /// <summary>
    /// Applies known-after-apply display overrides for computed attributes.
    /// </summary>
    /// <param name="change">Terraform change object.</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="key">Flattened attribute key.</param>
    /// <param name="isSensitive">Whether the attribute is sensitive.</param>
    /// <param name="beforeDisplay">The display before value to update when needed.</param>
    /// <param name="afterDisplay">The display after value to update.</param>
    /// <param name="valuesEqual">Equality flag to update when value should be forced as changed.</param>
    private static void ApplyComputedKnownAfterApplyOverride(
        Change change,
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        string key,
        bool isSensitive,
        ref string? beforeDisplay,
        ref string? afterDisplay,
        ref bool valuesEqual)
    {
        var isUnknownAfterApply = afterDisplay is null || string.Equals(afterDisplay, SensitiveMask, StringComparison.Ordinal);
        isUnknownAfterApply = isUnknownAfterApply
            && AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, key);
        if (!isUnknownAfterApply)
        {
            return;
        }

        var displayLabel = ResolveKnownAfterApplyLabel(configurationReferences, key);
        if (isSensitive)
        {
            beforeDisplay = SensitiveMask;
            afterDisplay = $"🔒{displayLabel}";
        }
        else
        {
            afterDisplay = displayLabel;
        }

        valuesEqual = false;
    }

    /// <summary>
    /// Builds configuration references for one resource address.
    /// </summary>
    /// <param name="normalizedAddress">Normalized resource address without instance key.</param>
    /// <param name="configurationReferencesByAddress">Configuration references grouped by normalized resource address.</param>
    /// <returns>The attribute reference map for the resource.</returns>
    private static Dictionary<string, IReadOnlyList<string>> BuildConfigurationReferencesForResource(
        string normalizedAddress,
        IReadOnlyDictionary<string, Dictionary<string, IReadOnlyList<string>>> configurationReferencesByAddress)
    {
        if (string.IsNullOrWhiteSpace(normalizedAddress) || configurationReferencesByAddress.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        if (configurationReferencesByAddress.TryGetValue(normalizedAddress, out var references))
        {
            return references;
        }

        return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps a normalized action string to a display symbol.
    /// </summary>
    /// <param name="action">The normalized action string.</param>
    /// <returns>The corresponding display symbol.</returns>
    private static string GetActionSymbol(string action)
    {
        return TerraformActions.GetSymbol(action);
    }
}
