using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererTemplateFormattingTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_FirewallRules_DetailsSectionsAreOutsideMainTable()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - ensure that details blocks are not inline inside the main rule table
        result.Should().NotBeNull();
        var lines = result.Split('\n');
        // Find the index of the main Rule table header row (the row with 'Rule Name') and its separator line
        var headerRow = lines.Select((l, i) => (l, i)).First(t => t.l.Contains("Rule Name"));
        var headerSep = lines[headerRow.i + 1];
        // Ensure no <details> tags appear between the header separator and the first blank line after the table rows
        var afterHeader = lines.Skip(headerRow.i + 2).ToList();
        var indexOfBlank = afterHeader.FindIndex(l => string.IsNullOrWhiteSpace(l));
        indexOfBlank.Should().BeGreaterThan(0);
        var linesWithinTable = afterHeader.Take(indexOfBlank);
        linesWithinTable.Should().NotContain("<details>");
    }

    [Fact]
    public void Render_LargeAttributes_MoveToDetailsSection()
    {
        // Arrange
        var beforeJson = JsonDocument.Parse("{\"name\":\"vm-app-01\",\"custom_data\":\"line1\\nline2\"}").RootElement;
        var afterJson = JsonDocument.Parse("{\"name\":\"vm-app-02\",\"custom_data\":\"line1\\nline3\"}").RootElement;

        var plan = new TerraformPlan(
            FormatVersion: "1.0",
            TerraformVersion: "1.6.0",
            ResourceChanges: new List<ResourceChange>
            {
                new(
                    Address: "azurerm_example.large",
                    ModuleAddress: null,
                    Mode: "managed",
                    Type: "azurerm_example",
                    Name: "large",
                    ProviderName: "provider.azurerm",
                    Change: new Change(
                        Actions: new List<string>{"update"},
                        Before: beforeJson,
                        After: afterJson,
                        AfterUnknown: null,
                        BeforeSensitive: null,
                        AfterSensitive: null
                    )
                )
            }
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        var section = markdown.Split("#### ", StringSplitOptions.RemoveEmptyEntries).First(s => s.Contains("azurerm_example.large"));
        section.Should().Contain("| `name` | vm-app-01 | vm-app-02 |");
        section.Should().NotContain("custom_data | line1");
        section.Should().Contain("<summary>Large values: custom_data (3 lines, 2 changed)</summary>");
        section.Should().Contain("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
    }
}
