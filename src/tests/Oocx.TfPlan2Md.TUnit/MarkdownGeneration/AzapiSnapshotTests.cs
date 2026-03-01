using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzApi;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot tests covering AzAPI rendering scenarios.
/// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
/// </summary>
public class AzapiSnapshotTests
{
    /// <summary>
    /// Parses Terraform plan JSON files for AzAPI snapshot tests.
    /// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
    /// </summary>
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies the create plan snapshot with the complete body.
    /// </summary>
    [Test]
    public void Snapshot_AzapiCreateComplete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-create-complete-plan.json", "azapi-create-complete.md");
    }

    /// <summary>
    /// Verifies the minimal create plan snapshot.
    /// </summary>
    [Test]
    public void Snapshot_AzapiCreateMinimal_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-create-plan.json", "azapi-create.md");
    }

    /// <summary>
    /// Verifies the update plan snapshot with full body changes.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateComplete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-complete-plan.json", "azapi-update-complete.md");
    }

    /// <summary>
    /// Verifies the minimal update plan snapshot.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateMinimal_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-plan.json", "azapi-update.md");
    }

    /// <summary>
    /// Verifies the delete plan snapshot with full body output.
    /// </summary>
    [Test]
    public void Snapshot_AzapiDeleteComplete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-delete-complete-plan.json", "azapi-delete-complete.md");
    }

    /// <summary>
    /// Verifies the minimal delete plan snapshot.
    /// </summary>
    [Test]
    public void Snapshot_AzapiDeleteMinimal_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-delete-plan.json", "azapi-delete.md");
    }

    /// <summary>
    /// Verifies the replace plan snapshot with full body output.
    /// </summary>
    [Test]
    public void Snapshot_AzapiReplaceComplete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-replace-complete-plan.json", "azapi-replace-complete.md");
    }

    /// <summary>
    /// Verifies the minimal replace plan snapshot.
    /// </summary>
    [Test]
    public void Snapshot_AzapiReplaceMinimal_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-replace-plan.json", "azapi-replace.md");
    }

    /// <summary>
    /// Verifies array grouping and nested object rendering.
    /// </summary>
    [Test]
    public void Snapshot_AzapiComplexNested_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-complex-nested-plan.json", "azapi-complex-nested.md");
    }

    /// <summary>
    /// Verifies deeply nested arrays with large values.
    /// </summary>
    [Test]
    public void Snapshot_AzapiDeepNestedLarge_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-deep-nested-large-plan.json", "azapi-deep-nested-large.md");
    }

    /// <summary>
    /// Verifies large value handling for a single large body property.
    /// </summary>
    [Test]
    public void Snapshot_AzapiLargeValue_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-large-value-plan.json", "azapi-large-value.md");
    }

    /// <summary>
    /// Verifies handling of multiple large body values.
    /// </summary>
    [Test]
    public void Snapshot_AzapiMultipleLargeValues_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-multiple-large-values-plan.json", "azapi-multiple-large-values.md");
    }

    /// <summary>
    /// Verifies rendering when all body values are large.
    /// </summary>
    [Test]
    public void Snapshot_AzapiAllLargeBody_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-all-large-body-plan.json", "azapi-all-large-body.md");
    }

    /// <summary>
    /// Verifies update rendering when large body values change.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateLargeValues_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-large-values-plan.json", "azapi-update-large-values.md");
    }

    /// <summary>
    /// Verifies sensitive values are masked within body properties.
    /// </summary>
    [Test]
    public void Snapshot_AzapiSensitive_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-sensitive-plan.json", "azapi-sensitive.md");
    }

    /// <summary>
    /// Verifies sensitive body payloads remain masked as a whole.
    /// </summary>
    [Test]
    public void Snapshot_AzapiBodySensitive_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-body-sensitive-plan.json", "azapi-body-sensitive.md");
    }

    /// <summary>
    /// Verifies empty body handling is stable.
    /// </summary>
    [Test]
    public void Snapshot_AzapiEmptyBody_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-empty-body-plan.json", "azapi-empty-body.md");
    }

    /// <summary>
    /// Verifies special characters are escaped correctly.
    /// </summary>
    [Test]
    public void Snapshot_AzapiSpecialChars_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-special-chars-plan.json", "azapi-special-chars.md");
    }

    /// <summary>
    /// Verifies azapi_update_resource update action with grouping.
    /// Related feature: docs/features/095-azapi-update-resource-grouping/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateResourceUpdate_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-resource-update-plan.json", "azapi-update-resource-update.md");
    }

    /// <summary>
    /// Verifies azapi_update_resource delete action with grouping.
    /// Related feature: docs/features/095-azapi-update-resource-grouping/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateResourceDelete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-resource-delete-plan.json", "azapi-update-resource-delete.md");
    }

    // -----------------------------------------------------------------------
    // Feature 106: Separate table for azapi output values
    // Related feature: docs/features/106-azapi-output-values/specification.md
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that a create action with unknown output suppresses the Output Values section entirely
    /// (no heading or notice rendered, because there is no before or after output data available).
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputCreateUnknown_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-create-unknown-plan.json", "azapi-output-create-unknown.md");
    }

    /// <summary>
    /// Verifies that a create action with output present renders an Output Values table.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputCreatePresent_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-create-present-plan.json", "azapi-output-create-present.md");
    }

    /// <summary>
    /// Verifies that an update action with both before and after output renders a Before/After Output Values table.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputUpdate_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-update-plan.json", "azapi-output-update.md");
    }

    /// <summary>
    /// Verifies that sensitive output fields are masked with (sensitive) in the Output Values table.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputSensitive_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-sensitive-plan.json", "azapi-output-sensitive.md");
    }

    /// <summary>
    /// Verifies that nested output objects trigger Feature 034 attribute grouping in the Output Values section.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputGrouped_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-grouped-plan.json", "azapi-output-grouped.md");
    }

    /// <summary>
    /// Verifies that azapi_update_resource renders output values in a dedicated Output Values section.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiUpdateResourceOutput_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-update-resource-output-plan.json", "azapi-update-resource-output.md");
    }

    /// <summary>
    /// TC-04: Verifies that an update action with identical before/after output renders the Output Values
    /// section with a "no changes detected" notice rather than a diff table.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputUpdateUnchanged_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-update-unchanged-plan.json", "azapi-output-update-unchanged.md");
    }

    /// <summary>
    /// TC-05: Verifies that a delete action with before output renders the Output Values section
    /// in delete mode showing the values that will be removed.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputDelete_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-delete-plan.json", "azapi-output-delete.md");
    }

    /// <summary>
    /// TC-06: Verifies that a replace action with before output present and after output unknown
    /// renders the before output in delete mode followed by the "known after apply" notice.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputReplaceUnknown_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-replace-unknown-plan.json", "azapi-output-replace-unknown.md");
    }

    /// <summary>
    /// TC-10: Verifies that output values exceeding the large value threshold are rendered
    /// outside the table as large value blocks rather than inline table cells.
    /// Related feature: docs/features/106-azapi-output-values/specification.md.
    /// </summary>
    [Test]
    public void Snapshot_AzapiOutputLargeValue_MatchesBaseline()
    {
        AssertAzapiSnapshot("azapi-output-large-value-plan.json", "azapi-output-large-value.md");
    }

    /// <summary>
    /// Renders a markdown report from an AzAPI plan test data file.
    /// </summary>
    /// <param name="testDataFile">The test data file name under TestData.</param>
    /// <returns>The rendered markdown output.</returns>
    private string RenderAzapiPlan(string testDataFile)
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
    private void AssertAzapiSnapshot(string testDataFile, string snapshotName)
    {
        var markdown = RenderAzapiPlan(testDataFile);
        SnapshotTestAssertions.AssertMatchesSnapshot(snapshotName, markdown);
    }

    /// <summary>
    /// Creates a provider registry that includes AzAPI support.
    /// </summary>
    /// <returns>The configured provider registry.</returns>
    private static ProviderRegistry CreateProviderRegistry()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new AzApiModule());
        return registry;
    }
}
