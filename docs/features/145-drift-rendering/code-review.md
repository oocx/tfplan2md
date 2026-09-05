# Code Review: 145-drift-rendering

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-09-05

## Summary

The drift modes, deterministic grouping, masking, escaping, documentation, snapshots,
and ordinary empty-state behavior are implemented coherently, and the full local test
and coverage checks pass. The branch is nevertheless not ready: enabling
`--show-unchanged-values` causes unchanged attributes to be emitted as drift groups,
which violates the changed-attribute contract and can recreate the report-size problem
this feature is intended to solve.

## Verification Results

- Fresh PR-validation-style TUnit run: 1,372 passed, 0 failed, 0 skipped.
- Fresh CoverageEnforcer result: 88.80% line coverage against 84.48%; 79.94%
  branch coverage against 72.80%; both pass without an override.
- The Developer's recorded totals and percentages match the fresh run.
- No GitHub PR exists for `feature/145-drift-rendering`, so no authoritative CI
  result is available for the reviewed revision.
- Commit `3ed79bf7` authorizes the three snapshot changes with
  `SNAPSHOT_UPDATE_OK` and a specific grouped-layout justification.
- The drift snapshots and `artifacts/drift-single-entry-plan.md` contain the new
  collapsed grouped rendering; `CHANGELOG.md` is untouched; documentation-only
  commits use non-version-bumping types.
- Markdownlint could not be run because its local executable/container image was
  unavailable. `git diff --check origin/main...HEAD` reports one trailing blank line
  in `uat-plan.md`.

## Specification Compliance

1. Matching type, path, before, and after values group at
   `ReportModelBuilder.PlanContext.cs:95-122`; ordinary matching candidates are tested.
2. Different transitions split through the complete tuple key at
   `ReportModelBuilder.PlanContext.cs:106`; type, path, and after differences are tested,
   but a before-only difference is not.
3. `DriftGroupModel` carries all required summary data and
   `ReportRenderer.cs:98-106` renders it; renderer tests cover ordinary values.
4. `ReportRenderer.cs:107-115` emits collapsed details and every address; two-address,
   single-address, ordering, and deduplication scenarios are covered.
5. `CliParser.cs` defaults to `All`, accepts all three values case-insensitively, and
   `CompositionRoot` propagates the selection. Parser tests cover modes, but do not
   assert that the positional plan path remains intact.
6. `Relevant` intersects displayable planned changes with ordinal address equality at
   `ReportModelBuilder.PlanContext.cs:80-89`; no-op and case-distinct addresses are tested.
7. `None` short-circuits at `ReportModelBuilder.PlanContext.cs:52-55`; empty renderer
   behavior proves omission of the complete section.
8. Selection precedes grouping at `ReportModelBuilder.PlanContext.cs:70-74` and the
   mixed-address mode test proves excluded addresses do not leak into a group.
9. Attribute and display filtering run before selection at
   `ReportModelBuilder.PlanContext.cs:63-68`; no-op and naturally empty attribute diffs
   are covered, but an actual provider/injected suppression stage is not.
10. Invalid and missing `--drift` values name `all`, `relevant`, and `none`; both paths
    are tested.
11. Absent, no-op, and empty drift omit the section in the existing model, renderer,
    and snapshot coverage.
12. The three modes, grouping, masking, escaping, ordering, deduplication, and ordinary
    filtering have automated coverage, but the unchanged-value failure and several
    explicitly planned proof points remain uncovered.

## What I Tried To Break

I checked omitted/all/relevant/none selection, selection-before-grouping, each grouping
key component, deterministic ordering, duplicate addresses, multiple paths, sensitive
masking, no-op and suppressed resources, ordinal case-distinct relevance, empty and
single-member output, unsafe HTML/backticks/CR/LF, snapshots, generated drift artifacts,
architecture boundaries, role entries, commit types, and CI availability. A direct CLI
reproduction with one changed and one stable field showed that
`--show-unchanged-values` renders both `changed: old → new` and the invalid
`stable: same → same` drift group.

## Issues Found

### Blockers

- **`--show-unchanged-values` renders unchanged attributes as drift** —
  `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.PlanContext.cs:98`
  `BuildResourceDrift` reuses a `ResourceChangeStage` configured with
  `_showUnchangedValues`, and `GroupDrift` flattens every retained
  `AttributeChangeModel` without knowing whether its raw values changed. A resource
  with one real drift field and many stable fields therefore emits `same → same`
  groups for every stable path. This violates the requirement to group changed
  attribute paths and can recreate the excessive output the feature is meant to
  prevent. Filtering by normalized before/after equality is not a safe fix because
  distinct sensitive raw transitions intentionally normalize to the same mask.

### Majors

- **Required acceptance branches lack proving tests** —
  `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderPlanContextTests.cs:147`
  The suite has no regression for `ShowUnchangedValues`, does not vary only the before
  component of the grouping key, and represents “fully suppressed” resources with
  equal raw values instead of exercising `IAttributeFilteringStage` or a provider
  filter. `CliParserTests.cs:21` also does not assert the preserved plan path, while
  `ReportRendererTests.cs:35` checks content but not the planned exact count of details
  and summary elements. These leave DRIFT-02, DRIFT-03, DRIFT-07, DRIFT-09, and CLI-01
  incompletely proved.

### Minors

- **Generated UAT markdown has trailing whitespace at EOF** —
  `docs/features/145-drift-rendering/uat-plan.md:47`
  `git diff --check origin/main...HEAD` reports a newly added blank line at EOF. This
  is formatting cleanup rather than a functional defect.

### Suggestions

None.

## Decision

`VERDICT: REWORK`
