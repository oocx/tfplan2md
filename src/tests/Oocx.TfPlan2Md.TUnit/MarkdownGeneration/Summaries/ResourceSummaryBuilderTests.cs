using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Summaries;

public class ResourceSummaryBuilderTests
{
    /// <summary>
    /// Terraform action name for create operations.
    /// </summary>
    private const string CreateAction = "create";

    private readonly ResourceSummaryBuilder _builder = new();

    [Test]
    public void BuildSummary_Create_UsesResourceSpecificMapping()
    {
        var change = CreateChange(
            type: "azurerm_storage_account",
            action: CreateAction,
            afterJson: "{ \"name\": \"st1\", \"resource_group_name\": \"rg1\", \"location\": \"eastus\", \"account_tier\": \"Standard\", \"account_replication_type\": \"LRS\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`st1` in `rg1` (`eastus`) | `Standard LRS`");
    }

    [Test]
    public void BuildSummary_Create_UsesProviderFallback()
    {
        var change = CreateChange(
            type: "azurerm_unknown_resource",
            action: CreateAction,
            afterJson: "{ \"name\": \"res1\", \"resource_group_name\": \"rg1\", \"location\": \"westeurope\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`res1` in `rg1` (`westeurope`)");
    }

    [Test]
    public void BuildSummary_Create_UsesGenericFallback()
    {
        var change = CreateChange(
            type: "random_string",
            action: CreateAction,
            afterJson: "{ \"display_name\": \"token-name\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`token-name`");
    }

