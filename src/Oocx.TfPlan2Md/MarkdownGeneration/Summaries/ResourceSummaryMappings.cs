using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

/// <summary>
/// Provides resource type mappings for determining which attributes to include in summaries.
/// </summary>
/// <remarks>
/// Extracted from ResourceSummaryBuilder to improve maintainability.
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
/// </remarks>
internal static class ResourceSummaryMappings
{
    /// <summary>
    /// Maps resource types to their preferred summary attributes in order of importance.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ResourceMappings = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
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
        ["azurerm_subscription"] = ["subscription", "subscription_id"],
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

    /// <summary>
    /// Provider-level fallback attribute keys when a specific resource mapping doesn't exist.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ProviderFallbacks = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["azurerm"] = ["name", "resource_group_name", "location"],
        ["azuredevops"] = ["name", "project_id"],
        ["azuread"] = ["display_name"],
        ["azapi"] = ["name", "type"],
        ["msgraph"] = ["url", "body.displayName"]
    };

    /// <summary>
    /// Generic fallback when neither resource-specific nor provider-specific mappings exist.
    /// </summary>
    public static readonly IReadOnlyList<string> GenericFallback = ["name", "display_name"];

    /// <summary>
    /// Resolves the attribute keys to use for building a summary for the given resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_storage_account").</param>
    /// <returns>List of attribute keys in priority order.</returns>
    public static IReadOnlyList<string> ResolveKeys(string resourceType)
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

    /// <summary>
    /// Extracts the provider name from a resource type.
    /// </summary>
    /// <param name="resourceType">The resource type (e.g., "azurerm_storage_account").</param>
    /// <returns>The provider name (e.g., "azurerm"), or null if no underscore is found.</returns>
    private static string? GetProvider(string resourceType)
    {
        var underscore = resourceType.IndexOf('_');
        return underscore > 0 ? resourceType[..underscore] : null;
    }
}
