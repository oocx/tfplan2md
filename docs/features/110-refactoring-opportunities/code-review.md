# Code Review: Refactoring the Core Report Pipeline and Provider Architecture

## Summary

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

## Next Steps

1. **Blocker**: Stage `ProviderContributionSet.cs` with `git add`
2. **Major**: Split `ReportModelBuilderRefactoringTests.cs` (844 lines → ≤300 each)
3. **Major**: Add missing test plan test cases or update test plan Definition of Done
4. **Major**: Update `docs/architecture.md` file tree and component descriptions
5. After rework: return to Code Reviewer for re-approval
