# Test Plan: Terraform 1.14 / 1.15 Plan-JSON Support

**Work item:** `docs/features/122-terraform-1-15-support/`
**Spec:** [`specification.md`](./specification.md) (AC-1 … AC-13)
**ADRs:** [`adr-001`](./adr-001-plan-json-model-extensions.md) · [`adr-002`](./adr-002-h2-report-layout.md) · [`adr-003`](./adr-003-inline-action-rendering.md) · [`adr-004`](./adr-004-deprecation-warnings-via-existing-pipeline.md)

## Overview

This feature bundles three additive plan-JSON extensions (H1 action invocations, H2 plan-context awareness, M2 deprecations) behind one shipping unit. Verification therefore spans three concerns:

1. **Parsing correctness** — every new optional field on `TerraformPlan` deserialises into the model defined in ADR-001 (and is `null`/empty when absent, never throwing).
2. **Rendering correctness** — the report layout decisions in ADR-002, ADR-003, ADR-004 produce the expected markdown sections, headings, ordering, sensitivity redaction, and deferred/orphan callouts.
3. **Backwards compatibility** — Terraform 1.13-style plans that lack every new field render identically to today (NFR-1, AC-9), with the single intentional exception of the H3 rename `Code Analysis Warnings` → `Warnings` (ADR-004).

Tests are layered the same way the existing project tests are layered (see `docs/testing-strategy.md` and `src/tests/Oocx.TfPlan2Md.TUnit/`):

| Layer | Purpose | Where it lives | Pattern reused |
|-------|---------|----------------|----------------|
| **Parser unit tests** | Round-trip the new optional plan-JSON fields into the model. Exercise null / absent / present paths and graceful tolerance of unknown fields. | `src/tests/Oocx.TfPlan2Md.TUnit/Parsing/` | Existing `TerraformPlanParser*Tests` |
| **Builder unit tests** | Assert that `ReportModelBuilder` attaches actions to the right parent (lifecycle), routes orphans correctly (invoke / lifecycle-orphan), flips the deferred flag, propagates plan-status booleans into the banner model, populates the drift / relevant-attributes models, and emits one warning per *referenced* deprecated variable/output. | `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/` | `ReportModelBuilderParentChildTests`, `ReportModelBuilderCodeAnalysisTests`, `ReportModelBuilderRefactoringOperationTests` |
| **Renderer unit tests** | Assert the small, focused rendering primitives in isolation: the deferred `⏳` prefix, the sensitivity redaction wrapped around `JsonFlattener` for action `config_values`, the status-banner formatting for each boolean combination, the renamed `Warnings` H3. | `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/` | `MarkdownHelpers*Tests`, `DefaultResourceRendererScenarioTests` |
| **Snapshot tests** | End-to-end "plan JSON in → markdown out" coverage of every fixture, asserting the full report layout (headings, ordering, plan-context cluster, Other Actions section, warnings entries) against a checked-in `.md` baseline. | `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/Terraform114SnapshotTests.cs` (new), snapshots under `TestData/Snapshots/` | `EphemeralSnapshotTests`, `KnownAfterApplySnapshotTests`, `MarkdownSnapshotTests` |
| **Integration / regression** | Re-run the existing `MarkdownSnapshotTests` and `ComprehensiveDemoTests` suites unchanged. They are the AC-9 backwards-compatibility gate. | Existing tests | — |
| **Performance test** | Synthetic large-plan generator + wall-clock comparison to enforce O(N) scaling for action rendering (NFR-3 / AC-12). | `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ActionInvocationPerformanceTests.cs` (new) | `DiffComputationPerformanceTests` |

