using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the no-op parent with child changes bug fix.
/// Related issue: docs/issues/088-no-op-parent-hides-child-changes/analysis.md.
/// </summary>
/// <remarks>
/// Reproduces and verifies the fix for the bug where:
/// - A parent resource has action "no-op" (no changes)
/// - Child resources have actual changes (update/create/delete)
/// - The child resources are correctly counted in Summary but disappear from Resource Changes section
/// 
/// Root cause: The no-op filter removes the parent even when it has children with changes.
/// Fix: Preserve no-op parents when they have ChildResourceGroups.Count > 0.
/// </remarks>
public class ReportModelBuilderNoOpParentWithChildrenTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies no-op parent with child changes appears in the report model.
    /// </summary>
    /// <remarks>
    /// This test ensures that when a parent resource has no changes (no-op action)
    /// but has child resources with changes, the parent is NOT filtered out
    /// from the displayChanges list. This allows the child changes to be visible
    /// in the Resource Changes section.
    /// </remarks>
    [Test]
    public void Build_NoOpParentWithChildChanges_AppearsInDisplayChanges()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/nsg-with-separate-rule-updates.json"));
        var model = BuildModel(plan);

        // Parent NSG should be in the changes list despite being no-op
        // because it has children with changes
        model.Changes.Should().ContainSingle(
            c => c.Type == "azurerm_network_security_group",
            "no-op parent with children should appear in displayChanges");

        var parent = model.Changes.Single(c => c.Type == "azurerm_network_security_group");

        // Verify the parent has child resource groups
        parent.ChildResourceGroups.Should().ContainSingle(
            "parent should have child NSG rules grouped");

        // Verify the child group has 2 rows (rule-a and rule-b)
        parent.ChildResourceGroups[0].Rows.Should().HaveCount(2,
            "both child NSG rules should be in the child table");
    }

    /// <summary>
    /// Verifies the summary correctly counts child changes when parent is no-op.
    /// </summary>
    [Test]
    public void Build_NoOpParentWithChildChanges_SummaryCountsChildren()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/nsg-with-separate-rule-updates.json"));
        var model = BuildModel(plan);

        // Summary should show 2 updates (the 2 NSG rules)
        model.Summary.ToChange.Count.Should().Be(2,
            "summary should count both child NSG rule updates");
        model.Summary.ToChange.Breakdown.Should().ContainSingle(
            s => s.Type == "azurerm_network_security_rule" && s.Count == 2,
            "summary should show 2 network_security_rule updates");

        // Parent NSG should appear in no-op count
        model.Summary.NoOp.Count.Should().Be(1,
            "no-op parent NSG should appear in no-op summary");
    }

    /// <summary>
    /// Snapshot test verifying the full markdown output for no-op parent with child changes.
    /// </summary>
    /// <remarks>
    /// This test verifies that:
    /// 1. The Resource Changes section is NOT omitted
    /// 2. The parent NSG appears with its child rules in a table
    /// 3. The child rules show their attribute changes (description, source_address_prefixes)
    /// 4. The summary correctly shows 2 changes
    /// </remarks>
    [Test]
    public void Snapshot_NoOpParentWithChildChanges_MatchesBaseline()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/nsg-with-separate-rule-updates.json"));
        var model = BuildModel(plan);
        var renderer = new MarkdownRenderer(providerRegistry: CreateProviderRegistry());

        var markdown = renderer.Render(model);

        // Verify Resource Changes section exists
        markdown.Should().Contain("## Resource Changes",
            "Resource Changes section should be present when parent has children with changes");

        // Verify parent NSG appears in output
        markdown.Should().Contain("azurerm_network_security_group",
            "parent NSG should appear in output");

        // Verify child rules appear
        markdown.Should().Contain("rule-a",
            "child rule-a should appear in output");
        markdown.Should().Contain("rule-b",
            "child rule-b should appear in output");

        // Verify snapshot matches
        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "nsg-with-separate-rule-updates.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("nsg-with-separate-rule-updates.md", markdown);
    }

    /// <summary>
    /// Verifies that no-op parent with only no-op children is filtered from display changes.
    /// </summary>
    /// <remarks>
    /// This is the opposite edge case: when both parent and all children are no-op,
    /// the parent should still be filtered out to avoid showing resources with no changes.
    /// Related issue: docs/issues/088-no-op-parent-hides-child-changes/analysis.md.
    /// Addresses maintainer concern about the fix not causing the opposite error.
    /// </remarks>
    [Test]
    public void Build_NoOpParentWithNoOpChildren_FilteredFromDisplayChanges()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/nsg-with-no-op-rules.json"));
        var model = BuildModel(plan);

        // Parent NSG should NOT be in the changes list because both parent and children are no-op
        model.Changes.Should().BeEmpty(
            "no-op parent with only no-op children should be filtered from displayChanges");

        // Summary should show all 3 resources (parent + 2 children) in no-op count
        model.Summary.NoOp.Count.Should().Be(3,
            "summary should count parent NSG and both no-op child rules");
    }

    /// <summary>
    /// Builds a report model from a Terraform plan.
    /// </summary>
    /// <param name="plan">The Terraform plan to build from.</param>
    /// <returns>The constructed report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan)
    {
        var providerRegistry = CreateProviderRegistry();
        var builder = new ReportModelBuilder(
            services: new ReportModelBuilderServices(ProviderRegistry: providerRegistry, MetadataProvider: TestMetadataProvider.Instance));
        return builder.Build(plan);
    }

    /// <summary>
    /// Creates a provider registry with the AzureRM provider registered.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        return providerRegistry;
    }
}
