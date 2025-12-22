using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderSummaryTests
{
    [Fact]
    public void Build_PopulatesSummaryAndReplacePaths()
    {
        // Arrange
        var json = File.ReadAllText("TestData/replace-paths-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.ReplacePaths.Should().NotBeNull();
        change.ReplacePaths!.Should().HaveCount(1);
        change.Summary.Should().Be("recreate `snet-db` (address_prefixes[0] changed: force replacement)");
    }
}
