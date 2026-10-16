# Code Review: 148-compact-collection-diffs

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-10-16

## Summary

The round-5 blocker is correctly addressed: large collections that become wholly unknown, or contain unknown elements, now report a nonzero change count, with focused regression tests. No additional correctness defect was found in the reviewed round-6 fix. Release readiness remains unproven because current-HEAD CI and coverage evidence are unavailable.

## Verification Results

The Developer recorded 1,687 passed, 0 failed, 0 skipped for round 6. The two new summary tests reportedly failed before the fix and passed afterward. CoverageEnforcer passed on the earlier commit 6d500b36 at 89.24% line and 81.17% branch coverage, but coverage was not rerun after the round-6 production-code change. GitHub CI could not be queried because api.github.com was unreachable, and the work protocol explicitly says current-HEAD CI remains outstanding. The sandbox did not permit running tests. git diff --check passed; CHANGELOG.md is untouched. Snapshot commits contain SNAPSHOT_UPDATE_OK with specific justifications.

## Specification Compliance

C1: AttributePathComparer / AttributePathComparerTests. C2-C6: PrimitiveCollectionDiffer and CollectionElementDiffer / model, differ, and rendering tests. C7-C8: underlying typed keys and annotations / CollectionNameAnnotationTests. C9: collection states and two-sided large rendering / ElementlessCollectionTests and LargeCollectionStateTests. C10: ResourceChangeStage unknown modeling plus LargeValueSummary unknown counting / PrimitiveCollectionModelTests, CollectionRenderingTests, and the two new LargeCollectionStateTests; the prior behavioral blocker is fixed. C11-C12: sensitivity masking and typed scalar identity / SensitiveCollection*, MaskedElementDifferTests, and mixed-kind tests. C13: primitive rejection and natural ordering / model and comparer tests. C14-C18: per-resource collection drift and selection / drift model and rendering tests. C19: logical row/path counts / CollectionConsumerTests and drift tests. C20: provider-specific isolation / regression snapshots and formatter tests. C21-C26: opaque masked comparison, per-side rendering, invariance, and show-sensitive behavior / MaskedValueInvarianceTests and SensitiveCollection*Tests. C27-C28: collection-aware Azure ID case filtering / CollectionAttributeFilterTests, MaskedCollectionFilterTests, and CaseOnlyCollectionEndToEndTests. Every acceptance criterion has identified implementation and test coverage; only authoritative execution evidence for current HEAD is missing.

## What I Tried To Break

Checked the corrected whole-collection-unknown path, partially unknown elements, double-counting between element and whole-state markers, add/remove/reorder summaries, absent/null/empty rendering, sensitive-value exposure, changed-attribute consistency, snapshot authorization, commit types, file-size compliance, documentation updates, and whitespace errors. The new logic counts Unknown element statuses once and adds one only for a wholly unknown after side; the focused tests cover both paths. No new behavioral defect was identified.

## Issues Found

### Majors

- **Authoritative verification for current HEAD is still unavailable** — `docs/features/148-compact-collection-diffs/work-protocol.md:196`
  The round-6 change modifies production counting logic, but CoverageEnforcer was not rerun and no CI result exists for the current commit. The protocol explicitly carries this forward as unresolved, while GitHub status could not be reached. The recorded full-suite result is useful but cannot establish the required coverage and repository validation gates for the release candidate. Push the branch and obtain green CI, including coverage enforcement, before approval.

## Decision

`VERDICT: REWORK`
