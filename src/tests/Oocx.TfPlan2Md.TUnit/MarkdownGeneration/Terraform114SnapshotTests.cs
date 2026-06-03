using System.IO;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot (golden file) tests for Terraform 1.14/1.15 plan-JSON extensions:
/// action invocations (H1), plan-context awareness (H2), and deprecation warnings (M2).
/// Each test corresponds to a hand-crafted fixture from the feature test plan.
/// Related feature: docs/features/122-terraform-1-15-support/test-plan.md.
/// </summary>
public class Terraform114SnapshotTests
{
    private readonly TerraformPlanParser _parser = new();

    // ──────────────────────────────────────────────────────────────────────────
    // H1 — Action invocations (F-01 .. F-13)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>AC-1: lifecycle action triggered before_create attached inline.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleBeforeCreate_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-before-create-plan.json", "actions-lifecycle-before-create.md");

    /// <summary>AC-1: lifecycle action triggered after_create.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleAfterCreate_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-after-create-plan.json", "actions-lifecycle-after-create.md");

    /// <summary>AC-1: lifecycle action triggered before_update.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleBeforeUpdate_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-before-update-plan.json", "actions-lifecycle-before-update.md");

    /// <summary>AC-1: lifecycle action triggered after_update.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleAfterUpdate_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-after-update-plan.json", "actions-lifecycle-after-update.md");

    /// <summary>AC-1: lifecycle action triggered before_destroy.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleBeforeDestroy_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-before-destroy-plan.json", "actions-lifecycle-before-destroy.md");

    /// <summary>AC-1: lifecycle action triggered after_destroy.</summary>
    [Test]
    public void Snapshot_ActionsLifecycleAfterDestroy_MatchesBaseline() =>
        AssertSnapshot("actions-lifecycle-after-destroy-plan.json", "actions-lifecycle-after-destroy.md");

    /// <summary>AC-1 fallback: invoke-mode action routes to Other Actions section.</summary>
    [Test]
    public void Snapshot_ActionsInvokeOnly_MatchesBaseline() =>
        AssertSnapshot("actions-invoke-only-plan.json", "actions-invoke-only.md");

    /// <summary>AC-2: deferred action rendered with hourglass prefix and callout.</summary>
    [Test]
    public void Snapshot_ActionsDeferred_MatchesBaseline() =>
        AssertSnapshot("actions-deferred-plan.json", "actions-deferred.md");

    /// <summary>AC-1: multiple actions on one resource rendered in order.</summary>
    [Test]
    public void Snapshot_ActionsMultipleOnOneResource_MatchesBaseline() =>
        AssertSnapshot("actions-multiple-on-one-resource-plan.json", "actions-multiple-on-one-resource.md");

    /// <summary>AC-4: sensitive config_values are redacted via SensitivityHelper.</summary>
    [Test]
    public void Snapshot_ActionsSensitiveConfig_MatchesBaseline() =>
        AssertSnapshot("actions-sensitive-config-plan.json", "actions-sensitive-config.md");

    /// <summary>AC-1 fallback: lifecycle orphan routes to Other Actions section.</summary>
    [Test]
    public void Snapshot_ActionsOrphanLifecycle_MatchesBaseline() =>
        AssertSnapshot("actions-orphan-lifecycle-plan.json", "actions-orphan-lifecycle.md");

    /// <summary>FR-H1.8: action with diagnostics payload surfaces the diagnostic block.</summary>
    [Test]
    public void Snapshot_ActionsWithDiagnostics_MatchesBaseline() =>
        AssertSnapshot("actions-with-diagnostics-plan.json", "actions-with-diagnostics.md");

    /// <summary>AC-2: mixed immediate + deferred actions on same resource render together.</summary>
    [Test]
    public void Snapshot_ActionsMixedDeferredAndImmediate_MatchesBaseline() =>
        AssertSnapshot("actions-mixed-deferred-and-immediate-plan.json", "actions-mixed-deferred-and-immediate.md");

    // ──────────────────────────────────────────────────────────────────────────
    // H2 — Plan-context awareness (F-20 .. F-28)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>AC-5: single drift entry emits the Drift Detected H2 section.</summary>
    [Test]
    public void Snapshot_DriftSingleEntry_MatchesBaseline() =>
        AssertSnapshot("drift-single-entry-plan.json", "drift-single-entry.md");

    /// <summary>AC-5: multiple drift entries render correctly.</summary>
    [Test]
    public void Snapshot_DriftMultipleEntries_MatchesBaseline() =>
        AssertSnapshot("drift-multiple-entries-plan.json", "drift-multiple-entries.md");

    /// <summary>AC-5 negative: no resource_drift means no Drift Detected section.</summary>
    [Test]
    public void Snapshot_DriftEmptyBaseline_MatchesBaseline() =>
        AssertSnapshot("drift-empty-baseline-plan.json", "drift-empty-baseline.md");

