using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Default implementation for building concise resource change summaries.
/// Related feature: docs/features/replacement-reasons-and-summaries/specification.md
/// </summary>
public class ResourceSummaryBuilder : IResourceSummaryBuilder
{
    private static readonly Dictionary<string, IReadOnlyList<string>> ResourceMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // azurerm
        ["azurerm_resource_group"] = ["name", "location"],
        ["azurerm_storage_account"] = ["name", "resource_group_name", "location", "account_tier", "account_replication_type"],
        ["azurerm_virtual_network"] = ["name", "resource_group_name", "location", "address_space[0]"],
        ["azurerm_subnet"] = ["name", "virtual_network_name", "address_prefixes[0]"],
        ["azurerm_public_ip"] = ["name", "resource_group_name", "location", "allocation_method"],
        ["azurerm_key_vault"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_key_vault_secret"] = ["name", "key_vault_id"],
        ["azurerm_key_vault_key"] = ["name", "key_vault_id", "key_type"],
        ["azurerm_linux_virtual_machine"] = ["name", "resource_group_name", "location", "size"],
        ["azurerm_windows_virtual_machine"] = ["name", "resource_group_name", "location", "size"],
        ["azurerm_linux_virtual_machine_scale_set"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_windows_virtual_machine_scale_set"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_kubernetes_cluster"] = ["name", "resource_group_name", "location", "kubernetes_version"],
        ["azurerm_container_registry"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_container_app"] = ["name", "resource_group_name", "location", "container_app_environment_id"],
        ["azurerm_app_service"] = ["name", "resource_group_name", "location", "app_service_plan_id"],
        ["azurerm_app_service_plan"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_linux_web_app"] = ["name", "resource_group_name", "location", "service_plan_id"],
        ["azurerm_windows_web_app"] = ["name", "resource_group_name", "location", "service_plan_id"],
        ["azurerm_linux_function_app"] = ["name", "resource_group_name", "location", "service_plan_id"],
        ["azurerm_windows_function_app"] = ["name", "resource_group_name", "location", "service_plan_id"],
        ["azurerm_mssql_server"] = ["name", "resource_group_name", "location", "version"],
        ["azurerm_mssql_database"] = ["name", "server_id"],
        ["azurerm_mysql_flexible_server"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_postgresql_flexible_server"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_cosmosdb_account"] = ["name", "resource_group_name", "location", "offer_type"],
        ["azurerm_redis_cache"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_log_analytics_workspace"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_application_insights"] = ["name", "resource_group_name", "location", "application_type"],
        ["azurerm_monitor_action_group"] = ["name", "resource_group_name"],
        ["azurerm_service_plan"] = ["name", "resource_group_name", "location", "os_type", "sku_name"],
        ["azurerm_dns_zone"] = ["name", "resource_group_name"],
        ["azurerm_private_dns_zone"] = ["name", "resource_group_name"],
        ["azurerm_lb"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_application_gateway"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_firewall"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_express_route_circuit"] = ["name", "resource_group_name", "location", "service_provider_name"],
        ["azurerm_api_management"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_databricks_workspace"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_automation_account"] = ["name", "resource_group_name", "location", "sku_name"],
        ["azurerm_recovery_services_vault"] = ["name", "resource_group_name", "location", "sku"],
        ["azurerm_storage_blob"] = ["name", "storage_account_name", "storage_container_name"],
        ["azurerm_managed_disk"] = ["name", "resource_group_name", "location", "storage_account_type"],

        // azuredevops
        ["azuredevops_project"] = ["name", "visibility"],

        // azuread
        ["azuread_group"] = ["display_name", "security_enabled"],
        ["azuread_user"] = ["display_name", "user_principal_name"],
        ["azuread_service_principal"] = ["display_name", "application_id"],

        // azapi
        ["azapi_resource"] = ["name", "type", "parent_id"],
        ["azapi_resource_action"] = ["action", "resource_id"],

        // msgraph (generic)
        ["msgraph_resource"] = ["url", "body.displayName"],
        ["msgraph_update_resource"] = ["url", "body.displayName"],
        ["msgraph_resource_action"] = ["url", "action"],
        ["msgraph_resource_collection"] = ["url", "parent_id"]
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> ProviderFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["azurerm"] = ["name", "resource_group_name", "location"],
        ["azuredevops"] = ["name", "project_id"],
        ["azuread"] = ["display_name"],
        ["azapi"] = ["name", "type"],
        ["msgraph"] = ["url", "body.displayName"]
    };

    private static readonly IReadOnlyList<string> GenericFallback = ["name", "display_name"];

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
                .Select(FormatReplacePath)
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
        if (ResourceMappings.TryGetValue(resourceType, out var mapped))
        {
            return mapped;
        }

        var provider = GetProvider(resourceType);
        if (provider is not null && ProviderFallbacks.TryGetValue(provider, out var providerKeys))
        {
            return providerKeys;
        }

        return GenericFallback;
    }

    private static string? GetProvider(string resourceType)
    {
        var underscore = resourceType.IndexOf('_');
        return underscore > 0 ? resourceType[..underscore] : null;
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

        if (state is JsonElement element)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            FlattenJsonElement(element, string.Empty, dict);
            return dict;
        }

        return null;
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, result);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = string.IsNullOrEmpty(prefix) ? $"[{index}]" : $"{prefix}[{index}]";
                    FlattenJsonElement(item, key, result);
                    index++;
                }
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;
            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;
            case JsonValueKind.Null:
                result[prefix] = null;
                break;
        }
    }

    private static string? FormatReplacePath(IReadOnlyList<object> path)
    {
        if (path.Count == 0)
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < path.Count; i++)
        {
            var segment = FormatPathSegment(path[i]);
            if (segment is null)
            {
                continue;
            }

            if (i == 0)
            {
                builder.Append(segment);
                continue;
            }

            if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                builder.Append('[');
                builder.Append(segment);
                builder.Append(']');
            }
            else if (segment.Length > 0 && segment[0] == '[')
            {
                builder.Append(segment);
            }
            else
            {
                builder.Append('.');
                builder.Append(segment);
            }
        }

        return builder.ToString();
    }

    private static string? FormatPathSegment(object segment)
    {
        switch (segment)
        {
            case JsonElement jsonElement:
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number when jsonElement.TryGetInt32(out var number) => number.ToString(CultureInfo.InvariantCulture),
                    JsonValueKind.Number => jsonElement.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => null
                };
            case string s:
                return s;
            case int i:
                return i.ToString(CultureInfo.InvariantCulture);
            case long l:
                return l.ToString(CultureInfo.InvariantCulture);
            default:
                return segment?.ToString();
        }
    }

    private static bool IsNameOrContextKey(string key)
    {
        return key.Equals("name", StringComparison.OrdinalIgnoreCase)
               || key.Equals("display_name", StringComparison.OrdinalIgnoreCase)
               || key.Equals("displayName", StringComparison.OrdinalIgnoreCase)
               || key.Equals("body.displayName", StringComparison.OrdinalIgnoreCase)
               || key.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase)
               || key.Equals("location", StringComparison.OrdinalIgnoreCase)
               || key.Equals("url", StringComparison.OrdinalIgnoreCase);
    }
}
