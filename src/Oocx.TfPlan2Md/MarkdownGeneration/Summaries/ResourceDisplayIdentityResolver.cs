using System;
using System.Collections.Immutable;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Stores a safe scalar identity selected for an otherwise unmapped resource.
/// </summary>
/// <param name="Path">The selected Terraform attribute path.</param>
/// <param name="Value">The selected scalar value.</param>
internal sealed record ResourceDisplayIdentity(string Path, string Value);

/// <summary>
/// Selects display identities for resource types without explicit summary mappings.
/// </summary>
internal static class ResourceDisplayIdentityResolver
{
    /// <summary>
    /// Default candidate paths used when an invocation has no custom order.
    /// </summary>
    private static readonly ImmutableArray<string> DefaultCandidatePaths =
    [
        "name",
        "display_name",
        "displayName",
        "body.displayName",
        "body.properties.displayName",
        "body.name"
    ];

    /// <summary>
    /// Legacy identifying paths used to expose explicit mapping identity through the shared model.
    /// </summary>
    private static readonly ImmutableArray<string> MappedCandidatePriority =
    [
        "name",
        "display_name",
        "body.displayName",
        "displayName",
        "principal_name",
        "url"
    ];

    /// <summary>
    /// Resolves an identity for shared context while keeping custom paths limited to generic resources.
    /// </summary>
    /// <param name="resourceType">Terraform resource type.</param>
    /// <param name="action">Normalized Terraform action.</param>
    /// <param name="before">Prior resource state.</param>
    /// <param name="after">Desired resource state.</param>
    /// <param name="afterUnknown">Terraform's after-unknown tree.</param>
    /// <param name="beforeSensitive">Terraform's prior-state sensitivity tree.</param>
    /// <param name="afterSensitive">Terraform's desired-state sensitivity tree.</param>
    /// <param name="showSensitive">Whether this invocation may display sensitive values.</param>
    /// <param name="configuredCandidatePaths">Optional invocation-specific candidate order.</param>
    /// <returns>The selected identity, or <see langword="null"/> when no candidate is usable.</returns>
    internal static ResourceDisplayIdentity? Resolve(
        string resourceType,
        string action,
        object? before,
        object? after,
        object? afterUnknown,
        object? beforeSensitive,
        object? afterSensitive,
        bool showSensitive,
        ImmutableArray<string> configuredCandidatePaths)
    {
        var candidates = ResourceSummaryMappings.ResolveIdentityCandidatePaths(
            resourceType,
            configuredCandidatePaths,
            DefaultCandidatePaths,
            MappedCandidatePriority);
        var beforeSensitiveValues = JsonFlattener.ConvertToFlatDictionary(beforeSensitive);
        var afterSensitiveValues = JsonFlattener.ConvertToFlatDictionary(afterSensitive);
        var preferBefore = action.Equals("delete", StringComparison.OrdinalIgnoreCase);

        foreach (var path in candidates)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var identity = ResolveAtPath(
                path,
                preferBefore ? before : after,
                preferBefore ? after : before,
                afterUnknown,
                beforeSensitiveValues,
                afterSensitiveValues,
                showSensitive,
                preferredStateIsAfter: !preferBefore);
            if (identity is not null)
            {
                return identity;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries the action-preferred state and then the other state for one candidate path.
    /// </summary>
    /// <param name="path">Candidate attribute path.</param>
    /// <param name="preferredState">State favored for this action.</param>
    /// <param name="fallbackState">The other state.</param>
    /// <param name="afterUnknown">Terraform's after-unknown tree.</param>
    /// <param name="beforeSensitiveValues">Flattened prior-state sensitivity markers.</param>
    /// <param name="afterSensitiveValues">Flattened desired-state sensitivity markers.</param>
    /// <param name="showSensitive">Whether this invocation may display sensitive values.</param>
    /// <param name="preferredStateIsAfter">Whether the preferred state is the desired state.</param>
    /// <returns>A usable value at the path, or <see langword="null"/>.</returns>
    private static ResourceDisplayIdentity? ResolveAtPath(
        string path,
        object? preferredState,
        object? fallbackState,
        object? afterUnknown,
        System.Collections.Generic.Dictionary<string, string?> beforeSensitiveValues,
        System.Collections.Generic.Dictionary<string, string?> afterSensitiveValues,
        bool showSensitive,
        bool preferredStateIsAfter)
    {
        var preferredValue = ResolveStateValue(
            path,
            preferredState,
            afterUnknown,
            beforeSensitiveValues,
            afterSensitiveValues,
            showSensitive,
            preferredStateIsAfter);
        if (preferredValue is not null)
        {
            return new ResourceDisplayIdentity(path, preferredValue);
        }

        var fallbackValue = ResolveStateValue(
            path,
            fallbackState,
            afterUnknown,
            beforeSensitiveValues,
            afterSensitiveValues,
            showSensitive,
            !preferredStateIsAfter);
        return fallbackValue is null ? null : new ResourceDisplayIdentity(path, fallbackValue);
    }

    /// <summary>
    /// Extracts a scalar value unless its desired state is unknown or its path remains sensitive.
    /// </summary>
    /// <param name="path">Candidate attribute path.</param>
    /// <param name="state">State to inspect.</param>
    /// <param name="afterUnknown">Terraform's after-unknown tree.</param>
    /// <param name="beforeSensitiveValues">Flattened prior-state sensitivity markers.</param>
    /// <param name="afterSensitiveValues">Flattened desired-state sensitivity markers.</param>
    /// <param name="showSensitive">Whether this invocation may display sensitive values.</param>
    /// <param name="stateIsAfter">Whether <paramref name="state"/> is the desired state.</param>
    /// <returns>The scalar value when usable, or <see langword="null"/>.</returns>
    private static string? ResolveStateValue(
        string path,
        object? state,
        object? afterUnknown,
        System.Collections.Generic.Dictionary<string, string?> beforeSensitiveValues,
        System.Collections.Generic.Dictionary<string, string?> afterSensitiveValues,
        bool showSensitive,
        bool stateIsAfter)
    {
        if (!showSensitive && SensitivityHelper.IsSensitiveAttribute(path, beforeSensitiveValues, afterSensitiveValues))
        {
            return null;
        }

        if (stateIsAfter && AfterUnknownHelper.IsAttributeUnknownAfterApply(afterUnknown, path))
        {
            return null;
        }

        if (!TryGetJsonElement(state, out var stateElement)
            || !TryGetPathValue(stateElement, path, out var valueElement))
        {
            return null;
        }

        return valueElement.ValueKind switch
        {
            JsonValueKind.String => GetNonEmptyString(valueElement),
            JsonValueKind.Number => valueElement.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    /// <summary>
    /// Reads a string candidate while rejecting only the empty string.
    /// </summary>
    /// <param name="value">Candidate JSON string.</param>
    /// <returns>The original non-empty string, or <see langword="null"/>.</returns>
    private static string? GetNonEmptyString(JsonElement value)
    {
        var text = value.GetString();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Walks a dotted path through nested JSON objects without treating dots as literal key text.
    /// </summary>
    /// <param name="state">JSON state root.</param>
    /// <param name="path">Dotted property path.</param>
    /// <param name="value">Resolved JSON value.</param>
    /// <returns><see langword="true"/> when every object property exists.</returns>
    private static bool TryGetPathValue(JsonElement state, string path, out JsonElement value)
    {
        value = state;
        foreach (var segment in path.Split('.'))
        {
            if (string.IsNullOrWhiteSpace(segment)
                || value.ValueKind != JsonValueKind.Object
                || !value.TryGetProperty(segment, out value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Normalizes a plan-state object into a JSON element when available.
    /// </summary>
    /// <param name="state">State to inspect.</param>
    /// <param name="element">The JSON element when <paramref name="state"/> is one.</param>
    /// <returns><see langword="true"/> when the input is a JSON element.</returns>
    private static bool TryGetJsonElement(object? state, out JsonElement element)
    {
        if (state is JsonElement jsonElement)
        {
            element = jsonElement;
            return true;
        }

        element = default;
        return false;
    }
}
