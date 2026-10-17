# Test Plan: Compact, Semantically Accurate Collection Diffs

## Overview

Verifies [specification.md](specification.md) as designed in [architecture.md](architecture.md).
All tests are TUnit, fully automated, in `src/tests/Oocx.TfPlan2Md.TUnit`, named
`MethodName_Scenario_ExpectedResult`. Plan JSON is built inline or from small fixtures;
no test needs network, Docker or human judgement.

Test types: **U** unit (helper or stage in isolation), **I** integration (plan JSON
through `ReportModelBuilder` and renderer to Markdown, asserting on text), **S** snapshot
(existing snapshot mechanism, regenerated with `update-test-snapshots`), **R** regression
(existing tests that must stay green unchanged).

Success criteria are numbered C1-C28 in the order they appear in the specification.

| ID | Criterion (short) |
|---|---|
| C1 | Natural numeric index ordering, including nested paths |
| C2 | One logical row per all-primitive collection (create/update/replace/delete) |
| C3 | Added/removed occurrences, no index-shift cascade |
| C4 | Duplicate multiplicity preserved |
| C5 | Order-only change visible as order change |
| C6 | Sorted collections: membership change reports no order change |
| C7 | Equality by underlying value, independent of display names |
| C8 | Understandable with raw GUIDs; names are optional annotations |
| C9 | Empty / absent / null: distinct when shown, equivalent for change detection |
| C10 | Whole or partial unknown stays unresolved |
| C11 | No sensitive value shown; non-sensitive entries still shown; only entries Terraform marks are masked; whole-collection mark gives one masked value with no count |
| C12 | Mixed scalar types not conflated |
| C13 | Object/nested collections keep existing rendering, natural index order |
| C14 | One collection-drift entry per affected resource |
| C15 | Several paths of one resource share one entry, each with its own diff |
| C16 | Two resources never merged in collection drift |
| C17 | Scalar drift still grouped across resources (feature 145) |
| C18 | `--drift` all/default/relevant/none apply to collection drift |
| C19 | Count: one per compact collection; drift entry count = number of paths |
| C20 | Provider-specific tables unchanged |
| C21 | Masked entry carries added/removed/unchanged/moved marker and takes part in order change (masked-to-masked comparison only) |
| C22 | Masked and non-masked entries always differ, whatever the values |
| C23 | Masked entry reveals no value, part, length, casing, or value-derived annotation (feature 147 name) |
| C24 | Masked entries on one side never collapsed, counted, or labelled duplicates |
| C25 | Invariance: replacing a masked value (relation to other masked entries kept) leaves the report unchanged in table, large section, drift, summaries, counts |
| C26 | `--show-sensitive`: same full diff as without sensitive entries |
| C27 | Case-only Azure ID collection hidden (also under `--show-unchanged-values`), no drift entry, not counted; any other change shows it |
| C28 | Difference involving a masked entry is never case-only |

## Test Coverage Matrix

| Criterion | Test cases | Type |
|---|---|---|
| C1 | TC-01, TC-02, TC-03 | U, I |
| C2 | TC-10, TC-11, TC-12, TC-13, TC-14 | I |
| C3 | TC-20, TC-21, TC-22 | U, I |
| C4 | TC-23, TC-24, TC-25 | U, I |
| C5 | TC-26, TC-27, TC-28 | U, I |
| C6 | TC-29, TC-30 | U |
| C7 | TC-31, TC-32, TC-33 | U, I |
| C8 | TC-34, TC-35 | I |
| C9 | TC-40, TC-41, TC-42, TC-43, TC-116 | U, I |
| C10 | TC-44, TC-45, TC-46 | U, I |
| C11 | TC-50 to TC-56, TC-120, TC-130 | U, I |
| C12 | TC-60, TC-61 | U, I |
| C13 | TC-62, TC-63, TC-64 | I |
| C14 | TC-70, TC-71 | I |
| C15 | TC-72, TC-73 | I |
| C16 | TC-74 | I |
| C17 | TC-75, TC-76 | I, R |
| C18 | TC-77, TC-78, TC-79, TC-80 | I |
| C19 | TC-81, TC-82, TC-83, TC-84 | I |
| C20 | TC-90, TC-91, TC-92 | R, S |
| C21 | TC-51, TC-57, TC-58, TC-121, TC-122 | U, I |
| C22 | TC-59, TC-120 | U, I |
| C23 | TC-53, TC-125, TC-124 | U, I |
| C24 | TC-123 | U, I |
| C25 | TC-124, TC-126, TC-127, TC-128 | U, I |
| C26 | TC-54 | I |
| C27 | TC-100, TC-102, TC-131, TC-132 | U, I |
| C28 | TC-101, TC-133 | U, I |

