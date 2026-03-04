using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzApi;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzApi;

/// <summary>
/// Unit tests for <see cref="AzApiResourceIdCaseChangeFilter"/>.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// Related issue: docs/issues/108-azapi-body-casing-filter/analysis.md.
/// </summary>
[Category("Unit")]
public class AzApiResourceIdCaseChangeFilterTests
{
    private readonly AzApiResourceIdCaseChangeFilter _filter = new();

    // -------------------------------------------------------------------------
    // Azure ID casing-only change is suppressed for azapi provider.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully-qualified azapi provider + Azure ID differing only in casing → suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdCasingOnlyChange_FullyQualifiedProvider_ReturnsTrue()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/azure/azapi",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC",
            AfterValue: "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue("a casing-only Azure ID change for azapi should be suppressed");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Short provider name "azapi" also triggers suppression.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdCasingOnlyChange_ShortProviderName_ReturnsTrue()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "azapi",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/APP-RG-GWC",
            AfterValue: "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/app-rg-gwc");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue("short provider name 'azapi' should also match");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hashicorp-namespaced azapi provider path is also matched.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdCasingOnlyChange_HashicorpNamespacedProvider_ReturnsTrue()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azapi",
            AttributeName: "parent_id",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/My-RG",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeTrue("hashicorp-namespaced azapi provider should also match");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Non-Azure-ID string casing change is NOT suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// azapi provider, non-Azure-ID attribute "display_name: MyApp → myapp" → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzureIdStringCasingChange_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/azure/azapi",
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
    // Non-azapi providers are NOT filtered.
    // -------------------------------------------------------------------------

    /// <summary>
    /// azurerm provider with Azure-ID-shaped values differing only in casing → not suppressed
    /// (handled by <see cref="AzureResourceIdCaseChangeFilter"/> instead).
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzApiProvider_Azurerm_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("azurerm provider is handled by a separate filter, not this one");
        await Task.CompletedTask;
    }

    /// <summary>
    /// AWS provider with Azure-ID-shaped values differing only in casing → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NonAzApiProvider_Aws_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/hashicorp/aws",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("non-azapi provider 'aws' should not be filtered");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Null values → not suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Null before value → not suppressed regardless of after value.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NullBeforeValue_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/azure/azapi",
            AttributeName: "resource_id",
            BeforeValue: null,
            AfterValue: "/subscriptions/abc123/resourceGroups/my-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("null before value should not be suppressed");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Null after value → not suppressed regardless of before value.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_NullAfterValue_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/azure/azapi",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/abc123/resourceGroups/my-rg",
            AfterValue: null);

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("null after value should not be suppressed");
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Genuine content change (not just casing) is NOT suppressed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Genuine content difference (not casing-only) on an Azure ID → not suppressed.
    /// </summary>
    [Test]
    public async Task ShouldSuppress_AzureIdGenuineContentChange_ReturnsFalse()
    {
        // Arrange
        var context = new AttributeChangeFilterContext(
            ProviderName: "registry.terraform.io/azure/azapi",
            AttributeName: "resource_id",
            BeforeValue: "/subscriptions/ABC123/resourceGroups/old-rg",
            AfterValue: "/subscriptions/abc123/resourceGroups/new-rg");

        // Act
        var result = _filter.ShouldSuppress(context);

        // Assert
        result.Should().BeFalse("a genuine content change (different resource group) should not be suppressed");
        await Task.CompletedTask;
    }
}
