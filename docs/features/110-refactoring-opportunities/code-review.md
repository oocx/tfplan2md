# Code Review: Refactoring the Core Report Pipeline and Provider Architecture

## Review 3 Summary (2026-03-07)

This review covers two things: (1) confirming all issues from Review 2 (Changes Requested) are
resolved, and (2) reviewing the Tasks 6–9 scope additions against the updated test plan and
tasks.md.

**Key finding:** All three Review-2 issues are resolved. However, the entire implementation of
Tasks 7, 8, and 9 (12+ files) plus three documentation updates for Task 9 are **not committed to
git** — they exist only in the working tree. This is a Blocker. Additionally, Task 6 is not yet
implemented, and several TC-30–TC-48 test cases specified as "Must be added" in the test plan are
absent.

All 1174 tests pass from the working tree. Build succeeds with zero errors. Comprehensive demo
generates clean Markdown (0 markdownlint errors). No snapshot data changes.

---

## Review 2 Issue Resolution

| Issue | Status | Notes |
|-------|--------|-------|
| **Blocker: Untracked `ProviderContributionSet.cs`** | ✅ Resolved | File is now tracked |
| **Major: `ReportModelBuilderRefactoringTests.cs` (844 lines)** | ✅ Resolved | Replaced by three files each ≤284 lines |
| **Major: Missing TC-18, TC-19, TC-23, TC-24, TC-25, TC-26** | ✅ Resolved | All six test cases now exist |
| **Major: `docs/architecture.md` not updated** | ✅ Resolved | `Stages/`, diagnostics decomposition, and `IProvider` model are documented |

---

## Original Summary (Review 2 — 2026-03-06)

Reviewed the implementation of feature 110 — an internal architectural refactoring decomposing
`ReportModelBuilder` into explicit pipeline stages, replacing `IProviderModule` with narrow
capability interfaces, and removing static mutable state from Azure role definition resolution.
The implementation is structurally sound and all 1161 tests pass. The refactoring preserves
rendered Markdown output as verified by snapshot tests and comprehensive demo generation. One
blocker (untracked file) must be addressed before merging.

## Verification Results

- Tests: **Pass** (1161 succeeded, 0 failed, 0 skipped)
- Coverage: Not explicitly measured in this review (existing CI thresholds apply)
- Build: **Success** (dotnet build passes; no compile errors)
- Docker: **Pre-existing failure** — `docker build -t tfplan2md:local -f src/Dockerfile src/`
  fails on `main` with `MSB1009: Project file does not exist` due to incorrect path in
  Dockerfile. Not caused by this feature.
- Errors: **None** in changed files
- Comprehensive demo: **Regenerated and passes markdownlint** (0 errors)
- Snapshots: **No snapshot data changes** — no `SNAPSHOT_UPDATE_OK` token required

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| FR-1: Explicit report-generation stages | ✅ | ✅ | 5 stages extracted: ResourceChange, AttributeFiltering, SummaryEnrichment, DisplayFiltering, ReportAssembly |
| FR-2: Preserved output behavior | ✅ | ✅ | All snapshot tests pass; comprehensive demo unchanged |
| FR-3: Narrower provider integration boundary | ✅ | ✅ | `IProvider` + 6 optional capability interfaces replace `IProviderModule` |
| FR-4: Instance-based role resolution | ✅ | ✅ | `IRoleDefinitionResolver` / `AzureRoleDefinitionResolver` with run-scoped custom roles |
| FR-5: Composition-root compatibility | ✅ | ✅ | Pure DI maintained; `CompositionRoot` registers via `ProviderRegistry` → `ProviderContributionSet` |
| No CLI/behavior changes | ✅ | ✅ | Internal-only refactoring |
| No snapshot baseline updates required | ✅ | ✅ | Zero snapshot data file changes |

**Spec Deviations Found:**

- The architecture described 6 stages (including `IParentChildMergeStage` and
  `ICodeAnalysisEnrichmentStage`), but the implementation extracted 5 stages. Parent-child merging
  and code analysis remain in `ReportModelBuilder`. The specification was updated post-implementation
  to reflect the actual design, which is reasonable since the spec says "at least the major current
  phases."
- The test plan lists 6 test cases as "Must be added" (TC-18, TC-19, TC-23, TC-24, TC-25, TC-26)
  that do not have matching dedicated tests. Some are partially covered by existing composition and
  snapshot tests, but the explicit structural/architectural tests are missing. See **Major Issues**.

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty input | Pass | `ResourceChangeStage` handles empty plan |
| Null values | Pass | Role resolver handles null IDs gracefully |
| Special characters | N/A | Internal refactoring; inputs unchanged |
| Very large input | Pass | Existing comprehensive demo plan exercises large input |
| Error conditions | Pass | Role resolution fallback paths tested |
| Two sequential compositions | Not directly tested | TC-25 test missing (see Major Issues) |