All tests must remain fully automated and runnable via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`.

## Hand-Crafted JSON Fixture Inventory

All fixtures live under `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` and are minimal hand-crafted plan JSON files (per NFR-6 / AC-11). Each fixture targets one focused concern so failures localise quickly. Every fixture keeps `format_version = "1.2"`. Resource addresses, action names, and provider names use neutral placeholders (`example_resource.a`, `example_action`, `registry.terraform.io/example/example`) so the fixtures do not look like real provider regressions.

### H1 — Action invocations

| # | Fixture | Purpose | Required content |
|---|---------|---------|------------------|
| F-01 | `actions-lifecycle-before-create-plan.json` | AC-1 happy path: single lifecycle action triggered `before_create`, attached to a resource that has a matching `resource_changes[]` entry. | `action_invocations[]` length 1; `lifecycle_action_trigger.action_trigger_event = "before_create"`; `triggering_resource_address` matches a `create` resource change. |
| F-02 | `actions-lifecycle-after-create-plan.json` | Trigger-event coverage. | As F-01 with `action_trigger_event = "after_create"`. |
| F-03 | `actions-lifecycle-before-update-plan.json` | Trigger-event coverage. | As F-01 with `action_trigger_event = "before_update"` and parent change `update`. |
| F-04 | `actions-lifecycle-after-update-plan.json` | Trigger-event coverage. | `action_trigger_event = "after_update"`. |
| F-05 | `actions-lifecycle-before-destroy-plan.json` | Trigger-event coverage. | `action_trigger_event = "before_destroy"` with parent change `delete`. |
| F-06 | `actions-lifecycle-after-destroy-plan.json` | Trigger-event coverage. | `action_trigger_event = "after_destroy"` with parent change `delete`. |
| F-07 | `actions-invoke-only-plan.json` | AC-1 fallback: invoke-mode action with no matching `resource_changes[]` entry. Renders into the `🎬 Other Actions` section's `invoke` H3 sub-group (ADR-003). | `action_invocations[]` length 1; carries `invoke_action_trigger` instead of `lifecycle_action_trigger`; no `resource_changes[]`. |
| F-08 | `actions-deferred-plan.json` | AC-2: deferred action present in `deferred_action_invocations[]`. | One entry with the same shape as a lifecycle action; builder is expected to flip the deferred flag (ADR-001/003); rendered with `⏳` prefix and "deferred" callout. |
| F-09 | `actions-multiple-on-one-resource-plan.json` | Confirms ordering / list-rendering when one resource triggers several actions (`actions_list_index` differentiates). | Two `action_invocations[]` entries sharing the same `triggering_resource_address`, distinct `actions_list_index`. |
| F-10 | `actions-sensitive-config-plan.json` | AC-4: sensitivity-aware rendering of `config_values` via existing `SensitivityHelper` + `JsonFlattener`. | One action whose `config_values` contains both regular and sensitive keys; `config_sensitive` marks at least one path as sensitive; `config_unknown` marks at least one path as unknown. Expected: sensitive value redacted, unknown value rendered as the existing unknown sentinel. |
| F-11 | `actions-orphan-lifecycle-plan.json` | AC-1 fallback: lifecycle action whose `triggering_resource_address` is **not** present in `resource_changes[]` (e.g. parent in a no-op module). Rendered in `🎬 Other Actions` → `lifecycle-orphan` sub-group (ADR-003). | Lifecycle action with `triggering_resource_address` pointing to a resource not declared anywhere in `resource_changes[]`. |
| F-12 | `actions-with-diagnostics-plan.json` | FR-H1.8: action carries status / error / diagnostic payload. | Action JSON includes whatever diagnostic field the plan format exposes (e.g. an `errored` flag or a `diagnostics[]` array). Renderer must surface it when present. |
| F-13 | `actions-mixed-deferred-and-immediate-plan.json` | Confirms that deferred and non-deferred actions on the same resource render in the same inline location (FR-H1.5) and that ordering is stable. | One `action_invocations[]` and one `deferred_action_invocations[]` against the same parent. |

### H2 — Plan-context awareness (drift, status, relevant attributes)

| # | Fixture | Purpose | Required content |
|---|---------|---------|------------------|
| F-20 | `drift-single-entry-plan.json` | AC-5 happy path. | `resource_drift[]` length 1; `resource_changes[]` non-empty (so the drift section appears between Resource Changes and the relevant-attributes / Other Actions cluster, per ADR-002). |
| F-21 | `drift-multiple-entries-plan.json` | Multi-entry drift list rendering. | `resource_drift[]` length ≥ 2 across at least two providers. |
| F-22 | `drift-empty-baseline-plan.json` | AC-5 negative path. | Plan that contains only `resource_changes[]` and no `resource_drift[]` field at all → no `🌀 Drift Detected` section emitted. |
| F-23 | `status-errored-plan.json` | AC-6: errored plan banner. | `errored = true`, `applyable = false`, `complete = false`. |
| F-24 | `status-not-applyable-plan.json` | AC-6: non-applyable banner. | `applyable = false`, `complete = true`, `errored = false`. |
| F-25 | `status-incomplete-plan.json` | AC-6: incomplete banner. | `applyable = true`, `complete = false`, `errored = false`. |
| F-26 | `status-all-true-baseline-plan.json` | AC-6 negative path: ordinary plan, no banner OR quiet confirmation per ADR-002. | `applyable = true`, `complete = true`, `errored = false`. |
| F-27 | `relevant-attributes-present-plan.json` | AC-7: relevant attributes surfaced. | `relevant_attributes[]` length ≥ 2 referencing at least two distinct upstream resources. |
| F-28 | `relevant-attributes-absent-plan.json` | AC-7 negative path. | Plan with `resource_changes[]` but no `relevant_attributes[]` field → no `Relevant Attributes` section emitted. |

### M2 — Deprecations (Terraform 1.15)

| # | Fixture | Purpose | Required content |
|---|---------|---------|------------------|
| F-40 | `deprecation-variable-referenced-plan.json` | AC-8 happy path. | `configuration.root_module.variables.<name>.deprecated` set; the variable is referenced from a `resource_changes[]` configuration so the warning is emitted (ADR-004 — one warning per *referenced* deprecation). |
| F-41 | `deprecation-variable-unreferenced-plan.json` | ADR-004 boundary: declared-but-unreferenced deprecation does **not** emit a warning. | Variable carries `deprecated`, but no resource references it. Expected: 0 warnings. |
| F-42 | `deprecation-output-plan.json` | AC-8: deprecated output emits exactly one warning. | `configuration.root_module.outputs.<name>.deprecated` set; output appears in `outputs` map. |
| F-43 | `deprecation-output-with-explicit-type-plan.json` | FR-M2.3: explicit `outputs[*].type` is parsed and surfaced when it improves rendering. | Output with `deprecated` plus an explicit `type` field. |
| F-44 | `deprecation-multiple-plan.json` | Multi-source warnings — confirms `Source` discriminator (ADR-004) co-exists cleanly with SARIF entries when both are present. | Two deprecated variables + one deprecated output, all referenced. Optional: one synthetic SARIF code-analysis warning to verify both sources render under the renamed `Warnings` H3. |

### Backwards compatibility

| # | Fixture | Purpose | Required content |
|---|---------|---------|------------------|
| F-60 | `tf-1-13-baseline-plan.json` | AC-9 / NFR-1: a plan that contains **none** of the new fields (`action_invocations`, `deferred_action_invocations`, `resource_drift`, `relevant_attributes`, `applyable`, `complete`, `errored`, `variables[].deprecated`, `outputs[].deprecated`, `outputs[].type`). Snapshot must be byte-identical to the equivalent pre-feature output, with the single permitted exception of the H3 rename `Code Analysis Warnings` → `Warnings`. | Minimal plan with one `update` resource change, one variable, one output — none carrying any of the new fields. |

**Total new fixtures: 27** (13 action, 9 plan-context, 5 deprecation). All numeric IDs are local to this plan and are not part of any filename — file names follow the existing `kebab-case-plan.json` convention.

## Test Coverage Matrix

One row per acceptance criterion. Each row lists the test method(s), the fixture(s) the test consumes, and the snapshot file (if any). Test method names follow the existing `MethodName_Scenario_ExpectedResult` convention.

| AC | Test Method(s) | Fixture(s) | Snapshot |
|----|----------------|-----------|----------|
| **AC-1** Lifecycle inline + invoke fallback | `Snapshot_ActionsLifecycleBeforeCreate_MatchesBaseline` (and per-trigger-event variants for completeness), `Snapshot_ActionsInvokeOnly_MatchesBaseline`, `Snapshot_ActionsOrphanLifecycle_MatchesBaseline`, `Build_LifecycleAction_AttachesToTriggeringResource`, `Build_InvokeAction_RoutesToOtherActionsInvokeGroup`, `Build_OrphanLifecycleAction_RoutesToOtherActionsLifecycleOrphanGroup` | F-01..F-06, F-07, F-11 | `actions-lifecycle-before-create.md` (+ per-event), `actions-invoke-only.md`, `actions-orphan-lifecycle.md` |
| **AC-2** Deferred actions inline + clearly marked | `Snapshot_ActionsDeferred_MatchesBaseline`, `Snapshot_ActionsMixedDeferredAndImmediate_MatchesBaseline`, `Render_DeferredAction_PrefixesWithHourglass`, `Build_DeferredAction_FlipsDeferredFlag` | F-08, F-13 | `actions-deferred.md`, `actions-mixed-deferred-and-immediate.md` |
| **AC-3** Single generic action renderer (no provider-specific code) | `Architecture_NoProviderSpecificActionRenderer_Exists` (NetArchTest rule asserting no type whose name matches `*ActionRenderer` lives outside the generic action-rendering namespace) | — (architecture rule, no fixture) | — |
| **AC-4** Sensitivity redaction via `SensitivityHelper` | `Snapshot_ActionsSensitiveConfig_MatchesBaseline`, `Render_ActionConfigValues_RedactsSensitive_KeepsUnknownSentinel` | F-10 | `actions-sensitive-config.md` |
| **AC-5** Drift section emitted iff non-empty | `Snapshot_DriftSingleEntry_MatchesBaseline`, `Snapshot_DriftMultipleEntries_MatchesBaseline`, `Snapshot_DriftEmptyBaseline_MatchesBaseline` (negative — asserts absence of `🌀 Drift Detected` H2), `Build_ResourceDrift_PopulatesDriftModel`, `Build_NoResourceDrift_OmitsDriftModel` | F-20, F-21, F-22 | `drift-single-entry.md`, `drift-multiple-entries.md`, `drift-empty-baseline.md` |
| **AC-6** Plan status banner | `Snapshot_StatusErrored_MatchesBaseline`, `Snapshot_StatusNotApplyable_MatchesBaseline`, `Snapshot_StatusIncomplete_MatchesBaseline`, `Snapshot_StatusAllTrueBaseline_MatchesBaseline` (negative — no misleading banner), `Render_StatusBanner_PerBooleanCombination_EmitsExpectedCallout` (table-driven unit test) | F-23, F-24, F-25, F-26 | `status-errored.md`, `status-not-applyable.md`, `status-incomplete.md`, `status-all-true-baseline.md` |
| **AC-7** Relevant attributes surfaced | `Snapshot_RelevantAttributesPresent_MatchesBaseline`, `Snapshot_RelevantAttributesAbsent_MatchesBaseline` (negative), `Build_RelevantAttributes_PopulatesModel`, `Build_NoRelevantAttributes_OmitsSection` | F-27, F-28 | `relevant-attributes-present.md`, `relevant-attributes-absent.md` |
| **AC-8** Deprecations via existing warnings pipeline | `Snapshot_DeprecationVariableReferenced_MatchesBaseline`, `Snapshot_DeprecationOutput_MatchesBaseline`, `Snapshot_DeprecationOutputWithExplicitType_MatchesBaseline`, `Snapshot_DeprecationMultiple_MatchesBaseline`, `Snapshot_DeprecationVariableUnreferenced_EmitsZeroWarnings` (negative), `Build_DeprecatedVariable_EmitsExactlyOneWarningPerReference`, `Build_DeprecatedOutput_EmitsExactlyOneWarning`, `Build_DeprecatedVariable_NotReferenced_EmitsNoWarning`, `Render_WarningsHeading_RenamedFromCodeAnalysisWarnings` | F-40, F-41, F-42, F-43, F-44 | `deprecation-variable-referenced.md`, `deprecation-output.md`, `deprecation-output-with-explicit-type.md`, `deprecation-multiple.md`, `deprecation-variable-unreferenced.md` |
| **AC-9** TF 1.13 backwards compat (byte-identical) | `Snapshot_Tf113Baseline_MatchesBaseline`, plus the **entire** existing `MarkdownSnapshotTests` / `ComprehensiveDemoTests` / `EphemeralSnapshotTests` / `KnownAfterApplySnapshotTests` suites must continue to pass with **only** the `Code Analysis Warnings` → `Warnings` rename diff (committed under `SNAPSHOT_UPDATE_OK`, see snapshot note below). | F-60 + all existing fixtures | `tf-1-13-baseline.md` + every existing snapshot |
| **AC-10** No new CLI flags | `Cli_HelpOutput_DoesNotExposeNewFeatureFlags` (asserts `--help` text contains no new feature toggles) | — | — |
| **AC-11** Hand-crafted fixtures present | Implicit — every fixture above lives under `TestData/` and is referenced by at least one test in this plan. A discovery test `Fixtures_Tf114_AllFilesReferencedByAtLeastOneTest` MAY enforce this. | F-01..F-60 | — |
| **AC-12** O(N) performance for many actions | `Performance_RenderPlanWith200Actions_StaysWithinSameOrderOfMagnitudeAsBaseline` (see [Performance test note](#performance-test-note-ac-12)) | Synthesised in-test; no static fixture | — |
| **AC-13** Feature folder + `docs/features.md` updated | `Docs_Features_ListsFeature122` (markdown link presence test, mirroring existing docs assertions if any), or verified manually by Code Reviewer per definition-of-done | — | — |

### Negative-path / graceful-absence coverage

These are explicit negative tests (each AC's negative branch above plus the parser-level absence tests). All flow through the same fixtures or through tightly-scoped parser unit tests:

| Concern | Test Method | Fixture |
|---------|-------------|---------|
| Missing `action_invocations` parses to empty | `Parse_PlanWithoutActions_LeavesActionsCollectionEmpty` | F-60 |
| Missing `deferred_action_invocations` parses to empty | `Parse_PlanWithoutDeferredActions_LeavesDeferredCollectionEmpty` | F-60 |
| Missing `resource_drift` parses to empty | `Parse_PlanWithoutDrift_LeavesDriftCollectionEmpty` | F-22 / F-60 |
| Missing `relevant_attributes` parses to empty | `Parse_PlanWithoutRelevantAttributes_LeavesCollectionEmpty` | F-28 / F-60 |
| Missing status booleans parse to null (not false) | `Parse_PlanWithoutStatusBooleans_LeavesBooleansNull` | F-60 |
| Missing `variables[].deprecated` / `outputs[].deprecated` | `Parse_PlanWithoutDeprecations_LeavesDeprecationStringsNull` | F-60 |
| Action with no matching resource_change | `Build_OrphanLifecycleAction_RoutesToOtherActionsLifecycleOrphanGroup` | F-11 |
| Action diagnostic fields absent on most actions | `Render_ActionWithoutDiagnostics_OmitsDiagnosticBlock` | F-01 |

## Snapshot Update Note (per ADR-004)

ADR-004 renames the H3 emitted by `CodeAnalysisSectionRenderer` from `### Code Analysis Warnings` to `### Warnings`. This is the single intentional global rendering change in this feature and AC-9 explicitly carves it out from the byte-identical-output requirement.

