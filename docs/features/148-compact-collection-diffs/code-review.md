# Code Review: 148-compact-collection-diffs

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-10-16

## Summary

The implementation and test coverage are broad, and the prior sensitive-empty-collection defect is now covered. Release is still blocked because the implementation deliberately treats absent, null, and empty collections as equivalent for change detection, directly contradicting the approved specification. The unresolved conflict is already recorded in state.json.

## Verification Results

Read-only audit only; I did not run tests. The Developer records 1,584 passed, 0 failed, 0 skipped after round-three rework, with markdownlint clean for uat-plan.md and regenerated demo output unchanged except timestamps. An earlier reviewer independently recorded 1,580 passing tests and passing coverage (89.18% line, 80.92% branch) before the final four regression tests. No PR exists, and GitHub CI could not be verified; the API was unreachable. Snapshot commits include SNAPSHOT_UPDATE_OK with explanations. git diff --check is clean.

## Specification Compliance

C1: AttributePathComparer; AttributePathComparerTests. C2: PrimitiveCollectionDiffer/ResourceChangeStage collection rows and DefaultResourceRenderer.Collections; CollectionRenderingTests create/update/replace/delete cases. C3-C6: CollectionElementDiffer and collection rendering; CollectionElementDifferTests and CollectionRenderingTests cover shifts, duplicates, reorder-only changes, and sorted insertions. C7-C8: raw CollectionElementKey identity plus indexed value-formatting annotations; CollectionNameAnnotationTests, though real feature-147 integration is unavailable on this branch. C9: code and tests intentionally implement element-less equivalence, contradicting specification.md criterion 9; Blocker. C10: AfterUnknownHelper and BuildCollectionModel unresolved paths; PrimitiveCollectionModelTests and CollectionRenderingTests. C11: whole-collection masking in BuildCollectionAttribute; sensitive model/rendering/drift tests, including round-three empty-sensitive regressions. C12: JsonValueKind plus raw-text keys and mixed-kind quoting; PrimitiveCollectionModelTests and CollectionRenderingTests. C13: PrimitiveCollectionDiffer rejects object/nested elements while AttributePathComparer sorts remaining indexed paths; model and ordering tests. C14-C16: GroupCollectionDrift/CollectionDriftModel; CollectionDriftModelTests and CollectionDriftRenderingTests. C17-C18: scalar GroupDrift remains separate and BuildResourceDrift applies existing mode selection; drift model/rendering mode tests. C19: logical collection rows and path-count summaries; CollectionConsumerTests and drift tests. C20: provider factories remain separate; provider snapshots/regression suite are recorded as passing, with intentional generic/summarization snapshot changes.

## What I Tried To Break

Inspected natural path comparison, LCS/multiset matching, duplicate handling, mixed JSON kinds, partial/whole unknown values, sensitive masking, element-wise filters, collection drift grouping/rendering, large values, snapshots, provider regressions, and the round-two/round-three fixes. The sensitive empty-list path now maps absent, null, and [] to the same masked comparison key and has targeted tests. The remaining failure is contractual: the same equivalence is applied to ordinary collection change detection, suppressing transitions the approved specification says must remain observably distinct.

## Issues Found

### Blockers

- **C9 is implemented contrary to the approved specification** — `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Collections.cs:176`
  Success criterion C9 says empty, absent, and null collection states must retain distinct meanings and must not be silently treated as equivalent. IsCollectionChanged returns false whenever both sides are any combination of Absent, Null, or Empty, so absent→[], null→[], and absent→null disappear by default, including from drift. RawKey applies the same equivalence to masked collections. Architecture and tests were subsequently changed to bless this behavior, but specification.md was not; state.json explicitly records the conflict as an unresolved question awaiting Requirements Engineer/Maintainer confirmation. Either implement the approved criterion or obtain specification approval and update it before release.

### Minors

- **Current review evidence is not durably recorded** — `docs/features/148-compact-collection-diffs/code-review.md:1`
  The checked-in code-review.md still describes round two and its now-fixed sensitive-empty-list defect. The work protocol also has no current Code Reviewer entry for HEAD. The read-only sandbox prevents this review from writing those required artifacts, so the workflow record must be updated by the driver before handoff.

## Decision

`VERDICT: REWORK`
