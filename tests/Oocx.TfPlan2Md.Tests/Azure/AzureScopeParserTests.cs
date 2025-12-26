using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.Tests.Azure;

public class AzureScopeParserTests
{
    [Fact]
    public void IsAzureResourceId_ReturnsTrue_ForValidScopes()
    {
        AzureScopeParser.IsAzureResourceId("/subscriptions/12345678-1234-1234-1234-123456789012").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv").Should().BeTrue();
        AzureScopeParser.IsAzureResourceId("/providers/Microsoft.Management/managementGroups/my-mg").Should().BeTrue();
    }

    [Fact]
    public void IsAzureResourceId_ReturnsFalse_ForNonAzureScopes()
    {
        AzureScopeParser.IsAzureResourceId("not-an-id").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("https://portal.azure.com").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("/subscriptions/").Should().BeFalse();
        AzureScopeParser.IsAzureResourceId("12345678-1234-1234-1234-123456789012").Should().BeFalse();
    }

    [Fact]
    public void ParseScope_ManagementGroupScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/providers/Microsoft.Management/managementGroups/my-mg";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("**my-mg** (Management Group)");
    }

    [Fact]
    public void ParseScope_SubscriptionScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/12345678-1234-1234-1234-123456789012";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("subscription **12345678-1234-1234-1234-123456789012**");
    }

    [Fact]
    public void ParseScope_ResourceGroupScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/my-rg";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("**my-rg** in subscription **12345678-1234-1234-1234-123456789012**");
    }

    [Fact]
    public void ParseScope_ResourceScope_ReturnsFormattedString()
    {
        // Arrange
        const string scope = "/subscriptions/sub-id/resourceGroups/my-rg/providers/Microsoft.KeyVault/vaults/my-kv";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be("Key Vault **my-kv** in resource group **my-rg** of subscription **sub-id**");
    }

    [Fact]
    public void ParseScope_AppServiceScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/app-rg/providers/Microsoft.Web/sites/myapp";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("App Service **myapp** in resource group **app-rg** of subscription **sub-id**");
    }

    [Fact]
    public void ParseScope_SqlDatabaseScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/db-rg/providers/Microsoft.Sql/servers/sqlsrv/databases/mydb";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("SQL Database **mydb** in resource group **db-rg** of subscription **sub-id**");
    }

    [Fact]
    public void ParseScope_AksScope_ReturnsFriendlyType()
    {
        const string scope = "/subscriptions/sub-id/resourceGroups/aks-rg/providers/Microsoft.ContainerService/managedClusters/aks1";

        var result = AzureScopeParser.ParseScope(scope);

        result.Should().Be("AKS Cluster **aks1** in resource group **aks-rg** of subscription **sub-id**");
    }

    [Fact]
    public void ParseScope_InvalidFormat_ReturnsOriginalString()
    {
        // Arrange
        const string scope = "not-a-scope";

        // Act
        var result = AzureScopeParser.ParseScope(scope);

        // Assert
        result.Should().Be(scope);
    }

    [Fact]
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
        result.Details.Should().Be("my-rg in subscription 12345678-1234-1234-1234-123456789012");
    }

    [Fact]
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
        result.Details.Should().Be("Storage Account sttfplan2mddata in resource group my-rg of subscription sub-id");
    }
}