Architecture-derived behaviours with no direct criterion but required by the design
(each traces to the criterion shown): case-only ID filter (C27, C28) TC-100 to TC-103, TC-131 to TC-133;
large elements (C2, C5) TC-104, TC-105; diff size cap (C3, C5) TC-106; formatter pass-through
by indexed name (C8, C20) TC-107; consumer audit (C19, C20) TC-108, TC-109.

## Test Cases

Notation: `L(a,b)` is a JSON array of strings. "Row" is an `AttributeChangeModel` in
`ResourceChangeModel.AttributeChanges`. Marker glyphs are `➖` (removed), `➕` (added),
`🔀 order changed`.

### Natural ordering (C1)

- **TC-01** `AttributePathComparer_MultiDigitIndices_OrdersNumerically` (U). Sort
  `a[10]`, `a[2]`, `a[1]` -> `a[1]`, `a[2]`, `a[10]`.
- **TC-02** `AttributePathComparer_NestedIndexedPaths_OrdersNumericallyPerSegment` (U).
  `r[2].x[10]`, `r[2].x[9]`, `r[10].x[1]`, `r[9].y` -> `r[2].x[9]`, `r[2].x[10]`, `r[9].y`, `r[10].x[1]`.
  Also: non-index segments stay ordinal (`Zed` before `abc`, `a.b` vs `a_b` identical to
  the previous `Order()` result), equal paths compare 0, a path without brackets and a
  path with a non-numeric bracket (`tags["a"]`) fall back to ordinal.
- **TC-03** `ResourceChangeStage_ObjectListWithTwelveItems_RowsInNumericOrder` (I).
  Object list (not compactable) with indices 0..11 changed; rendered table lists `[2]`
  before `[10]`. A second case orders drift (`GroupDrift`) `AttributePath` the same way.

### One logical row (C2)

- **TC-10** `Create_PrimitiveList_RendersOneRow` (I). Create with `L(a,b,c)`; exactly
  one row for the path, no `path[0]` rows, cell shows the three values separated by `<br>`.
- **TC-11** `Update_PrimitiveList_RendersOneRowWithBeforeAndAfterColumns` (I).
- **TC-12** `Replace_PrimitiveList_RendersOneRow` (I). `replace` action, same assertions.
- **TC-13** `Delete_PrimitiveList_RendersOneRow` (I). Single-value table, full before collection.
- **TC-14** `Update_PrimitiveListNestedInObjectList_CompactsInnerListOnly` (I). A
  `security_rule[0].source_address_prefixes` list collapses to one row; the sibling
  scalar rows of `security_rule[0]` remain per-attribute and naturally sorted.

### Membership (C3, C4)

- **TC-20** `Diff_InsertionInMiddle_ReportsOneAddedNoRemoved` (U). `[a,c,d]` -> `[a,b,c,d]`:
  one `Added` (b), three `Unchanged`, `OrderChanged` false.
- **TC-21** `Diff_RemovalInMiddle_ReportsOneRemoved` (U). Mirror of TC-20.
- **TC-22** `Update_InsertionIntoLongList_NoReplacementCascade` (I). 12-element list,
  insert at index 3; rendered table contains exactly one `➕` and no `➖`.
- **TC-23** `Diff_RemoveOneOfTwoEqualValues_RemovesExactlyOneOccurrence` (U). `[a,a,b]` -> `[a,b]`:
  one `Removed`, `Unchanged` count 2.
- **TC-24** `Diff_AddOneEqualOccurrence_AddsExactlyOneOccurrence` (U). `[a,b]` -> `[a,a,b]`.
- **TC-25** `Update_DuplicateOccurrences_RenderedWithMultiplicity` (I). Output shows `a`
  twice in the after column and a single `➕` mark; value not collapsed.
