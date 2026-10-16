# Code Review: Compact, Semantically Accurate Collection Diffs

Reviewer: Claude (fallback, because the Codex review failed twice with 401 Unauthorized). Branch
`feature/148-compact-collection-diffs` at `f6d6c38f`, diff `origin/main...HEAD` (87 files).

## Summary

The implementation follows the architecture closely and is well built. The element differ
compares values by type, duplicates keep their multiplicity, and an order-only change is
reported as an order change, not as removed and added values. Sensitive collections carry no
element data in the model, and collection drift is grouped per resource. The test suite passes
and coverage clears both thresholds.

One defect blocks release. Empty arrays now count as primitive collections. So on every create
and delete, each unset nested block list (`[]`) becomes its own `| x | [] |` row. On main these
rows did not exist. In a realistic `azurerm_linux_web_app` create, 11 of 16 rows are `[]`. The
demo artifacts and the UAT fixture have no realistic create with empty blocks, so UAT would not
catch this.

## Verification Results

I ran these myself in the worktree:

- Full suite: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` gave
  1573 total, 1573 passed, 0 failed, 0 skipped (Debug, 1m33s). This matches the Developer's claim.
- CI-equivalent run: Release with Cobertura coverage, then `Oocx.TfPlan2Md.CoverageEnforcer`
  with the PR-validation thresholds:
  - line 89.20% (threshold 84.48%): PASS
  - branch 80.94% (threshold 72.80%): PASS
  - the Release run took 1m34s.
- markdownlint 0.49.1 with `.markdownlint.json`:
  - `artifacts/comprehensive-demo.md` (the CI-linted file) and `uat-plan.md` are clean, and so
    are my probe outputs.
  - Each changed artifact and example has the same findings as its `origin/main` version, so
    the change adds no lint findings there.
  - `docs/features.md`, `report-style-guide.md`, `architecture.md` and `testing-strategy.md`
    have exactly the same finding counts as on main (pre-existing).
- Rendered the probe plans with the built CLI, listed under "What I Tried To Break".
- CI status on a PR: there is no PR yet, so I could not check it.

## Specification Compliance

| Criterion | Implementing code | Proving test |
|---|---|---|
| C1 natural index order | `AttributePathComparer`, `ResourceChangeStage.cs` key order, `GroupDrift` | `AttributePathComparerTests` (6 tests incl. 12-item object list, drift ordering) |
| C2 one row create/update/replace/delete | `PrimitiveCollectionDiffer.Detect`, `BuildCollectionAttribute`, `FormatCollectionCell` | `CollectionRenderingTests` Create_/Update_/Replace_/Delete_PrimitiveList_* |
| C3 no index-shift cascade | `CollectionElementDiffer` | `Diff_InsertionInMiddle_*`, `Update_InsertionIntoLongList_NoReplacementCascade` |
| C4 duplicates | `MatchRemainingByKey` multiset | `Diff_RemoveOneOfTwoEqualValues_*`, `Diff_AddOneEqualOccurrence_*`, `Update_DuplicateOccurrences_*` |
| C5 order-only change | `Moved` status, `OrderChangedLine` | `Diff_SameOccurrencesReordered_*`, `Update_OrderOnlyChange_*` |
| C6 sorted set membership | LCS of sorted input | `Diff_SortedSetMembershipChange_*`, `Update_SortedSetMembershipChange_*` |
| C7 value equality | `CollectionElementKey(Kind, Raw)` | `Diff_EqualKeys_*`, `CollectionNameAnnotationTests` (formatter stand-in for 147) |
| C8 raw GUIDs with optional names | element formatting by `path[i]` | `Update_GuidCollectionWithoutMappings_*`, `Update_GuidCollectionWithMapping_*` (147 mapper not on branch, recorded gap) |
| C9 empty/absent/null distinct | `CollectionValues.StateOf`, `FormatCollectionCell` | `Update_EmptyAbsentAndNull_RenderDistinctCells`, model state tests. See Blocker B1 for the create/delete side effect |
| C10 unknown stays unresolved | `IsUnresolved`, `wholeUnknown` | `Update_PartialUnknownElement_*`, `Update_WholeCollectionUnknownAfter_*`, `Drift_UnknownCollection_*` |
| C11 sensitive protected | `masked` branch, empty element lists, `IsLarge = !masked` | `Update_SensitiveCollectionReorderAndDuplicates_LeakNothing`, `Update_OneSensitiveElement_*`, `Drift_SensitiveCollection_*` |
| C12 mixed scalar types | key includes `JsonValueKind`; quoting in `ElementDisplayText` | `Update_StringOneToNumberOne_*`, `Update_BooleanVersusString*`, `Create_MixedScalarList_*` |
| C13 object/nested lists unchanged | `VisitArrays` fall-through | `Update_ListOfObjects_KeepsIndexedRows`, `Update_ListContainingNestedList_*`, `Update_ObjectElementOnEitherSide_*` |
| C14 to C16 drift per resource | `GroupCollectionDrift`, `CollectionDriftRenderer` | `CollectionDriftModelTests` (one entry, two paths, two resources) |
| C17 scalar drift grouping | `GroupDrift` filters `Collection is null` | `Drift_ScalarChangeOnTwoResources_StillGrouped`, `Drift_ScalarDriftOnly_RendersExactlyAsBefore` |
| C18 drift modes | selection before grouping | `Drift_ModeAllAndDefault_*`, `Drift_ModeRelevant_*`, `Drift_ModeNone_*`, `Drift_NoOpCollectionDrift_*` |
| C19 counts | one row per collection; path count in drift | `CollectionConsumerTests` Summary_*, `Drift_EntryWithThreePaths_*` |
| C20 provider tables unchanged | no `Providers/` changes | provider snapshots unchanged; the AzureDevOps snapshot changed only in the summary text, because its fixture models `override` as a string list (explained in the work protocol) |

Every criterion has both implementing code and a test.

## What I Tried To Break

I built probe plans in the scratchpad and rendered them with the built CLI:

- **Empty block lists on create and delete.** Fails: see B1.
- **Type confusion.**
  - `[1,"1",true,null]` → `["1",1,"true",null]` keeps `1` and `"1"` distinct, removes
    boolean `true`, adds string `"true"` and reports the order change. Correct.
- **Numeric strings in an all-string list.**
  - NSG `destination_port_ranges` `["80","443"]` renders as `"80"`, `"443"` (quoted).
  - Correct, but noisy: see m1.
- **Whitespace-only strings.** `" "` and `"  "` are marked removed and added, but both render
  as blank code spans: see m2.
- **Markdown injection.**
  - `a|b`, `<b>x</b>` and backticks are escaped inside code spans.
  - The markers sit outside the span, so element text cannot imitate a marker.
- **null → elements.** `null` → ➕ `a` renders correctly.
- **Partial unknown.** `["a","b"]` → `["a",(unknown)]` shows `(known after apply)` and no
  markers. Correct.
- **Sensitive element, reorder and duplicate.** Both sides show `(sensitive)`. No count, order
  or value leaks into the table, the summary, the large section (masked rows are never large)
  or drift.
- **Drift with insertion and reorder.**
  - `members` insertion gives exactly one ➕ line.
  - `owners` swap gives a single order-change line.
  - A sensitive drift collection shows `(sensitive)` only.
  - NBSP after the glyphs is present, and the output is markdownlint-clean.
- **Size cap.**
  - 4000-element list reversed plus one insertion: 16M cells is over the 10M cap, so the
    fallback is used.
  - It renders in 1.6 s with the order change and one addition.
- **Path consistency.**
  - `Detect` builds paths with the same `name.child[i]` grammar as `JsonFlattener`, so no
    duplicate flat row can appear next to a collection row.
  - `IsUnderCollection` correctly skips nested keys such as `rule[0].prefixes[1]`.
- **Comparer.** `AttributePathComparer` returns 0 for equal strings and handles leading zeros
  and non-numeric brackets without overflow.
- **Case-only filter.** Element-wise, only for same-length lists, and never consulted for
  masked or unresolved rows.

## Issues Found

### Blockers

**B1. Every empty nested block list now renders as a `[]` row on create and delete.**
Locations:

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/PrimitiveCollectionDiffer.cs:256`:
  `All(IsPrimitive)` is vacuously true for `[]`, including empty lists of blocks.
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Collections.cs:176-188`:
  only `Null` is normalized to `Absent`, so Absent → Empty counts as a change.

Terraform writes every unset `NestingList` block as `[]` in `after` on create and in `before`
on delete. Examples are `auth_settings`, `backup`, `identity`, `logs` and
`site_config[0].ip_restriction`.

I probed with an `azurerm_linux_web_app` create that has 8 top-level empty blocks and 3 empty
lists inside `site_config`. The table had 16 rows, and 11 of them were `| x | [] |`. The delete
of the same resource also showed 11 `[]` rows. On main, `JsonFlattener` emits no key for `[]`,
so none of these rows existed. The architecture says so itself: "an empty array produces no key
at all".

This contradicts the scope wording that states stay distinct "according to the report's
existing value-handling rules", and it defeats the feature's goal of fewer, more readable rows.
`Create_EmptyCollection_RendersEmptyBrackets` (`CollectionRenderingTests.cs:156`) locks the
behaviour in.

None of the following contains a realistic create or delete with empty blocks, so the noise is
invisible in review and at the UAT gate:

- the regenerated artifacts and snapshots
- `collection-diffs-plan.json`
- `uat-plan.json`

Fix direction (Developer decides; record an open question if the Maintainer must choose):

- Do not emit a row when neither side has elements and the only difference is Absent/Null
  versus Empty (unless `--show-unchanged-values` is set). This covers create, delete, and
  update from absent to `[]`.
- Keep `[]` visible when the other side has elements (`[x]` → `[]`, `[]` → `[x]`), where the
  distinction carries meaning.
- Add a regression test with a realistic create and delete that include empty block lists, and
  extend the UAT fixture with one.

### Major

None.

### Minor

**m1. Numeric-looking strings are always quoted, even in all-string lists.**
`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/CollectionValues.cs:414-416`. Port lists render
as `"80"`, `"443"`, `"8080"` (probe on `azurerm_network_security_rule.destination_port_ranges`),
where main showed `80`. Quoting is only needed to tell kinds apart when both sides of the
collection together contain more than one `JsonValueKind`. Consider quoting only in that case.
The UAT gate may judge this as well.

**m2. Whitespace-only strings cannot be told apart.**
`CollectionValues.cs:409` quotes only the empty string, and
`DefaultResourceRenderer.Collections.cs:89-92` renders whitespace through `FormatCodeTable`.
`" "` and `"  "` both appear as blank code spans, yet one is marked ➖ and the other ➕. Quote
whitespace-only strings the same way as `""`.

**m3. Architecture §7 is out of date.** It says `Null` renders as `-`, but the code renders
`null` (`DefaultResourceRenderer.Collections.cs:57-58`), and the user docs describe `null`. The
code and docs agree, but the architecture text is stale.

**m4. `tasks.md` has 30 markdownlint findings.** They are MD022 and MD032, missing blank lines
around headings and lists. CI does not lint `docs/`, but every other doc in this work item is
close to clean.

### Suggestions

- **s1.** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/collection-diffs-plan.json` is byte-identical
  to `uat-plan.json`, and no test loads it. Either wire it into an integration or snapshot test,
  or drop the duplicate.
