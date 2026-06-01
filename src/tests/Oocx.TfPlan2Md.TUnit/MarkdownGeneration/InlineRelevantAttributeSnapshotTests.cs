using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class InlineRelevantAttributeSnapshotTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Snapshot_ForcedReplacementUpstreamChanging_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-forced-replacement-upstream-changing-plan.json", "relevant-attrs-forced-replacement-upstream-changing.md");

    [Test]
    public void Snapshot_ForcedReplacementUpstreamStatic_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-forced-replacement-upstream-static-plan.json", "relevant-attrs-forced-replacement-upstream-static.md");

    [Test]
    public void Snapshot_DependsOnOnly_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-depends-on-only-plan.json", "relevant-attrs-depends-on-only.md");

    [Test]
    public void Snapshot_CombinedCard_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-combined-card-plan.json", "relevant-attrs-combined-card.md");

    [Test]
    public void Snapshot_FallbackOnly_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-fallback-only-plan.json", "relevant-attrs-fallback-only.md");

    [Test]
    public void Snapshot_AllCorrelated_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-all-correlated-plan.json", "relevant-attrs-all-correlated.md");

    [Test]
    public void Snapshot_DriftWithRelevantAttributes_MatchesBaseline() =>
        AssertSnapshot("relevant-attrs-drift-with-relevant-attrs-plan.json", "relevant-attrs-drift-with-relevant-attrs.md");

    private void AssertSnapshot(string testDataFile, string snapshotName)
    {
        var json = File.ReadAllText(Path.Combine("TestData", "tf114", testDataFile));
        var plan = _parser.Parse(json);
        var providerRegistry = new ProviderRegistry();
        var model = new ReportModelBuilder(
            services: new ReportModelBuilderServices(
                MetadataProvider: TestMetadataProvider.Instance,
                ProviderRegistry: providerRegistry))
            .Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, snapshotName);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }
}