- Replacement of the whole content (`[a,b]` -> `[c,d]`): two `Removed`, two `Added`, no `Moved` (U, add to TC-20 group as TC-20b).

### Order (C5, C6)

- **TC-26** `Diff_SameOccurrencesReordered_ReportsOrderChangeOnly` (U). `[a,b]` -> `[b,a]`:
  no `Removed`, no `Added`, `OrderChanged` true, at least one `Moved`.
- **TC-27** `Update_OrderOnlyChange_RendersRowWithOrderChangedLine` (I). Row present (not
  filtered as a no-op), no `➖`/`➕`, After ends with `🔀 order changed`.
- **TC-28** `Diff_ReorderPlusAdd_ReportsBothAddedAndOrderChange` (U). `[a,b,c]` -> `[c,a,b,d]`.
  Also a duplicates reorder `[a,b,a]` -> `[a,a,b]` is an order change with no add/remove.
- **TC-29** `Diff_SortedSetInsertion_ReportsNoOrderChange` (U). `[a,c,e]` -> `[a,b,c,e]` and
  removal, plus sorted GUID lists (parameterised, 3 cases).
- **TC-30** `Update_SortedSetMembershipChange_OutputHasNoOrderChangedLine` (I).

### Underlying-value equality and names (C7, C8)

- **TC-31** `Diff_EqualValuesDifferentDisplayNames_Unchanged` (U). The differ API takes raw
  values only; asserting on the model that no name field participates. Integration form below.
- **TC-32** `Update_SameGuidBeforeAndAfter_WithNameMappingEnabled_NoChange` (I). Mapping
  resolves GUID X to "Alice" on one side only (simulated through the formatter/lookup);
  the row is still absent/unchanged.
- **TC-33** `Update_TwoDistinctGuidsMappedToSameName_RemainDistinct` (I). Both appear, one
  `➖` and one `➕`, neither merged.
- **TC-34** `Update_GuidCollectionWithoutMappings_ShowsRawGuidsWithMarkers` (I). The spec's
  `excludeUsers` example (`msgraph_resource`, path `body.conditions.users.excludeUsers`).
- **TC-35** `Update_GuidCollectionWithMapping_AnnotatesNameBesideGuid` (I). Requires feature
  147 mapping; the GUID text is still present beside the name.

### Empty, absent, null (C9)

- **TC-40** `Diff_AbsentToEmpty_StatesAreAbsentAndEmpty` (U). Absent -> `[]` has no elements on either side, so no row by default; row present with `--show-unchanged-values`. Model keeps the absent and empty states distinct.
- **TC-41** `Diff_NullToEmpty_StatesAreNullAndEmpty` (U). Same as TC-40 for `null` -> `[]`: no row by default, row with `--show-unchanged-values`. Sensitive element-less collections behave the same.
- **TC-42** `Diff_EmptyToEmpty_NoRow` (U). Both `[]` -> unchanged, not a row (unless `--show-unchanged-values`). `[]` is shown only when the other side has elements (TC-43).
- **TC-43** `Update_EmptyToElements_ShowsAddedNotCreateFromNull` (I). `[]` -> `[a]` shows `[]` before.
  `null` -> `[a]` and absent -> `[a]` each render distinct before cells; no pair collapses to the same cell where today they differed.

### Unknown (C10)

- **TC-44** `Update_WholeCollectionUnknownAfter_RendersKnownAfterApply` (I). Includes
  configuration-reference hint when present.
- **TC-45** `Update_PartialUnknownElement_IsUnresolvedNoMarkers` (I). `after_unknown: [false,true,false]`:
  unknown element shows the known-after-apply label; no `➖`, `➕` or `🔀`.
- **TC-46** `Diff_PartialUnknown_IsUnresolvedTrueAndNoStatusesComputed` (U). Also drift
  variant: unknown in drift renders "unresolved", not an element diff.

### Sensitive (C11, C21 to C26)

Notation: `<s1>` is an entry listed in `before_sensitive`/`after_sensitive` as `true` at
its index. "Sentinel" values (`SECRET-AAA`, `SECRET-BBB`, ...) are unique strings that
must not appear anywhere in the default-option output. All tests below run without
`--show-sensitive` unless stated.

