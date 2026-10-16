# Tasks: Compact, Semantically Accurate Collection Diffs

Sources: [specification.md](specification.md), [architecture.md](architecture.md),
[test-plan.md](test-plan.md).

References used below: **SC n** is the n-th success criterion in `specification.md`
(numbered the same as C1-C28 in `test-plan.md`). **TC-nn** is a test-plan scenario.
Every test is TUnit, named `MethodName_Scenario_ExpectedResult`, in
`src/tests/Oocx.TfPlan2Md.TUnit`.

**Rework (2026-10-16).** Tasks 1 to 15 were built against the earlier specification and
are complete on the branch. The specification, architecture, test plan and UAT test plan
were then revised: per-entry, per-side sensitive masking with change markers, the
invariance criterion (SC 25), the element-less equivalence wording (SC 9), and the
case-only rule (SC 27, SC 28). Criteria in Tasks 1 to 15 that the revision supersedes are
marked **Superseded** in place. Tasks 16 to 25 are the remaining work, planned against the
code as it stands at `68d5dcf6`. Their criteria were checked against the code, not
assumed complete.

## Tasks

### Task 1: Natural ordering of numeric path indices

**Priority:** High
**Description:** Add `AttributePathComparer` in `MarkdownGeneration/Helpers/`. It splits a
path into segments, compares `[n]` index segments by numeric value and every other segment
ordinally (current semantics). Use it for the key ordering in `ResourceChangeStage.BuildAttributeChanges`
(replacing `Order()`) and for the `AttributePath` ordering in `ReportModelBuilder.GroupDrift`.
Scope is ordering only; no collection compaction in this task.
**Acceptance Criteria:**

- [x] `a[2]` sorts before `a[10]`, and `a[1]` before `a[2]` (SC 1, TC-01)
- [x] Nested indexed paths sort per segment: `r[2].x[9]`, `r[2].x[10]`, `r[9].y`, `r[10].x[1]` (SC 1, TC-02)
- [x] Non-index segments keep ordinal order (`Zed` before `abc`); equal paths compare 0; paths without brackets and non-numeric brackets such as `tags["a"]` fall back to ordinal (TC-02)
- [x] An object list with indices 0..11 renders `[2]` before `[10]` in the generic table, and drift orders its `AttributePath` the same way (TC-03)
- [x] Existing tests remain green; any snapshot change in this task is limited to multi-digit index ordering in generic or drift output
**Dependencies:** None

### Task 2: Element diff algorithm