**Action required when committing:**

1. After implementing the rename, run the full snapshot suite. Any existing snapshot that contains `### Code Analysis Warnings` will diff to `### Warnings`. A repo-wide grep currently finds no snapshot files asserting on that exact text (the heading appears only in agent / docs files), so the diff is expected to be small or empty — but the Developer must still verify and not assume.
2. If snapshots do diff, regenerate them per the standard snapshot-update workflow and **include the literal token `SNAPSHOT_UPDATE_OK` in the commit message body**. This is the protocol documented in `.github/agents/developer.agent.md` and validated by the Code Reviewer per `.github/agents/code-reviewer.agent.md`.
3. The Code Reviewer must confirm that every diff under `TestData/Snapshots/` for this feature is **either** (a) a brand-new snapshot file added alongside one of the fixtures listed above, **or** (b) the heading rename `Code Analysis Warnings` → `Warnings`. Any other pre-existing snapshot diff is a red flag for an unintended regression and must be rejected.

## Performance Test Note (AC-12)

**Goal.** AC-12 / NFR-3 require that rendering a plan with 200 resource changes and 200 action invocations stays within the same order of magnitude as a plan with 200 resource changes and zero actions. This is an **O(N) regression guard**, not a microbenchmark — the bar is "no quadratic blow-up", not a strict ms target.

