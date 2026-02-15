using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Default implementation for building concise resource change summaries.
/// Related feature: docs/features/011-replacement-reasons-and-summaries/specification.md.
/// </summary>
public class ResourceSummaryBuilder : IResourceSummaryBuilder
{
    /// <summary>
    /// Optional registry for formatting values in resource summaries.
    /// </summary>
    private readonly ValueFormatterRegistry? _valueFormatterRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSummaryBuilder"/> class.
    /// </summary>
    /// <param name="valueFormatterRegistry">Optional registry used to format values in summaries.</param>
    internal ResourceSummaryBuilder(ValueFormatterRegistry? valueFormatterRegistry = null)
    {
        _valueFormatterRegistry = valueFormatterRegistry;
    }

    private string? FormatSummaryValue(string? value, string providerName)
    {
        var formatted = FormatSummaryValueWithRegistry(value, providerName);
        return string.IsNullOrEmpty(formatted) ? null : formatted;
    }

    /// <summary>
    /// Formats a summary value using the registry when available.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <returns>The formatted summary value, or an empty string when no value is available.</returns>
    private string FormatSummaryValueWithRegistry(string? value, string providerName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (_valueFormatterRegistry is not null)
        {
            var context = new ServiceResolutionContext(providerName, null, null, value);
            var formatted = _valueFormatterRegistry.TryFormat(context);
            if (!string.IsNullOrEmpty(formatted))
            {
                return formatted;
            }
        }

        return FormatValue(value, providerName);
    }

    private static string? FormatPlainValue(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : EscapeMarkdown(value);
    }

    /// <inheritdoc />
    public string? BuildSummary(ResourceChangeModel change)
    {
        return change.Action switch
        {
            "create" => BuildCreateSummary(change),
            "update" => BuildUpdateSummary(change),
            "replace" => BuildReplaceSummary(change),
            "delete" => BuildDeleteSummary(change),
            _ => null
        };
    }

    private string? BuildCreateSummary(ResourceChangeModel change)
    {
        var state = GetStateDictionary(change.AfterJson);
        var keys = ResolveKeys(change.Type);
        var values = ExtractValues(keys, state);

        var name = FormatSummaryValue(GetDisplayName(values, state, change), change.ProviderName);
        var resourceGroup = FormatSummaryValue(TryGet(values, "resource_group_name"), change.ProviderName);
        var location = FormatSummaryValue(TryGet(values, "location"), change.ProviderName);
        var url = FormatPlainValue(TryGet(values, "url"));

        var parts = new List<string>();
        var namePart = BuildNamePart(name, resourceGroup, location);
        AppendSummaryPart(parts, namePart);
        AppendUrlPart(parts, url, location);
        AppendStorageTierPart(parts, values, change.ProviderName);
        AppendRemainingParts(parts, values, change.ProviderName);

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Builds the primary summary part based on name, resource group, and location.
    /// </summary>
    /// <param name="name">The resolved resource name.</param>
    /// <param name="resourceGroup">The resolved resource group name.</param>
    /// <param name="location">The resolved location.</param>
    /// <returns>The primary summary part when available.</returns>
    private static string? BuildNamePart(string? name, string? resourceGroup, string? location)
    {
        var namePart = name;
        if (!string.IsNullOrEmpty(resourceGroup))
        {
            namePart = namePart is null ? resourceGroup : $"{namePart} in {resourceGroup}";
        }

        if (!string.IsNullOrEmpty(location))
        {
            namePart = namePart is null ? location : $"{namePart} ({location})";
        }

        return string.IsNullOrEmpty(namePart) ? null : namePart;
    }

    /// <summary>
    /// Appends a summary part when present.
    /// </summary>
    /// <param name="parts">The list of summary parts to update.</param>
    /// <param name="value">The summary part value.</param>
    private static void AppendSummaryPart(List<string> parts, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parts.Add(value);
        }
    }

    /// <summary>
    /// Appends URL context when supplied, following existing summary rules.
    /// </summary>
    /// <param name="parts">The list of summary parts to update.</param>
    /// <param name="url">The URL value.</param>
    /// <param name="location">The resolved location value.</param>
    private static void AppendUrlPart(List<string> parts, string? url, string? location)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        if (parts.Count > 0 && !string.IsNullOrEmpty(location))
        {
            parts.Add(url);
            return;
        }

        if (parts.Count > 0)
        {
            parts[0] = $"{parts[0]} ({url})";
            return;
        }