- **s2.** TC-35 (annotation from the real feature 147 mapper) is covered only through the
  value-formatter stand-in. Add the real test when 147 is merged.
- **s3.** The open question in `state.json` (raised by the requirements engineer) still says
  "Known sets remain order-insensitive". The Maintainer's resolution, that every collection is
  ordered, supersedes it. The driver may want to mark it as resolved so the PR description does
  not repeat a stale assumption.
- **s4.** The Release test run takes about 94 s locally, against a 120 s CI timeout. There is
  little headroom.

### Process

- `work-protocol.md` has entries from the Requirements Engineer, Architect, Quality Engineer,
  Task Planner, Developer and Technical Writer.
- `CHANGELOG.md` is untouched.
- The snapshot commits (`c7403b80`, `c627158f`, `4356f5d4`) carry `SNAPSHOT_UPDATE_OK` with
  specific justifications. The snapshot diff contains only compact rows and summary-name
  changes, which matches.
- Commit types are `feat`, `test` and `docs`. No breaking-change markers.
- Global docs updated: `features.md`, `report-style-guide.md`, `architecture.md` and
  `testing-strategy.md`. README unchanged, which is right because there is no CLI change.

## Decision

Rework. B1 is a user-visible regression in the most common report shape (create and delete of
azurerm resources with unset blocks), and neither tests nor UAT cover it. m1 and m2 are cheap to
fix in the same pass. The rest of the implementation is sound and needs no rework.