**Test.** `Performance_RenderPlanWith200Actions_StaysWithinSameOrderOfMagnitudeAsBaseline` in `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ActionInvocationPerformanceTests.cs`.

**Method.**

1. Synthesise two in-memory `TerraformPlan` objects programmatically (no fixture file — the synthesis itself is the test data, mirroring `DiffComputationPerformanceTests`):
   - **Baseline:** 200 `resource_changes[]`, 0 `action_invocations[]`.
   - **With actions:** 200 `resource_changes[]` + 200 `action_invocations[]`, each lifecycle-triggered against one of the resource changes (round-robin over the 200 parents).
2. Warm up by rendering each plan once (JIT, source generators, cache warmup).
3. Time a second render of each plan with `Stopwatch`. Take the median of N=5 runs to dampen noise.
4. Assert: `withActionsMedian < (baselineMedian * K)` where `K` is set conservatively (proposed `K = 4`) to guard against quadratic regressions while tolerating reasonable per-action overhead.

**Why this passes the "fully automated" bar.** The test is deterministic, requires no external services, runs in seconds, and has a hard pass/fail assertion. It is wall-clock-based but the assertion is a *ratio*, not an absolute time — that makes it stable across CI runner classes (slow runner → both timings scale together; the ratio is preserved).

**Tagging.** Mark the test with `Category = "Performance"` so it can be excluded from fast-feedback runs via `--treenode-filter /**[Category!=Performance]` if needed, while still running by default in `pr-validation.yml`.

