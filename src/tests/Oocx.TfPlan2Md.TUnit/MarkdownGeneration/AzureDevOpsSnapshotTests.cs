using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests covering Azure DevOps rendering scenarios.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
public class AzureDevOpsSnapshotTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for Azure DevOps snapshots.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies the Azure DevOps snapshot output matches the approved baseline.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzureDevOps_Comprehensive_MatchesBaseline()
    {
        AssertAzureDevOpsSnapshot("azuredevops-snapshot-plan.json", "azuredevops-snapshot.md");
    }

    /// <summary>
    /// Verifies the Azure DevOps group member snapshot output matches the approved baseline.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzureDevOps_GroupMembers_MatchesBaseline()
    {
        AssertAzureDevOpsSnapshot("azuredevops-group-members-plan.json", "azuredevops-group-members.md");
    }

    /// <summary>
    /// Verifies the Azure DevOps team member snapshot output matches the approved baseline.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzureDevOps_TeamMembers_MatchesBaseline()
    {
        AssertAzureDevOpsSnapshot("azuredevops-team-members-plan.json", "azuredevops-team-members.md");
    }

    /// <summary>
    /// Renders a markdown report from an Azure DevOps plan test data file.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private string RenderAzureDevOpsPlan(string testDataFile)
    {
        var json = File.ReadAllText(Path.Combine("TestData", testDataFile));
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var valueFormatterRegistry = CreateValueFormatterRegistry(providerRegistry);
        var iconProviderRegistry = CreateIconProviderRegistry(providerRegistry);
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry,
            iconProviderRegistry: iconProviderRegistry).Build(plan);
        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry,
            valueFormatterRegistry: valueFormatterRegistry,
            iconProviderRegistry: iconProviderRegistry);

        return renderer.Render(model);
    }

    /// <summary>
    /// Asserts the rendered output matches the stored snapshot.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <param name="snapshotName">The snapshot file name under TestData/Snapshots.</param>
    private void AssertAzureDevOpsSnapshot(string testDataFile, string snapshotName)
    {
        var markdown = RenderAzureDevOpsPlan(testDataFile);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, snapshotName);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }

    /// <summary>
    /// Creates a provider registry that includes Azure DevOps support.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzureDevOpsModule(LargeValueFormat.SimpleDiff));
        return registry;
    }

    /// <summary>
    /// Registers Azure DevOps value formatters for snapshot rendering.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerRegistry">The registry containing provider modules.</param>
    /// <returns>The configured value formatter registry.</returns>
    private static ValueFormatterRegistry CreateValueFormatterRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(registry);
        return registry;
    }

    /// <summary>
    /// Registers Azure DevOps icon providers for snapshot rendering.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerRegistry">The registry containing provider modules.</param>
    /// <returns>The configured icon provider registry.</returns>
    private static IconProviderRegistry CreateIconProviderRegistry(ProviderRegistry providerRegistry)
    {
        var registry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(registry);
        return registry;
    }
}
