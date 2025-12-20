using System.Collections.Generic;

namespace Oocx.TfPlan2Md.Azure;

public static class AzureScopeParser
{
    public static string ParseScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return scope ?? string.Empty;
        }

        var parts = scope.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return scope;
        }

        if (IsManagementGroupScope(parts))
        {
            var managementGroup = parts[3];
            return $"**{managementGroup}** (Management Group)";
        }

        if (IsSubscriptionScope(parts))
        {
            var subscriptionId = parts[1];
            return $"subscription **{subscriptionId}**";
        }

        if (IsResourceScope(parts))
        {
            var subscriptionId = parts[1];
            var resourceGroup = parts[3];
            var resourceType = GetResourceType(parts);
            var resourceName = parts[^1];
            return $"{resourceType} **{resourceName}** in resource group **{resourceGroup}** of subscription **{subscriptionId}**";
        }

        if (IsResourceGroupScope(parts))
        {
            var subscriptionId = parts[1];
            var resourceGroup = parts[3];
            return $"**{resourceGroup}** in subscription **{subscriptionId}**";
        }

        return scope;
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

    private static string GetResourceType(string[] parts)
    {
        if (parts.Length < 7)
        {
            return "resource";
        }

        var provider = parts[5];
        var typeSegments = new List<string>();

        for (var i = 6; i < parts.Length - 1; i += 2)
        {
            typeSegments.Add(parts[i]);
        }

        if (typeSegments.Count == 0)
        {
            typeSegments.Add(parts[6]);
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
            _ => ToDisplayName(typeSegments[^1])
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