## Test Data Requirements

All fixtures listed in the [Hand-Crafted JSON Fixture Inventory](#hand-crafted-json-fixture-inventory) section above are new. They live under `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`. No fixtures need to be regenerated from a real Terraform CLI — every file is hand-crafted per NFR-6 / AC-11. The Developer should consult the schema references in [`analysis.md`](./analysis.md) when authoring each file.

## Non-Functional Tests

| NFR | Test |
|-----|------|
| NFR-1 backwards compatibility | `Snapshot_Tf113Baseline_MatchesBaseline` + entire pre-existing snapshot suite (with the `Warnings` rename carve-out under `SNAPSHOT_UPDATE_OK`). |
| NFR-2 format-version stability | `Parse_PlanWithFormatVersion12_AcceptsAllNewFields` (asserts no version gating). |
| NFR-3 performance | `Performance_RenderPlanWith200Actions_StaysWithinSameOrderOfMagnitudeAsBaseline`. |
| NFR-4 memory (no unbounded copies) | Implicit via NFR-3 test method (single allocation pass, no list materialisation hotspots). No additional dedicated test required for this feature; flagged for follow-up if regressions surface. |
| NFR-5 graceful absence | All "missing-field" parser tests in [Negative-path coverage](#negative-path--graceful-absence-coverage). |
| NFR-6 minimal hand-crafted fixtures | Inventory above — every fixture is < ~50 lines of JSON and exercises one concern. |
| NFR-7 always-on | `Cli_HelpOutput_DoesNotExposeNewFeatureFlags`. |

## Open Questions

None unresolved at QE handoff. All layout choices are locked by ADR-002 / ADR-003 / ADR-004; all decisions on fixture scope are locked by NFR-6 / AC-11; the snapshot-update workflow is documented in the Developer agent instructions referenced above.
