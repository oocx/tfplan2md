# Architecture: Compact, Semantically Accurate Collection Diffs

## Status

Accepted (auto-decided: one clearly superior placement; the table layout is
user-visible and is validated at the UAT gate)

Revised 2026-10-16 after the Maintainer approved the revised specification. Sections 3,
5, 6, 7, 8 and the Consequences replace whole-collection masking with per-entry,
per-side masking that carries change markers (specification "Sensitive Values in
Collections"), and section 6 records the Maintainer's case-only ruling. Section 5a is
new. The placement decision (Option A) is unchanged.

## Context

Feature specification: [specification.md](specification.md)

Today every attribute row comes from one place:
`ResourceChangeStage.BuildAttributeChanges` (`MarkdownGeneration/Stages/ResourceChangeStage.cs`).
It flattens `before`, `after`, `before_sensitive` and `after_sensitive` with
`JsonFlattener` into `path -> string` dictionaries, orders the union of keys with
ordinal `Order()`, and emits one `AttributeChangeModel` per leaf path. Three properties
of that pipeline drive this design:

1. **Flattening is lossy.** `JsonFlattener` turns the string `"1"` and the number `1`
   into the same text, and an empty array produces no key at all. The spec requires
   type-accurate equality and `[]` distinct from `null`/absent, so the collection diff
   must be computed from the `JsonElement` values, not from the flat dictionaries.
2. **Everything downstream counts rows.** The changed-attribute summary
   (`ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary`), `ResourceSummaryBuilder`,
   `AttributeFilteringStage`, `DisplayFilteringStage`, parent-child inline removal and
   the drift grouping (`ReportModelBuilder.GroupDrift`) all operate on
   `ResourceChangeModel.AttributeChanges`. The spec requires that summaries and tables
   agree on one changed attribute per collection.
3. **Drift reuses the same stage.** `BuildResourceDrift` runs `resource_drift[]` through
   `ResourceChangeStage`, `AttributeFilteringStage` and `DisplayFilteringStage`, then
   `GroupDrift` groups by `(type, path, before, after)`. Whatever the stage emits is what
   drift sees.

Consumers of `AttributeChanges` outside the core were checked:

| Consumer | Uses | Effect of a compact row |
|---|---|---|
| `MsGraphResourceViewModelFactory` | scalar setting names (`api_version`, `url`, ...) | none |
| `AzureRMApimNamedValueFactory` | scalar `value` | none |
| `RoleAssignmentViewModelFactory` | role-assignment attributes (no primitive lists) | none |
| `AzureAdSummaryBuilder.Groups` | exact top-level name, known-after-apply prefix | none; member counts come from JSON |
| `ReportModelBuilder.ParentChildMerging` | exact name or `name[` / `name.` prefix | compact row named `name` still matches |
| `DefaultResourceRenderer.GetAttributeFindingIndicator` | exact name or `name[` / `name.` prefix | SARIF path `x[3]` still marks row `x` |
| `AzApiResourceIdCaseChangeFilter`, `AzureResourceIdCaseChangeFilter` | per-row before/after text | needs element-wise evaluation (see below) |
| Value formatters (`MatchPattern` on attribute name) | indexed names, e.g. AzureDevOps `^members?(?:\[\d+\])?$` | must keep receiving `path[i]` per element |

Provider-specific tables that read plan JSON directly (azapi body tables, NSG and
firewall rule tables, role assignments) are not affected and are out of scope. The
motivating example in issue #674 (`msgraph_resource ... body.conditions.users.excludeUsers`)
is rendered by the generic table: `MsGraphResourceRenderer` delegates to
`DefaultResourceRenderer`.

## Options Considered

### Option A: Compact at model-build time in `ResourceChangeStage` (selected)

Detect primitive collections while building attribute changes, emit one
`AttributeChangeModel` per collection carrying a structured diff, and let every
downstream consumer see one logical attribute.

Pros:

- Summary counts, filtering, display filtering and drift all see one logical row with no
  extra code: the count requirement holds by construction.
- Drift gets the compact rows for free because it already runs through this stage.
- Has the raw `JsonElement` values, sensitivity and unknown markers, and configuration
  references (for known-after-apply labels) in one place, so type-accurate equality and
  masking are decided before anything is rendered.
- Provider-neutral: it lives entirely in `MarkdownGeneration`.

Cons:

- Changes the meaning of `AttributeChanges` from "leaf paths" to "logical attributes",
  which every consumer must tolerate. The audit above shows they do, except the
  case-only ID filters, which need a small adaptation.
- Provider snapshot tests act as the guard that no provider table changed.

### Option B: Compact at render time in `DefaultResourceRenderer`

Keep the model flat and regroup indexed rows when writing the generic table.

Pros:

- Zero change to the model or to provider factories.

Cons:

- The flat rows have already lost type information and the empty-collection case, so the
  renderer would have to re-read the plan JSON and redo sensitivity and unknown handling.
- The changed-attribute summary is built from the flat rows and would disagree with the
  table, violating the count success criterion unless it is also recomputed.
- Drift does not use the renderer, so collection drift would need a second, separate
  implementation of the same diff.

### Option C: A separate post-factory compaction stage

Run a new stage after `ApplyViewModel` that rewrites `AttributeChanges` only for
resources that the generic renderer will draw.

Pros:

- Provider factories see the flat rows they saw before.

Cons:

- Which renderer draws a resource is decided in the rendering layer
  (`ResourceRendererRegistry`); the model layer would need to duplicate that decision.
- Still has to re-read JSON (same lossiness as B) or carry extra data through the model.
- The audit shows no factory depends on indexed rows, so the protection it buys is not
  needed.

### Layout alternatives for the update table (sub-decision)

- **Delta only** (Before = removed occurrences, After = added occurrences): most
  compact, but the `Before`/`After` headers would no longer mean "value before/after", a
  pure addition renders as `-` in Before (reads as "was empty"), and an order change
  still needs the full sequences.
- **Full collections with markers** (selected): each column shows the whole collection
  as before/after, marking removed occurrences in Before and added occurrences in After.
  Column semantics stay truthful, duplicates and order are visible without extra
  notation, and partial-unknown, unchanged and create/delete rows use the same model
  without markers. Cost: a long list with one change produces a tall cell. It is still
  one row instead of one row per index.

The layout is user-visible and is confirmed at the UAT gate; it is not a contested
architecture choice.

## Decision

Option A, with the full-collection marker layout. The placement is clearly superior:
B and C both lose type information and need a second implementation for drift, and the
consumer audit removes C's only benefit.

### 1. Natural path ordering

Add `AttributePathComparer` (`MarkdownGeneration/Helpers/`): splits a path into
segments, compares `[n]` index segments numerically and all other segments ordinally
(the current semantics). Use it for:

- the key ordering in `BuildAttributeChanges` (replaces `Order()`), so non-primitive
  collections and nested indices sort `[2]` before `[10]`;
- the `AttributePath` ordering in `GroupDrift`.

### 2. Primitive collection detection

New `PrimitiveCollectionDiffer` (`MarkdownGeneration/Helpers/`). Before flattening, walk
`before` and `after` in parallel and record each path `P` where:

- at least one side is a JSON array at `P`;
- the other side is an array, `null` or absent at `P`;
- no element on either side is an object or array (an empty array qualifies).

Detection runs at any depth, so a primitive list inside an object list
(`security_rule[0].source_address_prefixes`) becomes one compact row; the outer object
list keeps its indexed rows (naturally sorted). `BuildAttributeChanges` skips every flat
key at or below a detected `P` and emits one row for `P` instead.

### 3. Model

`AttributeChangeModel` gains one optional property:

```csharp
public CollectionChangeModel? Collection { get; init; }   // null for scalar rows
```

`CollectionChangeModel` (`MarkdownGeneration/Models/`) holds:

- `BeforeState` / `AfterState`: `Absent | Null | Empty | Elements | Unknown | Masked`.
  `Masked` means the whole collection is masked (section 5, rule 1). A collection with
  individually masked entries is `Elements`;
- `Before` / `After`: ordered `CollectionElementModel` lists (`Index`, display `Value`,
  value `Kind`, `Status`: `Unchanged | Removed | Added | Moved | Unknown`, and
  `IsMasked`);
- `OrderChanged` and `IsUnresolved`.

A masked element (`IsMasked = true`) carries `Value = null` and a neutral
`Kind = JsonValueKind.Undefined`. Its index and status are real; nothing derived from its
value is stored. The model therefore cannot leak a masked value, its type, its length
or its casing, whatever a renderer does with it.

`Before` / `After` on the row keep a canonical text, so consumers that only read strings
keep working: `(sensitive)` when the whole collection is masked, the existing
known-after-apply label when the whole collection is unknown, `null` for a JSON `null`,
`-` when absent, and otherwise compact JSON of the array built **from the model
elements**, with each masked element written as the bare token `(sensitive)` (for
example `["a",(sensitive),"b"]`). The canonical text is never built from the raw JSON
array, because that contains the masked values (`CollectionValues.CanonicalText`). No
consumer uses the canonical text for equality; change detection comes from the model
(section 7a).
Collection rows set `IsLarge` from their non-masked elements (see 7), never from the
canonical text.

When the whole collection is masked, the element lists are left empty, so the renderer
cannot leak values, counts or order.

### 4. Diff algorithm

Element key = `(Masked, JsonValueKind category, raw text)`, so `"1"` and `1` are
different and display names never take part in equality. For a non-masked element,
`Masked = false` and the raw text is the value. For a masked element, `Masked = true` and
the raw text is an opaque token, never the value (section 5a). Because `Masked` is part of
the key, a masked and a non-masked element are never equal (specification rule 4).

The differ consults keys only through equality between a before key and an after key:
prefix and suffix trimming, the LCS, and multiset matching all compare across sides, and
nothing sorts, hashes into an output order, or compares keys on the same side. This is
what makes specification rule 7 hold: the statuses are a function of the cross-side
equality relation alone. Any later change to the differ must keep this property.

1. Trim the common prefix and suffix.
2. Compute the LCS of the remaining key sequences. Matched occurrences are `Unchanged`.
   Reuse the existing LCS in `MarkdownHelpers/DiffComputation.cs` (extract it into an
   internal helper if it has to be callable from outside `MarkdownHelpers`).
3. Match the remaining before/after occurrences by key, multiset style, in positional
   order. Matched pairs are `Moved` (retained, but out of order); the remainder are
   `Removed` (before) and `Added` (after).
4. `OrderChanged = any Moved`.

This gives the spec's semantics directly:

- Duplicates keep their multiplicity: removing one of two `a` leaves one `Unchanged`.
- An order-only change yields only `Moved` occurrences, never a removed/added pair.
- A sorted collection (how Terraform emits sets) has every retained element in the LCS,
  so a membership change never reports an order change.

If the trimmed matrix exceeds the existing LCS cell cap, skip step 2 and mark
occurrences `Unchanged` by first-occurrence multiset matching, with `OrderChanged`
true only when the retained key sequences differ. That keeps memory bounded and
semantics conservative.

### 5. Unknown and sensitive values

- **Whole collection unknown** (`after_unknown` at `P` is `true`): `AfterState = Unknown`.
  The existing `ApplyComputedKnownAfterApplyOverride` produces the After text, including
  the reference hint.
- **Partially unknown** (any element under `P` unknown): `IsUnresolved = true`. Elements
  flagged in `after_unknown` get `Status = Unknown` and render as the known-after-apply
  label. No `Removed`/`Added`/`Moved` markers are computed, so nothing is presented as a
  definitive known change.
- **Sensitive**: see section 5a. With `--show-sensitive` no element is masked, the normal
  diff renders, and `IsSensitive` stays `true` when Terraform marks `P` or any of its
  elements.

### 5a. Masking sensitive collections and entries (revised)

The specification ("Sensitive Values in Collections", rules 1 to 9) replaces the
original whole-collection masking. All of the following applies only when
`--show-sensitive` is off. `ResourceChangeStage.BuildCollectionAttribute` decides
masking once, before any element text or key exists, so no later stage has to repeat
the decision.

1. **Whole collection** (rule 1). If `SensitivityHelper.IsSensitiveAttribute(P, ...)` is
   true, meaning the resource, an ancestor of `P` or `P` itself is marked `true` on
   either side, both states are `Masked` and the element lists are empty. Change
   detection uses `RawKey`, as before, so the row only reveals that the collection
   changed (rule 5, last item). This is the unchanged path for sensitive attributes.
2. **Individual entries, per side** (rules 2 and 3). Otherwise, before element `i` is
   masked if and only if `before_sensitive` flattens to `"true"` at `P[i]`. After element
   `j` is masked if and only if `after_sensitive` flattens to `"true"` at `P[j]`. Each side
   is read only from its own map, so a value that Terraform does not mark on its side is
   shown. This differs deliberately from the per-index rows on `main`, which masked a
   position on both sides when either side was marked.
3. **Opaque comparison tokens** (rules 4, 6 and 7). Before calling
   `CollectionElementDiffer`, the stage replaces each masked element's key with
   `(Masked: true, Kind: Undefined, Raw: "#n")`. `n` numbers the distinct
   `(kind, raw value)` pairs of masked elements on both sides together. The token map is
   a local variable of the method and is discarded after the diff. Consequences:
   - masked-to-masked equality across sides is exactly value equality (rule 4);
   - masked-to-visible equality is always false, because `Masked` differs in the key
     (rule 4). Example 3 of the specification (`"x"` visible before, `<x>` after) yields
     `➖ x` and `➕ (sensitive)`;
   - the token text never reaches the model, so it cannot be rendered, and the differ
     only tests it for cross-side equality (section 4).
4. **Element models** (rules 5 and 6). Masked occurrences become `CollectionElementModel`
   records with `IsMasked = true`, `Value = null` and `Kind = Undefined`. Their status
   comes from the diff like any other element, so they carry `Removed`, `Added`,
   `Unchanged` and `Moved` and take part in `OrderChanged` (rule 5).
5. **Derived properties ignore masked elements.** `HasMixedKinds` (the quoting decision
   for visible strings), `HasLargeElement` (and therefore `IsLarge`) and the canonical
   text (section 3) skip masked elements or write the placeholder. Otherwise a masked
   value's type or length would change how visible entries render, which rule 7 forbids.
6. **No same-side duplicate indication** (rule 6, last item). Each masked occurrence is
   its own element model. Nothing groups, counts or labels masked elements, and the
   renderers never compare two elements of the same side.
7. **Unknown entries** (rule 9). In the partially unknown path (section 5), an after
   element that is unknown and marked in `after_sensitive` renders the existing combined
   placeholder `🔒` + known-after-apply label, as `ApplyComputedKnownAfterApplyOverride`
   does for a sensitive, computed scalar. A known masked element in that path renders
   `(sensitive)`. As before, no markers are computed while the collection is unresolved.
   A wholly unknown collection with a masked before side keeps the existing override.

**Rule 7 test seam.** Because masking happens before keys are built, a unit test can
assert rule 7 directly. It runs the stage twice on plans that differ only in the values of
masked entries, with the cross-side masked equality relation unchanged, including a
substitution that makes a masked value equal to a visible one. It then compares the
complete `AttributeChangeModel` and the rendered markdown. The Quality Engineer owns the
concrete cases.

Options considered for where to mask entries:

- **Mask in the stage, before diffing, with opaque tokens** (selected): the raw value
  never leaves one method, and rule 4 and rule 7 hold by construction.
- **Carry raw values in the model and mask in each renderer**: the generic table, the
  large-value section and drift would each need the masking rule, and a missed branch
  would leak the value. Rejected because the specification ranks an accidental leak as
  the most serious failure.
- **Diff on raw values, then strip them**: simpler key handling, but a masked entry
  would match an equal visible entry, which breaks rule 4 and leaks the value through the
  marker (specification example 3). Rejected.

The first option is clearly superior, so this is not a contested choice.

### 6. Attribute filters (case-only Azure ID suppression)

The Maintainer ruled that a collection is hidden when a case-only Azure ID difference is
its only change. When it has any other change, the row is shown, and the case-variant
entries may appear as removed and added. `AttributeFilteringStage` evaluates a
collection row element-wise, by position. The row is suppressed only when all of the
following hold:

- the collection is not unresolved, both states are `Elements`, both sides have the same
  length, and `OrderChanged` is false;
- at every position `i`, one of these is true:
  - both elements are non-masked and equal (same kind and text);
  - both elements are masked and both have status `Unchanged`;
  - both elements are non-masked, they differ, and the registry suppresses the pair
    (called with name `P[i]` and the two element texts);
- at least one position was suppressed by the registry.

Any other position stops the row from being suppressed, including a masked element
opposite a non-masked one, a masked element with any status other than `Unchanged`, or
a non-suppressed visible difference. The core never learns what the provider filters do.

The masked condition needs no masked value and no token. When every earlier position
satisfies the rule, each one contributes the same number of `Unchanged` occurrences to
both sides. The LCS pairs unchanged occurrences in order, so a masked `Unchanged` pair at
the same position is matched to each other, and therefore equal under rule 4. A masked
entry can therefore never be part of a case-only difference (specification rule 8), and
the filter's outcome depends on masked values only through masked-to-masked equality
(rule 7).

The filter runs for drift as well, because drift reuses this stage. `--show-unchanged-values`
does not bring a suppressed collection back, matching the scalar filter.

### 7. Generic table rendering

`DefaultResourceRenderer` checks `attribute.Collection`:

- Each element is formatted with the existing
  `FormatAttributeValueTableWithRegistryResource`, passing the indexed name `P[i]`
  (the element's index on its own side). Existing formatters that match indexed names
  keep working, and feature 147 annotations arrive through the same pipeline. A `null`
  element renders as `null`.
- A masked element (`IsMasked`) renders as `` `(sensitive)` `` and is checked **before**
  the `null` and formatter branches. It never reaches a value formatter, so no display
  name or other annotation is derived from it (rule 6). The order of those checks matters,
  because a masked element's `Value` is also `null`. Markers apply to it exactly as to a
  visible element.
- Elements are joined with `<br>`, the existing table-cell line break.
- **Create / delete** (single-value table): the full collection, no markers.
- **Update / replace** (before/after table): Before holds the full before collection with
  `Removed` occurrences prefixed `➖`; After holds the full after collection with `Added`
  occurrences prefixed `➕`. When `OrderChanged`, After ends with a `🔀 order changed`
  line. `Moved` elements carry no per-element marker; both sequences are visible.
- Element-less sides render by state: `Empty` as `` `[]` ``, `Null` as `` `null` `` and
  `Absent` as `-`. `Masked` and `Unknown` render the row's canonical text from section 3.
  These cells are only reached when the row exists, which for an element-less side means
  the other side has elements, or `--show-unchanged-values` is set (see 7a).
- **Large elements**: if any element is a large value (the existing check, including the
  formatter consult), the row is `IsLarge` and renders in the large-attribute section,
  one occurrence per line, with the same markers. It does not use the line-LCS large
  diff, which would turn a reorder into a removed/added pair. Masked elements never
  make a row large (section 5a, item 5). In a large row they render as `(sensitive)` lines
  with their markers.

### 7a. Element-less collections (revised after code review)

`Absent`, `Null` and `Empty` stay distinct states in the model and in rendering, but they
are **equivalent for change detection** (`ResourceChangeStage.IsElementless`):

- When both sides are element-less (any combination of absent, `null` and `[]`), the
  collection counts as unchanged. The stage emits no row unless `--show-unchanged-values`
  is set, and it never emits a drift entry, because the drift stage always runs with
  unchanged values hidden.
- The same rule applies to wholly masked collections: `RawKey` maps absent, `null` and
  `[]` to the same comparison key, so a sensitive unset list produces no `(sensitive)` row
  on create or delete and no drift entry. Per-entry masking cannot occur on an
  element-less side, because there are no entries to mark.
- A transition between an element-less side and a side with elements is a change and
  renders normally, showing the element-less side as described in section 7. A masked
  `[]` to elements transition still renders `(sensitive)` on both sides.
- Unknown values and order changes are always changes, independent of this rule.

**Decision change.** The original design treated `[]` as distinct from `null` and absent
for change detection, which follows the specification's "observably distinct" wording
literally. Code review (B1, then M1 for the masked path) showed the cost: Terraform writes
every unset nested block as `[]`, so each create and delete gained an `absent -> []` row
per unset block, and destroying a resource with a sensitive empty list produced a
spurious `(sensitive)` row and drift entry. The per-key rows on main never showed these,
because the flattener emits no key for an empty array. The revised rule restores that
behaviour while keeping the states distinct wherever a row is shown.

Options considered:

- **Distinct for change detection** (original): literal reading of the spec, but adds a
  noise row for every unset block and regresses against main.
- **Equivalent for change detection, distinct for display** (selected): no noise rows,
  matches main, and the states remain observable whenever the collection actually changes
  or the user asks for unchanged values.
- **Equivalent everywhere** (collapse the states in the model): simplest, but loses the
  distinction the spec requires when one side has elements and under
  `--show-unchanged-values`.

Consequence: an `absent -> []` or `null -> []` transition alone is no longer reported,
even though Terraform's JSON differs. No plan in the test corpus or the issue gives that
transition a meaning to the reader, so this is accepted.

### 8. Collection drift

`GroupDrift` partitions each selected drift resource's attribute changes:

- rows with `Collection is null` go through the existing feature-145 grouping, unchanged;
- rows with a collection form one `CollectionDriftModel` per resource:
  `ResourceType`, `Address`, and `Paths` (naturally ordered list of `(path, CollectionChangeModel)`).

`ReportModel` gains `CollectionDrift` beside `Drift`. Mode selection
(`all`/`relevant`/`none`) happens on `ResourceChangeModel`s before grouping, so it applies
to both kinds unchanged.

`ReportRenderer.RenderDriftSection` renders when either list is non-empty, in order by
resource type; within a type, scalar groups first (existing order), then collection
entries by address. A collection entry is one `<details>` block:

```text
<summary>🌀 azuread_group `azuread_group.admins` — 1 changed collection: `members`</summary>

`members`
- ➖ `1111…`
- ➕ `2222…`
- 🔀 order changed: `a`, `b` → `b`, `a`
```

The count is the number of paths. Drift lists only deltas, because each line is labelled
and there are no Before/After columns to stay truthful to. Drift values use the existing
`FormatDriftCode`. A wholly masked collection renders `(sensitive)` and no element lines.
A masked element renders as `(sensitive)` in its `➖`/`➕` line and in the order-changed
sequences (`CollectionDriftRenderer`). To give every output path one guard,
`CollectionValues.ElementDisplayText` returns `(sensitive)` for a masked element before
any other branch. The generic table, large-value section and drift all reach element
text through it or through the masked check in section 7.

### Files

| Area | Files |
|---|---|
| New | `MarkdownGeneration/Helpers/AttributePathComparer.cs`, `MarkdownGeneration/Helpers/PrimitiveCollectionDiffer.cs`, `MarkdownGeneration/Models/CollectionChangeModel.cs` (+ element/state types), `MarkdownGeneration/Models/CollectionDriftModel.cs` |
| Changed | `AttributeChangeModel.cs`, `Stages/ResourceChangeStage.cs`, `Stages/AttributeFilteringStage.cs`, `Rendering/DefaultResourceRenderer.AttributeTables.cs` (+ large-attribute rendering), `ReportModelBuilder.PlanContext.cs`, `ReportModel.cs`, `Stages/ReportAssemblyStage.cs` / `IReportAssemblyStage.cs`, `Rendering/ReportRenderer.cs` |
| Changed by the sensitive-entry revision (5a, 6) | `Models/CollectionChangeModel.cs` (`IsMasked`, `HasMixedKinds`), `Helpers/CollectionElementDiffer.cs` (`CollectionElementKey.Masked`), `Helpers/CollectionValues.cs` (`ElementDisplayText`, `CanonicalText` from the model), `Stages/ResourceChangeStage.Collections.cs` (per-side masking, tokens, `HasLargeElement`), `Stages/AttributeFilteringStage.cs` (masked positions), `Rendering/DefaultResourceRenderer.Collections.cs`, `Rendering/CollectionDriftRenderer.cs` |
| Not touched | everything under `Providers/` and `Platforms/` |

## Consequences

Positive:

- One implementation serves tables, summaries, filters and drift; counts agree by
  construction.
- Equality is type-accurate and independent of display names, so feature 147 can
  annotate elements without affecting the diff.
- Masked values never enter the model: a wholly masked collection carries no elements,
  and a masked entry carries only its index, status and `IsMasked`. A rendering bug
  cannot leak a value that the model does not contain.
- Non-sensitive entries of a collection stay visible, and the reader can see whether a
  secret changed, was added or removed, or moved.
- No provider code changes; the provider isolation rule is untouched.

Negative:

- `AttributeChanges` now means "logical attributes". A future provider factory that
  expects indexed leaf rows for a primitive list must read the plan JSON instead. This
  is documented on the property.
- Per-entry masking reveals the number and positions of masked entries on each side and
  whether each one changed (specification rule 5). The Maintainer accepted this
  explicitly.
- Per-side masking can show a value on one side that is masked on the other. Rule 4
  makes the two always "different", so a value that becomes sensitive without changing
  is shown as `➖ x` / `➕ (sensitive)`. It looks like a change, but it is the price of
  not revealing the secret.
- Per-side masking differs from `main`, which masked a position on both sides when
  either side was marked sensitive.
- Rule 7 holds only while the differ compares keys across sides alone (section 4). A
  future optimisation that sorts keys or compares within one side could break it
  silently, so the rule 7 test (section 5a) is the regression guard.
- A list whose case-only Azure ID difference comes with another change now shows the
  case-variant elements as `➖`/`➕`, where before only the per-index row was hidden. The
  Maintainer accepted this. The case-only-only collection is still hidden.
- A change between absent, `null` and `[]` with no elements on either side is not
  reported (section 7a). The states are still shown whenever a row is rendered.
- Long lists with a single change produce tall cells. If UAT finds this unreadable, a
  follow-up can collapse unchanged runs without changing the model.
- azapi body tables (provider-specific) keep their per-index rows. Their flattener
  preserves array order, so they are already in numeric order.
- Generic-table and drift snapshots change wherever primitive collections or multi-digit
  indices appear; provider-table snapshots must not change, and that is the regression
  guard.
