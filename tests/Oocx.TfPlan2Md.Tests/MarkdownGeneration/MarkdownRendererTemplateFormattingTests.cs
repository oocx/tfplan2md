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
}
