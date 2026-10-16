# Code Review: 148-compact-collection-diffs

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-10-16

## Summary

The compact collection implementation broadly matches the revised specification, including type-accurate diffs, duplicate preservation, natural ordering, per-entry masking, case-only filtering, and resource-level collection drift. Release is blocked because large collection transitions involving null or absent states omit that side entirely, violating the requirement that those states remain distinguishable whenever a row is shown.

## Verification Results

Read-only audit; tests were not executed. The Developer records 1,675 passed, 0 failed, 0 skipped. The Technical Writer reports regenerated UAT output and no new markdownlint findings relative to main. Current CI and post-rework coverage could not be verified because GitHub was unreachable. Earlier coverage results predate the final sensitive-masking rework.

## Specification Compliance

C1: AttributePathComparer with AttributePathComparerTests. C2-C6: PrimitiveCollectionDiffer, CollectionElementDiffer, and collection renderers with model/rendering/differ tests. C7-C8: raw typed keys and indexed formatter annotations with CollectionNameAnnotationTests; real feature-147 integration remains unavailable as recorded. C9: ordinary table/model behavior is covered by ElementlessCollectionTests, but the large-value rendering path is noncompliant and lacks a null/absent transition test. C10: AfterUnknownHelper and unresolved model handling with primitive-model/rendering tests. C11 and C21-C26: masking/tokenization/rendering with SensitiveCollectionModelTests, SensitiveCollectionRenderingTests, MaskedElementDifferTests, SensitiveCollectionDriftTests, and MaskedValueInvarianceTests. C12-C13: typed keys and primitive detection with model and ordering tests. C14-C18: CollectionDriftModel/grouping/rendering and drift-mode tests. C19: logical row/path counts with CollectionConsumerTests and drift tests. C20: provider-specific rendering remains separate and snapshot regressions are recorded. C27-C28: AttributeFilteringStage with collection-filter and end-to-end case-only tests. All criteria have code and named tests, but C9's tests do not prove the large-value output path.

## What I Tried To Break

Checked null/absent/empty transitions, large-value rendering, duplicate and reorder behavior, LCS fallback, mixed JSON kinds, unknown values, whole and per-entry sensitivity, masked-value invariance, Azure ID case-only filtering, drift grouping, summary counts, snapshot authorization, commit types, and documentation/process changes. The large-value path drops null and absent sides; masking paths otherwise avoid placing secret values in rendered models, canonical text, formatters, or filters.

## Issues Found

### Blockers

- **Large collection rendering hides null and absent states** — `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.Collections.cs:129`
  FormatLargeCollection only includes a side when its state is Elements or Empty, apart from Unknown/Masked on the after side. Therefore an update such as null → [large value], absent → [large value], [large value] → null, or [large value] → absent renders only the populated side. Null and absent become indistinguishable, directly violating success criterion 9 and the requirement that each side's state remain visible whenever the other side has elements. Existing large-value tests cover element changes and reorders but not these state transitions.

### Majors

- **Current CI and coverage evidence is absent** — `docs/features/148-compact-collection-diffs/work-protocol.md:122`
  The final Developer entry gives a specific local test result of 1,675 passing tests, but no CI result or coverage-enforcer output exists for the final rework. GitHub status could not be queried because the API was unreachable. Earlier recorded coverage was measured before the final masking implementation and its new branches, so it is not evidence for HEAD.

### Minors

- **New collection stage file exceeds the repository size guideline** — `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Collections.cs:1`
  ResourceChangeStage.Collections.cs is 370 lines, exceeding the repository rule that files over approximately 300 lines be refactored. Sensitivity extraction/tokenization and model construction are separable responsibilities.

## Decision

`VERDICT: REWORK`