## Review Decision

**Status:** Changes Requested

## Snapshot Changes

- Snapshot data files changed: **No**
- Commit message token `SNAPSHOT_UPDATE_OK` present: **N/A** (not needed)
- No snapshot data changes occurred; the refactoring correctly preserved rendered output.

## Issues Found

### Blockers

1. **Untracked `ProviderContributionSet.cs`** — The file
   `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderContributionSet.cs` is untracked
   (`??` in git status). This is a critical new file that the entire provider contribution model
   depends on. It will not be included in any commit or PR unless explicitly staged with
   `git add`. **Fix:** Stage the file before committing.

### Major Issues

1. **`ReportModelBuilderRefactoringTests.cs` is 844 lines** — This test file is nearly 3x the
   300-line guideline. It contains 20 test methods spanning refactoring operations, action
   classification, and stage delegation tests. **Fix:** Split into at least two files — one for
   action/refactoring behavior tests and one for stage delegation/injection tests.

2. **Missing test plan test cases** — The test plan specifies several "Must be added" test cases
   that are not present:
   - **TC-18** (`ProviderContribution_AllCapabilityLists_AreNonNull`) — No dedicated test
     verifying that each provider's contribution has non-null capability collections.
   - **TC-19** (`ProviderRegistry_RegisterContribution_RegistersAllCapabilityTypesAtOnce`) — No
     dedicated test verifying centralized contribution registration.
   - **TC-23** (`ProviderContribution_Registration_UsesExplicitStaticTypes`) — No structural test
     guarding against reflection-based registration.
   - **TC-24** (`AfterCleanup_NoProviderModule_HasMutableStaticFields`) — No structural test
     verifying `IProviderModule` no longer exists in the assembly. The
     `AzureRoleDefinitionResolver_HasNoMutableStaticFields` test exists but doesn't cover
     `IProviderModule` removal or provider module types.
   - **TC-25** (`CompositionRoot_Startup_DoesNotMutateGlobalState`) — No test verifying two
     sequential `CompositionRoot` compositions produce identical results.
   - **TC-26** (`Architecture_RefactoringDoesNotIncreaseExemptionCount`) — No boundary test
     verifying exemption count.

   **Fix:** Either add the missing tests or update the test plan's Definition of Done to
   explicitly mark which tests are deferred (and why). The test plan currently lists all as
   required.

3. **Global `docs/architecture.md` not updated** — The global architecture document still
   references `IProviderModule.cs` at line 289 and the file tree (lines 217–222) does not include
   the new `Stages/` directory or `ProviderContributionSet.cs`. Since the refactoring fundamentally
   changes the report pipeline architecture and provider model, `docs/architecture.md` should be
   updated. **Fix:** Update the file tree and component descriptions to reflect the new pipeline
   stages, `ProviderContributionSet`, and the removal of `IProviderModule`.

### Minor Issues

1. **`ResourceChangeStage.cs` exceeds 300-line limit** — At 335 lines, the file is slightly over
   the project guideline. It's already split as a partial class, and the excess is modest.
   **Fix:** Consider moving the `BuildAttributeChanges` method (55 lines) or the
   `ApplyComputedKnownAfterApplyOverride` method into the helpers partial to bring the main file
   under limit.

2. **Duplicated action constants and `GetActionSymbol`** — The action constants (`CreateAction`,
   `DeleteAction`, etc.) are defined in three locations:
   - `ReportModelBuilder.ResourceChanges.cs`
   - `ResourceChangeStage.cs`
   - `SummaryEnrichmentStage.cs`

   Additionally, `GetActionSymbol` is duplicated between `ReportModelBuilder.ResourceChanges.cs`
   and `ResourceChangeStage.Helpers.cs`. **Fix:** Consider extracting shared action constants and
   `GetActionSymbol` into a shared helper or constants type.

3. **Unrelated agent model version changes** — Two `.github/agents/` files
   (`architect.agent.md`, `developer.agent.md`) have model version bumps (`GPT-5.2` → `GPT-5.4`,
   `GPT-5.3-Codex` → `GPT-5.4`) that are unrelated to feature 110 and should be in a separate
   commit/PR.

### Suggestions

1. **`ReportModelBuilder` constructor has 21 parameters** — The primary constructor, even with
   the `#pragma warning disable S107` suppression, is unwieldy. Consider grouping related options
   into a configuration record (e.g., `ReportBuildOptions`) in a follow-up.

