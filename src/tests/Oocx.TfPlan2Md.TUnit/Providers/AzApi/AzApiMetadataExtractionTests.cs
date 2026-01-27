using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Scriban.Runtime;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for azapi metadata extraction helper to ensure template inputs remain correct after refactoring.
/// </summary>
public class AzApiMetadataExtractionTests
{
    [Test]
    public async Task ExtractAzapiMetadata_WithCompleteChange_ExtractsExpectedFields()
    {
        var afterState = JsonDocument.Parse("""
            {
                "type": "Microsoft.Sql/servers@2023-01-01",
                "name": "sql-primary",
                "parent_id": "/subscriptions/sub-1/resourceGroups/rg-sql",
                "location": "westeurope",
                "tags": { "env": "prod" }
            }
            """).RootElement;

        var change = new ResourceChangeModel
        {
            Address = "azapi_resource.sql",
            Type = "azapi_resource",
            Name = "sql",
            ProviderName = "azapi",
            Action = "create",
            ActionSymbol = "+",
            AttributeChanges = Array.Empty<AttributeChangeModel>(),
            AfterJson = afterState
        };

        var result = AzApiHelpers.ExtractAzapiMetadata(change);

        result.Should().ContainKey("type").WhoseValue.Should().Be("Microsoft.Sql/servers@2023-01-01");
        result.Should().ContainKey("name").WhoseValue.Should().Be("sql-primary");
        result.Should().ContainKey("parent_id").WhoseValue.Should().NotBeNull();
        result.Should().ContainKey("location").WhoseValue.Should().Be("westeurope");
        result.Should().ContainKey("tags").WhoseValue.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ExtractAzapiMetadata_WithInvalidInput_ReturnsEmptyScriptObject()
    {
        var result = AzApiHelpers.ExtractAzapiMetadata(new object());

        result.Keys.Should().BeEmpty();

        await Task.CompletedTask;
    }
}
