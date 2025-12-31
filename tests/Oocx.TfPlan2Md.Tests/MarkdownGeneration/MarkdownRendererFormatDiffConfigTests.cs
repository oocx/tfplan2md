using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies that markdown rendering respects the configured diff format for small values.
/// Related feature: docs/features/003-consistent-value-formatting/specification.md
/// </summary>
public class MarkdownRendererFormatDiffConfigTests
{
    /// <summary>
    /// Parses fixture plans for rendering tests.
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Ensures standard diff formatting is used when the model requests standard diff (TC-07).
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md
    /// </summary>
    [Fact]
    public void Render_UsesSimpleDiff_WhenModelConfigIsSimple()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/firewall-rule-changes.json"));
        var builder = new ReportModelBuilder(largeValueFormat: LargeValueFormat.SimpleDiff);
        var model = builder.Build(plan);
        var change = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");
        var renderer = new MarkdownRenderer();

        // Act
        var markdown = renderer.RenderResourceChange(change, LargeValueFormat.SimpleDiff)!;

        // Assert - simple diff uses -/+ prefix with <br> separator; semantic icons are preserved inside code formatting without inline diff styling
        markdown.Should().Contain("- `🌐 10.0.1.0/24`<br>+ `🌐 10.0.1.0/24, 🌐 10.0.3.0/24`")
            .And.NotContain("background-color:");
    }

    /// <summary>
    /// Ensures inline diff formatting is used when the model requests inline diff (TC-07).
    /// Related feature: docs/features/003-consistent-value-formatting/specification.md
    /// </summary>
    [Fact]
    public void Render_UsesInlineDiff_WhenModelConfigIsInline()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/firewall-rule-changes.json"));
        var builder = new ReportModelBuilder(largeValueFormat: LargeValueFormat.InlineDiff);
        var model = builder.Build(plan);
        var change = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");
        var renderer = new MarkdownRenderer();

        // Act
        var markdown = renderer.RenderResourceChange(change, LargeValueFormat.InlineDiff)!;

        // Assert
        markdown.Should().Contain("background-color:")
            .And.NotContain("```diff");
    }
}
