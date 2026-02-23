using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests covering Terraform outputs rendering scenarios.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
public class OutputsSnapshotTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for outputs snapshot tests.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies basic global outputs with create, update, delete, no-op actions.
    /// </summary>
    [Test]
    public void Snapshot_BasicOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-basic-plan.json", "outputs-basic.md");
    }

    /// <summary>
    /// Verifies module outputs rendering within module sections.
    /// </summary>
    [Test]
    public void Snapshot_ModuleOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-module-plan.json", "outputs-module.md");
    }

    /// <summary>
    /// Verifies plan with both module and global outputs.
    /// </summary>
    [Test]
    public void Snapshot_MixedOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-mixed-plan.json", "outputs-mixed.md");
    }

    /// <summary>
    /// Verifies sensitive outputs are masked correctly.
    /// </summary>
    [Test]
    public void Snapshot_SensitiveOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-sensitive-plan.json", "outputs-sensitive.md");
    }

    /// <summary>
    /// Verifies computed outputs show "known after apply".
    /// </summary>
    [Test]
    public void Snapshot_ComputedOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-computed-plan.json", "outputs-computed.md");
    }

    /// <summary>
    /// Verifies outputs without description field render correctly.
    /// </summary>
    [Test]
    public void Snapshot_NoDescriptionOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-no-description-plan.json", "outputs-no-description.md");
    }

    /// <summary>
    /// Verifies sensitivity markers from different sources (after_sensitive, before_sensitive, configuration.sensitive).
    /// </summary>
    [Test]
    public void Snapshot_SensitivitySourcesOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-sensitivity-sources-plan.json", "outputs-sensitivity-sources.md");
    }

    /// <summary>
    /// Verifies Azure resource IDs in outputs use display name mapping.
    /// </summary>
    [Test]
    public void Snapshot_AzureIdsOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-with-azure-ids-plan.json", "outputs-with-azure-ids.md");
    }

    /// <summary>
    /// Verifies various output actions and edge cases.
    /// </summary>
    [Test]
    public void Snapshot_DiverseActionsOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-diverse-actions-plan.json", "outputs-diverse-actions.md");
    }

    /// <summary>
    /// Verifies plan with no outputs renders without outputs section.
    /// </summary>
    [Test]
    public void Snapshot_NoOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-no-outputs-plan.json", "outputs-no-outputs.md");
    }

    /// <summary>
    /// Verifies complex output values (arrays, objects, nested structures).
    /// </summary>
    [Test]
    public void Snapshot_ComplexValuesOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-complex-values-plan.json", "outputs-complex-values.md");
    }

    /// <summary>
    /// Verifies nested sensitivity objects are handled correctly.
    /// </summary>
    [Test]
    public void Snapshot_NestedSensitivityOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-nested-sensitivity-plan.json", "outputs-nested-sensitivity.md");
    }

    /// <summary>
    /// Verifies module with outputs but no resource changes.
    /// </summary>
    [Test]
    public void Snapshot_ModuleOnlyOutputs_MatchesBaseline()
    {
        AssertOutputsSnapshot("outputs-module-only-plan.json", "outputs-module-only.md");
    }

    /// <summary>
    /// Renders a markdown report from an outputs plan test data file.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private string RenderOutputsPlan(string testDataFile)
    {
        var json = File.ReadAllText(Path.Combine("TestData", testDataFile));
        var plan = _parser.Parse(json);
        var providerRegistry = CreateProviderRegistry();
        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry).Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        return renderer.Render(model);
    }

    /// <summary>
    /// Asserts the rendered output matches the stored snapshot.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <param name="snapshotName">The snapshot file name under TestData/Snapshots.</param>
    private void AssertOutputsSnapshot(string testDataFile, string snapshotName)
    {
        var markdown = RenderOutputsPlan(testDataFile);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }

    /// <summary>
    /// Creates a provider registry for rendering tests.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        return registry;
    }
}
