using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Parses and interprets Azure resource identifiers into structured scope information.
/// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
/// </summary>
public static class AzureScopeParser
{
    /// <summary>
    /// Non-breaking space for keeping the subscription icon attached to its identifier.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    private const string NonBreakingSpace = "\u00A0";

    /// <summary>
    /// Icon for resource group identifiers.
    /// </summary>
    private const string ResourceGroupIcon = "📁";

    /// <summary>
    /// Icon for resource name identifiers.
    /// </summary>
    private const string ResourceNameIcon = "🆔";

    /// <summary>
    /// Formats a subscription identifier with the key icon.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>Icon-prefixed subscription identifier.</returns>
    private static string FormatSubscriptionId(string subscriptionId)
    {
        return $"🔑{NonBreakingSpace}{subscriptionId}";
    }

    /// <summary>
    /// Formats a resource group label with icon.
    /// </summary>
    /// <param name="resourceGroup">The resource group name to format.</param>
    /// <returns>Icon-prefixed resource group name.</returns>
    private static string FormatResourceGroupLabel(string? resourceGroup)
    {
        if (string.IsNullOrWhiteSpace(resourceGroup))
        {
            return string.Empty;
        }
        return $"{ResourceGroupIcon}{NonBreakingSpace}{resourceGroup}";
    }

