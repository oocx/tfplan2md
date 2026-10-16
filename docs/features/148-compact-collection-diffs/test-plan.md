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

Success criteria are numbered C1-C20 in the order they appear in the specification.

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
| C9 | Empty / absent / null states distinct |
| C10 | Whole or partial unknown stays unresolved |
| C11 | Sensitive collections protected (membership, duplicates, order, count, drift) |
| C12 | Mixed scalar types not conflated |
| C13 | Object/nested collections keep existing rendering, natural index order |
| C14 | One collection-drift entry per affected resource |
| C15 | Several paths of one resource share one entry, each with its own diff |
| C16 | Two resources never merged in collection drift |
| C17 | Scalar drift still grouped across resources (feature 145) |
| C18 | `--drift` all/default/relevant/none apply to collection drift |
| C19 | Count: one per compact collection; drift entry count = number of paths |
| C20 | Provider-specific tables unchanged |

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
| C9 | TC-40, TC-41, TC-42, TC-43 | U, I |
| C10 | TC-44, TC-45, TC-46 | U, I |
| C11 | TC-50 to TC-56 | U, I |
| C12 | TC-60, TC-61 | U, I |
| C13 | TC-62, TC-63, TC-64 | I |
| C14 | TC-70, TC-71 | I |
| C15 | TC-72, TC-73 | I |
| C16 | TC-74 | I |
| C17 | TC-75, TC-76 | I, R |
| C18 | TC-77, TC-78, TC-79, TC-80 | I |
| C19 | TC-81, TC-82, TC-83, TC-84 | I |
| C20 | TC-90, TC-91, TC-92 | R, S |

Architecture-derived behaviours with no direct criterion but required by the design
(each traces to the criterion shown): case-only ID filter (C3, C20) TC-100 to TC-103;
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

### Sensitive (C11)

- **TC-50** `Update_SensitiveWholeCollection_MasksBothSides` (I). `(sensitive)` in both cells.
- **TC-51** `Update_OneSensitiveElement_MasksWholeCollection` (I). The non-sensitive
  sibling values and the collection length do not appear anywhere in the output.
- **TC-52** `Update_SensitiveCollection_DuplicateAndOrderChangesLeakNothing` (I). A
  reorder or duplicate change on a sensitive collection renders no `➖`, `➕`, `🔀`.
- **TC-53** `Diff_SensitiveCollection_ModelCarriesNoElements` (U). Element lists empty, states `Masked`.
- **TC-54** `Update_SensitiveCollectionWithShowSensitive_RendersNormalDiff` (I). `IsSensitive` stays true.
- **TC-55** `Drift_SensitiveCollection_MasksAndHasNoElementLines` (I).
- **TC-56** `Create_SensitiveCollectionOnlyInAfterSensitive_Masked` (I). Also delete via `before_sensitive`. Sentinel secrets asserted absent from the full output, summary and SARIF-free logs.

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

- **TC-100** `AttributeFilter_AllDifferingPositionsCaseOnly_RowSuppressed` (U/I). Same length lists whose only differences are case of Azure IDs.
- **TC-101** `AttributeFilter_OneNonCaseDifference_RowRenders` (U).
- **TC-102** `AttributeFilter_DifferentLengths_RowRenders` (U).
- **TC-103** `AttributeFilter_CollectionRow_CallsRegistryWithIndexedName` (U). Registry receives `P[i]` and element texts.
- **TC-104** `Update_LargeElement_RendersInLargeSectionOneOccurrencePerLine` (I). Marker semantics preserved.
- **TC-105** `Update_LargeElementReorder_ReportsOrderChangeNotRemoveAdd` (I).
- **TC-106** `Diff_ExceedsLcsCellCap_FallsBackConservatively` (U). Large lists (size beyond cap) complete quickly, retained
  occurrences are `Unchanged`, `OrderChanged` true only when retained key sequences differ.
  Include a timing guard modelled on `DiffComputationPerformanceTests` (generous bound).
- **TC-107** `Format_ElementValueFormatters_ReceiveIndexedName` (I). AzureDevOps `members[0]`-style
  MatchPattern formatter still applies to a compact list element.
- **TC-108** `ParentChildMerging_CompactRowNamedAsPrefix_StillMatches` (I).
- **TC-109** `FindingIndicator_SarifPathIndexedElement_MarksCompactRow` (I). SARIF path `x[3]` marks row `x`.

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
| `before_sensitive` shape `true`, array of bool, object | TC-51, TC-56 |
| Resource with zero changes after suppression (all filtered) | TC-100 |
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
