using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Azure;

/// <summary>
/// Tests for Azure entity mapping and scope enrichment.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </summary>
public class AzureEntityMapperTests
{
    private const string SubscriptionId = "sub-1";
    private const string ManagementGroupId = "mg-1";
    private const string TenantId = "tenant-1";

    [Test]
    public void AzureEntityMapper_SubscriptionId_ResolvesToDisplayName()
    {
        var mapper = CreateMapper(
            subscriptions: [new MappingEntry(SubscriptionId, "Production")],
            managementGroups: [],
            tenants: []);

        mapper.GetSubscriptionDisplayName(SubscriptionId).Should().Be("Production (sub-1)");
    }

    [Test]
    public void AzureEntityMapper_ManagementGroupId_ResolvesToDisplayName()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry(ManagementGroupId, "Core")],
            tenants: []);

        mapper.GetManagementGroupDisplayName(ManagementGroupId).Should().Be("Core");
    }

    [Test]
    public void AzureEntityMapper_TenantId_ResolvesToDisplayName()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: [new MappingEntry(TenantId, "Contoso")]);

        mapper.GetTenantDisplayName(TenantId).Should().Be("Contoso (tenant-1)");
    }

    [Test]
    public void AzureEntityMapper_SubscriptionId_FallsBackToRawId()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: []);

        mapper.GetSubscriptionDisplayName(SubscriptionId).Should().Be(SubscriptionId);
    }

    [Test]
    public void AzureEntityMapper_TenantId_FallsBackToRawId()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: []);

        mapper.GetTenantDisplayName(TenantId).Should().Be(TenantId);
    }

    [Test]
    public void EnrichedAzureScopeFormatter_ResourceScope_IncludesSubscriptionName()
    {
        var mapper = CreateMapper(
            subscriptions: [new MappingEntry("sub-1", "Prod")],
            managementGroups: [],
            tenants: []);
        var formatter = new EnrichedAzureScopeFormatter(mapper);
        const string scope = "/subscriptions/sub-1/resourceGroups/rg1/providers/Microsoft.KeyVault/vaults/kv1";

        var result = formatter.FormatScope(scope);

        result.Should().Be("Key Vault `kv1` in resource group `📁 rg1` of subscription `🔑 Prod (sub-1)`");
    }

    [Test]
    public void EnrichedAzureScopeFormatter_RootManagementGroup_FormatsCorrectly()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [],
            tenants: [new MappingEntry("tenant-1", "Contoso")]);
        var formatter = new EnrichedAzureScopeFormatter(mapper);
        const string scope = "/providers/Microsoft.Management/managementGroups/tenant-1";

        var result = formatter.FormatScope(scope);

        result.Should().Be("`🗂️\u00A0Tenant Contoso (tenant-1) root`");
    }

    [Test]
    public void EnrichedAzureScopeFormatter_ManagementGroup_FormatsWithIcon()
    {
        var mapper = CreateMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry(ManagementGroupId, "Core")],
            tenants: []);
        var formatter = new EnrichedAzureScopeFormatter(mapper);
        const string scope = "/providers/Microsoft.Management/managementGroups/mg-1";

        var result = formatter.FormatScope(scope);

        result.Should().Be("`🗂️\u00A0Core`");
    }

    private static AzureEntityMapper CreateMapper(
        IReadOnlyList<MappingEntry> subscriptions,
        IReadOnlyList<MappingEntry> managementGroups,
        IReadOnlyList<MappingEntry> tenants)
    {
        return new AzureEntityMapper(subscriptions, managementGroups, tenants);
    }
}
