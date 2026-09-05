using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;
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

    private static ResourceChange MakeNoOpChange(string address)
    {
        return new ResourceChange(
            address,
            null,
            "managed",
            "example_resource",
            address.Split('.')[1],
            "registry.terraform.io/example/example",
            new Change(
                ["no-op"],
                JsonDocument.Parse("{\"name\":\"same\"}").RootElement,
                JsonDocument.Parse("{\"name\":\"same\"}").RootElement,
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
    public void Build_ResourceDrift_GroupsNormalizedAttributeTransition()
    {
        var plan = MakePlan(resourceDrift: new[] { MakeUpdateChange("example_resource.drifted") });

        var model = new ReportModelBuilder().Build(plan);

        model.Drift.Should().NotBeNull();
        model.Drift.Should().HaveCount(1);
        model.Drift[0].ResourceType.Should().Be("example_resource");
        model.Drift[0].AttributePath.Should().Be("name");
        model.Drift[0].Before.Should().Be("old");
        model.Drift[0].After.Should().Be("new");
        model.Drift[0].Addresses.Should().ContainSingle().Which.Should().Be("example_resource.drifted");
    }

    [Test]
    public void Build_ResourceDriftNoOp_IsFilteredOut()
    {
        var plan = MakePlan(resourceDrift: new[] { MakeNoOpChange("example_resource.drifted") });

        var model = new ReportModelBuilder().Build(plan);

        model.Drift.Should().BeEmpty();
    }

    [Test]
    public void Build_DriftModes_GroupAndSelectDisplayableAddressesBeforeGrouping()
    {
        var api = MakeUpdateChange("example_resource.api");
        var worker = MakeUpdateChange("example_resource.worker");
        var release = new ResourceChange(
            "example_resource.release", null, "managed", "example_resource", "release", "registry.terraform.io/example/example",
            new Change(["update"], JsonDocument.Parse("{\"name\":\"old-release\"}").RootElement, JsonDocument.Parse("{\"name\":\"new-release\"}").RootElement, null, null, null));
        var plan = new TerraformPlan("1.2", "1.14.0", [api], ResourceDrift: [worker, api, release]);

        var all = new ReportModelBuilder().Build(plan);
        var relevant = new ReportModelBuilder(new ReportModelBuilderOptions(DriftDisplayMode: DriftDisplayMode.Relevant)).Build(plan);
        var none = new ReportModelBuilder(new ReportModelBuilderOptions(DriftDisplayMode: DriftDisplayMode.None)).Build(plan);

        all.Drift.Should().HaveCount(2);
        all.Drift[0].Addresses.Should().BeEquivalentTo(["example_resource.api", "example_resource.worker"], options => options.WithStrictOrdering());
        relevant.Drift.Should().ContainSingle();
        relevant.Drift[0].Addresses.Should().ContainSingle().Which.Should().Be("example_resource.api");
        none.Drift.Should().BeEmpty();
    }

    [Test]
    public void Build_RelevantDrift_NoOpPlannedChangeDoesNotMakeDriftRelevant()
    {
        var changing = MakeUpdateChange("example_resource.changing");
        var noOp = MakeNoOpChange("example_resource.no_op");
        var plan = new TerraformPlan(
            "1.2",
            "1.14.0",
            [changing, noOp],
            ResourceDrift: [changing, MakeUpdateChange("example_resource.no_op")]);

        var model = new ReportModelBuilder(
            new ReportModelBuilderOptions(DriftDisplayMode: DriftDisplayMode.Relevant)).Build(plan);

        model.Drift.Should().ContainSingle();
        model.Drift[0].Addresses.Should().ContainSingle().Which.Should().Be("example_resource.changing");
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
