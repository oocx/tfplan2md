using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot (golden file) tests that detect unexpected changes in markdown output.
/// These tests compare the current output against a previously approved baseline,
/// catching subtle regressions that other tests might miss.
/// </summary>
/// <remarks>
/// Snapshot testing works by:
/// 1. Generating markdown output from a fixed input
/// 2. Comparing against a stored "golden" file
/// 3. Failing if there are any differences
/// 
/// When intentional changes are made, developers update the snapshot files.
/// This provides a safety net against accidental output changes.
/// 
/// The snapshots directory stores the expected outputs, organized by test name.
/// </remarks>
public class MarkdownSnapshotTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies the comprehensive demo output matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_ComprehensiveDemo_MatchesBaseline()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var principalMapper = new PrincipalMapper(DemoPaths.DemoPrincipalsPath);
        var providerRegistry = CreateProviderRegistry(principalMapper);
        var model = new ReportModelBuilder(
            principalMapper: principalMapper,
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "comprehensive-demo.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("comprehensive-demo.md", markdown);
    }

    /// <summary>
    /// Verifies the summary template output matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_SummaryTemplate_MatchesBaseline()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model, "summary");

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "summary-template.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("summary-template.md", markdown);
    }

    /// <summary>
    /// Verifies the breaking plan output matches the approved snapshot.
    /// This ensures escaping behavior is consistent.
    /// </summary>
    [Test]
    public void Snapshot_BreakingPlan_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "breaking-plan.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("breaking-plan.md", markdown);
    }

    /// <summary>
    /// Verifies role assignment rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_RoleAssignments_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/role-assignments.json");
        var plan = _parser.Parse(json);
        var principalMapper = new PrincipalMapper(DemoPaths.DemoPrincipalsPath);
        var providerRegistry = CreateProviderRegistry(principalMapper);
        var model = new ReportModelBuilder(
            principalMapper: principalMapper,
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "role-assignments.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("role-assignments.md", markdown);
    }

    /// <summary>
    /// Verifies firewall rule rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_FirewallRules_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "firewall-rules.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("firewall-rules.md", markdown);
    }

    /// <summary>
    /// Verifies multi-module plan rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_MultiModule_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/multi-module-plan.json");
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "multi-module.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("multi-module.md", markdown);
    }

    /// <summary>
    /// Creates a ProviderRegistry with AzureRM module for testing.
    /// </summary>
    private static ProviderRegistry CreateProviderRegistry(IPrincipalMapper? principalMapper = null)
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: principalMapper ?? new NullPrincipalMapper()));
        return registry;
    }
}
