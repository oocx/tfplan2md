using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Builder tests for Terraform 1.14+ plan-context: plan status booleans, drift, relevant attributes.
/// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md (Task 4).
/// </summary>
public class ReportModelBuilderPlanContextTests
{
    private static ResourceChange MakeUpdateChange(string address)
    {
        return new ResourceChange(
            address,
            null,
            "managed",
            "example_resource",
            address.Split('.')[1],
            "registry.terraform.io/example/example",
            new Change(
                ["update"],
                JsonDocument.Parse("{\"name\":\"old\"}").RootElement,
                JsonDocument.Parse("{\"name\":\"new\"}").RootElement,
                null,
                null,
                null));
    }

    private static TerraformPlan MakePlan(
        IReadOnlyList<ResourceChange>? resourceDrift = null,
        IReadOnlyList<RelevantAttribute>? relevantAttributes = null,
        bool? applyable = null,
        bool? complete = null,
        bool? errored = null)
    {
        return new TerraformPlan(
            "1.2",
            "1.14.0",
            new[] { MakeUpdateChange("example_resource.kept") },
            ResourceDrift: resourceDrift,
            RelevantAttributes: relevantAttributes,
            Applyable: applyable,
            Complete: complete,
            Errored: errored);
    }

    [Test]
    public void Build_PlanStatusBooleans_PopulatePlanStatusModel()
    {
        var plan = MakePlan(applyable: false, complete: false, errored: true);

        var model = new ReportModelBuilder().Build(plan);

        model.PlanStatus.Should().NotBeNull();
        model.PlanStatus!.Applyable.Should().BeFalse();
        model.PlanStatus.Complete.Should().BeFalse();
        model.PlanStatus.Errored.Should().BeTrue();
    }

    [Test]
    public void Build_NoStatusBooleans_LeavesPlanStatusNull()
    {
        var plan = MakePlan();

        var model = new ReportModelBuilder().Build(plan);

        model.PlanStatus.Should().BeNull();
    }

    [Test]
    public void Build_ResourceDrift_PopulatesDriftModel()
    {
        var plan = MakePlan(resourceDrift: new[] { MakeUpdateChange("example_resource.drifted") });

        var model = new ReportModelBuilder().Build(plan);

        model.Drift.Should().NotBeNull();
        model.Drift.Should().HaveCount(1);
        model.Drift[0].Address.Should().Be("example_resource.drifted");
        model.Drift[0].Action.Should().Be("update");
    }

    [Test]
    public void Build_NoResourceDrift_OmitsDriftModel()
    {
        var plan = MakePlan();

        var model = new ReportModelBuilder().Build(plan);

        model.Drift.Should().NotBeNull();
        model.Drift.Should().BeEmpty();
    }

    [Test]
    public void Build_RelevantAttributes_PopulatesModel()
    {
        var attribute = new RelevantAttribute(
            "example_resource.upstream",
            new object[] { "network_interface", 0, "id" });
        var plan = MakePlan(relevantAttributes: new[] { attribute });

        var model = new ReportModelBuilder().Build(plan);

        model.RelevantAttributes.Should().HaveCount(1);
        model.RelevantAttributes[0].Resource.Should().Be("example_resource.upstream");
        model.RelevantAttributes[0].AttributePath.Should().Be("network_interface[0].id");
    }

    [Test]
    public void Build_NoRelevantAttributes_OmitsSection()
    {
        var plan = MakePlan();

        var model = new ReportModelBuilder().Build(plan);

        model.RelevantAttributes.Should().NotBeNull();
        model.RelevantAttributes.Should().BeEmpty();
    }
}
