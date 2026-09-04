# Tasks: Core Report Pipeline and Provider Refactoring

## Overview

Implement the three top-priority refactorings from the refactoring opportunities review without
changing user-facing behavior:

1. Extract explicit report-generation stages from `ReportModelBuilder`
2. Narrow the provider integration surface through a provider contribution model
3. Remove static mutable state from Azure role definition resolution

## Task 1: Introduce an instance-based role definition resolver

**Priority:** High

**Description:**
Create a run-scoped role definition resolver service and migrate existing consumers away from the
static mutable `AzureRoleDefinitionMapper` state.

**Acceptance Criteria:**

- [x] A new role definition resolver abstraction exists as an injected service
- [x] Built-in role definitions remain immutable and reusable
- [x] Custom role mappings are held per application run, not in static mutable state
- [x] Diagnostics associated with role resolution are scoped to the current run
- [x] Existing role-resolution behavior is preserved
- [x] Existing tests for role resolution continue to pass or are updated to validate the new
      service boundary

**Dependencies:** None

**Status:** Completed 2026-03-06

---

## Task 2: Extract explicit report-generation stages behind `ReportModelBuilder`

**Priority:** High

**Description:**
Refactor `ReportModelBuilder` so that distinct phases of report generation are implemented by
separate stage components, with `ReportModelBuilder` acting as a façade or coordinator.

**Acceptance Criteria:**

- [x] At least the major current phases are represented as explicit stage abstractions
- [x] `ReportModelBuilder` no longer directly owns all transformation logic across all phases
- [x] Parent-child merging remains behaviorally identical
- [x] Summary and filtered-resource calculations remain behaviorally identical
- [x] Snapshot or parity tests confirm unchanged Markdown output
- [x] Stage components can be tested in narrower units than the current builder allows

**Dependencies:** Task 1 recommended first, but not strictly required

**Status:** Completed 2026-03-06

---

## Task 3: Introduce a provider contribution model

**Priority:** High

**Description:**
Replace the current broad provider module fan-out model with a narrower provider contribution
shape that can be consumed centrally by the registry and composition root.

**Acceptance Criteria:**

- [x] Providers contribute capabilities through a narrower architectural contract than the current
      broad interface
- [x] `ProviderRegistry` no longer needs one fan-out method per capability area in its final form
- [x] `CompositionRoot` consumes provider contributions centrally rather than manually coordinating
      as many capability-specific registries
- [x] Existing provider-specific behavior remains unchanged
- [x] Provider registration remains explicit and AOT-safe

**Dependencies:** Task 2 preferred, since extracted pipeline stages make provider boundaries easier
to define cleanly

**Status:** Completed 2026-03-06

---

## Task 4: Remove compatibility scaffolding and simplify composition

**Priority:** Medium

**Description:**
After the new resolver, pipeline stages, and provider contribution model are in place, remove any
temporary adapters or bridging code used during migration.

**Acceptance Criteria:**

- [x] Temporary compatibility shims for the static role mapper are removed
- [x] Legacy provider registration paths are removed if replaced by the new contribution model
- [x] `CompositionRoot` is simpler and no longer mutates global state during startup
- [x] Architectural suppressions that are no longer needed are removed where practical

**Dependencies:** Tasks 1-3

**Status:** Completed 2026-03-07

---

## Task 5: Verification and documentation alignment

**Priority:** High

**Description:**
Validate that behavior remains unchanged and ensure repository documentation reflects the new
architecture.

**Acceptance Criteria:**

- [x] Relevant tests pass for report generation, provider behavior, and role resolution
- [x] Snapshot updates, if any, are intentional and explained
- [x] Architecture documentation is updated to reflect the new report pipeline and provider model
- [x] `docs/features.md` is updated if the repository’s feature index should mention this work

**Dependencies:** Tasks 1-4

**Status:** Completed 2026-03-07

## Recommended Order

1. Task 1: instance-based role definition resolver
2. Task 2: extract report-generation stages
3. Task 3: provider contribution model
4. Task 4: cleanup and removal of migration scaffolding
5. Task 5: verification and documentation alignment

## Notes

- Task 1 is first because it removes a correctness risk and simplifies later composition work.
- Task 2 comes before Task 3 because a clear pipeline makes it easier to define where provider
  contributions should attach.
- Task 3 completed as a direct migration to narrow provider capability interfaces; no adapter
      layer remains.
- `docs/features.md` remains unchanged because it documents user-facing features, and this work is
      intentionally internal-only.

## Follow-up Tasks: Remaining Refactoring Work

### Task 6: Extract remaining builder-owned pipeline stages

**Priority:** High

**Description:**
Complete the staged-pipeline refactoring by moving the remaining builder-owned orchestration logic
for parent-child consolidation and code-analysis enrichment out of `ReportModelBuilder` and into
 explicit stage components. Keep `ReportModelBuilder` as a thin coordinator that owns sequencing
 and shared run-scoped dependencies, not the underlying transformation logic.

**Acceptance Criteria:**

- [ ] Explicit stage abstractions exist for parent-child consolidation and code-analysis
      enrichment.
- [ ] `ReportModelBuilder.Build()` coordinates those stages rather than implementing their core
      logic directly.
- [ ] Parent-child merge behavior, callback invocation order, and child-group output remain
      behaviorally identical.
- [ ] Code-analysis finding attachment, ordering, and summary generation remain behaviorally
      identical.