2. **`ReportModelBuilder.Build` uses `?? CreateXxxStage()` pattern** — The null-coalescing
   fallback pattern for stages (`(_resourceChangeStage ?? CreateResourceChangeStage()).Build(...)`)
   is pragmatic for migration but creates the stages fresh on each `Build` call when no override is
   injected. In a follow-up, consider injecting the stages at construction time via
   `CompositionRoot` so the builder doesn't internally know how to create stages.

## Work Protocol & Documentation Verification

### Agent Work Log Verification

| Required Agent | Entry Present | Notes |
|---------------|---------------|-------|
| Requirements Engineer | ❌ Missing | Feature originated from Code Reviewer structural review, not a requirements gathering phase |
| Architect | ✅ | Designed target architecture |
| Task Planner | ✅ | Produced task breakdown |
| Developer | ✅ (7 entries) | Implemented all 5 tasks incrementally |
| Technical Writer | ✅ | Produced specification |
| Quality Engineer | ✅ | Produced test plan |
| Code Reviewer | ✅ (initial review) | Structural review that initiated the feature |
| UAT Tester | N/A | Internal-only, no user-facing changes |
| Release Manager | ⏳ Expected after approval | |

**Finding:** The Requirements Engineer entry is missing. This is partially explained by the
feature's origin as an internal review finding rather than a user requirement. However, per the
required agents table, this is technically required for Feature workflows. **Severity: Minor** — the
Technical Writer's specification effectively covers the requirements engineering role for this
internal-only feature.

### Global Documentation

| Document | Check | Status |
|----------|-------|--------|
| `docs/architecture.md` | Updated for new components/patterns | ❌ **Not updated** — still references `IProviderModule`, missing `Stages/` directory |
| `docs/features.md` | Updated with feature descriptions | ✅ N/A — intentionally skipped (internal-only, per specification) |
| `docs/testing-strategy.md` | Updated for new test approaches | ✅ N/A — no new test patterns introduced |
| `README.md` | Updated for usage/CLI changes | ✅ N/A — no CLI changes |
| `docs/agents.md` | Updated for workflow changes | ✅ N/A — no workflow changes |

## Critical Questions Answered

- **What could make this code fail?** The null-coalescing stage creation pattern in `Build` could
  mask stage initialization failures silently. If a stage constructor throws, the exception would
  surface on first `Build` call rather than at composition time. This is an existing pattern risk,
  not a new one introduced by the refactoring.

- **What edge cases might not be handled?** The `ProviderContributionSet` was not found in git
  tracked files — if this file is lost, the entire contribution model breaks. Additionally, the
  duplicated action constants could diverge if one location is updated without the others.

- **Are all error paths tested?** Role resolution error paths are well tested (null IDs, unknown
  IDs, custom role isolation). Stage error paths for empty/null inputs are covered at the
  `ResourceChangeStage` level but not all stages have explicit empty-input tests.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ⚠️ (file size limits, code duplication) |
| Architecture | ✅ |
| Testing | ⚠️ (missing test plan test cases) |
| Documentation | ❌ (global `docs/architecture.md` outdated) |

## Review 2 Next Steps (superseded — all resolved)

1. ~~**Blocker**: Stage `ProviderContributionSet.cs` with `git add`~~
2. ~~**Major**: Split `ReportModelBuilderRefactoringTests.cs` (844 lines → ≤300 each)~~
3. ~~**Major**: Add missing test plan test cases or update test plan Definition of Done~~
4. ~~**Major**: Update `docs/architecture.md` file tree and component descriptions~~

---

## Review 3 — Verification Results (2026-03-07)

- Tests: **Pass** (1174 succeeded, 0 failed, 0 skipped — includes uncommitted working-tree changes)
- Build: **Success** — 0 errors, 0 warnings
- Markdownlint: **0 errors** on `artifacts/comprehensive-demo.md`
- Snapshots: **No snapshot data changes** — `SNAPSHOT_UPDATE_OK` not required
- Docker: Not re-verified (pre-existing issue on `main` unrelated to this feature)

---

## Review 3 — Specification Compliance

### Tasks 1–5

All Tasks 1–5 acceptance criteria remain satisfied. Review 2 issues are fully resolved.
No regressions introduced.

### Task 6 — Extract remaining builder-owned pipeline stages

**Finding:** Task 6 is **not implemented**. The `tasks.md` acceptance criteria still have `[ ]`
checkboxes. No `IParentChildConsolidationStage` or `ICodeAnalysisEnrichmentStage` types exist.
TC-30–TC-35 are in the test plan as "Must be added" with no corresponding tests. This is
consistent with the tasks.md listing Task 6 without a `**Status: Completed**` marker; it is
genuinely open work and is expected to be addressed in a follow-up.