    [Test]
    public void BuildSummary_Update_WithFewChanges_ListsAll()
    {
        var change = CreateChange(
            type: "azurerm_app_service",
            action: "update",
            afterJson: "{ \"name\": \"app1\" }",
            attributeChanges: new List<AttributeChangeModel>
            {
                new() { Name = "tags" },
                new() { Name = "sku" }
            }
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`app1` | Changed: tags, sku");
    }

    [Test]
    public void BuildSummary_Update_WithManyChanges_Truncates()
    {
        var change = CreateChange(
            type: "azurerm_app_service",
            action: "update",
            afterJson: "{ \"name\": \"app1\" }",
            attributeChanges: new List<AttributeChangeModel>
            {
                new() { Name = "tags" },
                new() { Name = "sku" },
                new() { Name = "capacity" },
                new() { Name = "tier" }
            }
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`app1` | Changed: tags, sku, capacity, +1 more");
    }

    [Test]
    public void BuildSummary_Replace_WithReason_UsesReplacePaths()
    {
        var change = CreateChange(
            type: "azurerm_subnet",
            action: "replace",
            afterJson: "{ \"name\": \"snet\" }",
            replacePaths: new List<IReadOnlyList<object>>
            {
                new List<object> { "address_prefixes", 0 },
                new List<object> { "route_table_id" }
            }
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("recreate `snet` (address_prefixes[0], route_table_id changed: force replacement)");
    }

    [Test]
    public void BuildSummary_Replace_WithoutReason_UsesChangeCount()
    {
        var change = CreateChange(
            type: "azurerm_subnet",
            action: "replace",
            afterJson: "{ \"name\": \"snet\" }",
            attributeChanges: new List<AttributeChangeModel>
            {
                new() { Name = "address_prefixes" },
                new() { Name = "service_endpoints" }
            }
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("recreating `snet` (2 changed)");
    }

    [Test]
    public void BuildSummary_Delete_ShowsName()
    {
        var change = CreateChange(
            type: "azurerm_storage_account",
            action: "delete",
            beforeJson: "{ \"name\": \"st1\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`st1`");
    }

    /// <summary>
    /// Verifies that BuildDeleteSummary correctly returns null when no display name is available.
    /// This test confirms the fix for the redundant ternary expression (return name is not null ? name : null).
    /// </summary>
    [Test]
    public void BuildSummary_Delete_ReturnsNullWhenNoDisplayName()
    {
        // Create a change with empty before state to force null return from FormatSummaryValue
        var change = CreateChange(
            type: "test_resource",
            action: "delete",
            beforeJson: "{ }"
        );

        var summary = _builder.BuildSummary(change);

        // When there's no display name and address is used as fallback,
        // BuildDeleteSummary should return the address formatted as markdown
        summary.Should().NotBeNull();
        summary.Should().Be("`resource.example`");
    }

    /// <summary>
    /// TC-01: All three azuredevops_user_entitlement fields populated → all appear in summary.
    /// Related feature: docs/features/048-azuredevops-user-entitlement-summary/specification.md.
    /// </summary>
    [Test]
    public void BuildSummary_AzureDevOpsUserEntitlement_AllFieldsPopulated_ShowsAllFields()
    {
        var change = CreateChange(
            type: "azuredevops_user_entitlement",
            action: CreateAction,
            afterJson: "{ \"principal_name\": \"user@example.com\", \"account_license_type\": \"express\", \"licensing_source\": \"msdn\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`user@example.com` | `express` | `msdn`");
    }

    /// <summary>
    /// TC-02: licensing_source empty → omitted from summary.
    /// Related feature: docs/features/048-azuredevops-user-entitlement-summary/specification.md.
    /// </summary>
    [Test]
    public void BuildSummary_AzureDevOpsUserEntitlement_LicensingSourceEmpty_OmittedFromSummary()
    {
        var change = CreateChange(
            type: "azuredevops_user_entitlement",
            action: CreateAction,
            afterJson: "{ \"principal_name\": \"user@example.com\", \"account_license_type\": \"express\", \"licensing_source\": \"\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`user@example.com` | `express`");
    }

    /// <summary>
    /// TC-03: Only principal_name populated → only that appears in summary.
    /// Related feature: docs/features/048-azuredevops-user-entitlement-summary/specification.md.
    /// </summary>
    [Test]
    public void BuildSummary_AzureDevOpsUserEntitlement_OnlyPrincipalName_ShowsOnlyPrincipalName()
    {
        var change = CreateChange(
            type: "azuredevops_user_entitlement",
            action: CreateAction,
            afterJson: "{ \"principal_name\": \"user@example.com\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`user@example.com`");
    }

    /// <summary>
    /// TC-04: All fields empty → fallback (address-based summary).
    /// Related feature: docs/features/048-azuredevops-user-entitlement-summary/specification.md.
    /// </summary>
    [Test]
    public void BuildSummary_AzureDevOpsUserEntitlement_AllFieldsEmpty_FallsBackToAddress()
    {
        var change = CreateChange(
            type: "azuredevops_user_entitlement",
            action: CreateAction,
            afterJson: "{ }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`resource.example`");
    }

    [Test]
    public void BuildSummary_MsGraph_UsesUrlAndDisplayName()
    {
        var change = CreateChange(
            type: "msgraph_resource",
            action: CreateAction,
            afterJson: "{ \"url\": \"applications\", \"body\": { \"displayName\": \"myapp\" } }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`myapp` (applications)");
    }

    /// <summary>
    /// Verifies Azure resource IDs are formatted with subscription display names when a formatter registry is supplied.
    /// </summary>
    [Test]
    public void BuildSummary_Create_FormatsAzureResourceIdValuesWithMapping()
    {
        var entityMapper = new AzureEntityMapper(
            new List<MappingEntry> { new("sub-123", "Production") },
            new List<MappingEntry>(),
            new List<MappingEntry>());
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);
        var registry = new ValueFormatterRegistry();
        AzureRmValueFormatterRegistration.Register(registry, scopeFormatter);
        var builder = new ResourceSummaryBuilder(registry);

        var change = CreateChange(
            type: "azurerm_container_app",
            action: CreateAction,
            afterJson: "{ \"name\": \"app1\", \"resource_group_name\": \"rg1\", \"location\": \"eastus\", \"container_app_environment_id\": \"/subscriptions/sub-123/resourceGroups/rg-env/providers/Microsoft.App/managedEnvironments/env1\" }");

        var summary = builder.BuildSummary(change);

        summary.Should().Be("`app1` in `rg1` (`eastus`) | ManagedEnvironments `🆔 env1` in resource group `📁\u00A0rg-env` of subscription `🔑\u00A0Production (sub-123)`");
    }

    private static ResourceChangeModel CreateChange(
        string type,
        string action,
        string afterJson = "{ }",
        string? beforeJson = null,
        IReadOnlyList<AttributeChangeModel>? attributeChanges = null,
        IReadOnlyList<IReadOnlyList<object>>? replacePaths = null)
    {
        JsonElement? beforeElement = null;
        if (beforeJson is not null)
        {
            using var beforeDoc = JsonDocument.Parse(beforeJson);
            beforeElement = beforeDoc.RootElement.Clone();
        }

        using var afterDoc = JsonDocument.Parse(afterJson);
        var afterElement = afterDoc.RootElement.Clone();

        attributeChanges ??= new List<AttributeChangeModel>();

        return new ResourceChangeModel
        {
            Address = "resource.example",
            ModuleAddress = string.Empty,
            Type = type,
            Name = "example",
            ProviderName = GetProvider(type) ?? string.Empty,
            Action = action,
            ActionSymbol = "!",
            AttributeChanges = attributeChanges,
            BeforeJson = beforeElement,
            AfterJson = afterElement,
            ReplacePaths = replacePaths
        };
    }

    private static string? GetProvider(string resourceType)
    {
        var underscore = resourceType.IndexOf('_');
        return underscore > 0 ? resourceType[..underscore] : null;
    }
}
