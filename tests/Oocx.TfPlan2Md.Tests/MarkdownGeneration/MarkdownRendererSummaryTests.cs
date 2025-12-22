using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererSummaryTests
{
    [Fact]
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
        markdown.Should().Contain("**Summary:**");
        markdown.Should().Contain("recreate `snet-db` (address_prefixes[0] changed: force replacement)");
    }
}