### Tasks 7, 8, 9 — Implemented but NOT committed

Implementation is complete in the working tree and all 1174 tests pass. However, 6 new source
files are untracked and 16+ tracked source/test/doc files are modified but uncommitted.

| Acceptance Criterion | Implemented | Committed |
|---------------------|-------------|-----------|
| `IDiagnosticSink` typed append-only abstraction | ✅ | ❌ |
| `DiagnosticReport` immutable snapshot | ✅ | ❌ |
| `DiagnosticMarkdownFormatter` dedicated formatter | ✅ | ❌ |
| Producers migrated to sink boundary | ✅ | ❌ |
| `ProgramEntry` uses formatter (not `GenerateMarkdownSection`) | ✅ | ❌ |
| Debug markdown behaviorally unchanged | ✅ | ❌ |
| `AzApiBodyRenderPlanner` separates AzApi policy from emission | ✅ | ❌ |
| `DefaultResourceRenderPolicy` delegates DefaultResourceRenderer scenario detection | ✅ | ❌ |
| Active docs no longer instruct `IProviderModule` | ✅ | ❌ |
| `docs/features.md` updated with `IProvider` model | ✅ | ❌ |
| `docs/adr-006-dependency-injection.md` updated | ✅ | ❌ |

---

## Review 3 — Review Decision

**Status: Changes Requested**

---

## Review 3 — Issues Found

### Blockers

**B-1: All Tasks 7, 8, and 9 implementation is not committed to git.**