**Priority:** High
**Description:** Implement the pure diff over element keys, with no rendering and no JSON
walking. Element key is `(JsonValueKind category, raw text)`, so `"1"` and `1` differ and
display names never take part. Steps, as in architecture section 4: trim common prefix and
suffix; LCS of the remainder (reuse or extract the LCS in `MarkdownHelpers/DiffComputation.cs`)
gives `Unchanged`; match remaining occurrences by key in multiset style in positional order,
giving `Moved` pairs; the rest are `Removed` (before) and `Added` (after). `OrderChanged` is
true when any `Moved` exists. Above the LCS cell cap, fall back to first-occurrence multiset
matching with `OrderChanged` true only when the retained key sequences differ. Element and
state types are defined here (`CollectionChangeModel` and `CollectionElementModel` may be
introduced as the diff's output types, with the `AttributeChangeModel` property added in Task 3).
**Acceptance Criteria:**

- [x] `[a,c,d]` to `[a,b,c,d]` gives one `Added` (b), three `Unchanged`, `OrderChanged` false (SC 3, TC-20)
- [x] The mirror removal gives one `Removed` (TC-21)
- [x] `[a,a,b]` to `[a,b]` gives exactly one `Removed` occurrence of `a` and two `Unchanged` (`a`, `b`) occurrences (SC 4, TC-23)
- [x] `[a,b]` to `[a,a,b]` gives exactly one `Added` occurrence (SC 4, TC-24)
- [x] `[a,b]` to `[c,d]` gives two `Removed`, two `Added`, no `Moved` (TC-20b)
- [x] `[a,b]` to `[b,a]` gives no `Removed`, no `Added`, `OrderChanged` true, at least one `Moved` (SC 5, TC-26)
- [x] `[a,b,c]` to `[c,a,b,d]` gives `Added` d and `OrderChanged` true; `[a,b,a]` to `[a,a,b]` gives `OrderChanged` true with no add or remove (SC 5, TC-28)
- [x] Sorted-collection insertion and removal (`[a,c,e]` to `[a,b,c,e]` and back, plus sorted GUID lists, three parameterised cases) gives `OrderChanged` false (SC 6, TC-29)
- [x] Two equal-value elements with different display names compare equal; the differ API accepts raw values only (SC 7, TC-31)
- [x] `[]` to `[]` gives no change (TC-42, model level)
- [x] Above the cap, a 1000-element list with one change completes within a generous bound modelled on `DiffComputationPerformanceTests`; retained occurrences are `Unchanged`; `OrderChanged` is true only when retained key sequences differ (TC-106)
- [x] Running the diff twice on the same input yields identical statuses (TC-115)
**Dependencies:** None

### Task 3: Primitive collection detection and model rows for known values

**Priority:** High
**Description:** Add `PrimitiveCollectionDiffer` in `MarkdownGeneration/Helpers/`. Walk `before`
and `after` in parallel and record each path `P` where at least one side is an array at `P`,
the other side is an array, `null` or absent, and no element on either side is an object or
array (an empty array qualifies). Detection runs at any depth. Add the optional
`AttributeChangeModel.Collection` (`CollectionChangeModel`, with `BeforeState` and `AfterState`
using `Absent | Null | Empty | Elements | Unknown | Masked`, ordered `Before`/`After` element lists,
`OrderChanged`, `IsUnresolved`) and the canonical `Before`/`After` text (compact JSON of the
array, `null`, `-` for absent). `BuildAttributeChanges` skips every flat key at or below a detected
`P` and emits one row for `P`. Scalar rows keep `Collection` null. Document on the
`AttributeChanges` property that it now means logical attributes. Unknown and sensitive
handling are excluded here (Tasks 4 and 5). The same detection rule applies to `output_changes`
as to resource changes, and output paths keep their current naming (TC-114).
**Acceptance Criteria:**

- [x] A primitive list `[a,b,c]` on create produces exactly one row for its path, with no `path[0]` rows (SC 2, TC-14 row count)
- [x] A primitive list nested inside an object list (`security_rule[0].source_address_prefixes`) becomes one row; sibling scalar rows of `security_rule[0]` remain separate and naturally sorted (SC 2, TC-14)
- [x] `[]` to `[a]` yields distinct before states `Empty` and `Elements`; `null` to `[a]` and absent to `[a]` yield `Null` and `Absent` before states, not collapsed (SC 9, TC-40, TC-41, TC-43 model)
- [x] `["1"]` to `[1]` yields one `Removed` and one `Added`; `true` vs `"true"` and `null` vs `"null"` are different elements; `1` vs `1.0` assertion matches observed raw-text behaviour (SC 12, TC-60)
- [x] `[1,"a",true,null]` keeps each element with its own kind, and the `null` element is kept as `null` (SC 12, TC-61)
- [x] A list of objects, a nested list `[[1],[2]]`, and a list mixing one object element with primitives all keep indexed rows; one object element disqualifies the path on either side; a primitive list changed to a list of objects is not compacted (SC 13, TC-62, TC-63, TC-64)
- [x] An `output_changes` array keeps its current path behaviour (TC-114)
- [x] A type change from array to scalar or object renders through existing rendering with no exception (TC-112)
- [x] An empty-to-empty case produces no row (TC-42)
- [x] `Collection` is null on every scalar row; a `Collection` row never sets `IsLarge` from its canonical text
**Dependencies:** Task 1, Task 2

### Task 4: Unknown and partially unknown collections

**Priority:** High
**Description:** Handle `after_unknown` and `before_unknown` shapes. A whole collection unknown
(`true` at `P`) sets `AfterState = Unknown`, and the existing `ApplyComputedKnownAfterApplyOverride`
produces the After text including any reference hint. A partially unknown collection sets
`IsUnresolved = true`, marks unknown-flagged elements `Status = Unknown`, and computes no
`Removed`, `Added` or `Moved` markers. Malformed shapes (`true` vs array vs absent) must not throw.
**Acceptance Criteria:**

- [x] Whole-collection unknown after gives a known-after-apply rendering, with the configuration-reference hint when present (SC 10, TC-44)
- [x] `after_unknown: [false,true,false]` gives an unknown element with the known-after-apply label and no marker in the row (SC 10, TC-45)
- [x] The diff for a partially unknown collection reports `IsUnresolved` true and no statuses other than `Unknown` and `Unchanged` (SC 10, TC-46)
- [x] Malformed `after_unknown` shapes and unknown-only-in-before inputs produce a row and no exception (TC-113)
**Dependencies:** Task 3

### Task 5: Sensitive collections in the model and generic cell

**Priority:** High
**Description:** When `before_sensitive` or `after_sensitive` marks `P` or any element under it,
and `--show-sensitive` is off, set both states to `Masked`, empty the element lists, show
`(sensitive)` on both sides, and keep `IsSensitive` true. With `--show-sensitive` on, render the
normal diff and keep `IsSensitive` true. **Superseded (rework):** the masking rule in this paragraph (whole-collection level, any
sensitive element masks the collection) is replaced by per-entry, per-side masking in Task 17.
The `before_sensitive` shape handling (`true`, array of bool, object) still applies.
**Acceptance Criteria:**

- [x] A sensitive whole collection shows `(sensitive)` in both Before and After cells (SC 11, TC-50)
- [x] One sensitive element masks the whole collection; neither non-sensitive sibling values nor the collection length appear in the output (SC 11, TC-51) **Superseded:** the revised SC 11 and TC-51 mask only the marked entry; see Task 17
- [x] A reorder or duplicate change on a sensitive collection renders no `➖`, `➕` or `🔀` (SC 11, TC-52)
- [x] A masked row's element lists are empty and both states are `Masked` (SC 11, TC-53)
- [x] With `--show-sensitive`, the normal diff renders and `IsSensitive` stays true (SC 11, TC-54)
- [x] A sensitive value present only in `after_sensitive` or only in `before_sensitive` is masked on the corresponding delete or create (SC 11, TC-56)
- [x] Sentinel secret values are absent from the full Markdown output and the summary (SC 11, TC-56)
**Dependencies:** Task 3

### Task 6: Element-wise attribute filter for collection rows

**Priority:** High
**Description:** Make `AttributeFilteringStage` evaluate a collection row element-wise. When both sides
have equal length and every differing position is suppressed by the registry (called with name
`P[i]` and the two element texts), suppress the row. Otherwise render it normally. The core must
not learn provider filter rules. Preserves the fully-suppressed case-only Azure ID outcome.
**Acceptance Criteria:**

- [x] Same-length lists whose only differences are Azure ID case changes produce no row (SC 20, TC-100)
- [x] A list with one non-case difference renders its row (TC-101)
- [x] Lists of different lengths render their row (TC-102)
- [x] The registry receives indexed names `P[i]` and element texts for each differing position (TC-103)
- [x] A partially case-only change shows the case-variant elements as `➖` and `➕` (open question 3 in state.json, recorded as assumed; **Resolved 2026-10-16:** the row is shown when the collection has any other change, as the revised SC 27 states. The filter also needs the order check added in Task 20.)
**Dependencies:** Task 3

### Task 7: Generic table rendering for update and replace

**Priority:** High
**Description:** In `DefaultResourceRenderer.AttributeTables`, render a row with `Collection` and action
`update` or `replace` in the before/after table. Before holds the full before collection with `Removed`
occurrences prefixed `➖`; After holds the full after collection with `Added` occurrences prefixed `➕`.
When `OrderChanged`, After ends with a `🔀 order changed` line. `Moved` elements carry no marker.
Elements are joined with `<br>`, and each element is formatted with `FormatAttributeValueTableWithRegistryResource`
passing `P[i]` for the element's own-side index. Raw GUIDs are rendered without names in this task.
**Acceptance Criteria:**

- [x] An update to a primitive list renders one row with Before and After columns (SC 2, TC-11)
- [x] A replace renders the same single-row layout (SC 2, TC-12)
- [x] Inserting one element into a 12-element list renders exactly one `➕` and no `➖`, with no replacement cascade (SC 3, TC-22)
- [x] Duplicate values render with multiplicity: value `a` appears twice in After with a single `➕` marker (SC 4, TC-25)
- [x] An order-only change renders a row (not filtered as no-op), no `➖` or `➕`, and After ends with `🔀 order changed` (SC 5, TC-27)
- [x] A sorted-collection membership change renders no `🔀 order changed` line (SC 6, TC-30)
- [x] An `excludeUsers`-style GUID collection without any name mapping renders raw GUIDs with markers (SC 8, TC-34)
- [x] Element text identical to a marker (`➕ x`) stays distinguishable from a real added marker (TC-111)
**Dependencies:** Task 3

### Task 8: Generic table rendering for create, delete and value edge cases

**Priority:** Medium
**Description:** Render create and delete in the single-value table with the full collection and no markers.
`Empty` renders `[]`; `Null` and `Absent` keep the existing scalar `-` rendering. Escape element text
containing `|`, backticks, `<br>`, newlines, HTML characters and empty strings (TC-110). Ensure the
`--show-unchanged-values` option renders an unchanged compact collection. Confirm that existing value
formatters matching indexed names (AzureDevOps `^members?(?:\[\d+\])?$`) still apply per element.
**Acceptance Criteria:**

- [x] A create with `[a,b,c]` renders one row with the three values separated by `<br>` (SC 2, TC-10)
- [x] A delete renders the full before collection in the single-value table (SC 2, TC-13)
- [x] `[]` renders `[]`, absent renders `-` (SC 9, TC-40)
- [x] Empty before and element after renders distinct cells, with no pair collapsing where today they differed (SC 9, TC-43)
- [x] Special characters in elements are escaped and do not break the table (TC-110)
- [x] An AzureDevOps `members[0]`-style formatter still applies to a compact list element (SC 20, TC-107)
- [x] With `--show-unchanged-values`, an unchanged compact collection renders as one row (TC-116)
**Dependencies:** Task 3

### Task 9: Large collection elements

**Priority:** Medium
**Description:** When any element is a large value under the existing check (including the formatter
consult), mark the row `IsLarge` and render in the large-attribute section, one occurrence per line,
with the same markers. Do not use the line-LCS large diff for compact collections, so a reorder never
becomes a removed/added pair.
**Acceptance Criteria:**

- [x] A large element in an update renders in the large section, one occurrence per line, with `➖` and `➕` (SC 3, TC-104)
- [x] A large-element reorder reports an order change and not a removed/added pair (SC 5, TC-105)
- [x] A single-element large collection still respects the existing large-value threshold (existing tests stay green)
**Dependencies:** Task 7, Task 8

### Task 10: Counts and consumer compatibility

**Priority:** Medium
**Description:** Verify the consumers listed in architecture section "Consumers": changed-attribute
summary (`ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary`, `ResourceSummaryBuilder`),
`ReportModelBuilder.ParentChildMerging` prefix matching, and `DefaultResourceRenderer.GetAttributeFindingIndicator`
prefix matching for SARIF paths. Fix any consumer that does not tolerate a compact row, without changing
provider factories.
**Acceptance Criteria:**

- [x] A 50-element list with 10 adds and removes counts as one changed attribute in the summary text (SC 19, TC-81)
- [x] A collection plus a scalar counts as two, matching table row count (SC 19, TC-82)
- [x] A compact collection counts consistently in the summary and detail table (SC 19, TC-84)
- [x] A compact row named as a prefix of a child row still matches parent-child merging (SC 20, TC-108)
- [x] A SARIF path `x[3]` marks the compact row `x` (SC 20, TC-109)
**Dependencies:** Task 3

### Task 11: Collection drift model and grouping

**Priority:** High
**Description:** Add `CollectionDriftModel` in `MarkdownGeneration/Models/` with `ResourceType`, `Address`
and `Paths` (naturally ordered list of path and `CollectionChangeModel`). In `ReportModelBuilder.GroupDrift`,
partition each selected drift resource's changes: rows with `Collection is null` go through the existing
feature-145 grouping unchanged; rows with a collection form one `CollectionDriftModel` per resource.
Add `ReportModel.CollectionDrift` beside `Drift`, and wire it through `Stages/ReportAssemblyStage.cs`
and `IReportAssemblyStage.cs`. The count of changed collection paths is the number of paths.
**Acceptance Criteria:**

- [x] A group member inserted into `members` yields one collection entry with one path, not `members[n]` transitions (SC 14, TC-70)
- [x] Many shifted indices yield no per-index drift entries (SC 14, TC-71)
- [x] Two collection paths of one resource share one entry with both paths, naturally ordered (SC 15, TC-72)
- [x] Each path in one entry carries its own diff, including one add and one order-only change (SC 15, TC-73)
- [x] Two resources with identical collection changes get two separate entries (SC 16, TC-74)
- [x] Drift containing three collection paths reports three changed collections (SC 19, TC-83)
**Dependencies:** Task 1, Task 3

### Task 12: Collection drift rendering

**Priority:** High
**Description:** Extend `ReportRenderer.RenderDriftSection` to render when either the scalar or collection drift list
is non-empty. Order by resource type; within a type, scalar groups first (existing order), then collection entries
by address. Each collection entry is one `<details>` block with the summary text from architecture section 8
(`N changed collection(s): ...`). Lines are deltas only: `➖`, `➕`, `🔀 order changed`. Use `FormatDriftCode`
for values. Masked collections render `(sensitive)` and no element lines.
**Acceptance Criteria:**

- [x] A collection-only drift result still renders the drift section (TC-80b)
- [x] Ordering is by type, then scalar before collection, then address (TC-80c)
- [x] A masked collection in drift renders `(sensitive)` and no element lines (SC 11, TC-55)
- [x] Collection drift lines use the same marker glyphs as the table and no Before/After columns (SC 14, TC-70)
- [x] An unknown collection in drift renders as unresolved, not as an element diff (SC 10, TC-46 drift variant)
**Dependencies:** Task 11

### Task 13: Drift mode selection and scalar grouping regression

**Priority:** High
**Description:** Confirm that `--drift all`, omission of the option, `--drift relevant` and `--drift none` apply to
collection drift entries exactly as to scalar drift. Mode selection happens on `ResourceChangeModel`s before grouping.
Confirm that feature-145 scalar grouping across resources is unchanged. Fix nothing outside these paths.
**Acceptance Criteria:**

- [x] `--drift all` includes collection entries (SC 18, TC-77)
- [x] Omitting `--drift` behaves as `all` (SC 18, TC-78)
- [x] `--drift relevant` includes collection entries only for relevant resources; suppressed and no-op drift stays suppressed (SC 18, TC-79)
- [x] `--drift none` produces no collection entries and no drift section (SC 18, TC-80)
- [x] Two resources with the same scalar change still group across resources (SC 17, TC-75)
- [x] A resource with scalar and collection drift shows the scalar in its group and the collection in its own entry (SC 17, TC-76)
- [x] Existing feature-145 drift tests remain green without edits
**Dependencies:** Task 11, Task 12

### Task 14: GUID name annotations and equality independence

**Priority:** Low
**Description:** Verify that display-name mapping from feature 147 can annotate collection members beside the
raw value and never changes equality or merges distinct members. This task depends on feature 147 being present
on the branch. If it is not, write the tests against the formatter extension point and record the gap in
work-protocol.md.
**Acceptance Criteria:**

- [x] A GUID mapped to a name on one side only does not change the row, which is absent (SC 7, TC-32)
- [x] Two distinct GUIDs mapped to the same name remain two elements, one `➖` and one `➕` (SC 7, TC-33)
- [x] With a mapping, the name appears as an annotation beside the same GUID (SC 8, TC-35)
- [x] If feature 147 is absent, the gap is recorded in work-protocol.md (SC 8)
**Dependencies:** Task 7

### Task 15: Regression guard and snapshots

**Priority:** High
**Description:** Run the provider-specific tests and confirm no provider table snapshot changes. Regenerate generic
table, drift and comprehensive demo snapshots with the `update-test-snapshots` skill and review that the diff
contains only primitive-collection rows and multi-digit index ordering changes. Run the Markdown lint integration
tests over the new fixtures.
**Acceptance Criteria:**

- [x] Existing AzureAD, AzureDevOps, Azapi, NSG, firewall, role assignment, APIM and MsGraph provider tests pass with no snapshot diff in provider tables (SC 20, TC-90)
- [x] The MsGraph `excludeUsers` update shows a compact body row beside unchanged `api_version` and `url` settings rows (SC 20, TC-91)
- [x] Generic, drift and demo snapshot diffs contain only the intended collection rows and index-order changes (SC 20, TC-92)
- [x] `MarkdownLintIntegrationTests` and `MarkdownInvariantTests` pass over the new fixtures (regression, test-plan R type)
**Dependencies:** Task 9, Task 10, Task 12, Task 13, Task 14

### Task 16: Masked-aware element key in the differ

**Priority:** High
**Description:** Add a `Masked` flag to `CollectionElementKey` (`Helpers/CollectionElementDiffer.cs`)
and an `IsMasked` flag to `CollectionElementModel` (`Models/CollectionChangeModel.cs`). A masked key
equals only a masked key with the same token, never a non-masked key, whatever the raw text
(specification rule 4). The matching steps of Task 2 stay as they are; only equality changes. The
stage that builds the tokens and masked elements is Task 17.
**Acceptance Criteria:**

- [x] A masked key never equals a non-masked key, even with the same raw text (SC 22, TC-59 at diff level)
- [x] Masked keys with the same token are `Unchanged` across sides; different tokens give `Removed` and `Added` (SC 21, TC-57 at diff level)
- [x] Reordered masked occurrences give `OrderChanged` and `Moved` statuses, not `Removed` and `Added` (SC 21, TC-121 at diff level)
- [x] An added or removed masked occurrence carries `Added` or `Removed` (SC 21, TC-122)
- [x] Differ tests from Task 2 pass unchanged (regression)
**Dependencies:** Task 2, Task 3

### Task 17: Per-side, per-entry sensitive masking in the stage

**Priority:** High
**Description:** In `Stages/ResourceChangeStage.Collections.cs`, replace the rule that masks a whole
collection when any element is sensitive (`HasSensitiveElement`). Follow architecture 5a:

- Whole collection: `IsSensitiveAttribute` on either side (ancestors included) keeps whole masking.
  Both states are `Masked`, element lists are empty.
- Otherwise before element `i` is masked only when `before_sensitive` is `true` at `P[i]`, and after
  element `j` only when `after_sensitive` is `true` at `P[j]`. Each side reads its own map.
- Masked elements get the opaque key `(Masked, Undefined, "#n")`, where `n` numbers the distinct
  `(kind, raw)` pairs of masked elements across both sides. The map is local to the method. Their
  model element has `IsMasked`, `Value` null and `Undefined` kind; their text is never computed.
- The canonical `Before`/`After` text on the per-entry path is built from model elements, with masked
  elements written as bare `(sensitive)`. `CollectionValues.CanonicalText` is not used there.
- `HasMixedKinds` and `HasLargeElement` skip masked elements, so `IsLarge` is not forced off by a
  masked row.
- Unknown flags are read on per-entry masked rows (rule 9).
- The known-after-apply override (`ApplyComputedKnownAfterApplyOverride`) receives only the
  whole-collection sensitivity. Otherwise a whole-unknown after side would turn the before side of a
  per-entry row into `(sensitive)`.
- Rewrite the two tests that assert whole-collection masking for one sensitive element,
  `PrimitiveCollectionModelTests.Update_OneSensitiveElement_MasksWholeCollection` and
  `CollectionRenderingTests.Update_OneSensitiveElement_NeitherSiblingsNorCountAppear`, to the
  per-entry expectations.

**Acceptance Criteria:**

- [x] Only entries marked on a side are masked on that side; the other entries keep their markers (SC 11, TC-51, TC-120)
- [x] A collection marked as a whole on either side masks both sides with empty element lists and `IsSensitive` true (SC 11, TC-50, TC-130)
- [x] A masked element's model carries no value, type or length, and no masked text appears in any model property (SC 23, TC-53)
- [x] The canonical text of a per-entry row contains bare `(sensitive)` tokens, for example `["a",(sensitive),"b"]`
- [x] A visible `x` before and a masked `x` after yield `Removed` and `Added`, never `Unchanged` (SC 22, TC-59 at model level)
- [x] With `--show-sensitive` the row equals the row for the same input without sensitivity markers (SC 26, TC-54)
- [x] An entry both unknown and sensitive keeps `Unknown` status, a known masked entry beside it stays masked, and no markers are computed in the unresolved row (SC 10, SC 11, TC-129 at model level)
- [x] A whole-unknown after side with per-entry masking on the before side keeps per-entry before text (specification rule 2)
- [x] `HasMixedKinds` and `HasLargeElement` ignore masked elements; a row whose only large element is masked is not `IsLarge` (TC-128 at model level)
**Dependencies:** Task 16, Task 4, Task 5

### Task 18: Masked rendering in the generic table and large-value section

**Priority:** High
**Description:** In `Rendering/DefaultResourceRenderer.Collections.cs` and
`CollectionValues.ElementDisplayText`, a masked element renders as `` `(sensitive)` ``. That check
runs before the null and formatter branches, so no value formatter or feature 147 annotation receives
a masked element. Markers apply exactly as for visible elements: `➖` in Before and `➕` in After.
Create and delete show the placeholder without markers. In the large-value section, masked
occurrences are `(sensitive)` lines with their markers.
**Acceptance Criteria:**

- [x] A changed secret between masked positions renders `➖ (sensitive)` before and `➕ (sensitive)` after; an unchanged secret at the same position has no marker (SC 21, TC-57, TC-58)
- [x] A visible `x` before and a masked `x` after render `➖ x` and `➕ (sensitive)` (SC 22, TC-59)
- [x] Two equal secrets on one side render as two placeholders with no count, `×2` or duplicate label; `[<s>,<s>]` and `[<s1>,<s2>]` with the same other side render identically (SC 24, TC-123)
- [x] A masked GUID with a feature 147 name mapping shows no annotation and no name text (SC 23, TC-125)
- [x] A masked order change renders the `🔀 order changed` line and no marker on moved entries (SC 21, TC-121 at render level)
- [x] A masked entry with a value above the large-value threshold does not move the row to the large section, and masked entries in a large row render as `(sensitive)` lines with markers (TC-128)
- [x] No sentinel value appears in the generic table or the large-value section (SC 23, TC-56 for these paths)
**Dependencies:** Task 17, Task 7, Task 8, Task 9

### Task 19: Masked entries in collection drift

**Priority:** High
**Description:** In `Rendering/CollectionDriftRenderer.cs` (`BuildLines`), a per-entry masked element
renders as `➖ (sensitive)` or `➕ (sensitive)`, and as `(sensitive)` inside order-changed sequences.
A whole-collection mask keeps its single `(sensitive)` line and no element lines. Masked values never
reach `FormatDriftCode`. Entry selection and path counts are unchanged.
**Acceptance Criteria:**

- [x] A per-entry masked change shows its marker line with no value, and the visible members still show (SC 11, TC-55 per-entry)
- [x] A whole-collection mask shows `(sensitive)` and no element lines; the existing drift test stays green (TC-55 whole)
- [x] An order change involving masked entries lists them as `(sensitive)` in the retained sequence (SC 21, TC-121 at drift level)
- [x] A drift entry containing a masked change counts its paths as before (SC 19)
**Dependencies:** Task 17, Task 12

### Task 20: Element-wise filter for masked positions and order

**Priority:** High
**Description:** In `Stages/AttributeFilteringStage.cs` (`ShouldSuppress`, collection branch):

1. A row with `OrderChanged` is never suppressed. The filter does not read `OrderChanged` today, so a
   case-variant reorder such as `[ABC,abc]` to `[abc,ABC]` is hidden by its positional case-only
   matches.
2. A position with a masked element is neutral only when both sides are masked and both statuses are
   `Unchanged`. Any other masked position stops suppression. Today a pair of masked nulls compares
   equal and is skipped, so a changed secret beside a case-only change could be hidden once masking is
   per entry.
3. The registry is never called with a masked element. Masked values are never read.

The core still learns no provider rule.
**Acceptance Criteria:**

- [x] A row where a masked entry changed is never suppressed, even beside case-only Azure ID differences (SC 28, TC-133 a and b)
- [x] A masked entry opposite a visible entry with case-variant text shows the row (SC 28, TC-133 c)
- [x] A masked unchanged pair beside a case-only visible difference hides the row (SC 28, TC-133 d)
- [x] The registry receives `P[i]` and texts for visible differing positions and is never called for a masked position (TC-103)
- [x] Same-length case-only reorder shows the row (SC 27, TC-132 order variant)
- [x] TC-100, TC-101 and TC-102 stay green (regression)
**Dependencies:** Task 17, Task 6

### Task 21: Invariance and leak tests across every output path

**Priority:** High
**Description:** Add the rule 7 test seam (architecture 5a). Run the stage and renderer on plans that
differ only in the values of masked entries, keeping each masked entry's equality with the masked
entries on the other side. Assert that the complete `AttributeChangeModel` and the Markdown are
identical. Cover cases (a) to (k) of TC-124, and a negative control in which the cross-side masked
relation changes and the output must differ. Extend the same substitutions to drift, summaries and
counts, and sweep the sentinels through every output. If a test shows a leak, fix the owning code path
and log it in `work-protocol.md` as a problem.
**Acceptance Criteria:**

- [ ] TC-124 cases (a) to (k) give identical model and Markdown, including replacing a masked value with one equal to a visible entry on either side (SC 25)
- [ ] The TC-124 negative control changes the output, proving the test can fail (SC 25)
- [ ] TC-126 drift entries are identical under the same substitutions (SC 25)
- [ ] TC-127 summary text and changed-attribute counts are identical (SC 25, SC 19)
- [ ] TC-56 sentinels, including their prefix, length and case variants, are absent from the generic table, large section, drift, summaries, diagnostics and log output (SC 23, SC 11)
**Dependencies:** Task 18, Task 19, Task 20

### Task 22: Case-only Azure ID filter, end to end

**Priority:** Medium
**Description:** Add the full-report tests for TC-131 and TC-132. A collection whose only difference is
Azure ID letter case is hidden under the default options, and also under `--show-unchanged-values`. It
produces no drift entry and no changed-attribute count. With `--ignore-azure-id-case-changes` disabled
it is shown. A case-only collection that also has an added member, a removed member, an order change
or an unrelated element change is shown. Fix any path that does not match the expected output.
**Acceptance Criteria:**

- [x] A case-only collection gives no row by default (SC 27, TC-131)
- [x] The same collection gives no row under `--show-unchanged-values` (SC 27, TC-131)
- [x] It produces no drift entry and does not count as a changed attribute (SC 27, TC-131)
- [x] With the flag disabled the row is shown (SC 27, TC-131)
- [x] Case-only plus an added member, a removed member, an order change or an unrelated change shows the row, and a drift entry appears only for that collection (SC 27, TC-132)
**Dependencies:** Task 20, Task 6

### Task 23: Element-less and empty-state verification

**Priority:** Medium
**Description:** Verify criterion 9 as reworded. Absent, `null` and `[]` are equivalent for change
detection and distinct whenever a row is shown (`ResourceChangeStage.IsElementless`, architecture 7a).
Add the tests for TC-40 to TC-43 and TC-116 at row and drift level that do not already exist, including a
sensitive element-less collection on create and on delete. Fix the stage only if a test fails.
**Acceptance Criteria:**

- [x] absent to `[]` and `null` to `[]` give no row by default (SC 9, TC-40, TC-41)
- [x] `[]` to `[]` gives no row by default (SC 9, TC-42)
- [x] Under `--show-unchanged-values` each of these shows a row in which each side's state is distinguishable (SC 9, TC-40, TC-41, TC-42)
- [x] `[]` beside elements renders `[]`; absent or `null` beside elements renders `-` or `null` (SC 9, TC-43)
- [x] A sensitive element-less collection on create or delete gives no row and no drift entry (SC 9, TC-41)
- [x] An unchanged compact collection under `--show-unchanged-values` renders as one row (TC-116)
**Dependencies:** Task 17

### Task 24: Regression, snapshots and provider tables

**Priority:** High
**Description:** After Tasks 18 to 23, re-verify the Task 15 criteria against the final output. Run the
provider-specific tests and confirm that no provider table snapshot changes. Regenerate the generic,
drift and demo snapshots with the `update-test-snapshots` skill only where output changed, and review
that each diff contains only per-entry masked rows, collection rows, and index-order changes. Run the
Markdown lint and invariant tests over the fixtures.
**Acceptance Criteria:**

- [ ] No provider table snapshot changes (SC 20, TC-90)
- [ ] The MsGraph `excludeUsers` update still shows a compact body row beside unchanged settings rows (SC 20, TC-91)
- [ ] Snapshot diffs contain only intended collection output and index-order changes (SC 20, TC-92)
- [ ] `MarkdownLintIntegrationTests` and `MarkdownInvariantTests` pass over the fixtures
**Dependencies:** Tasks 18, 19, 20, 21, 22, 23

### Task 25: UAT fixtures for the revised scenarios

**Priority:** Medium
**Description:** Rebuild `uat-plan.json` and `uat-plan.md` in this work-item folder to the resource
inventory in `uat-test-plan.md`. The current files have no per-entry secret, visible-to-sensitive,
whole-sensitive, large-secret, unknown-secret or case-only Azure ID scenarios. The Developer builds
fixtures only. The UAT Tester runs the platform checks later.
**Acceptance Criteria:**

- [ ] Every row of the resource inventory in `uat-test-plan.md` has a fixture entry
- [ ] Sentinel secrets appear only in entries marked sensitive, and the default-run rendering of the fixture contains none of them
- [ ] `uat-plan.md` lists the rendering runs and the expected result for each scenario
- [ ] Any scenario that cannot be built is recorded in `work-protocol.md`
**Dependencies:** Task 24

## Implementation Order (initial build, Tasks 1-15)

1. **Task 1 and Task 2 (parallel-capable, foundational).** Task 1 is path ordering used by all
   later row building. Task 2 is the pure diff with no dependencies, so it can be built and tested in
   isolation before any model work.
2. **Task 3.** The model and detection are the shared base. Every later rendering, filter, count, and
   drift task reads `AttributeChangeModel.Collection`, so it must exist first.
3. **Tasks 4, 5 and 6 (state handling and filtering).** Unknown, sensitive and element-wise filter rules
   refine the rows Task 3 emits. They are needed before rendering so that the renderer never sees
   an unhandled state.
4. **Tasks 7 and 8 (generic table rendering).** Update and replace rendering (Task 7) and create,
   delete and escaping (Task 8) use the finished model and states.
5. **Task 9 (large values).** Depends on the normal rendering path it extends.
6. **Task 10 (consumers).** Checks the summary and merging consumers against the finished row shape.
7. **Tasks 11, 12 and 13 (drift).** Drift reuses the same stage output, so it follows the model. Task 11
   builds the grouped data, 12 renders it, 13 verifies mode selection and scalar regression.
8. **Task 14 (names).** Depends on the generic rendering and on feature 147. Scheduled late so that
   the external dependency can be checked without blocking the core.
9. **Task 15 (regression and snapshots).** Last, because snapshots must reflect the final output.
   Regenerating earlier would produce snapshot churn.

## Rework Implementation Order

1. **Task 16** (differ key). Foundational: every masked row depends on the masked key.
2. **Task 17** (stage masking). Needs Task 16. Every rendering, filter and drift task reads the
   masked elements it produces.
3. **Tasks 18, 19 and 20** (table and large section, drift, filter). Each needs Task 17 and they touch
   different files, so they can run in parallel. Task 20 also builds on the Task 6 filter.
4. **Task 23** (element-less verification). Needs only Task 17, so it can run alongside Tasks 18 to 20.
5. **Task 22** (case-only end to end). Needs Task 20.
6. **Task 21** (invariance and leak sweep). Needs Tasks 18 to 20, because it tests their output.
7. **Task 24** (regression and snapshots). Last code task, after 18 to 23, so snapshots are regenerated
   once.
8. **Task 25** (UAT fixtures). Last, so the fixtures match the final output.

## Coverage Check

Success criteria, SC 1 to 28. Tasks 1 to 15 carry the earlier coverage; Tasks 16 to 25 carry the rework.

| SC | Tasks |
|---|---|
| 1 | 1 |
| 2 | 3, 7, 8 |
| 3 | 2, 7, 9 |
| 4 | 2, 7 |
| 5 | 2, 7, 9 |
| 6 | 2, 7 |
| 7 | 2, 14 |
| 8 | 7, 14 |
| 9 | 3, 8, 23 |
| 10 | 4, 12, 17 |
| 11 | 5 (superseded), 17, 18, 19, 21 |
| 12 | 3 |
| 13 | 1, 3 |
| 14 | 11, 12 |
| 15 | 11, 12 |
| 16 | 11 |
| 17 | 13 |
| 18 | 13 |
| 19 | 10, 11, 19, 21 |
| 20 | 6, 8, 10, 24 |
| 21 | 16, 17, 18, 19 |
| 22 | 16, 17, 18 |
| 23 | 17, 18, 21 |
| 24 | 18 |
| 25 | 21 |
| 26 | 17, 24 |
| 27 | 6, 20, 22 |
| 28 | 20 |

Test-plan scenarios: every `TC-` identifier in `test-plan.md` appears in at least one task. Tasks 1
to 15 carry the earlier scenarios, and the new scenarios are TC-57, TC-58, TC-59, TC-120 to TC-133 and
TC-53 to TC-56, mapped in Tasks 16 to 22 and 24. The mechanical check runs on the committed file.
