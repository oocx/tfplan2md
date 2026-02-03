using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Azure;

public class AzureScopeParserTests
{
    [Test]
    public void IsAzureResourceId_ReturnsTrue_ForValidScopes()
    {
        AzureScopeParser.IsAzureResourceId("/subscriptions/12345678-1234-1234-1234-123456789012").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/providers/Microsoft.Management/managementGroups/my-mg").Should().BeTrue();
    }

    [Test]
    public void IsAzureResourceId_ReturnsFalse_ForNonAzureScopes()
    {
        AzureScopeParser.IsAzureResourceId("not-an-id").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("https://portal.azure.com").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("/subscriptions/").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("12345678-1234-1234-1234-123456789012").Should().BeFalse();
    }

    [Test]
    public void ParseScope_ManagementGroupScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/providers/Microsoft.Management/managementGroups/my-mg";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("`my-mg` (Management Group)");
    }

    [Test]
    public void ParseScope_SubscriptionScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/12345678-1234-1234-1234-123456789012";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("subscription `🔑 12345678-1234-1234-1234-123456789012`");
    }

    [Test]
    public void ParseScope_ResourceGroupScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("`my-rg` in subscription `🔑 12345678-1234-1234-1234-123456789012`");
    }

    [Test]
    public void ParseScope_ResourceScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("Key Vault `my-kv` in resource group `my-rg` of subscription `🔑 sub-id`");
    }

    [Test]
    public void ParseScope_SubscriptionProviderScope_ReturnsFormattedString()
    {
        const string scope = "/subscriptions/sub-id/providers/Microsoft.Storage/storageAccounts/st1";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("Storage Account `st1` in subscription `🔑 sub-id`");
    }

    [Test]
    public void ParseScope_KnownResourceTypes_ReturnFriendlyNames()
    {
        var cases = new Dictionary<string, string>
        {
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/acc1/blobServices/default"] = "Storage Account Blob Service",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/acc1/fileServices/default"] = "Storage Account File Service",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Compute/virtualMachines/vm1"] = "Virtual Machine",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Compute/virtualMachineScaleSets/vmss1"] = "Virtual Machine Scale Set",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Compute/disks/disk1"] = "Managed Disk",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.ContainerRegistry/registries/acr1"] = "Container Registry",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Web/serverfarms/plan1"] = "App Service Plan",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Sql/servers/sql1"] = "SQL Server",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.DocumentDB/databaseAccounts/cosmos1"] = "Cosmos DB Account",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.EventHub/namespaces/eh1"] = "Event Hubs Namespace",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.ServiceBus/namespaces/sb1"] = "Service Bus Namespace",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/virtualNetworks/vnet1"] = "Virtual Network",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/virtualNetworks/vnet1/subnets/sub1"] = "Subnet",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/networkSecurityGroups/nsg1"] = "Network Security Group",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/publicIPAddresses/pip1"] = "Public IP Address",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/loadBalancers/lb1"] = "Load Balancer",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/applicationGateways/appgw1"] = "Application Gateway",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/azureFirewalls/afw1"] = "Azure Firewall",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/vpnGateways/vpng1"] = "VPN Gateway",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/privateEndpoints/pe1"] = "Private Endpoint",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Network/trafficManagerProfiles/tm1"] = "Traffic Manager Profile",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.OperationalInsights/workspaces/log1"] = "Log Analytics Workspace",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Insights/components/ai1"] = "Application Insights",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.Cache/Redis/cache1"] = "Azure Cache for Redis",
            ["/subscriptions/sub-id/resourceGroups/rg/providers/Microsoft.AppConfiguration/configurationStores/appcfg1"] = "App Configuration Store"
        };

        foreach (var entry in cases)
        {
            var result = AzureScopeParser.ParseScope(entry.Key);

            result.Should().StartWith(entry.Value);
        }
    }

    [Test]
    public void ParseScope_AppServiceScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/app-rg/providers/Microsoft.Web/sites/myapp";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("App Service `myapp` in resource group `app-rg` of subscription `🔑 sub-id`");
    }

    [Test]
    public void ParseScope_SqlDatabaseScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/db-rg/providers/Microsoft.Sql/servers/sqlsrv/databases/mydb";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("SQL Database `mydb` in resource group `db-rg` of subscription `🔑 sub-id`");
    }

    [Test]
    public void ParseScope_AksScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/aks-rg/providers/Microsoft.ContainerService/managedClusters/aks1";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("AKS Cluster `aks1` in resource group `aks-rg` of subscription `🔑 sub-id`");
    }

    [Test]
    public void ParseScope_InvalidFormat_ReturnsOriginalString()
    {
        // Arrange
        const string scope = "not-a-scope";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be(scope);
    }

    [Test]
    public void Parse_ReturnsScopeInfo_ForResourceGroup()
    {
        const string scope = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg";

        var result = AzureScopeParser.Parse(scope);

        result.Name.Should().Be("my-rg");
        result.Type.Should().Be("Resource Group");
        result.SubscriptionId.Should().Be("12345678-1234-1234-1234-123456789012");
        result.ResourceGroup.Should().Be("my-rg");
        result.Level.Should().Be(ScopeLevel.ResourceGroup);
        result.Summary.Should().Be("my-rg");
        result.Details.Should().Be("my-rg in subscription 🔑 12345678-1234-1234-1234-123456789012");
    }

    [Test]
    public void Parse_ReturnsScopeInfo_ForResource()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.Storage/storageAccounts/sttfplan2mddata";

        var result = AzureScopeParser.Parse(scope);

        result.Name.Should().Be("sttfplan2mddata");
        result.Type.Should().Be("Storage Account");
        result.SubscriptionId.Should().Be("sub-id");
        result.ResourceGroup.Should().Be("my-rg");
        result.Level.Should().Be(ScopeLevel.Resource);
        result.Summary.Should().Be("Storage Account sttfplan2mddata");
        result.Details.Should().Be("Storage Account sttfplan2mddata in resource group my-rg of subscription 🔑 sub-id");
    }

    [Test]
    public void Parse_WithEmptyScope_ReturnsEmptyScopeInfo()
    {
        var result = AzureScopeParser.Parse(" ");

        result.Should().Be(ScopeInfo.Empty);
    }
}
