# Tasks: Terraform 1.14 / 1.15 Plan-JSON Support

## Overview

Implement parsing and rendering of the new optional plan-JSON fields introduced in Terraform 1.14 / 1.15 (and the pre-existing fields tfplan2md silently dropped):

- **H1** — `action_invocations[]`, `deferred_action_invocations[]` (1.14)
- **H2** — `resource_drift[]`, `relevant_attributes[]`, `applyable` / `complete` / `errored`
- **M2** — `configuration.root_module.{variables,outputs}[*].deprecated`, `outputs[*].type` (1.15)

References: [specification.md](specification.md), [adr-001](adr-001-plan-json-model-extensions.md), [adr-002](adr-002-h2-report-layout.md), [adr-003](adr-003-inline-action-rendering.md), [adr-004](adr-004-deprecation-warnings-via-existing-pipeline.md), [test-plan.md](test-plan.md).

## Working principles

- **Test-first.** Each task adds (or extends) tests before or alongside the production code it covers. The fixture inventory is in [test-plan.md § Hand-Crafted JSON Fixture Inventory](test-plan.md#hand-crafted-json-fixture-inventory) (fixtures F-01..F-60); each task below claims a slice.
- **Always-green build.** Tasks are ordered so `dotnet build` and `dotnet test` succeed after every commit. Production code added in early tasks is not yet wired into renderers (so existing snapshots are unaffected) until the explicit wire-in tasks fire.
- **Backwards compatibility (AC-9, NFR-1).** Existing snapshots must keep passing throughout, with the **single** intentional exception of the `Code Analysis Warnings` → `Warnings` H3 rename in Task 13 (committed with `SNAPSHOT_UPDATE_OK`, see Task 14).
- **No provider-specific code.** AC-3 forbids `*ActionRenderer` types outside the generic namespace; the architecture-rule test in Task 16 enforces this.

## Phase 1 — JSON model & parsing (ADR-001)

### Task 1: Add new parsing records and register them in `TfPlanJsonContext`

**Priority:** High

**Implements:** ADR-001 (records section); FR-H1.1, FR-H1.2, FR-H2.1, FR-H2.3, NFR-5.

**Inputs:** ADR-001 §§ Implementation notes 2–4; existing `Parsing/ReplacePathsConverter.cs` for the heterogeneous-path pattern reused by `RelevantAttribute`.

**Deliverables:**
- New files under `src/Oocx.TfPlan2Md/Parsing/`:
  - `ActionInvocation.cs` (`address`, `type`, `name`, `provider_name`, `config_values`, `config_sensitive`, `config_unknown`, `lifecycle_action_trigger`, `invoke_action_trigger`, `status` and `diagnostics` as `JsonElement?`).
  - `LifecycleActionTrigger.cs` (`triggering_resource_address`, `action_trigger_event`, `action_trigger_block_index`, `actions_list_index`).
  - `InvokeActionTrigger.cs` (placeholder record; presence is what matters per FR-H1.1).
  - `RelevantAttribute.cs` (`resource`, `attribute` — heterogeneous path array, parsed with the same approach as `ReplacePathsConverter`).
- Register every new record (and every `IReadOnlyList<>` / `Dictionary<>` wrapper) in `TfPlanJsonContext`.
- Parser unit tests under `src/tests/Oocx.TfPlan2Md.TUnit/Parsing/` exercising minimal stand-alone JSON snippets for each record.

**Done when:**
- `dotnet build` succeeds; new types are reachable via `TfPlanJsonContext` (no AOT trim warnings).
- Each new record has at least one round-trip test asserting all properties (including the heterogeneous `RelevantAttribute.attribute` path).
- No existing test fails. **No** changes to `TerraformPlan` yet (Task 2 owns that).

---

### Task 2: Extend `TerraformPlan` with the seven optional properties

**Priority:** High

**Implements:** ADR-001 (Option A); FR-H1.1, FR-H1.2, FR-H2.1, FR-H2.2, FR-H2.3, NFR-1, NFR-5, AC-9.

**Inputs:** ADR-001 § Decision and § Implementation notes 1; the records added in Task 1.

**Deliverables:**
- Modify `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` to add (positional, end-of-list, defaulting to `null`):
  - `IReadOnlyList<ActionInvocation>? ActionInvocations`
  - `IReadOnlyList<ActionInvocation>? DeferredActionInvocations`
  - `IReadOnlyList<ResourceChange>? ResourceDrift` (reuse existing `ResourceChange`)
  - `IReadOnlyList<RelevantAttribute>? RelevantAttributes`
  - `bool? Applyable`, `bool? Complete`, `bool? Errored`
- Add fixture **F-60** `tf-1-13-baseline-plan.json` (one `update` resource change, one variable, one output; **none** of the new fields).
- Parser unit tests:
  - `Parse_PlanWithoutActions_LeavesActionsCollectionEmpty`
  - `Parse_PlanWithoutDeferredActions_LeavesDeferredCollectionEmpty`
  - `Parse_PlanWithoutDrift_LeavesDriftCollectionEmpty`
  - `Parse_PlanWithoutRelevantAttributes_LeavesCollectionEmpty`
  - `Parse_PlanWithoutStatusBooleans_LeavesBooleansNull`
  - `Parse_PlanWithFormatVersion12_AcceptsAllNewFields` (NFR-2 guard, using a tiny in-test JSON literal that carries every new top-level field)

**Done when:**
- All existing tests pass unchanged (positional record still constructs correctly; new params default to `null`).
- New parser tests assert null/empty for absent fields and non-null for present fields.
- `TerraformPlan` is the only file modified outside tests / fixtures.

**Dependencies:** Task 1.

---

### Task 3: Add `ConfigurationDeprecationReader` helper

**Priority:** Medium

**Implements:** ADR-001 § Implementation notes 5; FR-M2.1, FR-M2.2, FR-M2.3.

**Inputs:** ADR-001 § Decision (last paragraph); existing `ConfigurationReferenceResolver` for the file-location pattern.

**Deliverables:**
- New file `src/Oocx.TfPlan2Md/Parsing/ConfigurationDeprecationReader.cs` — a static helper that, given the plan's `JsonElement? Configuration`, yields tuples `(name, kind, deprecationMessage, optionalCtyType)` for both `variables` and `outputs`. `kind` is `"variable"` or `"output"`. `optionalCtyType` is populated only for outputs.
- Unit tests covering: variable with `deprecated`, output with `deprecated` and `type`, output with only `type`, configuration without `root_module`, null configuration.

**Done when:**
- The helper compiles and is independently unit-tested (no production caller yet — Task 12 wires it).
- Existing tests unchanged.

**Dependencies:** None (operates on raw `JsonElement`).

## Phase 2 — H2 plan-context model & renderers (ADR-002)

### Task 4: Add H2 plan-context model + builder population

**Priority:** High

**Implements:** ADR-002 § Consequences (model); FR-H2.4–FR-H2.7, AC-5, AC-6, AC-7.

**Inputs:** ADR-002 § Decision; ADR-001 (parsed sources); existing `ReportModel` and `ReportModelBuilder`.

**Deliverables:**
- New types under `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/`:
  - `PlanStatusModel` — three nullable bools (`Applyable`, `Complete`, `Errored`).
  - `RelevantAttributeModel` — `Resource`, `AttributePath` (preformatted string using the same path-formatter `replace_paths` uses).
- Extend `ReportModel` with optional properties:
  - `PlanStatusModel? PlanStatus`
  - `IReadOnlyList<ResourceChangeModel> Drift` (default empty)
  - `IReadOnlyList<RelevantAttributeModel> RelevantAttributes` (default empty)
- New builder partial `ReportModelBuilder.PlanContext.cs` that populates these from the plan.
- Builder unit tests:
  - `Build_ResourceDrift_PopulatesDriftModel`
  - `Build_NoResourceDrift_OmitsDriftModel`
  - `Build_RelevantAttributes_PopulatesModel`
  - `Build_NoRelevantAttributes_OmitsSection`
  - `Build_PlanStatusBooleans_PopulatePlanStatusModel`
  - `Build_NoStatusBooleans_LeavesPlanStatusNull`

**Done when:**
- New properties have empty/null defaults; existing builder tests pass unchanged.
- No renderer changes yet — section is silent.

**Dependencies:** Tasks 1, 2.

---

### Task 5: Plan-status banner in `HeaderRenderer`

**Priority:** High

**Implements:** ADR-002 Option A1; FR-H2.4, AC-6, AC-9.

**Inputs:** ADR-002 § Decision step 2; existing `HeaderRenderer`.

**Deliverables:**
- Add fixtures **F-23** `status-errored-plan.json`, **F-24** `status-not-applyable-plan.json`, **F-25** `status-incomplete-plan.json`, **F-26** `status-all-true-baseline-plan.json`.
- Modify `HeaderRenderer` to emit a blockquote callout immediately after the H1 title and before the existing metadata line, **only when** at least one of `Applyable=false` / `Complete=false` / `Errored=true` is present. Stack multiple signals as separate blockquote lines in a single block.
- Renderer unit test `Render_StatusBanner_PerBooleanCombination_EmitsExpectedCallout` (table-driven over all 8 combinations + null baseline).
- Snapshot tests `Snapshot_StatusErrored_MatchesBaseline`, `Snapshot_StatusNotApplyable_MatchesBaseline`, `Snapshot_StatusIncomplete_MatchesBaseline`, `Snapshot_StatusAllTrueBaseline_MatchesBaseline` with checked-in `.md` baselines under `TestData/Snapshots/`.

**Done when:**
- Banner present for the three non-baseline fixtures; absent for the all-true baseline; absent when `PlanStatus` is null. Existing snapshots unaffected.

**Dependencies:** Task 4.

---

### Task 6: `RenderDriftSection` (H2 `🌀 Drift Detected`)

**Priority:** High

**Implements:** ADR-002 Option B1; FR-H2.5, AC-5.

**Inputs:** ADR-002 § Implementation notes (drift framing); existing `DefaultResourceRenderer`.

**Deliverables:**
- Add fixtures **F-20** `drift-single-entry-plan.json`, **F-21** `drift-multiple-entries-plan.json`, **F-22** `drift-empty-baseline-plan.json`.
- Add `RenderDriftSection` to `ReportRenderer` (NOT yet wired into `Render` — see Task 8). Reuse `DefaultResourceRenderer` to render each drift entry; prepend `🌀 ` to the resource heading via the least-invasive existing hook (do **not** create a parallel `DriftResourceRenderer`).
- Snapshot tests `Snapshot_DriftSingleEntry_MatchesBaseline`, `Snapshot_DriftMultipleEntries_MatchesBaseline`, `Snapshot_DriftEmptyBaseline_MatchesBaseline` (negative — asserts absence).

**Done when:**
- Section renders correctly when invoked directly from a unit test against a populated `ReportModel`; section is omitted when `Drift` is empty.

**Dependencies:** Task 4.

---

### Task 7: `RenderRelevantAttributes` section

**Priority:** Medium

**Implements:** ADR-002 Option C1; FR-H2.6, AC-7.

**Inputs:** ADR-002 § Decision step 10; existing `MarkdownHelpers` path formatter.

**Deliverables:**
- Add fixtures **F-27** `relevant-attributes-present-plan.json`, **F-28** `relevant-attributes-absent-plan.json`.
- Add `RenderRelevantAttributes` to `ReportRenderer` (NOT yet wired — see Task 8). Single H2 `Relevant Attributes`, two-column table (`Resource | Attribute path`). Section omitted when collection empty.
- Snapshot tests `Snapshot_RelevantAttributesPresent_MatchesBaseline`, `Snapshot_RelevantAttributesAbsent_MatchesBaseline`.

**Done when:**
- Table renders correctly under direct invocation; absent on empty.

**Dependencies:** Task 4.

---

### Task 8: Wire H2 sections into `ReportRenderer.Render` ordering

**Priority:** High

**Implements:** ADR-002 § Decision (final ordering, steps 9–10); AC-9 (existing snapshots unchanged because every new section is omitted on absence).

**Inputs:** ADR-002 § Decision; existing `ReportRenderer.Render`.

**Deliverables:**
- Update `ReportRenderer.Render` to call (in order): `RenderResourceChanges` → `CodeAnalysisSectionRenderer.RenderOtherFindings` → **(placeholder for `RenderOtherActions` from Task 11 — leave a `// TODO Task 11` no-op call site)** → `RenderDriftSection` → `RenderRelevantAttributes` → `RenderRefactoring` → `RenderOutputs` → `RenderFilteredResourceInfo`.
- Re-run the entire existing snapshot suite. **All existing snapshots must pass without diff** (every new section is silent on absence).

**Done when:**
- All existing tests green.
- New H2 snapshot tests from Tasks 5–7 still pass.
- The `RenderOtherActions` placeholder is wired but does nothing (stub method that early-returns).

**Dependencies:** Tasks 5, 6, 7.

## Phase 3 — H1 action model & rendering (ADR-003)

### Task 9: Action model + builder attachment + orphan routing

**Priority:** High

**Implements:** ADR-003 Options 1A, 5A; FR-H1.1, FR-H1.2, FR-H1.3, FR-H1.5, FR-H1.7, AC-1, AC-2.

**Inputs:** ADR-003 §§ Decision 1 and 5; ADR-001 (parsed `ActionInvocation`).

**Deliverables:**
- New types under `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/`:
  - `ActionInvocationModel` (carries the parsed `ActionInvocation` + an `IsDeferred` bool + the resolved trigger event / list-index for ergonomic rendering).
  - `OtherActionsModel` (two `IReadOnlyList<ActionInvocationModel>` — `InvokeActions`, `LifecycleOrphanActions`).
- Extend `ResourceChangeModel` with `IReadOnlyList<ActionInvocationModel> Actions { get; }` (default empty list).
- Extend `ReportModel` with `OtherActionsModel? OtherActions`.
- Add `ReportModelBuilder.Actions.cs` partial that runs **after** `MergeParentChildRelationships`. For each entry in `plan.ActionInvocations` and `plan.DeferredActionInvocations`:
  1. If entry has `lifecycle_action_trigger` and `triggering_resource_address` matches an existing post-merge `ResourceChangeModel.Address` → append to that resource's `Actions` list with `IsDeferred = (sourceList == DeferredActionInvocations)`.
  2. Else if entry has `invoke_action_trigger` → append to `OtherActions.InvokeActions`.
  3. Else (lifecycle action whose parent is not in `resource_changes[]`) → append to `OtherActions.LifecycleOrphanActions`.
- Add fixtures **F-01** `actions-lifecycle-before-create-plan.json`, **F-07** `actions-invoke-only-plan.json`, **F-08** `actions-deferred-plan.json`, **F-11** `actions-orphan-lifecycle-plan.json`.
- Builder unit tests:
  - `Build_LifecycleAction_AttachesToTriggeringResource`
  - `Build_InvokeAction_RoutesToOtherActionsInvokeGroup`
  - `Build_OrphanLifecycleAction_RoutesToOtherActionsLifecycleOrphanGroup`
  - `Build_DeferredAction_FlipsDeferredFlag`

**Done when:**
- Builder partials populate the new collections correctly for all four fixtures; defaults remain empty for plans without actions.
- **No** rendering yet — existing snapshots unaffected. `ParentChildRelationshipRegistry` is not modified.

**Dependencies:** Tasks 1, 2.

---

### Task 10: `ActionInvocationSectionRenderer` — generic per-action rendering

**Priority:** High

**Implements:** ADR-003 Options 2A, 3A, 4A; FR-H1.4, FR-H1.5, FR-H1.6, FR-H1.8, AC-2, AC-3, AC-4.

**Inputs:** ADR-003 §§ Decision 2–4 and Implementation notes; existing `JsonFlattener`, `SensitivityHelper`, `IRenderContext`.

**Deliverables:**
- New file `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ActionInvocationSectionRenderer.cs`. Public method renders **one** `ActionInvocationModel` as:
  1. Bold paragraph + horizontal rule (NOT H5) with `⏳ ` prefix when `IsDeferred`, action address, type, provider.
  2. When `IsDeferred`: a `> ⏳ **Deferred** — will run on a subsequent apply.` callout immediately after the heading paragraph.
  3. Properties two-column table: `Trigger`, optional `Action block index` / `Actions list index`, `Deferred` (`Yes` only when applicable).
  4. `Config` block: `JsonFlattener.Flatten(config_values)` then per-leaf `SensitivityHelper.IsSensitiveAttribute` (sensitive map) and the parallel unknown check; respect `IRenderContext.ShowSensitive`.
  5. `Status` block: emit a fenced JSON code block of `status` ⊕ `diagnostics` only when at least one is present; omit otherwise.
- Add fixtures **F-02..F-06** (per-trigger-event coverage), **F-09** `actions-multiple-on-one-resource-plan.json`, **F-10** `actions-sensitive-config-plan.json`, **F-12** `actions-with-diagnostics-plan.json`, **F-13** `actions-mixed-deferred-and-immediate-plan.json`.
- Renderer unit tests:
  - `Render_DeferredAction_PrefixesWithHourglass`
  - `Render_ActionConfigValues_RedactsSensitive_KeepsUnknownSentinel`
  - `Render_ActionWithoutDiagnostics_OmitsDiagnosticBlock`
  - `Render_ActionWithDiagnostics_EmitsJsonCodeBlock`

**Done when:**
- Renderer is unit-testable in isolation against synthesised `ActionInvocationModel` instances; all four renderer unit tests pass.
- Renderer is **not yet wired** into `ReportRenderer` or any resource renderer (Task 11 owns that).

**Dependencies:** Task 9.

---

### Task 11: Wire actions into the report (inline `🎬 Actions` H4 + H2 `🎬 Other Actions`)

**Priority:** High

**Implements:** ADR-003 Decisions 1, 5; ADR-002 § Decision step 8 (Other Actions placement); AC-1, AC-2.

**Inputs:** ADR-003 § Implementation notes; ADR-002 § Decision (final ordering); the placeholder added in Task 8.

**Deliverables:**
- In the resource-rendering path used by `RenderResourceChanges`, after the existing per-resource content, render an H4 `🎬 Actions` sub-section when `ResourceChangeModel.Actions` is non-empty, calling `ActionInvocationSectionRenderer` once per action.
- Replace the Task 8 `RenderOtherActions` stub with a real implementation: H2 `🎬 Other Actions` containing optional H3 `Invoke actions` and optional H3 `Lifecycle actions without a matching resource change` sub-groups (each calling `ActionInvocationSectionRenderer`). Whole section omitted when both groups empty.
- Snapshot tests for every H1 fixture:
  - `Snapshot_ActionsLifecycleBeforeCreate_MatchesBaseline` (+ per-event variants for F-02..F-06)
  - `Snapshot_ActionsInvokeOnly_MatchesBaseline`
  - `Snapshot_ActionsDeferred_MatchesBaseline`
  - `Snapshot_ActionsMultipleOnOneResource_MatchesBaseline`
  - `Snapshot_ActionsSensitiveConfig_MatchesBaseline`
  - `Snapshot_ActionsOrphanLifecycle_MatchesBaseline`
  - `Snapshot_ActionsWithDiagnostics_MatchesBaseline`
  - `Snapshot_ActionsMixedDeferredAndImmediate_MatchesBaseline`

**Done when:**
- All H1 snapshot tests pass against checked-in `.md` baselines.
- Existing snapshots remain unchanged (an empty `Actions` list renders nothing; an absent `OtherActions` renders nothing).

**Dependencies:** Tasks 8, 9, 10.

## Phase 4 — M2 deprecations into the warnings pipeline (ADR-004)

### Task 12: Extend `CodeAnalysisWarningModel` (additive, default preserves SARIF behaviour)

**Priority:** Medium

**Implements:** ADR-004 Option 1A; FR-M2.4, FR-M2.5.

**Inputs:** ADR-004 § Implementation notes; existing `CodeAnalysisWarningModel` and `BuildWarningModels`.

**Deliverables:**
- Add `enum CodeAnalysisWarningSource { SarifProcessingFailure = 0, PlanDeprecation }` next to the model.
- Modify `CodeAnalysisWarningModel`:
  - Make `FilePath` nullable (drop `required`).
  - Add `Source { get; init; } = SarifProcessingFailure`.
  - Add `string? SubjectName { get; init; }` and `string? SubjectKind { get; init; }`.
- Update existing call-sites in `BuildWarningModels` to set `Source = SarifProcessingFailure` explicitly (or rely on default) and to keep populating `FilePath`.
- All existing warning-related unit tests must still pass without modification of expected output.

**Done when:**
- `dotnet build` and `dotnet test` green; SARIF-warning behaviour is byte-identical (renderer not yet branched — Task 13 owns that).

**Dependencies:** None (independent of Phase 3).

---

### Task 13: Emit deprecation warnings + rename H3 + render output `type`

**Priority:** Medium

**Implements:** ADR-004 Decisions 1, 2, 3; FR-M2.1–FR-M2.5, AC-8; FR-M2.3 type rendering; **the single intentional `Code Analysis Warnings` → `Warnings` rename** (AC-9 carve-out).

**Inputs:** ADR-004 § Implementation notes; `ConfigurationDeprecationReader` from Task 3.

**Deliverables:**
- New builder partial `ReportModelBuilder.Deprecations.cs` (or fold into `ReportModelBuilder.CodeAnalysis.cs` if cohesion improves):
  1. Walk `ConfigurationDeprecationReader` over `plan.Configuration`.
  2. Filter to **referenced** entries: variable name appears in `plan.Variables`; output name appears in `plan.OutputChanges`.
  3. Append `CodeAnalysisWarningModel { Source = PlanDeprecation, SubjectKind = "variable"|"output", SubjectName = name, Message = deprecationMessage, FilePath = null }` to the existing warnings collection on `CodeAnalysisReportModel`.
- Modify `CodeAnalysisSectionRenderer.RenderSummary`:
  - Rename `writer.Heading("Code Analysis Warnings", 3)` → `writer.Heading("Warnings", 3)`.
  - Branch per `warning.Source`:
    - `SarifProcessingFailure` → existing two-paragraph rendering, unchanged.
    - `PlanDeprecation` → ``⚠️ **Deprecated {kind}** `{name}`: {EscapeMarkdown(message)}`` (single paragraph).
- Optionally surface output `type` (parsed in ADR-001): annotate the `Description` cell of the existing outputs table with the cty type when no description is provided. Implementation detail per ADR-004 § Implementation notes.
- Add fixtures **F-40** `deprecation-variable-referenced-plan.json`, **F-41** `deprecation-variable-unreferenced-plan.json`, **F-42** `deprecation-output-plan.json`, **F-43** `deprecation-output-with-explicit-type-plan.json`, **F-44** `deprecation-multiple-plan.json`.
- Builder unit tests:
  - `Build_DeprecatedVariable_EmitsExactlyOneWarningPerReference`
  - `Build_DeprecatedOutput_EmitsExactlyOneWarning`
  - `Build_DeprecatedVariable_NotReferenced_EmitsNoWarning`
- Renderer unit test `Render_WarningsHeading_RenamedFromCodeAnalysisWarnings`.
- Snapshot tests:
  - `Snapshot_DeprecationVariableReferenced_MatchesBaseline`
  - `Snapshot_DeprecationOutput_MatchesBaseline`
  - `Snapshot_DeprecationOutputWithExplicitType_MatchesBaseline`
  - `Snapshot_DeprecationMultiple_MatchesBaseline`
  - `Snapshot_DeprecationVariableUnreferenced_EmitsZeroWarnings` (negative — section absent / no deprecation entry)

**Done when:**
- All new deprecation tests pass; the renamed H3 appears in both new snapshots and any existing snapshot that exercises a SARIF processing failure (handled in Task 14).
- No parallel warnings collection or renderer is introduced.

**Dependencies:** Tasks 3, 12.

## Phase 5 — Backwards-compat regen, performance, and cross-cutting verification

### Task 14: TF 1.13 backwards-compat snapshot + global snapshot rebaseline for the Warnings rename

**Priority:** High

**Implements:** AC-9, NFR-1; the snapshot-update protocol described in [test-plan.md § Snapshot Update Note](test-plan.md#snapshot-update-note-per-adr-004).

**Inputs:** F-60 (added in Task 2); the renamed heading from Task 13.

**Deliverables:**
- Snapshot test `Snapshot_Tf113Baseline_MatchesBaseline` that loads F-60 and asserts a checked-in `tf-1-13-baseline.md` baseline. The baseline must be byte-identical to what the renderer would have emitted before this feature, with the **only** permitted difference being the renamed H3 `Warnings`.
- Run the full snapshot suite (`scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`). For any pre-existing snapshot that diffs **only** by `Code Analysis Warnings` → `Warnings`, regenerate the snapshot file per the standard snapshot-update workflow.
- The commit that regenerates these snapshots **MUST** include the literal token `SNAPSHOT_UPDATE_OK` in the commit message body.

**Done when:**
- Every diff under `TestData/Snapshots/` is either (a) a brand-new snapshot file added in Tasks 5–13, **or** (b) an existing snapshot whose only change is the heading rename. Any other diff is treated as a regression and must be investigated.
- Full test suite green.

**Dependencies:** Tasks 8, 11, 13.

---

### Task 15: Performance regression guard (`ActionInvocationPerformanceTests`)

**Priority:** Medium

**Implements:** NFR-3, NFR-4, AC-12; pattern from existing `DiffComputationPerformanceTests`.

**Inputs:** [test-plan.md § Performance Test Note](test-plan.md#performance-test-note-ac-12).

**Deliverables:**
- New file `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ActionInvocationPerformanceTests.cs`:
  - Synthesise two in-memory `TerraformPlan` instances (200 resource changes ± 200 lifecycle-triggered actions round-robin).
  - Warm up once; time N=5 renders each; take median; assert `withActionsMedian < baselineMedian * 4`.
  - Tag with `Category = "Performance"`.

**Done when:**
- Test passes deterministically in local runs and in CI.

**Dependencies:** Task 11.

---

### Task 16: Cross-cutting verification (CLI, architecture rule, fixture inventory, docs)

**Priority:** Medium

**Implements:** AC-3, AC-10, AC-11, AC-13; FR-M2.5 (architecture).

**Deliverables:**
- `Cli_HelpOutput_DoesNotExposeNewFeatureFlags` — asserts `tfplan2md --help` text contains no new feature toggles (AC-10, NFR-7).
- `Architecture_NoProviderSpecificActionRenderer_Exists` — NetArchTest rule asserting no type whose name matches `*ActionRenderer` lives outside the generic action-rendering namespace (AC-3).
- Optional: `Fixtures_Tf114_AllFilesReferencedByAtLeastOneTest` — discovery test asserting every new fixture under `TestData/` is consumed by at least one test (AC-11).
- Update `docs/features.md` to list this feature with a link to `docs/features/122-terraform-1-15-support/`. Update any other top-level feature index that exists (AC-13).
- Final pass through ADR-004 § Implementation notes confirming the `outputs[*].type` rendering decision is documented in the rendered baseline (Task 13 deliverable).

**Done when:**
- All cross-cutting tests pass; `docs/features.md` shows the new entry; `dotnet test` is fully green.

**Dependencies:** Tasks 11, 13.

## Implementation Order

Recommended sequential order — each task can be implemented and tested in isolation, and the build remains green after every commit:

1. **Task 1** — parsing records + JsonContext (foundation; no behaviour change).
2. **Task 2** — `TerraformPlan` extension + F-60 baseline (foundation; absence-tested).
3. **Task 3** — `ConfigurationDeprecationReader` helper (independent, unit-tested in isolation).
4. **Task 4** — H2 model + builder population (silent until renderers wire in).
5. **Task 5** — plan-status banner (smallest renderer, easiest to verify).
6. **Task 6** — drift section.
7. **Task 7** — relevant-attributes section.
8. **Task 8** — wire H2 sections + reserve `RenderOtherActions` placeholder.
9. **Task 9** — action models + builder attachment + orphan routing (no rendering yet).
10. **Task 10** — generic action renderer (unit-tested standalone).
11. **Task 11** — wire actions inline + Other Actions H2 (replaces Task 8 stub).
12. **Task 12** — extend `CodeAnalysisWarningModel` (additive, no behaviour change).
13. **Task 13** — emit deprecation warnings + rename H3 + output `type`.
14. **Task 14** — TF 1.13 baseline snapshot + global snapshot rebaseline (`SNAPSHOT_UPDATE_OK`).
15. **Task 15** — performance test.
16. **Task 16** — CLI / architecture rule / docs / fixture inventory.

## Open Questions

None. All architectural decisions are locked in ADR-001..ADR-004; all fixtures and test methods are specified in the test plan; the snapshot-update protocol is documented and referenced in Task 14.