- **TC-50** `Update_SensitiveWholeCollection_MasksBothSides` (I). Collection flagged `true`
  in `after_sensitive` only, then in `before_sensitive` only, then both: `(sensitive)` in
  both cells, no per-entry markers, no `➖`/`➕`/`🔀`, no element count, no sibling value.
- **TC-51** `Update_OneSensitiveElement_MasksOnlyThatEntry` (I). `["a",<s1>,"b"]` with a
  change elsewhere: `a` and `b` are shown, only index 1 shows `(sensitive)`; sentinel absent;
  the collection is not masked as a whole.
- **TC-52** `Update_SensitiveWholeCollection_DuplicateOrderAndMembershipChangesLeakNothing` (I).
  Reorder, duplicate change and add/remove on a wholly sensitive collection render one
  masked value per side, no markers, no length.
- **TC-53** `Stage_MaskedEntries_ModelCarriesNoValueTypeOrLength` (U). Wholly masked
  collection: element lists empty, states `Masked`. Per-entry masking: each masked
  `CollectionElementModel` has `IsMasked` true, `Value` null, `Kind` Undefined; canonical text
  contains the bare token `(sensitive)`; no sentinel in any serialized model property.
- **TC-54** `Update_SensitiveEntriesWithShowSensitive_RendersSameDiffAsNonSensitive` (I). Same plan
  with and without sensitivity markers, `--show-sensitive`: identical rendered row and counts;
  `IsSensitive` true on the sensitive variant. Includes whole-collection and per-entry variants.
- **TC-55** `Drift_SensitiveCollection_MasksAndHasNoElementLines` (I). Whole-collection mask
  gives `(sensitive)` and no element lines; per-entry mask shows visible members, masked
  entries as `(sensitive)` with markers; sentinel absent.
- **TC-56** `Output_SentinelSecrets_AbsentFromEveryOutputPath` (I). One plan with sentinels in
  create, update, delete, replace and drift; sentinel, its prefix, length-bearing padding and its
  upper/lower-case variants are absent from the generic table, large-value section, drift,
  resource summaries, diagnostics and log output.
- **TC-57** `Update_MaskedEntryChanged_ShowsRemovedAndAddedPlaceholders` (I). Spec example 1:
  `["a",<s1>,"b"]` -> `["a",<s2>,"c"]`, `s1` != `s2`: before `a`, `➖ (sensitive)`, `➖ b`; after
  `a`, `➕ (sensitive)`, `➕ c`; neither secret value appears.
- **TC-58** `Update_MaskedEntryUnchanged_ShowsPlaceholderWithoutMarker` (I). Spec example 2: same
  secret on both sides at the same position: `(sensitive)` unchanged like `a`; only `b`/`c` marked.
- **TC-59** `Update_VisibleBecomesMasked_SameValue_ShowsRemovedAndAdded` (I). Spec example 3:
  `["x"]` -> `[<x>]`: `➖ x` before, `➕ (sensitive)` after, no `Moved`, no unchanged. Mirror case
  `[<x>]` -> `["x"]`: `➖ (sensitive)` before, `➕ x` after. Output equals the output of the same
  plan where the secret holds any other value (also asserted in TC-124).

### Sensitive: per side, order, duplicates, invariance (C11, C21 to C25)

- **TC-120** `Update_SensitiveOnOneSideOnly_MasksPerSide` (I). Entry marked only in
  `after_sensitive`: before shows the value, after shows `(sensitive)`, marked as different
  (rule 4). Mirror with `before_sensitive` only. The unmarked side is never masked.
- **TC-121** `Diff_MaskedEntriesReordered_ReportsOrderChangeFromMaskedToMaskedComparison` (U/I).
  `[<s1>,"a"]` -> `["a",<s1>]` is an order change with a `Moved` marker on the masked entry.
  `[<s1>,<s2>]` -> `[<s2>,<s1>]` (distinct secrets) is an order change. `[<s1>,<s2>]` -> `[<s3>,<s4>]`
  with all different: two removed, two added placeholders, no order change.
- **TC-122** `Diff_MaskedEntryAddedOrRemoved_CarriesAddedOrRemovedMarker` (U/I). `["a"]` -> `["a",<s>]` and
  the removal; the marker is present, the value is not.
