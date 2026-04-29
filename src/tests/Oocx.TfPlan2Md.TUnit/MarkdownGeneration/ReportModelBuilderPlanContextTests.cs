using System.Collections.Generic;
using TUnit.Assertions.Extensions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.TUnit.MarkdownGeneration;

/// <summary>
/// Tests for Terraform 1.14+ plan-context model building (plan status, drift, relevant attributes).
/// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
/// </summary>
public class ReportModelBuilderPlanContextTests
{
    [Test]
    public void Build_ResourceDrift_PopulatesDriftModel()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift:
            [
                new ResourceChange(
                    Address: "example_resource.drifted",
                    ModuleAddress: null,
                    Mode: "managed",
                    Type: "example_resource",
                    Name: "drifted",
                    Index: null,
                    ProviderName: "registry.terraform.io/example/example",
                    Change: new Change(
                        Actions: ["update"],
                        Before: new { id = "abc", value = "old" },
                        After: new { id = "abc", value = "new" },
                        BeforeSensitive: null,
                        AfterSensitive: null,
                        AfterUnknown: null,
                        ReplacePaths: null,
                        Importing: null),
                    PreviousAddress: null,
                    ActionReason: null)
            ])
        {
            Configuration = null,
            OutputChanges = null
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.Drift.Should().NotBeNull();
        report.Drift.Count.Should().Be(1);
        report.Drift[0].Address.Should().Be("example_resource.drifted");
        report.Drift[0].Action.Should().Be("update");
    }

    [Test]
    public void Build_NoResourceDrift_OmitsDriftModel()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift: null)
        {
            Configuration = null,
            OutputChanges = null
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.Drift.Should().NotBeNull();
        report.Drift.Count.Should().Be(0);
    }

    [Test]
    public void Build_RelevantAttributes_PopulatesModel()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift: null)
        {
            Configuration = null,
            OutputChanges = null,
            RelevantAttributes =
            [
                new RelevantAttribute(
                    Resource: "example_resource.upstream",
                    Attribute: ["tags", "Name"]),
                new RelevantAttribute(
                    Resource: "example_resource.other",
                    Attribute: ["network_interface", 0, "id"])
            ]
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.RelevantAttributes.Should().NotBeNull();
        report.RelevantAttributes.Count.Should().Be(2);
        report.RelevantAttributes[0].Resource.Should().Be("example_resource.upstream");
        report.RelevantAttributes[0].AttributePath.Should().Be("tags.Name");
        report.RelevantAttributes[1].Resource.Should().Be("example_resource.other");
        report.RelevantAttributes[1].AttributePath.Should().Be("network_interface[0].id");
    }

    [Test]
    public void Build_NoRelevantAttributes_OmitsSection()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift: null)
        {
            Configuration = null,
            OutputChanges = null,
            RelevantAttributes = null
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.RelevantAttributes.Should().NotBeNull();
        report.RelevantAttributes.Count.Should().Be(0);
    }

    [Test]
    public void Build_PlanStatusBooleans_PopulatePlanStatusModel()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift: null)
        {
            Configuration = null,
            OutputChanges = null,
            Applyable = false,
            Complete = false,
            Errored = true
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.PlanStatus.Should().NotBeNull();
        report.PlanStatus!.Applyable.Should().Be(false);
        report.PlanStatus!.Complete.Should().Be(false);
        report.PlanStatus!.Errored.Should().Be(true);
    }

    [Test]
    public void Build_NoStatusBooleans_LeavesPlanStatusNull()
    {
        // Arrange
        var plan = new TerraformPlan(
            TerraformVersion: "1.14.0",
            FormatVersion: "1.2",
            ResourceChanges: [],
            ResourceDrift: null)
        {
            Configuration = null,
            OutputChanges = null,
            Applyable = null,
            Complete = null,
            Errored = null
        };
        var builder = new ReportModelBuilder();

        // Act
        var report = builder.Build(plan);

        // Assert
        report.PlanStatus.Should().BeNull();
    }
}
