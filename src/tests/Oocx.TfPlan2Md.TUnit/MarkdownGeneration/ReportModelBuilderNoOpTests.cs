using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderNoOpTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Build_NoOpPlan_CountsNoOpCorrectly()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-op-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert - no-op resources are counted in summary but not included in Changes
        // to avoid exceeding Scriban's iteration limit on large plans
        model.Summary.ToAdd.Count.Should().Be(0);
        model.Summary.ToChange.Count.Should().Be(0);
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.ToReplace.Count.Should().Be(0);
        model.Summary.NoOp.Count.Should().Be(1);
        model.Summary.Total.Should().Be(0);
        model.Changes.Should().BeEmpty(); // no-op resources are filtered out
    }

    [Test]
    public void Build_SummaryTotal_ExcludesNoOpActions()
    {
        // Arrange - one create and one no-op resource
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "type_a.create",
                    null,
                    "managed",
                    "type_a",
                    "create",
                    "provider",
                    new Change(["create"])),
                new(
                    "type_a.noop",
                    null,
                    "managed",
                    "type_a",
                    "noop",
                    "provider",
                    new Change(["no-op"]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(1);
        model.Summary.NoOp.Count.Should().Be(1);
        model.Summary.Total.Should().Be(1);
    }
}
