# Architecture: Compact, Semantically Accurate Collection Diffs

## Status

Accepted (auto-decided: one clearly superior placement; the table layout is
user-visible and is validated at the UAT gate)

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

- `BeforeState` / `AfterState`: `Absent | Null | Empty | Elements | Unknown | Masked`;
- `Before` / `After`: ordered `CollectionElementModel` lists (`Index`, display `Value`,
  value `Kind`, `Status`: `Unchanged | Removed | Added | Moved | Unknown`);
- `OrderChanged` and `IsUnresolved`.

`Before` / `After` on the row keep a canonical text, so consumers that only read strings
keep working: compact JSON of the array, `(sensitive)` when masked, the existing
known-after-apply label when the whole collection is unknown, `null` for absent/null.
Collection rows set `IsLarge` from their elements (see 7), never from the canonical text.

When a collection is masked, the element lists are left empty, so the renderer cannot
leak values, counts or order.

### 4. Diff algorithm

Element key = `(JsonValueKind category, raw text)`, so `"1"` and `1` are different and
display names never take part in equality.

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
- **Sensitive**: if `before_sensitive` or `after_sensitive` marks `P` or any element
  under it, and `--show-sensitive` is off, both states are `Masked` and the row shows
  `(sensitive)` on both sides. Masking the whole collection rather than individual
  elements is deliberate: per-element masking would reveal counts and positions. With
  `--show-sensitive` the normal diff renders and `IsSensitive` stays `true`.

### 6. Attribute filters (case-only Azure ID suppression)

`AttributeFilteringStage` evaluates a collection row element-wise. When both sides have
the same length and every position whose values differ is suppressed by the registry
(called with name `P[i]` and the two element texts), the row is suppressed. Otherwise it
renders normally. This reproduces today's outcome exactly when the per-index rows would
all have been filtered. The core never learns what the provider filters do.

### 7. Generic table rendering

`DefaultResourceRenderer` checks `attribute.Collection`:

- Each element is formatted with the existing
  `FormatAttributeValueTableWithRegistryResource`, passing the indexed name `P[i]`
  (the element's index on its own side). Existing formatters that match indexed names
  keep working, and feature 147 annotations arrive through the same pipeline. A `null`
  element renders as `null`.
- Elements are joined with `<br>`, the existing table-cell line break.
- **Create / delete** (single-value table): the full collection, no markers.
- **Update / replace** (before/after table): Before holds the full before collection with
  `Removed` occurrences prefixed `➖`; After holds the full after collection with `Added`
  occurrences prefixed `➕`. When `OrderChanged`, After ends with a `🔀 order changed`
  line. `Moved` elements carry no per-element marker; both sequences are visible.
- `Empty` renders as `[]`. `Null` and `Absent` keep the existing scalar rendering (`-`).
  `Masked`, `Unknown` and the canonical text come from section 3.
- **Large elements**: if any element is a large value (the existing check, including the
  formatter consult), the row is `IsLarge` and renders in the large-attribute section,
  one occurrence per line, with the same markers. It does not use the line-LCS large
  diff, which would turn a reorder into a removed/added pair.

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
`FormatDriftCode`; masked collections render `(sensitive)` and no element lines.

### Files

| Area | Files |
|---|---|
| New | `MarkdownGeneration/Helpers/AttributePathComparer.cs`, `MarkdownGeneration/Helpers/PrimitiveCollectionDiffer.cs`, `MarkdownGeneration/Models/CollectionChangeModel.cs` (+ element/state types), `MarkdownGeneration/Models/CollectionDriftModel.cs` |
| Changed | `AttributeChangeModel.cs`, `Stages/ResourceChangeStage.cs`, `Stages/AttributeFilteringStage.cs`, `Rendering/DefaultResourceRenderer.AttributeTables.cs` (+ large-attribute rendering), `ReportModelBuilder.PlanContext.cs`, `ReportModel.cs`, `Stages/ReportAssemblyStage.cs` / `IReportAssemblyStage.cs`, `Rendering/ReportRenderer.cs` |
| Not touched | everything under `Providers/` and `Platforms/` |

## Consequences

Positive:

- One implementation serves tables, summaries, filters and drift; counts agree by
  construction.
- Equality is type-accurate and independent of display names, so feature 147 can
  annotate elements without affecting the diff.
- Masked collections carry no element data in the model, so a rendering bug cannot leak
  them.
- No provider code changes; the provider isolation rule is untouched.

Negative:

- `AttributeChanges` now means "logical attributes". A future provider factory that
  expects indexed leaf rows for a primitive list must read the plan JSON instead. This
  is documented on the property.
- Any sensitive element masks the entire collection, including its non-sensitive
  members, when `--show-sensitive` is off. This is less information than today's
  per-index rows, chosen because per-index masking leaks counts and positions.
- A partially suppressed case-only Azure ID change in a list now shows the case-variant
  elements as `➖`/`➕`, where before only the per-index row was hidden. The fully
  suppressed case is preserved.
- Long lists with a single change produce tall cells. If UAT finds this unreadable, a
  follow-up can collapse unchanged runs without changing the model.
- azapi body tables (provider-specific) keep their per-index rows. Their flattener
  preserves array order, so they are already in numeric order.
- Generic-table and drift snapshots change wherever primitive collections or multi-digit
  indices appear; provider-table snapshots must not change, and that is the regression
  guard.
