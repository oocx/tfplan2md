# Tasks: Compact, Semantically Accurate Collection Diffs

Sources: [specification.md](specification.md), [architecture.md](architecture.md),
[test-plan.md](test-plan.md).

References used below: **SC n** is the n-th success criterion in `specification.md`
(numbered the same as C1-C20 in `test-plan.md`). **TC-nn** is a test-plan scenario.
Every test is TUnit, named `MethodName_Scenario_ExpectedResult`, in
`src/tests/Oocx.TfPlan2Md.TUnit`.

## Tasks

### Task 1: Natural ordering of numeric path indices
**Priority:** High
**Description:** Add `AttributePathComparer` in `MarkdownGeneration/Helpers/`. It splits a
path into segments, compares `[n]` index segments by numeric value and every other segment
ordinally (current semantics). Use it for the key ordering in `ResourceChangeStage.BuildAttributeChanges`
(replacing `Order()`) and for the `AttributePath` ordering in `ReportModelBuilder.GroupDrift`.
Scope is ordering only; no collection compaction in this task.
**Acceptance Criteria:**
- [ ] `a[2]` sorts before `a[10]`, and `a[1]` before `a[2]` (SC 1, TC-01)
- [ ] Nested indexed paths sort per segment: `r[2].x[9]`, `r[2].x[10]`, `r[9].y`, `r[10].x[1]` (SC 1, TC-02)
- [ ] Non-index segments keep ordinal order (`Zed` before `abc`); equal paths compare 0; paths without brackets and non-numeric brackets such as `tags["a"]` fall back to ordinal (TC-02)
- [ ] An object list with indices 0..11 renders `[2]` before `[10]` in the generic table, and drift orders its `AttributePath` the same way (TC-03)
- [ ] Existing tests remain green; any snapshot change in this task is limited to multi-digit index ordering in generic or drift output
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
- [ ] `[a,c,d]` to `[a,b,c,d]` gives one `Added` (b), three `Unchanged`, `OrderChanged` false (SC 3, TC-20)
- [ ] The mirror removal gives one `Removed` (TC-21)
- [ ] `[a,a,b]` to `[a,b]` gives exactly one `Removed` occurrence of `a` and two `Unchanged` (`a`, `b`) occurrences (SC 4, TC-23)
- [ ] `[a,b]` to `[a,a,b]` gives exactly one `Added` occurrence (SC 4, TC-24)
- [ ] `[a,b]` to `[c,d]` gives two `Removed`, two `Added`, no `Moved` (TC-20b)
- [ ] `[a,b]` to `[b,a]` gives no `Removed`, no `Added`, `OrderChanged` true, at least one `Moved` (SC 5, TC-26)
- [ ] `[a,b,c]` to `[c,a,b,d]` gives `Added` d and `OrderChanged` true; `[a,b,a]` to `[a,a,b]` gives `OrderChanged` true with no add or remove (SC 5, TC-28)
- [ ] Sorted-collection insertion and removal (`[a,c,e]` to `[a,b,c,e]` and back, plus sorted GUID lists, three parameterised cases) gives `OrderChanged` false (SC 6, TC-29)
- [ ] Two equal-value elements with different display names compare equal; the differ API accepts raw values only (SC 7, TC-31)
- [ ] `[]` to `[]` gives no change (TC-42, model level)
- [ ] Above the cap, a 1000-element list with one change completes within a generous bound modelled on `DiffComputationPerformanceTests`; retained occurrences are `Unchanged`; `OrderChanged` is true only when retained key sequences differ (TC-106)
- [ ] Running the diff twice on the same input yields identical statuses (TC-115)
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
- [ ] A primitive list `[a,b,c]` on create produces exactly one row for its path, with no `path[0]` rows (SC 2, TC-14 row count)
- [ ] A primitive list nested inside an object list (`security_rule[0].source_address_prefixes`) becomes one row; sibling scalar rows of `security_rule[0]` remain separate and naturally sorted (SC 2, TC-14)
- [ ] `[]` to `[a]` yields distinct before states `Empty` and `Elements`; `null` to `[a]` and absent to `[a]` yield `Null` and `Absent` before states, not collapsed (SC 9, TC-40, TC-41, TC-43 model)
- [ ] `["1"]` to `[1]` yields one `Removed` and one `Added`; `true` vs `"true"` and `null` vs `"null"` are different elements; `1` vs `1.0` assertion matches observed raw-text behaviour (SC 12, TC-60)
- [ ] `[1,"a",true,null]` keeps each element with its own kind, and the `null` element is kept as `null` (SC 12, TC-61)
- [ ] A list of objects, a nested list `[[1],[2]]`, and a list mixing one object element with primitives all keep indexed rows; one object element disqualifies the path on either side; a primitive list changed to a list of objects is not compacted (SC 13, TC-62, TC-63, TC-64)
- [ ] An `output_changes` array keeps its current path behaviour (TC-114)
- [ ] A type change from array to scalar or object renders through existing rendering with no exception (TC-112)
- [ ] An empty-to-empty case produces no row (TC-42)
- [ ] `Collection` is null on every scalar row; a `Collection` row never sets `IsLarge` from its canonical text
**Dependencies:** Task 1, Task 2

