# Code Review: 148-compact-collection-diffs

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-10-16

## Summary

The round-4 rendering blocker is fixed: large collection updates now preserve absent, null, empty, unknown, and populated sides, and the oversized collection stage was split. However, large wholly-unknown transitions are still summarized as having zero changes, contradicting the specification, and authoritative CI evidence for the final branch is unavailable.

## Verification Results

The Developer recorded 1,685 passed, 0 failed, 0 skipped at commit 6d500b36, with CoverageEnforcer passing at 89.24% line coverage and 81.17% branch coverage. The final two commits are documentation-only. I could not run tests in the read-only sandbox. GitHub CI could not be verified because api.github.com was unreachable; the work protocol also states the branch had not been pushed and its CI remained unavailable. Snapshot changes are authorized by commit c7403b80 with a specific SNAPSHOT_UPDATE_OK justification. Demo artifacts were recorded as regenerated with metadata-only differences, and documentation lint counts were recorded as unchanged from main.

## Specification Compliance

C1: AttributePathComparer / AttributePathComparerTests. C2-C6: PrimitiveCollectionDiffer and CollectionElementDiffer / collection model, differ, and rendering tests. C7-C8: typed underlying keys and formatter annotations / CollectionNameAnnotationTests. C9: state modeling and two-sided large rendering / ElementlessCollectionTests and LargeCollectionStateTests. C10: whole and partial unknown modeling/rendering has tests, but the large-value summary violates the criterion and lacks a summary assertion. C11-C12: masking and typed scalar identity / sensitive model, rendering, drift, and mixed-kind tests. C13: primitive rejection plus natural ordering / model and comparer tests. C14-C18: CollectionDriftModel, ReportModelBuilder grouping, and drift selection / drift model, rendering, and consumer tests. C19: logical row and path counting / CollectionConsumerTests and drift tests. C20: provider-specific paths remain separate; regression snapshots are recorded as passing. C21-C26: opaque masked keys, per-side models, renderer guards, and show-sensitive path / MaskedElementDifferTests, SensitiveCollection*Tests, and MaskedValueInvarianceTests. C27-C28: collection-aware AttributeFilteringStage / CollectionAttributeFilterTests, MaskedCollectionFilterTests, and CaseOnlyCollectionEndToEndTests. All criteria have identified code and tests, but C10 remains behaviorally noncompliant in LargeAttributesSummary.

## What I Tried To Break

Audited natural path ordering; primitive detection; duplicate, insertion, deletion, and reorder matching; the LCS size fallback; mixed JSON types; absent/null/empty handling; whole and partial unknown states; whole and per-entry masking; masked-to-visible comparisons and secret invariance; case-only Azure ID suppression; drift grouping and selection; logical attribute counts; large-value rendering and summaries; provider-specific isolation; snapshot authorization; commit types; documentation updates; and file-size compliance. The large-value body now preserves both sides, but its summary reports a wholly unknown transition as zero changes.

## Issues Found

### Blockers

- **A wholly unknown large collection is reported as having zero changes** — `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/LargeValueSummary.cs:89`
  For a whole-collection unknown transition, BuildCollectionModel intentionally leaves every before occurrence Unchanged, supplies no after occurrences, and marks AfterState as Unknown. LargeAttributesSummary nevertheless derives CollectionChangedLines solely from Removed/Added statuses plus OrderChanged, producing “0 changes.” This contradicts the specification’s explicit rule that an unknown value is always a change and makes the summary disagree with the resource’s changed-attribute count. The Technical Writer reproduced this exact output. LargeCollectionStateTests verifies the Before/After blocks but never asserts the summary count. CountCollectionChanges must account for Unknown/IsUnresolved state, with a regression test for the rendered summary.

### Majors

- **Authoritative CI evidence is unavailable for the final branch** — `docs/features/148-compact-collection-diffs/work-protocol.md:170`
  The local evidence is specific and includes CoverageEnforcer output, but the Developer explicitly records that nothing was pushed and CI remained the authoritative outstanding check. GitHub status could not be queried during review because api.github.com was unreachable. Release readiness therefore lacks CI confirmation for tests, coverage, architecture checks, and repository validation.

## Decision

`VERDICT: REWORK`