- [ ] Existing snapshot coverage continues to pass without intentional snapshot baseline changes.
- [ ] New stage-level tests verify the extracted stages independently from `ReportModelBuilder`.
- [ ] Builder orchestration tests verify stage ordering and preserve the pre-merge summary /
      post-merge display-filtering lifecycle.

**Dependencies:** None

**Notes:**
- Start with parent-child consolidation because it is the largest remaining behavior block inside
  `ReportModelBuilder`.
- Keep output-model construction out of scope for this task unless the implementation naturally
  reduces coupling without widening stage dependencies.
- Preserve the current provider callback extension point during extraction; replacing it is a
  separate design decision.

---

### Task 7: Split diagnostics collection from diagnostics rendering

**Priority:** High

**Status:** Completed 2026-03-07

**Description:**
Refactor the diagnostics model so components emit typed diagnostic events into a sink boundary,
while markdown formatting moves into a dedicated formatter. Remove the current coupling where
`DiagnosticContext` serves as mutable storage and markdown renderer at the same time.

**Acceptance Criteria:**

- [x] A typed diagnostic sink abstraction exists for recording diagnostic events without exposing
      internal mutable collections to callers.
- [x] Diagnostic event/state models are represented separately from markdown formatting logic.
- [x] Markdown debug output is generated by a dedicated formatter type rather than by the mutable
      collection object itself.
- [x] Existing producers that record failed resolutions, template resolutions, and mapping-load
      diagnostics are migrated to the new sink boundary.
- [x] `ProgramEntry` and any other final output assembly paths use the dedicated formatter for
      debug-section emission.
- [x] Existing debug markdown remains behaviorally unchanged unless explicitly documented.
- [x] Tests cover both event collection semantics and markdown formatting semantics separately.

**Dependencies:** Task 6

**Notes:**
- Preserve the current `--debug` behavior and the shape of the emitted markdown section.
- Prefer an append-only event model over exposing mutable lists for direct caller mutation.
- Keep this task scoped to debug/report diagnostics; do not expand into general logging or
  telemetry infrastructure.

---

### Task 8: Separate render policy from markdown emission

**Priority:** High

**Status:** Completed 2026-03-07

**Description:**
Decompose the rendering layer so policy decisions and data preparation are computed before markdown
 is emitted. Start with the densest rendering paths, especially `AzApiBodyRenderer`, then apply
 the same pattern to `DefaultResourceRenderer` scenario logic where it materially reduces risk and
 improves testability.

**Acceptance Criteria:**

- [x] The AzApi rendering path separates comparison/grouping/sensitivity decisions from markdown
      table emission.
- [x] Render-ready intermediate models or policy results exist for the extracted AzApi path.
- [x] `DefaultResourceRenderer` no longer mixes all scenario-detection policy directly into the
      main `Render()` method where practical; named policy helpers or strategy objects own those
      decisions.
- [x] Markdown writers/renderers focus primarily on layout and emission rather than domain-policy
      branching.
- [x] Existing snapshot output remains unchanged unless an intentional compatibility adjustment is
      explicitly documented.
- [x] New unit tests validate transformation/policy behavior without relying solely on snapshot
      diffs.

**Dependencies:** Task 7

**Notes:**
- Execute this incrementally: complete AzApi first, then reassess whether `DefaultResourceRenderer`
  should be split by strategy object, view-model builder, or extracted policy helpers.
- Preserve compatibility quirks that are currently protected by snapshot tests; the goal is
  relocation and naming of policy, not silent behavior changes.

---

### Task 9: Align stale documentation with the current provider model

**Priority:** Medium

**Status:** Completed 2026-03-07

**Description:**
Remove or correct stale documentation that still describes the deleted `IProviderModule` model and
 other pre-refactor guidance. Ensure current, normative docs point contributors at `IProvider`
 plus optional capability interfaces, while historical feature docs remain clearly historical.

**Acceptance Criteria:**

- [x] Current guidance documents that describe active architecture or extension patterns no longer
      instruct contributors to use `IProviderModule`.
- [x] `docs/features.md` is updated to reflect explicit registration via `IProvider` plus optional
      capability interfaces.
- [x] `docs/adr-006-dependency-injection.md` is updated where it currently describes the obsolete
      provider contract.
- [x] Any additional active architecture docs that still present `IProviderModule` as current are
      corrected or cross-referenced to the newer provider model.
- [x] Historical feature/issue documents are either left untouched as historical records or
      annotated only where a stale statement could mislead future implementation work.
- [x] Documentation changes are limited to alignment work and do not introduce new architectural
      requirements beyond the implemented design.

**Dependencies:** Task 8

**Notes:**
- Prioritize living documents over archival feature history.
- Avoid broad rewriting of old feature documents unless they are still treated as normative by
  current contribution guidance.

## Follow-up Implementation Order

Recommended sequence for the remaining refactoring work:

1. Task 6 - completes the main pipeline story and removes the largest remaining concentration of
   hidden orchestration logic.
2. Task 7 - becomes cleaner once pipeline ownership is explicit and reduces cross-cutting mutable
   state before renderer work.
3. Task 8 - should build on the clearer diagnostic and pipeline boundaries so rendering policy can
   be isolated without compounding current coupling.
4. Task 9 - should come last because it documents the architecture that actually exists after the
   follow-up refactors settle.

## Follow-up Assumptions

- These tasks are intended as a continuation of the same internal refactoring initiative, not as a
  new user-facing feature.
- Behavior preservation remains the default; output or CLI changes are not part of this plan.
- The plan intentionally excludes the shared tool CLI consolidation item from the immediate next
  wave because it is lower architectural leverage than Tasks 6-9.