        parts.Add(url);
    }

    /// <summary>
    /// Appends the storage tier and replication details when available.
    /// </summary>
    /// <param name="parts">The list of summary parts to update.</param>
    /// <param name="values">The remaining attribute values.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    private void AppendStorageTierPart(List<string> parts, Dictionary<string, string?> values, string providerName)
    {
        var accountTier = TryGet(values, "account_tier");
        var accountReplication = TryGet(values, "account_replication_type");
        if (string.IsNullOrEmpty(accountTier) && string.IsNullOrEmpty(accountReplication))
        {
            return;
        }

        var combined = $"{accountTier} {accountReplication}".Trim();
        var formattedCombined = FormatSummaryValue(combined, providerName);
        AppendSummaryPart(parts, formattedCombined);
        values.Remove("account_tier");
        values.Remove("account_replication_type");
    }

    /// <summary>
    /// Appends remaining formatted values excluding context keys.
    /// </summary>
    /// <param name="parts">The list of summary parts to update.</param>
    /// <param name="values">The remaining attribute values.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    private void AppendRemainingParts(List<string> parts, Dictionary<string, string?> values, string providerName)
    {
        foreach (var (key, value) in values.ToArray())
        {
            if (string.IsNullOrEmpty(value) || IsNameOrContextKey(key))
            {
                continue;
            }

            var formatted = FormatSummaryValue(value, providerName);
            AppendSummaryPart(parts, formatted);
        }
    }

    private string? BuildUpdateSummary(ResourceChangeModel change)
    {
        var state = GetStateDictionary(change.AfterJson) ?? GetStateDictionary(change.BeforeJson);
        var name = FormatSummaryValue(GetDisplayName(state, change, preferAfter: true), change.ProviderName);
        var changeNames = change.AttributeChanges
            .Select(a => EscapeMarkdown(a.Name))
            .ToList();

        if (changeNames.Count == 0)
        {
            return name;
        }

        var visible = changeNames.Take(3).ToList();
        var suffix = changeNames.Count > 3 ? $", +{changeNames.Count - 3} more" : string.Empty;

        return name is not null
            ? $"{name} | Changed: {string.Join(", ", visible)}{suffix}"
            : $"Changed: {string.Join(", ", visible)}{suffix}";
    }

    private string? BuildReplaceSummary(ResourceChangeModel change)
    {
        var state = GetStateDictionary(change.AfterJson) ?? GetStateDictionary(change.BeforeJson);
        var name = FormatSummaryValue(GetDisplayName(state, change, preferAfter: true), change.ProviderName);

        if (change.ReplacePaths is { Count: > 0 })
        {
            var formatted = change.ReplacePaths
                .Select(ResourceSummaryPathFormatter.FormatReplacePath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Take(3)
                .ToList();

            var suffix = change.ReplacePaths.Count > 3 ? $", +{change.ReplacePaths.Count - 3} more" : string.Empty;
            var reason = string.Join(", ", formatted.Select(r => EscapeMarkdown(r!)));
            return name is not null
                ? $"recreate {name} ({reason} changed: force replacement{suffix})"
                : $"recreate ({reason} changed: force replacement{suffix})";
        }

        var changedCount = change.AttributeChanges.Count;
        return name is not null
            ? $"recreating {name} ({changedCount} changed)"
            : $"recreating ({changedCount} changed)";
    }

    private string? BuildDeleteSummary(ResourceChangeModel change)
    {
        var state = GetStateDictionary(change.BeforeJson);
        var name = FormatSummaryValue(GetDisplayName(state, change, preferAfter: false), change.ProviderName);
        return name is not null ? name : null;
    }

    private static string? GetDisplayName(Dictionary<string, string?>? values, ResourceChangeModel change, bool preferAfter)
    {
        return GetDisplayName(values, GetStateDictionary(preferAfter ? change.AfterJson : change.BeforeJson), change);
    }

    private static string? GetDisplayName(Dictionary<string, string?>? values, Dictionary<string, string?>? fallbackState, ResourceChangeModel change)
    {
        var dictionary = values ?? fallbackState ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var name = TryGet(dictionary, "name")
                   ?? TryGet(dictionary, "display_name")
                   ?? TryGet(dictionary, "body.displayName")
                   ?? TryGet(dictionary, "displayName")
                   ?? TryGet(dictionary, "url");

        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        // If no name/display_name/url exists, fall back to the other state
        if (values is null && fallbackState is not null)
        {
            var fromOtherState = TryGet(fallbackState, "name")
                ?? TryGet(fallbackState, "display_name")
                ?? TryGet(fallbackState, "url");
            if (!string.IsNullOrEmpty(fromOtherState))
            {
                return fromOtherState;
            }
        }

        // Final fallback: Terraform address
        return change.Address;
    }

    private static string? TryGet(Dictionary<string, string?>? values, string key)
    {
        if (values is null)
        {
            return null;
        }

        return values.TryGetValue(key, out var value) ? value : null;
    }

    private static IReadOnlyList<string> ResolveKeys(string resourceType)
    {
        return ResourceSummaryMappings.ResolveKeys(resourceType);
    }

    private static Dictionary<string, string?> ExtractValues(IReadOnlyList<string> keys, Dictionary<string, string?>? state)
    {
        Dictionary<string, string?> result = new(StringComparer.OrdinalIgnoreCase);
        if (state is null)
        {
            return result;
        }

        foreach (var key in keys)
        {
            if (state.TryGetValue(key, out var value))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static Dictionary<string, string?>? GetStateDictionary(object? state)
    {
        if (state is null)
        {
            return null;
        }

        var result = JsonFlattener.ConvertToFlatDictionary(state);
        return result.Count > 0 ? result : null;
    }

    private static bool IsNameOrContextKey(string key)
    {
        return ResourceSummaryPathFormatter.IsNameOrContextKey(key)
               || key.Equals("display_name", StringComparison.OrdinalIgnoreCase)
               || key.Equals("displayName", StringComparison.OrdinalIgnoreCase)
               || key.Equals("body.displayName", StringComparison.OrdinalIgnoreCase)
               || key.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase)
               || key.Equals("location", StringComparison.OrdinalIgnoreCase)
               || key.Equals("url", StringComparison.OrdinalIgnoreCase);
    }
}