- **TC-123** `Render_SameSideEqualMaskedEntries_NotCollapsedCountedOrLabelled` (I). `[<s>,<s>]`
  (equal values, one side) renders two separate `(sensitive)` placeholders, with no count, `×2` or
  duplicate label. Equivalence check: `[<s>,<s>]` -> `[<t>]` and `[<s1>,<s2>]` -> `[<t>]`, with `t`
  different from the others, render identically.
- **TC-124** `Stage_ReplacingMaskedValue_RelationUnchanged_ModelAndMarkdownIdentical` (U, parameterised).
  Rule 7 test seam. Run the stage and renderer twice on plans that differ only in the values of
  masked entries, keeping the equal/different relation to each masked entry on the other side,
  and assert the complete `AttributeChangeModel` and the Markdown are identical. Cases:
  (a) a different value of the same kind; (b) a value equal to a visible entry on the before
  side; (c) equal to a visible entry on the after side; (d) equal to visible entries on both
  sides; (e) different length (empty string, 1 char, 500 chars); (f) different kind (string,
  number, bool, GUID, Azure resource ID); (g) different casing; (h) Markdown-special characters
  (`|`, backtick, `<br>`); (i) a value above the large-value threshold, which must not make
  the row large nor change any visible entry's rendering; (j) a number versus a string, which must
  not change the quoting of visible strings (`HasMixedKinds`); (k) a GUID with a feature 147
  name mapping that would resolve it. Negative control: changing the cross-side
  masked equality relation (masked-equal to masked-different) must change the output, to prove the
  test can fail.
- **TC-125** `Render_MaskedGuidWithNameMapping_ShowsNoAnnotation` (I). Mapping resolves the
  masked GUID; `(sensitive)` shows no name, and the name text is absent from the output.
- **TC-126** `Drift_ReplacingMaskedValue_RelationUnchanged_DriftEntryIdentical` (I). TC-124 for
  collection drift entries (visible-equal substitution included).
- **TC-127** `Summary_ReplacingMaskedValue_SummariesAndCountsIdentical` (I). TC-124 for
  resource summary text and changed-attribute counts, and drift path counts.
- **TC-128** `LargeSection_MaskedEntries_RenderAsPlaceholdersAndDoNotMakeRowLarge` (I). A collection
  with a large visible element plus masked entries: masked entries appear as `(sensitive)` lines
  with markers in the large-value section, with no value, length or wrapping. A collection whose
  only large element is masked does not move to the large section. Substituting the masked value
  leaves output identical.
- **TC-129** `Update_UnknownSensitiveEntry_UsesCombinedPlaceholder` (I). Entry both in
  `after_unknown` and `after_sensitive`: existing combined sensitive/known-after-apply placeholder;
  a known masked entry in the same unresolved collection renders `(sensitive)`; no markers computed.
- **TC-130** `Update_SensitiveMarkOnOneSideOnly_AtCollectionLevel_MasksBothSidesWithoutCount` (I).
  Collection flagged `true` on one side only masks both sides as one value (rule 1), reveals no
  count or positions, and reports only that the collection changed.

### Mixed types (C12)

- **TC-60** `Diff_StringOneVsNumberOne_AreDifferentElements` (U). `["1"]` -> `[1]`: one removed, one added.
  Also `true` vs `"true"`, `null` vs `"null"`, `1` vs `1.0` is documented as raw-text compared (assert actual behaviour).
- **TC-61** `Update_MixedScalarList_RendersEachElementWithOwnKind` (I). `[1,"a",true,null]` shows `null` for the null element.

### Non-primitive collections (C13)

- **TC-62** `Update_ListOfObjects_KeepsIndexedRows` (I). Identical rows to before (apart from ordering).
- **TC-63** `Update_ListContainingNestedList_KeepsIndexedRows` (I). `[[1],[2]]` not compacted.
- **TC-64** `Update_MixedPrimitiveAndObjectElements_NotCompacted` (U). One object element disqualifies the whole path on either side.
  Also primitive list -> list of objects across a change is not compacted.

### Drift (C14-C19)

- **TC-70** `Drift_GroupMemberInserted_OneCollectionEntryOnePath` (I). The spec's `azuread_group`
  `members` example; no `members[n]` transitions.
