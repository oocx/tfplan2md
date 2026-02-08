using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Azure;

/// <summary>
/// Tests for Azure value formatters registered through the value formatter registry.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </summary>
public class AzureValueFormatterTests
{
    /// <summary>
    /// Sample tenant ID used for formatter tests.
    /// </summary>
    private const string TenantId = "12345678-1234-1234-1234-123456789012";

    /// <summary>
    /// Sample management group ID used for formatter tests.
    /// </summary>
    private const string ManagementGroupId = "mg-core";

    /// <summary>
    /// Sample role definition GUID used to validate precedence.
    /// </summary>
    private const string RoleGuid = "acdd72a7-3385-48ef-bd42-f606fba81ae7";

    /// <summary>
    /// Regex pattern for GUID values used by fallback matching.
    /// </summary>
    private const string GuidPattern = "(?i)^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    /// <summary>
    /// TC-01: Verifies mapped tenant IDs format with the tenant icon.
    /// </summary>
    [Test]
    public void TenantIdFormatter_MappedValue_FormatsWithIcon()
    {
        var registry = CreateRegistryWithTenantFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "tenant_id", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// TC-02: Verifies unmapped tenant IDs fall back by returning null.
    /// </summary>
    [Test]
    public void TenantIdFormatter_UnmappedValue_ReturnsNull()
    {
        var registry = CreateRegistryWithTenantFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "tenant_id", "00000000-0000-0000-0000-000000000000");

        var formatted = registry.TryFormat(context);

        formatted.Should().BeNull();
    }

    /// <summary>
    /// TC-03: Verifies management group values format with the management group icon.
    /// </summary>
    [Test]
    public void ManagementGroupIdFormatter_MappedValue_FormatsWithIcon()
    {
        var registry = CreateRegistryWithManagementGroupFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "management_group_id", ManagementGroupId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🗂️\u00A0Core Platform`");
    }

    /// <summary>
    /// TC-07: Verifies tenantId attribute name variants are matched.
    /// </summary>
    [Test]
    public void TenantIdFormatter_HandlesTenantIdAttributeVariant()
    {
        var registry = CreateRegistryWithTenantFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "tenantId", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// TC-07: Verifies managementGroupId attribute name variants are matched.
    /// </summary>
    [Test]
    public void ManagementGroupIdFormatter_HandlesManagementGroupIdAttributeVariant()
    {
        var registry = CreateRegistryWithManagementGroupFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "managementGroupId", ManagementGroupId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🗂️\u00A0Core Platform`");
    }

    /// <summary>
    /// TC-08: Verifies GUID fallback formatting when the GUID is mapped to a tenant.
    /// </summary>
    [Test]
    public void TenantIdFormatter_MatchesGuidFallbackWhenMapped()
    {
        var registry = CreateRegistryWithTenantFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "some_random_id", TenantId);

        var formatted = registry.TryFormat(context);

        formatted.Should().Be("`🏢\u00A0Contoso (12345678-1234-1234-1234-123456789012)`");
    }

    /// <summary>
    /// TC-09: Ensures role definition formatting takes precedence over tenant formatting.
    /// </summary>
    [Test]
    public void TenantIdFormatter_DoesNotOverrideRoleDefinitionFormatting()
    {
        var registry = CreateRegistryWithRoleAndTenantFormatter();
        var context = new ServiceResolutionContext("azurerm", null, "role_definition_id", RoleGuid);

        var formatted = registry.TryFormat(context);

        formatted.Should().Contain("🛡️");
        formatted.Should().Contain("Reader");
    }

    /// <summary>
    /// Builds a formatter registry for tenant matching rules.
    /// </summary>
    /// <returns>Configured formatter registry for tenant formatting.</returns>
    private static ValueFormatterRegistry CreateRegistryWithTenantFormatter()
    {
        var registry = new ValueFormatterRegistry();
        var mapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: [new MappingEntry(TenantId, "Contoso")]);
        var formatter = new TenantIdFormatter(mapper);

        registry.Register(
            new MatchPattern("(^azurerm$|.*/azurerm$)", null, "^tenant_id$|^tenantId$", null),
            formatter);
        registry.Register(
            new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, GuidPattern),
            formatter);

        return registry;
    }

    /// <summary>
    /// Builds a formatter registry for management group matching rules.
    /// </summary>
    /// <returns>Configured formatter registry for management group formatting.</returns>
    private static ValueFormatterRegistry CreateRegistryWithManagementGroupFormatter()
    {
        var registry = new ValueFormatterRegistry();
        var mapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry(ManagementGroupId, "Core Platform")],
            tenants: []);
        var formatter = new ManagementGroupIdFormatter(mapper);

        registry.Register(
            new MatchPattern("(^azurerm$|.*/azurerm$)", null, "^management_group_id$|^managementGroupId$", null),
            formatter);

        return registry;
    }

    /// <summary>
    /// Builds a formatter registry that includes both role and tenant formatters.
    /// </summary>
    /// <returns>Configured formatter registry with role precedence.</returns>
    private static ValueFormatterRegistry CreateRegistryWithRoleAndTenantFormatter()
    {
        var registry = new ValueFormatterRegistry();
        var mapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: [new MappingEntry(RoleGuid, "Contoso")]);

        registry.Register(
            new MatchPattern(
                "(^azurerm$|.*/azurerm$)",
                null,
                "^role_definition_id$|^role_definition_resource_id$",
                "(?i)^(?:/subscriptions/[^/]+/providers/Microsoft.Authorization/roleDefinitions/[^/]+|/providers/Microsoft.Authorization/roleDefinitions/[^/]+|[0-9a-f-]{36})$"),
            new RoleDefinitionFormatter());

        var tenantFormatter = new TenantIdFormatter(mapper);
        registry.Register(
            new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, GuidPattern),
            tenantFormatter);

        return registry;
    }
}