    /// <summary>AC-5 negative: drift entries that are effectively no-op are filtered out.</summary>
    [Test]
    public void Snapshot_DriftNoOpEntries_AreHidden() =>
        AssertSnapshot("drift-no-op-entries-plan.json", "drift-no-op-entries.md");

    /// <summary>AC-6: errored plan emits the error status banner.</summary>
    [Test]
    public void Snapshot_StatusErrored_MatchesBaseline() =>
        AssertSnapshot("status-errored-plan.json", "status-errored.md");

    /// <summary>AC-6: non-applyable no-change plan does not emit misleading warning banner.</summary>
    [Test]
    public void Snapshot_StatusNotApplyable_MatchesBaseline() =>
        AssertSnapshot("status-not-applyable-plan.json", "status-not-applyable.md");

    /// <summary>AC-6: actionable non-applyable plan still emits the warning banner.</summary>
    [Test]
    public void Snapshot_StatusNotApplyableActionable_MatchesBaseline() =>
        AssertSnapshot("status-not-applyable-actionable-plan.json", "status-not-applyable-actionable.md");

    /// <summary>AC-6: incomplete plan emits the incomplete banner.</summary>
    [Test]
    public void Snapshot_StatusIncomplete_MatchesBaseline() =>
        AssertSnapshot("status-incomplete-plan.json", "status-incomplete.md");

    /// <summary>AC-6 negative: all-true status emits no misleading banner.</summary>
    [Test]
    public void Snapshot_StatusAllTrueBaseline_MatchesBaseline() =>
        AssertSnapshot("status-all-true-baseline-plan.json", "status-all-true-baseline.md");

    /// <summary>AC-7: relevant_attributes present surfaces the Relevant Attributes H2 section.</summary>
    [Test]
    public void Snapshot_RelevantAttributesPresent_MatchesBaseline() =>
        AssertSnapshot("relevant-attributes-present-plan.json", "relevant-attributes-present.md");

    /// <summary>AC-7 negative: absent relevant_attributes omits the section.</summary>
    [Test]
    public void Snapshot_RelevantAttributesAbsent_MatchesBaseline() =>
        AssertSnapshot("relevant-attributes-absent-plan.json", "relevant-attributes-absent.md");

    // ──────────────────────────────────────────────────────────────────────────
    // M2 — Deprecation warnings (F-40 .. F-44)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>AC-8: referenced deprecated variable emits exactly one warning.</summary>
    [Test]
    public void Snapshot_DeprecationVariableReferenced_MatchesBaseline() =>
        AssertSnapshot("deprecation-variable-referenced-plan.json", "deprecation-variable-referenced.md");

    /// <summary>AC-8 negative: unreferenced deprecated variable emits zero warnings.</summary>
    [Test]
    public void Snapshot_DeprecationVariableUnreferenced_EmitsZeroWarnings() =>
        AssertSnapshot("deprecation-variable-unreferenced-plan.json", "deprecation-variable-unreferenced.md");

    /// <summary>AC-8: deprecated output emits exactly one warning.</summary>
    [Test]
    public void Snapshot_DeprecationOutput_MatchesBaseline() =>
        AssertSnapshot("deprecation-output-plan.json", "deprecation-output.md");

    /// <summary>FR-M2.3: deprecated output with explicit type is parsed and surfaced.</summary>
    [Test]
    public void Snapshot_DeprecationOutputWithExplicitType_MatchesBaseline() =>
        AssertSnapshot("deprecation-output-with-explicit-type-plan.json", "deprecation-output-with-explicit-type.md");

    /// <summary>AC-8: multiple deprecated variables and one output all emit warnings.</summary>
    [Test]
    public void Snapshot_DeprecationMultiple_MatchesBaseline() =>
        AssertSnapshot("deprecation-multiple-plan.json", "deprecation-multiple.md");

    // ──────────────────────────────────────────────────────────────────────────
    // Backwards compatibility (F-60)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>AC-9: Terraform 1.13 baseline plan renders identically (except H3 rename).</summary>
    [Test]
    public void Snapshot_Tf113Baseline_MatchesBaseline()
    {
        var json = File.ReadAllText(Path.Combine("TestData", "tf-1-13-baseline-plan.json"));
        var plan = _parser.Parse(json);
        var providerRegistry = new ProviderRegistry();
        var model = new ReportModelBuilder(
            services: new ReportModelBuilderServices(
                MetadataProvider: TestMetadataProvider.Instance,
                ProviderRegistry: providerRegistry))
            .Build(plan);
        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        var markdown = renderer.Render(model);

        SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace(markdown, "tf-1-13-baseline.md");
        SnapshotTestAssertions.AssertMatchesSnapshot("tf-1-13-baseline.md", markdown);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shared helper
    // ──────────────────────────────────────────────────────────────────────────

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
