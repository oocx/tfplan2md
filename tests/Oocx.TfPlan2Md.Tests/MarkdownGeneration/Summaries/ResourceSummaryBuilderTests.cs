using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Summaries;

public class ResourceSummaryBuilderTests
{
    private readonly ResourceSummaryBuilder _builder = new();

    [Fact]
    public void BuildSummary_Create_UsesResourceSpecificMapping()
    {
        var change = CreateChange(
            type: "azurerm_storage_account",
            action: "create",
            afterJson: "{ \"name\": \"st1\", \"resource_group_name\": \"rg1\", \"location\": \"eastus\", \"account_tier\": \"Standard\", \"account_replication_type\": \"LRS\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`st1` in `rg1` (`eastus`) | `Standard LRS`");
    }

    [Fact]
    public void BuildSummary_Create_UsesProviderFallback()
    {
        var change = CreateChange(
            type: "azurerm_unknown_resource",
            action: "create",
            afterJson: "{ \"name\": \"res1\", \"resource_group_name\": \"rg1\", \"location\": \"westeurope\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`res1` in `rg1` (`westeurope`)");
    }

    [Fact]
    public void BuildSummary_Create_UsesGenericFallback()
    {
        var change = CreateChange(
            type: "random_string",
            action: "create",
            afterJson: "{ \"display_name\": \"token-name\" }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`token-name`");
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void BuildSummary_MsGraph_UsesUrlAndDisplayName()
    {
        var change = CreateChange(
            type: "msgraph_resource",
            action: "create",
            afterJson: "{ \"url\": \"applications\", \"body\": { \"displayName\": \"myapp\" } }"
        );

        var summary = _builder.BuildSummary(change);

        summary.Should().Be("`myapp` (applications)");
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
