using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Unit tests for <see cref="AzureResourceIdCaseChangeFilter"/>.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
[Category("Unit")]
public class AzureResourceIdCaseChangeFilterTests
{
    private readonly AzureResourceIdCaseChangeFilter _filter = new();

    // -------------------------------------------------------------------------
    // TC-17: Azure ID casing-only change is suppressed for azurerm provider.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-17a: Fully-qualified azurerm provider + Azure ID differing only in casing → suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdCasingOnlyChange_FullyQualifiedProvider_ReturnsTrue()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "scope",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue("a casing-only Azure ID change for azurerm should be suppressed");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-17b: Short provider name "azurerm" also triggers suppression.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdCasingOnlyChange_ShortProviderName_ReturnsTrue()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "azurerm",
            AttributeName: "scope",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue("short provider name 'azurerm' should also match");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-17c: Subscription-scoped role definition path (casing-only) is suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_SubscriptionScopedRoleDefinitionCasingOnly_ReturnsTrue()
    {
        // Arrange - use a subscription-scoped path which IS recognized by AzureScopeParser
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "role_definition_id",
            BeforeValue: "/subscriptions/ABC123/providers/Microsoft.Authorization/roleDefinitions/XYZ",
            AfterValue: "/subscriptions/abc123/providers/Microsoft.Authorization/roleDefinitions/xyz");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue();
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-18: Non-Azure-ID string casing change is NOT suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-18: azurerm provider, non-Azure-ID attribute "display_name: MyApp → myapp" → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzureIdStringCasingChange_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "display_name",
            BeforeValue: "MyApp",
            AfterValue: "myapp");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("non-Azure-ID casing changes should never be suppressed");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-19: Non-azurerm providers are NOT filtered.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-19a: AWS provider with Azure-ID-shaped values differing only in casing → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzureRmProvider_Aws_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/aws",
            AttributeName: "scope",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("non-azurerm provider 'aws' should not be filtered");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-19b: azapi provider with Azure-ID-shaped values → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzureRmProvider_Azapi_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azapi",
            AttributeName: "scope",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("non-azurerm provider 'azapi' should not be filtered");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-20: Null BeforeValue → not suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-20: Null before value → not suppressed regardless of after value.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NullBeforeValue_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "tenant_id",
            BeforeValue: null,
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("null before value should not be suppressed");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // TC-21: Null AfterValue → not suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// TC-21: Null after value → not suppressed regardless of before value.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NullAfterValue_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "tenant_id",
            BeforeValue: "/subscriptions/abc123/resourceGroups/my-rg",
            AfterValue: null);

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("null after value should not be suppressed");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Additional: Genuine content change (not just casing) is NOT suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Genuine content difference (not casing-only) on an Azure ID → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdGenuineContentChange_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "scope",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/old-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/new-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("a genuine content change (different resource group) should not be suppressed");
        await Task.CompletedTask;
    }
}
