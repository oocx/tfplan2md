using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers;

/// <summary>
/// Validates provider value formatter registrations.
/// </summary>
public class ProviderValueFormatterRegistryTests
{
    /// <summary>
    /// Ensures AzureRM registers the Azure resource ID formatter.
    /// </summary>
    [Test]
    public void AzureRmModule_RegisterValueFormatters_FormatsAzureResourceIds()
    {
        var registry = new ValueFormatterRegistry();
        var module = new AzureRMModule(LargeValueFormat.SimpleDiff, new NullPrincipalMapper());

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext(
            "azurerm",
            null,
            null,
            "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/acc");

        var formatted = registry.TryFormat(context);

        formatted.Should().Contain("Storage Account");
    }

    /// <summary>
    /// Ensures AzApi registers the Azure resource ID formatter.
    /// </summary>
    [Test]
    public void AzApiModule_RegisterValueFormatters_FormatsAzureResourceIds()
    {
        var registry = new ValueFormatterRegistry();
        var module = new AzApiModule();

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext(
            "azapi",
            null,
            null,
            "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/acc");

        var formatted = registry.TryFormat(context);

        formatted.Should().Contain("Storage Account");
    }

    /// <summary>
    /// Ensures Azure resource IDs are formatted even when the attribute name is unknown.
    /// </summary>
    [Test]
    public void AzureRmModule_RegisterValueFormatters_FormatsUnknownAttributeAzureIds()
    {
        var registry = new ValueFormatterRegistry();
        var module = new AzureRMModule(LargeValueFormat.SimpleDiff, new NullPrincipalMapper());

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext(
            "azurerm",
            null,
            "some_custom_prop",
            "/providers/Microsoft.Management/managementGroups/mg-contoso");

        var formatted = registry.TryFormat(context);

        formatted.Should().Contain("Management Group");
    }

    /// <summary>
    /// Ensures AzureRM registers the role definition formatter.
    /// </summary>
    [Test]
    public void AzureRmModule_RegisterValueFormatters_FormatsRoleDefinitionIds()
    {
        var registry = new ValueFormatterRegistry();
        var module = new AzureRMModule(LargeValueFormat.SimpleDiff, new NullPrincipalMapper());

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext(
            "azurerm",
            null,
            "role_definition_id",
            "/subscriptions/sub-one/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7");

        var formatted = registry.TryFormat(context);

        formatted.Should().Contain("Reader");
        formatted.Should().Contain("acdd72a7-3385-48ef-bd42-f606fba81ae7");
    }
}