The following new source files are **untracked** (`??` in `git status`) and will be excluded
from any commit or PR:
- `src/Oocx.TfPlan2Md/Diagnostics/IDiagnosticSink.cs`
- `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticReport.cs`
- `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticMarkdownFormatter.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderPolicy.cs`
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/AzApiBodyRenderPlanner.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/AzApiBodyRenderPlannerTests.cs`

The following **modified tracked files** for Tasks 7–9 are also uncommitted:
`DiagnosticContext.cs`, `ProgramEntry.cs`, `MarkdownRenderer.cs`,
`DefaultResourceRenderer.cs`, `GlobalSuppressions.cs`, `AzureEntityMapper.cs`,
`AzureMappingFileLoader.cs`, `AzureMappingFileParser.cs`, `AzureRoleDefinitionResolver.cs`,
`PrincipalMapper.cs`, `AzApiBodyRenderer.cs`, `AzdoGroupMapper.cs`, `AzdoProjectMapper.cs`,
`AzdoRepositoryMapper.cs`, `AzdoUserMapper.cs`, `DiagnosticContextTests.cs`,
`DebugOutputIntegrationTests.cs`, `DefaultResourceRendererScenarioTests.cs`,
`PrincipalMapperDiagnosticsTests.cs`, `docs/architecture.md`, `docs/features.md`,
`docs/adr-006-dependency-injection.md`, `tasks.md`, `work-protocol.md`.

**Fix:** Stage all untracked files and commit all modified working-tree files with separate
commit messages for Tasks 7, 8, and 9 before requesting re-review.

---

### Major Issues

**M-1: Missing test cases TC-36, TC-37, TC-42, TC-45, TC-46, TC-47, TC-48.**

These are all listed as "Must be added" in the test plan. None exist in the committed code and
only some have partial coverage in the uncommitted working tree:

| TC | Description | Status |
|----|-------------|--------|
| TC-36 | `DiagnosticSink_Append_RecordsEventWithoutExposingMutableCollection` — direct `IDiagnosticSink` interface-level test | ❌ Missing |
| TC-37 | `DiagnosticEventTypes_DoNotContainMarkdownGenerationLogic` — structural NetArchTest/reflection test | ❌ Missing |
| TC-42 | `AzApiRenderModel_AllPolicyCapturedBeforeEmission` — no policy branching after first write | ❌ Missing |
| TC-45 | `AzApiBodyComparisonPolicy_Evaluate_*` — parametrized AzApi comparison scenario matrix | ❌ Missing |
| TC-46 | `DocumentationFiles_DoNotReferToIProviderModuleAsActiveContract` | ❌ Missing |
| TC-47 | `Adr006_DescribesIProvider_NotIProviderModule` | ❌ Missing |
| TC-48 | `ActiveDocumentationFiles_DoNotInstructImplementingIProviderModule` | ❌ Missing |

TC-38 and TC-39 have partial coverage through `DiagnosticContextTests.Format_*` and
`DebugOutputIntegrationTests` but lack the explicit structural guard (TC-39a) that
`ProgramEntry` no longer calls `GenerateMarkdownSection()`. Since that method was completely
removed, the structural test could be simplified to "method does not exist in production
assembly."

**Fix:** Add the missing test cases, or explicitly mark them as deferred in the test plan with
justification and a follow-up tracking issue.

**M-2: Four new/modified files exceed the 300-line guideline.**

| File | Lines | Limit | Excess |
|------|-------|-------|--------|
| `AzApiBodyRenderPlanner.cs` | 630 | 300 | +330 |
| `DefaultResourceRenderer.cs` | 465 | 300 | +165 |
| `AzApiBodyRenderer.cs` | 393 | 300 | +93 |
| `DiagnosticMarkdownFormatter.cs` | 387 | 300 | +87 |

`AzApiBodyRenderPlanner.cs` at 630 lines is the most severe — its `BuildUpdatePlan` path
alone warrants a helpers partial (similar to `ResourceChangeStage.Helpers.cs`).
`DefaultResourceRenderer.cs` may now be reducible now that policy logic moved to
`DefaultResourceRenderPolicy`.

**Fix:** Split `AzApiBodyRenderPlanner.cs` into `AzApiBodyRenderPlanner.cs` +
`AzApiBodyRenderPlanner.Helpers.cs`. Reassess `DefaultResourceRenderer.cs` to see if the
policy extraction already eliminates enough lines to reach the limit.

**M-3: Work protocol not updated for Tasks 7–9 implementation work.**

Developer Entry 7 only documents Task 5 verification. Tasks 7–9 are marked 
`Status: Completed 2026-03-07` in `tasks.md` but there is no work-protocol entry for that work.

**Fix:** Add Developer Entry 8 to `work-protocol.md` documenting the implementation of Tasks
7, 8, and 9 once the code is committed.

---

### Minor Issues

**m-1: Two test files are slightly over the 300-line limit.**

| File | Lines |
|------|-------|
| `DiagnosticContextTests.cs` | 355 |
| `DefaultResourceRendererScenarioTests.cs` | 317 |

These are modest overages. Moving the snapshot-immutability test from `DiagnosticContextTests`
to a dedcated file would likely bring it under limit.

**m-2: Duplicate action constants still present.**

The `CreateAction`, `DeleteAction`, etc. constants and `GetActionSymbol` remain duplicated
across `ReportModelBuilder.ResourceChanges.cs`, `ResourceChangeStage.cs`, and
`SummaryEnrichmentStage.cs`. (from Review 2 — not escalated since `TerraformActions.cs` was
added in the feature commit but the other copies were not removed.)

**Fix:** Consolidate by using `TerraformActions` constants from all three locations and removing
the local copies.

---

## Review 3 — Critical Questions

- **What could make this code fail?** If the 6 untracked files are not staged before the next
  commit, Tasks 7–9 will be silently absent from the PR. This is the highest-risk issue.
- **What edge cases might not be handled?** The `AzApiBodyRenderPlanner`'s nested sensitivity
  masking logic at 630 lines warrants deeper unit coverage than the three tests in
  `AzApiBodyRenderPlannerTests.cs`. TC-45's parametrized scenario matrix is specifically designed
  to catch policy regressions there.
- **Are all error paths tested?** Role resolution and debug formatter error paths are well tested.
  AzApi render planner with empty body JSON, or missing before/after, is not explicitly tested.

---

## Review 3 — Checklist Summary

| Category | Tasks 1–5 | Tasks 7–9 (Working Tree) |
|----------|-----------|--------------------------|
| Correctness | ✅ | ✅ |
| Spec Compliance | ✅ | ⚠️ (TC-36, TC-37, TC-42, TC-45, TC-46–48 missing) |
| Code Quality | ✅ | ⚠️ (file size violations in 4 files) |
| Architecture | ✅ | ✅ |
| Testing | ✅ | ⚠️ (7 required test cases absent) |
| Documentation | ✅ | ⚠️ (uncommitted) |
| Committed to git | ✅ | ❌ |

---

## Review 3 — Next Steps

1. **Blocker B-1**: Stage all untracked files and commit all modified working-tree files for
   Tasks 7, 8, 9, and documentation updates.
2. **Major M-1**: Add (or explicitly defer with justification) missing test cases TC-36, TC-37,
   TC-42, TC-45, TC-46, TC-47, TC-48.
3. **Major M-2**: Split `AzApiBodyRenderPlanner.cs` (630 lines) into main + helpers file.
4. **Major M-3**: Add Developer Entry 8 to `work-protocol.md`.
5. After rework: return to Code Reviewer for re-approval.
