using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers;

/// <summary>
/// Validates provider value formatter registrations.
/// </summary>
public class ProviderValueFormatterRegistryTests
{
    /// <summary>
    /// Sample tenant identifier used for provider registration tests.
    /// </summary>
    private const string TenantId = "12345678-1234-1234-1234-123456789012";

    /// <summary>
    /// Sample management group identifier used for provider registration tests.
    /// </summary>
    private const string ManagementGroupId = "mg-core";

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
    /// Ensures AzApi registers tenant value formatting.
    /// </summary>
    [Test]
    public void AzApiModule_RegisterValueFormatters_FormatsTenantIds()
    {
        var registry = new ValueFormatterRegistry();
        var entityMapper = CreateEntityMapper();
        var module = new AzApiModule(entityMapper: entityMapper);

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext("azapi", null, "tenant_id", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// Ensures AzApi registers management group value formatting.
    /// </summary>
    [Test]
    public void AzApiModule_RegisterValueFormatters_FormatsManagementGroupIds()
    {
        var registry = new ValueFormatterRegistry();
        var entityMapper = CreateEntityMapper();
        var module = new AzApiModule(entityMapper: entityMapper);

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext("azapi", null, "management_group_id", ManagementGroupId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🗂️\u00A0Core Platform`");
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

        formatted.Should().Be("`🛡️\u00A0Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)`");
    }

    /// <summary>
    /// Ensures AzureAD registers tenant value formatting.
    /// </summary>
    [Test]
    public void AzureAdModule_RegisterValueFormatters_FormatsTenantIds()
    {
        var registry = new ValueFormatterRegistry();
        var entityMapper = CreateEntityMapper();
        var module = new AzureADModule(entityMapper);

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext("azuread", null, "tenant_id", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// Ensures Azure DevOps registers tenant value formatting.
    /// </summary>
    [Test]
    public void AzureDevOpsModule_RegisterValueFormatters_FormatsTenantIds()
    {
        var registry = new ValueFormatterRegistry();
        var entityMapper = CreateEntityMapper();
        var module = new AzureDevOpsModule(LargeValueFormat.SimpleDiff, entityMapper);

        module.RegisterValueFormatters(registry);

        var context = new ServiceResolutionContext("azuredevops", null, "tenant_id", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// Builds an entity mapper with tenant and management group mappings.
    /// </summary>
    /// <returns>Configured entity mapper for provider tests.</returns>
    private static AzureEntityMapper CreateEntityMapper()
    {
        return new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry(ManagementGroupId, "Core Platform")],
            tenants: [new MappingEntry(TenantId, "Contoso")]);
    }
}
