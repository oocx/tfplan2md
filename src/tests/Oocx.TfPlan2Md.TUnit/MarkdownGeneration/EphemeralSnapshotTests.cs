using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests covering OpenTofu/Terraform ephemeral resource action rendering.
/// Verifies that ephemeral resources with 'open' action and replace variants
/// render correctly without warnings or errors.
/// Related issue: docs/issues/573-open-action-support/analysis.md.
/// </summary>
public class EphemeralSnapshotTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for ephemeral resource snapshot tests.
    /// Related issue: docs/issues/573-open-action-support/analysis.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies ephemeral resource 'open' action renders correctly without warnings.
    /// Tests three scenarios:
    /// - ["open"] action → "open" with ➕ icon
    /// - ["create", "forget"] → "replace" with ♻️ icon
    /// - ["forget", "create"] → "replace" with ♻️ icon
    /// </summary>
    [Test]
    public void Snapshot_EphemeralOpen_MatchesBaseline()
    {
        AssertEphemeralSnapshot("ephemeral-open-plan.json", "ephemeral-open.md");
    }

    /// <summary>
    /// Renders a markdown report from an ephemeral resource plan test data file
    /// and asserts it matches the stored snapshot.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <param name="snapshotName">The snapshot file name under TestData/Snapshots.</param>
    private void AssertEphemeralSnapshot(string testDataFile, string snapshotName)
    {
        var json = File.ReadAllText(Path.Combine("TestData", testDataFile));
        var plan = _parser.Parse(json);
        var providerRegistry = new ProviderRegistry();
        var model = new ReportModelBuilder(
            services: new ReportModelBuilderServices(MetadataProvider: TestMetadataProvider.Instance, ProviderRegistry: providerRegistry)).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, snapshotName);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }
}