### Task 4: Unknown and partially unknown collections
**Priority:** High
**Description:** Handle `after_unknown` and `before_unknown` shapes. A whole collection unknown
(`true` at `P`) sets `AfterState = Unknown`, and the existing `ApplyComputedKnownAfterApplyOverride`
produces the After text including any reference hint. A partially unknown collection sets
`IsUnresolved = true`, marks unknown-flagged elements `Status = Unknown`, and computes no
`Removed`, `Added` or `Moved` markers. Malformed shapes (`true` vs array vs absent) must not throw.
**Acceptance Criteria:**
- [ ] Whole-collection unknown after gives a known-after-apply rendering, with the configuration-reference hint when present (SC 10, TC-44)
- [ ] `after_unknown: [false,true,false]` gives an unknown element with the known-after-apply label and no marker in the row (SC 10, TC-45)
- [ ] The diff for a partially unknown collection reports `IsUnresolved` true and no statuses other than `Unknown` and `Unchanged` (SC 10, TC-46)
- [ ] Malformed `after_unknown` shapes and unknown-only-in-before inputs produce a row and no exception (TC-113)
**Dependencies:** Task 3

### Task 5: Sensitive collections in the model and generic cell
**Priority:** High
**Description:** When `before_sensitive` or `after_sensitive` marks `P` or any element under it,
and `--show-sensitive` is off, set both states to `Masked`, empty the element lists, show
`(sensitive)` on both sides, and keep `IsSensitive` true. With `--show-sensitive` on, render the
normal diff and keep `IsSensitive` true. Masking is at the whole-collection level; no count, order
or duplicate information may reach the model for a masked row. Handle the `before_sensitive` shapes
`true`, array of bool, and object.
**Acceptance Criteria:**
- [ ] A sensitive whole collection shows `(sensitive)` in both Before and After cells (SC 11, TC-50)
- [ ] One sensitive element masks the whole collection; neither non-sensitive sibling values nor the collection length appear in the output (SC 11, TC-51)
- [ ] A reorder or duplicate change on a sensitive collection renders no `➖`, `➕` or `🔀` (SC 11, TC-52)
- [ ] A masked row's element lists are empty and both states are `Masked` (SC 11, TC-53)
- [ ] With `--show-sensitive`, the normal diff renders and `IsSensitive` stays true (SC 11, TC-54)
- [ ] A sensitive value present only in `after_sensitive` or only in `before_sensitive` is masked on the corresponding delete or create (SC 11, TC-56)
- [ ] Sentinel secret values are absent from the full Markdown output and the summary (SC 11, TC-56)
**Dependencies:** Task 3

### Task 6: Element-wise attribute filter for collection rows
**Priority:** High
**Description:** Make `AttributeFilteringStage` evaluate a collection row element-wise. When both sides
have equal length and every differing position is suppressed by the registry (called with name
`P[i]` and the two element texts), suppress the row. Otherwise render it normally. The core must
not learn provider filter rules. Preserves the fully-suppressed case-only Azure ID outcome.
**Acceptance Criteria:**
- [ ] Same-length lists whose only differences are Azure ID case changes produce no row (SC 20, TC-100)
- [ ] A list with one non-case difference renders its row (TC-101)
- [ ] Lists of different lengths render their row (TC-102)
- [ ] The registry receives indexed names `P[i]` and element texts for each differing position (TC-103)
- [ ] A partially case-only change shows the case-variant elements as `➖` and `➕` (open question 3 in state.json, recorded as assumed)
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
- [ ] An update to a primitive list renders one row with Before and After columns (SC 2, TC-11)
- [ ] A replace renders the same single-row layout (SC 2, TC-12)
- [ ] Inserting one element into a 12-element list renders exactly one `➕` and no `➖`, with no replacement cascade (SC 3, TC-22)
- [ ] Duplicate values render with multiplicity: value `a` appears twice in After with a single `➕` marker (SC 4, TC-25)
- [ ] An order-only change renders a row (not filtered as no-op), no `➖` or `➕`, and After ends with `🔀 order changed` (SC 5, TC-27)
- [ ] A sorted-collection membership change renders no `🔀 order changed` line (SC 6, TC-30)
- [ ] An `excludeUsers`-style GUID collection without any name mapping renders raw GUIDs with markers (SC 8, TC-34)
- [ ] Element text identical to a marker (`➕ x`) stays distinguishable from a real added marker (TC-111)
**Dependencies:** Task 3