- **TC-71** `Drift_ManyShiftedIndices_NoPerIndexEntries` (I).
- **TC-72** `Drift_TwoCollectionPathsOneResource_SingleEntryBothPaths` (I). Paths naturally ordered.
- **TC-73** `Drift_EachPathInEntry_HasOwnDiff` (I). One path with add, one with order change only.
- **TC-74** `Drift_TwoResourcesIdenticalCollectionChange_TwoEntries` (I).
- **TC-75** `Drift_ScalarChangeOnTwoResources_StillGrouped` (I).
- **TC-76** `Drift_ResourceWithScalarAndCollectionDrift_ScalarGroupedCollectionPerResource` (I). Existing 145 tests stay green (R).
- **TC-77** `Drift_ModeAll_IncludesCollectionEntries` (I).
- **TC-78** `Drift_ModeDefault_MatchesAllBehaviour` (I). Omission equals `all`, per spec wording.
- **TC-79** `Drift_ModeRelevant_OnlyRelevantResourcesHaveCollectionEntries` (I). Also a
  non-relevant resource's collection drift is omitted; no-op/suppressed drift stays suppressed.
- **TC-80** `Drift_ModeNone_NoCollectionEntriesAndNoSection` (I).
- Also: collection-drift entries alone (no scalar groups) still render the drift section (I, TC-80b); ordering by type then scalar-before-collection then address (TC-80c).

### Counts (C19)

- **TC-81** `Summary_CompactCollectionWithManyOccurrences_CountsAsOneAttribute` (I). `ResourceSummaryHtmlBuilder` text
  says one changed attribute for a 50-element list with 10 adds/removes.
- **TC-82** `Summary_CollectionPlusScalar_CountsTwo` (I). Table row count equals summary count.
- **TC-83** `Drift_EntryWithThreePaths_ReportsThreeChangedCollections` (I). Summary line text.
- **TC-84** `Summary_CompactCollection_ConsistentAcrossSummaryAndDetailTable` (I).

### Provider-specific regression (C20)

- **TC-90** Existing provider snapshot/unit tests (AzureAD, AzureDevOps, Azapi, NSG,
  firewall, role assignment, APIM, MsGraph) pass with **no snapshot diff** in provider
  tables (R). The Developer must not regenerate those provider snapshot files; any diff is a defect.
- **TC-91** `MsGraphResource_ExcludeUsersUpdate_GenericTableShowsCompactRow` (I). Provider
  settings table (`api_version`, `url`) unchanged beside the compact body row.
- **TC-92** Snapshot tests for generic table, drift, comprehensive demo are regenerated
  (S), and the diff is reviewed to contain only primitive-collection rows and multi-digit
  index ordering changes.

### Architecture-derived (filters, large values, consumers)

- **TC-100** `AttributeFilter_AllDifferingPositionsCaseOnly_RowSuppressed` (U/I). Same length lists whose only differences are case of Azure IDs (C27). Also with the order unchanged and at least one registry-suppressed position.
- **TC-101** `AttributeFilter_OneNonCaseDifference_RowRenders` (U). Includes a masked entry opposite a non-masked one, and a masked entry with any status other than `Unchanged` (C28).
- **TC-102** `AttributeFilter_DifferentLengths_RowRenders` (U). Also: a case-only difference plus an added or removed member, plus an order change (C27).
- **TC-103** `AttributeFilter_CollectionRow_CallsRegistryWithIndexedName` (U). Registry receives `P[i]` and element texts, and is never called for a masked entry.
- **TC-104** `Update_LargeElement_RendersInLargeSectionOneOccurrencePerLine` (I). Marker semantics preserved.
- **TC-105** `Update_LargeElementReorder_ReportsOrderChangeNotRemoveAdd` (I).
- **TC-106** `Diff_ExceedsLcsCellCap_FallsBackConservatively` (U). Large lists (size beyond cap) complete quickly, retained
  occurrences are `Unchanged`, `OrderChanged` true only when retained key sequences differ.
  Include a timing guard modelled on `DiffComputationPerformanceTests` (generous bound).
- **TC-107** `Format_ElementValueFormatters_ReceiveIndexedName` (I). AzureDevOps `members[0]`-style
  MatchPattern formatter still applies to a compact list element.
- **TC-108** `ParentChildMerging_CompactRowNamedAsPrefix_StillMatches` (I).
- **TC-109** `FindingIndicator_SarifPathIndexedElement_MarksCompactRow` (I). SARIF path `x[3]` marks row `x`.

