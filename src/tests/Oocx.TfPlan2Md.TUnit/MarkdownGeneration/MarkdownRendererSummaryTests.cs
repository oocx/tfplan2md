using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererSummaryTests
{
    private const string Nbsp = "\u00A0";

    [Test]
    public void Render_IncludesSummaryLineAboveDetails()
    {
        // Arrange
        var json = File.ReadAllText("TestData/replace-paths-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        // Act
        var markdown = renderer.Render(model);

        // Assert
        markdown.Should().Contain($"♻️{Nbsp}azurerm_subnet <b><code>db</code></b>");
    }

    [Test]
    public void Render_WithReportTitle_UsesCustomTitleInSummaryTemplate()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var model = new ReportModelBuilder(reportTitle: "Summary Title").Build(plan);
        var renderer = new MarkdownRenderer();

        // Act
        var markdown = renderer.Render(model, "summary");

        // Assert
        var firstNonEmptyLine = markdown.Split('\n').First(line => !string.IsNullOrWhiteSpace(line));
        firstNonEmptyLine.Should().Be("# Summary Title");
    }
}