### Task 8: Generic table rendering for create, delete and value edge cases
**Priority:** Medium
**Description:** Render create and delete in the single-value table with the full collection and no markers.
`Empty` renders `[]`; `Null` and `Absent` keep the existing scalar `-` rendering. Escape element text
containing `|`, backticks, `<br>`, newlines, HTML characters and empty strings (TC-110). Ensure the
`--show-unchanged-values` option renders an unchanged compact collection. Confirm that existing value
formatters matching indexed names (AzureDevOps `^members?(?:\[\d+\])?$`) still apply per element.
**Acceptance Criteria:**
- [ ] A create with `[a,b,c]` renders one row with the three values separated by `<br>` (SC 2, TC-10)
- [ ] A delete renders the full before collection in the single-value table (SC 2, TC-13)
- [ ] `[]` renders `[]`, absent renders `-` (SC 9, TC-40)
- [ ] Empty before and element after renders distinct cells, with no pair collapsing where today they differed (SC 9, TC-43)
- [ ] Special characters in elements are escaped and do not break the table (TC-110)
- [ ] An AzureDevOps `members[0]`-style formatter still applies to a compact list element (SC 20, TC-107)
- [ ] With `--show-unchanged-values`, an unchanged compact collection renders as one row (TC-116)
**Dependencies:** Task 3

### Task 9: Large collection elements
**Priority:** Medium
**Description:** When any element is a large value under the existing check (including the formatter
consult), mark the row `IsLarge` and render in the large-attribute section, one occurrence per line,
with the same markers. Do not use the line-LCS large diff for compact collections, so a reorder never
becomes a removed/added pair.
**Acceptance Criteria:**
- [ ] A large element in an update renders in the large section, one occurrence per line, with `➖` and `➕` (SC 3, TC-104)
- [ ] A large-element reorder reports an order change and not a removed/added pair (SC 5, TC-105)
- [ ] A single-element large collection still respects the existing large-value threshold (existing tests stay green)
**Dependencies:** Task 7, Task 8

### Task 10: Counts and consumer compatibility
**Priority:** Medium
**Description:** Verify the consumers listed in architecture section "Consumers": changed-attribute
summary (`ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary`, `ResourceSummaryBuilder`),
`ReportModelBuilder.ParentChildMerging` prefix matching, and `DefaultResourceRenderer.GetAttributeFindingIndicator`
prefix matching for SARIF paths. Fix any consumer that does not tolerate a compact row, without changing
provider factories.
**Acceptance Criteria:**
- [ ] A 50-element list with 10 adds and removes counts as one changed attribute in the summary text (SC 19, TC-81)
- [ ] A collection plus a scalar counts as two, matching table row count (SC 19, TC-82)
- [ ] A compact collection counts consistently in the summary and detail table (SC 19, TC-84)
- [ ] A compact row named as a prefix of a child row still matches parent-child merging (SC 20, TC-108)
- [ ] A SARIF path `x[3]` marks the compact row `x` (SC 20, TC-109)
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
- [ ] A group member inserted into `members` yields one collection entry with one path, not `members[n]` transitions (SC 14, TC-70)
- [ ] Many shifted indices yield no per-index drift entries (SC 14, TC-71)
- [ ] Two collection paths of one resource share one entry with both paths, naturally ordered (SC 15, TC-72)
- [ ] Each path in one entry carries its own diff, including one add and one order-only change (SC 15, TC-73)
- [ ] Two resources with identical collection changes get two separate entries (SC 16, TC-74)
- [ ] Drift containing three collection paths reports three changed collections (SC 19, TC-83)
**Dependencies:** Task 1, Task 3

### Task 12: Collection drift rendering
**Priority:** High
**Description:** Extend `ReportRenderer.RenderDriftSection` to render when either the scalar or collection drift list
is non-empty. Order by resource type; within a type, scalar groups first (existing order), then collection entries
by address. Each collection entry is one `<details>` block with the summary text from architecture section 8
(`N changed collection(s): ...`). Lines are deltas only: `➖`, `➕`, `🔀 order changed`. Use `FormatDriftCode`
for values. Masked collections render `(sensitive)` and no element lines.
**Acceptance Criteria:**
- [ ] A collection-only drift result still renders the drift section (TC-80b)
- [ ] Ordering is by type, then scalar before collection, then address (TC-80c)
- [ ] A masked collection in drift renders `(sensitive)` and no element lines (SC 11, TC-55)
- [ ] Collection drift lines use the same marker glyphs as the table and no Before/After columns (SC 14, TC-70)
- [ ] An unknown collection in drift renders as unresolved, not as an element diff (SC 10, TC-46 drift variant)
**Dependencies:** Task 11

