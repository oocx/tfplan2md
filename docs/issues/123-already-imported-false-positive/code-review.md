# Code Review: Pending Import False-Positive Fix

## Summary

Reviewed the current bug-fix branch for issue 123 (`abf1b7e0`, `ec524808`, `5b765431`).

The core bug fix works: pending imports with `change.importing.id` plus `actions: ["no-op"]` now render as `✅ Ready` instead of `⚠️ Already imported`, and moved no-op resources still render as already moved.

**Review Decision:** ❌ **Changes Requested**

## Verification Results

- **Tests:** ✅ Pass — 1328 passed, 0 failed, 0 skipped
- **Build:** ✅ Success — 0 warnings, 0 errors
- **Manual rendering:** ✅ Verified
  - `no-op-import.json` renders pending import as `✅ Ready`
  - `refactoring-comprehensive.json` no longer shows `⚠️ Already imported`
  - `examples/comprehensive-demo/plan.json` still shows moved no-op resources as `already moved`
- **Docker:** ⚠️ Not fully verified — `docker build -f src/Dockerfile .` failed with `403 Forbidden` when resolving the pinned MCR base-image digest
- **CHANGELOG.md:** ✅ Not modified

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---|---|---|---|
| Pending imports must not be labeled `Already imported` from `no-op` alone | ✅ | ✅ | Verified with `src/tests/Oocx.TfPlan2Md.TUnit/TestData/no-op-import.json` and `ReportModelBuilderRefactoringOperationTests.cs:32-46` |
| Moved no-op resources may still be marked `Already moved` | ✅ | ✅ | Verified in `ReportModelBuilderRefactoringOperationTests.cs:48-102` and manual comprehensive-demo render |
| Import and move already-applied state must be tracked independently | ✅ | ✅ | Separate `IsImportAlreadyApplied` / `IsMoveAlreadyApplied` fields added in `ResourceChangeModel.cs:105-129` |
| Resource summary annotations inside `<summary>` must use HTML formatting from the spec/style guide | ❌ | ❌ | Current code/output still uses markdown emphasis (`*Imported*`, `*Moved from*`) instead of `<i>…</i>` |

**Spec Deviations Found:**
- `ResourceSummaryHtmlBuilder` still emits markdown italics inside HTML `<summary>` tags instead of the required `<i>` tags (`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs:155-206`, `docs/features/038-terraform-import-moved-blocks/specification.md:68-80`).

## Adversarial Testing

| Test Case | Result | Notes |
|---|---|---|
| Pending import with `no-op` action | Pass | Rendered as `📥 Imported` with `✅ Ready`, no warning |
| Moved no-op resource | Pass | Comprehensive demo still shows `(⚠️ already moved)` |
| Snapshot-backed rendering regression | Pass | `refactoring-comprehensive.md` now reflects `✅ Ready` for the pending import |
| HTML summary formatting required by spec | Fail | Output uses `*Imported*` / `*Moved from*`, not `<i>Imported</i>` / `<i>Moved from</i>` |
| Docker/container verification | Not fully verified | Build blocked by base-image registry `403 Forbidden`, appears infra-related |

## Review Decision

**Status:** ❌ **Changes Requested**

## Snapshot Changes

- Snapshot files changed: Yes
- Commit message token `SNAPSHOT_UPDATE_OK` present: No
- Why the snapshot diff is correct: The updated `refactoring-comprehensive.md` snapshot changes the pending import from `⚠️ Already imported` to `✅ Ready`, which matches the intended bug fix.

## Issues Found

### Blockers

1. **Snapshot update token is missing from the branch history**

   The branch changes `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/refactoring-comprehensive.md`, but neither `ec524808` nor `5b765431` includes the required `SNAPSHOT_UPDATE_OK` token in the commit message (`git log --oneline origin/main..HEAD`).

   Per repository workflow, intentional snapshot changes must carry that token so reviewers and release tooling can distinguish approved snapshot churn from hidden regressions.

### Major Issues

1. **Summary annotation formatting still violates the report spec/style guide**

   `ResourceSummaryHtmlBuilder` renders `📥 *Imported*`, `🔀 *Moved from*`, and `*already moved*` using markdown emphasis inside an HTML `<summary>` tag (`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs:155-206`). The updated spec requires HTML `<i>` tags inside `<summary>` (`docs/features/038-terraform-import-moved-blocks/specification.md:68-80`), and the current tests lock in the wrong output (`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ResourceSummaryHtmlBuilderRefactoringTests.cs:13-142`).

   This is not just a doc mismatch: the project’s style guide explicitly calls out HTML formatting requirements inside summary tags for cross-platform rendering consistency.

2. **The updated UAT plan is internally inconsistent**

   `docs/features/038-terraform-import-moved-blocks/uat-test-plan.md:26-37` says no-op changes should show `⚠️ Already imported/moved`, then immediately says pending no-op imports should remain `📥 Imported` without a warning.

   That contradiction will send UAT in two different directions and leaves the documentation set misaligned even after the technical-writer follow-up.

### Minor Issues

None.

### Suggestions

1. Once the `<i>` formatting is corrected, add a direct end-to-end rendering assertion (snapshot or renderer test) that checks the exact `<summary>` HTML, not just model flags, so this formatting rule cannot regress silently again.

## Critical Questions Answered

- **What could make this code fail?** The bug could regress if a future change reintroduces shared import/move state or revives the `no-op => already imported` heuristic. The new split flags reduce that risk.
- **What edge cases might not be handled?** There is still no positive Terraform signal for “already imported,” so imports are effectively always treated as ready. That is safer than the false positive, but it means any future true-already-imported state still needs an explicit discriminator from Terraform metadata.
- **Are all error paths tested?** The primary regression path is tested well. Docker/container verification was not completed because the pinned base image could not be resolved from MCR in this environment.

## Checklist Summary

| Category | Status |
|---|---|
| Correctness | ✅ |
| Spec Compliance | ❌ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ❌ |
| Work Protocol | ✅ |

## Work Protocol & Documentation Verification

| Item | Status | Notes |
|---|---|---|
| `work-protocol.md` exists | ✅ | `docs/issues/123-already-imported-false-positive/work-protocol.md` present |
| Issue Analyst logged | ✅ | Present |
| Developer logged | ✅ | Present |
| Technical Writer logged | ✅ | Present |
| Code Reviewer logged | ✅ | This review |
| `docs/features.md` update needed | N/A | This fix changes refactoring-import edge-case behavior already documented in feature 038 docs |
| `docs/architecture.md` update needed | N/A | No global architecture change |
| `docs/testing-strategy.md` update needed | N/A | No new test framework/pattern |
| `README.md` update needed | N/A | No CLI/usage change |
| `docs/agents.md` update needed | N/A | No workflow change |
| Directly related docs aligned | ❌ | `uat-test-plan.md` contradicts itself; spec/example formatting and implementation also diverge |
| CHANGELOG.md not modified | ✅ | Confirmed |

## Next Steps

1. Developer should add a follow-up commit with `SNAPSHOT_UPDATE_OK` covering the intentional snapshot change.
2. Developer should update `ResourceSummaryHtmlBuilder` and its tests/snapshots to use `<i>` tags inside `<summary>` annotations.
3. Technical Writer (or Developer if explicitly asked to repair docs in rework) should fix the contradictory UAT wording in `docs/features/038-terraform-import-moved-blocks/uat-test-plan.md`.
4. Return to **Code Reviewer** for re-review after those changes.
