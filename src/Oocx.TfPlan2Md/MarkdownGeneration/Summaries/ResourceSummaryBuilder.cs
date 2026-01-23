using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Default implementation for building concise resource change summaries.
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
/// </summary>
public class ResourceSummaryBuilder : IResourceSummaryBuilder
{
    private static string? FormatSummaryValue(string? value, string providerName)
    {
        var formatted = ScribanHelpers.FormatValue(value, providerName);
        return string.IsNullOrEmpty(formatted) ? null : formatted;
    }

    private static string? FormatPlainValue(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : ScribanHelpers.EscapeMarkdown(value);
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
        var namePart = name;
        if (!string.IsNullOrEmpty(resourceGroup))
        {
            namePart = namePart is null ? resourceGroup : $"{namePart} in {resourceGroup}";
        }

        if (!string.IsNullOrEmpty(location))
        {
            namePart = namePart is null ? location : $"{namePart} ({location})";
        }

        if (!string.IsNullOrEmpty(namePart))
        {
            parts.Add(namePart);
        }

        // If a URL is present (e.g., msgraph), include it alongside the name when no location is available
        if (!string.IsNullOrEmpty(url))
        {
            if (parts.Count > 0 && !string.IsNullOrEmpty(location))
            {
                parts.Add(url);
            }
            else if (parts.Count > 0)
            {
                parts[0] = $"{parts[0]} ({url})";
            }
            else
            {
                parts.Add(url);
            }
        }

        // Combine storage tier + redundancy when both are present for a compact output
        var accountTier = TryGet(values, "account_tier");
        var accountReplication = TryGet(values, "account_replication_type");
        if (!string.IsNullOrEmpty(accountTier) || !string.IsNullOrEmpty(accountReplication))
        {
            var combined = $"{accountTier} {accountReplication}".Trim();
            var formattedCombined = FormatSummaryValue(combined, change.ProviderName);
            if (!string.IsNullOrEmpty(formattedCombined))
            {
                parts.Add(formattedCombined);
            }
            values.Remove("account_tier");
            values.Remove("account_replication_type");
        }

        // Add remaining values (excluding ones already used)
        foreach (var (key, value) in values.ToArray())
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (IsNameOrContextKey(key))
            {
                continue;
            }

            var formatted = FormatSummaryValue(value, change.ProviderName);
            if (!string.IsNullOrEmpty(formatted))
            {
                parts.Add(formatted);
            }
        }

        return string.Join(" | ", parts);
    }

    private string? BuildUpdateSummary(ResourceChangeModel change)
    {
        var state = GetStateDictionary(change.AfterJson) ?? GetStateDictionary(change.BeforeJson);
        var name = FormatSummaryValue(GetDisplayName(state, change, preferAfter: true), change.ProviderName);
        var changeNames = change.AttributeChanges
            .Select(a => ScribanHelpers.EscapeMarkdown(a.Name))
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
            var reason = string.Join(", ", formatted.Select(r => ScribanHelpers.EscapeMarkdown(r!)));
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

    private IReadOnlyList<string> ResolveKeys(string resourceType)
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