### Task 13: Drift mode selection and scalar grouping regression
**Priority:** High
**Description:** Confirm that `--drift all`, omission of the option, `--drift relevant` and `--drift none` apply to
collection drift entries exactly as to scalar drift. Mode selection happens on `ResourceChangeModel`s before grouping.
Confirm that feature-145 scalar grouping across resources is unchanged. Fix nothing outside these paths.
**Acceptance Criteria:**
- [ ] `--drift all` includes collection entries (SC 18, TC-77)
- [ ] Omitting `--drift` behaves as `all` (SC 18, TC-78)
- [ ] `--drift relevant` includes collection entries only for relevant resources; suppressed and no-op drift stays suppressed (SC 18, TC-79)
- [ ] `--drift none` produces no collection entries and no drift section (SC 18, TC-80)
- [ ] Two resources with the same scalar change still group across resources (SC 17, TC-75)
- [ ] A resource with scalar and collection drift shows the scalar in its group and the collection in its own entry (SC 17, TC-76)
- [ ] Existing feature-145 drift tests remain green without edits
**Dependencies:** Task 11, Task 12

### Task 14: GUID name annotations and equality independence
**Priority:** Low
**Description:** Verify that display-name mapping from feature 147 can annotate collection members beside the
raw value and never changes equality or merges distinct members. This task depends on feature 147 being present
on the branch. If it is not, write the tests against the formatter extension point and record the gap in
work-protocol.md.
**Acceptance Criteria:**
- [ ] A GUID mapped to a name on one side only does not change the row, which is absent (SC 7, TC-32)
- [ ] Two distinct GUIDs mapped to the same name remain two elements, one `➖` and one `➕` (SC 7, TC-33)
- [ ] With a mapping, the name appears as an annotation beside the same GUID (SC 8, TC-35)
- [ ] If feature 147 is absent, the gap is recorded in work-protocol.md (SC 8)
**Dependencies:** Task 7

### Task 15: Regression guard and snapshots
**Priority:** High
**Description:** Run the provider-specific tests and confirm no provider table snapshot changes. Regenerate generic
table, drift and comprehensive demo snapshots with the `update-test-snapshots` skill and review that the diff
contains only primitive-collection rows and multi-digit index ordering changes. Run the Markdown lint integration
tests over the new fixtures.
**Acceptance Criteria:**
- [ ] Existing AzureAD, AzureDevOps, Azapi, NSG, firewall, role assignment, APIM and MsGraph provider tests pass with no snapshot diff in provider tables (SC 20, TC-90)
- [ ] The MsGraph `excludeUsers` update shows a compact body row beside unchanged `api_version` and `url` settings rows (SC 20, TC-91)
- [ ] Generic, drift and demo snapshot diffs contain only the intended collection rows and index-order changes (SC 20, TC-92)
- [ ] `MarkdownLintIntegrationTests` and `MarkdownInvariantTests` pass over the new fixtures (regression, test-plan R type)
**Dependencies:** Task 9, Task 10, Task 12, Task 13, Task 14

## Implementation Order

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

## Coverage Check

Every spec success criterion SC 1-20 is covered: SC 1 by Task 1; SC 2 by Tasks 3, 7, 8; SC 3 by Tasks 2, 7, 9;
SC 4 by Tasks 2, 7; SC 5 by Tasks 2, 7, 9; SC 6 by Tasks 2, 7; SC 7 by Tasks 2, 14; SC 8 by Tasks 7, 14;
SC 9 by Tasks 3, 8; SC 10 by Tasks 4, 12; SC 11 by Tasks 5, 12; SC 12 by Task 3; SC 13 by Tasks 1, 3;
SC 14 by Tasks 11, 12; SC 15 by Tasks 11, 12; SC 16 by Task 11; SC 17 by Task 13; SC 18 by Task 13;
SC 19 by Tasks 10, 11; SC 20 by Tasks 6, 8, 10, 15.

Every test-plan scenario is mapped to a task, including architecture-derived ones (TC-100 to TC-116).
The reverse mapping was checked mechanically: each `TC-` identifier in `test-plan.md` appears in this file.