    /// <summary>
    /// Formats a resource name label with icon.
    /// </summary>
    /// <param name="resourceName">The resource name to format.</param>
    /// <returns>Icon-prefixed resource name.</returns>
    private static string FormatResourceNameLabel(string? resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return string.Empty;
        }
        return $"{ResourceNameIcon}{NonBreakingSpace}{resourceName}";
    }

    /// <summary>
    /// Determines whether the provided scope string is a valid Azure resource identifier.
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    /// <param name="scope">The scope string to evaluate.</param>
    /// <returns>True when the scope parses to a known Azure scope level; otherwise false.</returns>
    public static bool IsAzureResourceId(string? scope)
    {
        var parsed = Parse(scope);
        return parsed.Level != ScopeLevel.Unknown;
    }

    /// <summary>
    /// Parses an Azure resource identifier into structured scope information.
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    /// <param name="scope">The Azure resource identifier string to parse.</param>
    /// <returns>A ScopeInfo object containing parsed scope details and level.</returns>
    public static ScopeInfo Parse(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return ScopeInfo.Empty;
        }

        var parts = scope.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return new ScopeInfo(scope, string.Empty, string.Empty, string.Empty, ScopeLevel.Unknown, scope, string.Empty, scope, scope);
        }

        if (IsManagementGroupScope(parts))
        {
            var managementGroup = parts[3];
            var summary = $"{managementGroup} (Management Group)";
            return new ScopeInfo(
                managementGroup,
                "Management Group",
                null,
                null,
                ScopeLevel.ManagementGroup,
                summary,
                "management group ",
                managementGroup,
                $"{managementGroup} (Management Group)");
        }

        if (IsSubscriptionScope(parts))
        {
            var subscriptionId = parts[1];
            return new ScopeInfo(
                subscriptionId,
                "Subscription",
                subscriptionId,
                null,
                ScopeLevel.Subscription,
                $"subscription {FormatSubscriptionId(subscriptionId)}",
                "subscription ",
                subscriptionId,
                $"subscription {FormatSubscriptionId(subscriptionId)}");
        }

        if (IsResourceScope(parts))
        {
            var subscriptionId = parts[1];
            var resourceGroup = parts[3];
            var resourceType = GetResourceType(parts, providerIndex: 5);
            var resourceName = parts[^1];
            var formattedSubscriptionId = FormatSubscriptionId(subscriptionId);
            return new ScopeInfo(
                resourceName,
                resourceType,
                subscriptionId,
                resourceGroup,
                ScopeLevel.Resource,
                $"{resourceType} {resourceName}",
                $"{resourceType} ",
                resourceName,
                $"{resourceType} {resourceName} in resource group {resourceGroup} of subscription {formattedSubscriptionId}");
        }

        if (IsSubscriptionProviderScope(parts))
        {
            var subscriptionId = parts[1];
            var resourceType = GetResourceType(parts, providerIndex: 3);
            var resourceName = parts[^1];
            var formattedSubscriptionId = FormatSubscriptionId(subscriptionId);
            return new ScopeInfo(
                resourceName,
                resourceType,
                subscriptionId,
                null,
                ScopeLevel.Resource,
                $"{resourceType} {resourceName}",
                $"{resourceType} ",
                resourceName,
                $"{resourceType} {resourceName} in subscription {formattedSubscriptionId}");
        }

        if (IsResourceGroupScope(parts))
        {
            var subscriptionId = parts[1];
            var resourceGroup = parts[3];
            var formattedSubscriptionId = FormatSubscriptionId(subscriptionId);
            return new ScopeInfo(
                resourceGroup,
                "Resource Group",
                subscriptionId,
                resourceGroup,
                ScopeLevel.ResourceGroup,
                resourceGroup,
                string.Empty,
                resourceGroup,
                $"{resourceGroup} in subscription {formattedSubscriptionId}");
        }

        return new ScopeInfo(scope, string.Empty, string.Empty, string.Empty, ScopeLevel.Unknown, scope, string.Empty, scope, scope);
    }

    /// <summary>
    /// Formats an Azure resource identifier into a human-readable scope description with markdown formatting.
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    /// <param name="scope">The Azure resource identifier string to format.</param>
    /// <returns>A markdown-formatted string describing the scope in human-readable terms.</returns>
    public static string ParseScope(string? scope)
    {
        var parsed = Parse(scope);

        return parsed.Level switch
        {
            ScopeLevel.ManagementGroup => $"`{parsed.Name}` (Management Group)",
            ScopeLevel.Subscription => $"subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
            ScopeLevel.Resource when !string.IsNullOrWhiteSpace(parsed.ResourceGroup) =>
                $"{parsed.Type} `{FormatResourceNameLabel(parsed.Name)}` in resource group `{FormatResourceGroupLabel(parsed.ResourceGroup)}` of subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
            ScopeLevel.Resource => $"{parsed.Type} `{FormatResourceNameLabel(parsed.Name)}` in subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
            ScopeLevel.ResourceGroup => $"`{FormatResourceGroupLabel(parsed.ResourceGroup)}` in subscription `{FormatSubscriptionId(parsed.SubscriptionId ?? string.Empty)}`",
            _ => parsed.Details
        };
    }

    private static bool IsManagementGroupScope(string[] parts)
    {
        return parts.Length >= 4
            && parts[0].Equals("providers", StringComparison.OrdinalIgnoreCase)
            && parts[1].Equals("Microsoft.Management", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("managementGroups", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubscriptionScope(string[] parts)
    {
        return parts.Length == 2
            && parts[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResourceGroupScope(string[] parts)
    {
        return parts.Length >= 4
            && parts[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResourceScope(string[] parts)
    {
        return parts.Length >= 7
            && parts[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase)
            && parts[4].Equals("providers", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubscriptionProviderScope(string[] parts)
    {
        return parts.Length >= 5
            && parts[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals("providers", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetResourceType(string[] parts, int providerIndex)
    {
        if (parts.Length - providerIndex < 2)
        {
            return "resource";
        }

        var provider = parts[providerIndex];
        var typeSegments = new List<string>();
        var typeStartIndex = providerIndex + 1;

        for (var i = typeStartIndex; i < parts.Length - 1; i += 2)
        {
            typeSegments.Add(parts[i]);
        }

        if (typeSegments.Count == 0 && typeStartIndex < parts.Length - 1)
        {
            typeSegments.Add(parts[typeStartIndex]);
        }

        var typePath = string.Join('/', typeSegments);
        var fullType = $"{provider}/{typePath}";

        return fullType switch
        {
            "Microsoft.KeyVault/vaults" => "Key Vault",
            "Microsoft.Storage/storageAccounts" => "Storage Account",
            "Microsoft.Storage/storageAccounts/blobServices" => "Storage Account Blob Service",
            "Microsoft.Storage/storageAccounts/fileServices" => "Storage Account File Service",
            "Microsoft.Compute/virtualMachines" => "Virtual Machine",
            "Microsoft.Compute/virtualMachineScaleSets" => "Virtual Machine Scale Set",
            "Microsoft.Compute/disks" => "Managed Disk",
            "Microsoft.ContainerService/managedClusters" => "AKS Cluster",
            "Microsoft.ContainerRegistry/registries" => "Container Registry",
            "Microsoft.Web/sites" => "App Service",
            "Microsoft.Web/serverfarms" => "App Service Plan",
            "Microsoft.Sql/servers" => "SQL Server",
            "Microsoft.Sql/servers/databases" => "SQL Database",
            "Microsoft.DocumentDB/databaseAccounts" => "Cosmos DB Account",
            "Microsoft.EventHub/namespaces" => "Event Hubs Namespace",
            "Microsoft.ServiceBus/namespaces" => "Service Bus Namespace",
            "Microsoft.Network/virtualNetworks" => "Virtual Network",
            "Microsoft.Network/virtualNetworks/subnets" => "Subnet",
            "Microsoft.Network/networkSecurityGroups" => "Network Security Group",
            "Microsoft.Network/publicIPAddresses" => "Public IP Address",
            "Microsoft.Network/loadBalancers" => "Load Balancer",
            "Microsoft.Network/applicationGateways" => "Application Gateway",
            "Microsoft.Network/azureFirewalls" => "Azure Firewall",
            "Microsoft.Network/vpnGateways" => "VPN Gateway",
            "Microsoft.Network/privateEndpoints" => "Private Endpoint",
            "Microsoft.Network/trafficManagerProfiles" => "Traffic Manager Profile",
            "Microsoft.OperationalInsights/workspaces" => "Log Analytics Workspace",
            "Microsoft.Insights/components" => "Application Insights",
            "Microsoft.Cache/Redis" => "Azure Cache for Redis",
            "Microsoft.AppConfiguration/configurationStores" => "App Configuration Store",
            _ => ToDisplayName(typeSegments.LastOrDefault() ?? provider)
        };
    }

    private static string ToDisplayName(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return "resource";
        }

        var cleaned = type.Replace('_', ' ').Replace('-', ' ');
        return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
    }
}