### Case-only Azure ID filter (C27, C28)

- **TC-131** `Update_CollectionDiffersOnlyInAzureIdCase_HiddenEverywhere` (I). Default options
  (`--ignore-azure-id-case-changes` on): `/subscriptions/ABC/...` -> `/subscriptions/abc/...` in a
  list gives no row, and none under `--show-unchanged-values`; no collection-drift entry in
  drift; the resource's changed-attribute count and summary do not include it; a resource with no
  other change shows no entry. With `--ignore-azure-id-case-changes` disabled, the row is shown.
- **TC-132** `Update_CaseOnlyDifferencePlusOtherChange_ShowsRow` (I). Each of: an added member, a
  removed member, an order change, a non-case change in another position, an unrelated element
  changing: the row is shown, and the case-variant entries may appear as removed and added.
  Drift variant: such a collection produces an entry; the case-only variant does not.
- **TC-133** `AttributeFilter_MaskedDifference_NeverTreatedAsCaseOnly` (U/I). (a) A case-only
  visible difference together with a changed masked entry shows the row; (b) two masked Azure IDs
  that differ only in case (`<ABC>` -> `<abc>`) are different masked entries, so the row is
  shown, as `➖`/`➕ (sensitive)` without any value; (c) a masked entry opposite a visible one
  with a case-variant value shows the row; (d) a masked unchanged entry beside a case-only visible
  difference is hidden; (e) the filter outcome is identical when the masked values are replaced
  while the masked-to-masked relation is kept (rule 7).

## Edge Cases and Error Conditions

| Edge case | Test |
|---|---|
| Empty arrays on both sides; before absent, after `[]` | TC-40..TC-43 |
| `null` elements inside list; all-null list | TC-61 |
| Single-element list; very long list (1000 elements, one change) | TC-22, TC-106 |
| Elements with `|`, `` ` ``, `<br>`, newline, HTML characters, empty string | TC-110 `Render_ElementsWithMarkdownSpecialCharacters_AreEscaped` (I) |
| Element text identical to a marker (`➕ x`) | TC-111 (I): rendering does not make it ambiguous with a real marker (marker is only prefix on added occurrences; assert both visible and distinct) |
| Array on one side, scalar or object on the other (type change) | TC-112 (I): not compacted, falls to existing rendering, no exception |
| Unknown inside before only; `after_unknown` shape mismatch (`true` vs array vs absent) | TC-46 plus TC-113 (U): malformed shapes do not throw |
| `before_sensitive`/`after_sensitive` shape `true`, array of bool, object, sparse array, shorter than the data array | TC-50, TC-51, TC-56, TC-120, TC-130 |
| Resource with zero changes after suppression (all filtered) | TC-100, TC-131 |
| Sensitive empty collection (absent, `null`, `[]`) on create/delete | TC-41 (no row, no drift entry) |
| Only masked entries on one side, none on the other; all entries masked | TC-121, TC-122, TC-124 |
| Masked entry containing a value equal to a visible entry on either side | TC-59, TC-124 (b to d) |
| Masked value with Markdown-special characters or oversized length | TC-124 (e, h, i) |
| Masked entries and `--show-sensitive` together with case-only filter | TC-54, TC-133 |
| Module/indexed addresses (`module.m.res["k"]`) in collection drift | TC-74 uses keyed addresses |
| Drift in resource also present in `resource_changes` | TC-76 |
| Output (not resource) arrays in plan `output_changes` | TC-114 (I): outputs path unchanged |
| Differ is deterministic (same input twice -> same statuses) | TC-115 (U) |
| `--show-unchanged-values` with unchanged compact collection | TC-116 (I) |
| `--hide-unchanged-outputs`, `--drift` options with all three platforms | covered by UAT |
| Markdown lint of produced tables/details | `MarkdownLintIntegrationTests` and `MarkdownInvariantTests` run over new fixtures (R) |

## Specification gaps noted

- C8 requires name annotations "when feature 147 can resolve them". TC-35 depends on the
  mapping feature being merged on this branch; if absent, the test must be written against the
  formatter extension point and the gap recorded by the Developer.
- Criterion C18 says omission of `--drift` equals `all`; the existing default is asserted in TC-78 rather than redefined.
