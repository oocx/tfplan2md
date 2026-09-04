# Architecture: Configurable, Aggregated Drift Rendering

## Status

Proposed

## Context

Feature specification: [specification.md](specification.md)

The current pipeline converts `resource_drift[]` through the same resource-change
construction path used for planned changes, then applies the existing display filter.
Consequently, `ReportModel.Drift` already contains provider-neutral,
display-ready `ResourceChangeModel` instances with normalized attribute paths and
values; no-op and fully suppressed drift entries have already been removed.

`ReportRenderer` currently renders each drift resource as a normal resource card. The
new behavior instead needs to select drift by CLI mode and aggregate it by resource
type, attribute path, and normalized value transition. This is report-wide data
shaping, not provider-specific presentation.

## Options Considered

### Option A: Group directly in `ReportRenderer`

The renderer could filter and flatten `ReportModel.Drift`, create groups while writing
markdown, and emit their summaries and address lists.

Pros:

- Requires few new types.
- Keeps the existing `ReportModel.Drift` shape.

Cons:

- Mixes selection, grouping, ordering, and rendering in one method.
- Makes grouping behavior harder to unit test independently of exact markdown.
- Requires the renderer to know about planned-change membership for `relevant` mode.
- Leaves `ReportModel` claiming drift is resource-card data even though the report no
  longer renders it that way.

### Option B: Select and group into a dedicated report model (selected)

After both planned changes and drift have passed existing display filtering, apply the
selected drift mode and flatten each retained drift resource's attribute changes into
dedicated `DriftGroupModel` instances. Store only those groups on `ReportModel` and let
the renderer handle layout.

Pros:

- Preserves the pipeline boundary: model building decides content; rendering decides
  markdown layout.
- Reuses existing normalization, masking, provider formatting, and suppression.
- Makes mode selection and grouping independently testable.
- Keeps the implementation provider-neutral and avoids changes in provider modules.
- Gives the renderer a model that directly represents the required output.

Cons:

- Adds a small model type and a grouping step.
- Changes the type of `ReportModel.Drift`, requiring coordinated assembly and renderer
  changes.

### Option C: Select and aggregate raw Terraform `resource_drift[]`

Group parsed `ResourceChange` data before it enters the resource-change pipeline, then
construct a report model from the raw JSON values.

Pros:

- Can avoid constructing full resource-card models for excluded drift.
- Keeps aggregation close to the Terraform input.

Cons:

- Duplicates attribute flattening, normalization, sensitivity masking, and filtering.
- Risks grouping values differently from how they are displayed.
- Couples the feature to Terraform JSON representation rather than the report model.
- Makes the existing no-op and fully-suppressed filtering guarantees harder to retain.

## Decision

Choose **Option B: select and group into a dedicated report model**. It is the only
option that reuses all established display semantics while keeping aggregation out of
both parsing and markdown layout.

This choice is not contested. Options A and C save a small amount of model code but
introduce materially worse responsibility boundaries and duplicated behavior; they do
not offer a competing long-term advantage.

## Technical Design

### Drift mode

Add a provider-neutral `DriftDisplayMode` enum with `All`, `Relevant`, and `None`.
`CliOptions` carries the parsed value and defaults to `All`. `CliParser` accepts the
three values case-insensitively and reports all accepted values when the option is
missing a value or invalid. The composition root passes the enum into
`ReportModelBuilderOptions`.

`None` short-circuits drift construction and produces no groups. `All` selects every
drift resource surviving the existing display filter. `Relevant` selects only drift
whose exact Terraform address occurs in the already computed `displayChanges` list.
Address comparison uses ordinal equality: Terraform addresses are identifiers, and
case-insensitive comparison could falsely correlate distinct resources.

Selection happens before grouping. The membership set must come from
`displayChanges`, not raw `plan.ResourceChanges` or pre-filter `allChanges`, so no-op or
fully suppressed planned changes cannot make drift relevant.

### Group model and key

Replace the resource-card-shaped drift report property with a list shaped for the new
output. A `DriftGroupModel` contains:

- resource type;
- attribute path;
- normalized before value;
- normalized after value; and
- all affected resource addresses.

For each selected, displayable drift resource, create one grouping candidate per
remaining `AttributeChangeModel`. The group key is the tuple of resource type,
attribute name/path, before value, and after value using ordinal equality. The
`AttributeChangeModel` strings are the canonical normalized display values produced by
the existing resource-change pipeline, including its masking and formatting rules.
Using the same values for grouping and display prevents the summary from disagreeing
with its key.

A resource with several changed paths therefore participates in one group per path.
Duplicate addresses within a group are removed. Groups and addresses use deterministic
ordinal ordering (resource type, path, before, after; then address) so output is stable
regardless of dictionary iteration.

### Pipeline placement

Retain the existing drift construction and display-filtering path, then add a focused
drift grouping operation in model building:

1. Build and attribute-filter planned resource changes.
2. Produce `displayChanges` with the existing display filter.
3. Build drift through `ResourceChangeStage` and the same attribute/display filtering
   semantics used today.
4. Apply `DriftDisplayMode`, using `displayChanges` for relevant membership.
5. Flatten and group the selected drift attribute changes.
6. Pass the resulting groups through `ReportAssemblyInput` to `ReportModel`.

The drift path must apply the configured `IAttributeFilteringStage` before display
filtering, matching planned changes. This ensures provider-contributed suppression is
honored before grouping and retains the specification's fully suppressed behavior.

### Rendering

`ReportRenderer` remains responsible only for formatting the groups. It omits the
section when the group list is empty. For every group it writes one collapsed
`<details>` element whose summary contains the drift icon, count, resource type,
attribute path, and before-to-after transition, followed by a bullet list containing
every address.

Use `MarkdownWriter`/existing markdown escaping helpers for inline code and text rather
than interpolating unescaped Terraform values into HTML. This is especially important
for values containing HTML delimiters, backticks, or line breaks. The grouped renderer
is core and provider-neutral; it must not consult the resource renderer registry or
contain provider-specific display rules.

### Compatibility

Omitting `--drift` is identical to `--drift all`. The new default changes only drift
layout, as required by the feature: selection breadth remains unchanged. Plans without
displayable drift, and `none` mode, both result in an empty group list and therefore no
heading.

## Consequences

### Positive

- Repeated drift becomes compact while all addresses remain available.
- Selection and grouping are deterministic and independently testable.
- Existing no-op, suppression, masking, and value-normalization behavior is reused.
- Provider-specific knowledge remains isolated from core drift logic.

### Negative

- Existing drift snapshots intentionally change from provider-specific resource cards
  to grouped summaries, including the single-entry case.
- A multi-attribute resource appears in multiple groups and address lists; this is
  necessary because the grouping unit is an attribute transition.
- Grouping operates on masked display values. Distinct sensitive raw transitions that
  normalize to the same masked values may share a group, but no sensitive information
  is exposed and the rendered grouping key remains truthful to the visible report.

## Verification Guidance

- CLI parser tests: omitted/default, all, relevant, none, missing value, invalid value.
- Model tests: selection precedes grouping; relevant uses only displayable planned
  changes; no-op and fully suppressed drift remain absent.
- Grouping tests: matching tuples combine; differing type, path, before, or after split;
  multi-attribute resources produce multiple groups; addresses and groups are stable.
- Rendering/snapshot tests: exact collapsed structure, escaped values, complete address
  lists, no heading for empty/none, and updated default all-mode snapshots.

## Architecture Compliance

- The design is provider-neutral and adds no provider knowledge to
  `MarkdownGeneration/`.
- Existing provider-specific filtering and formatting remain registered through their
  established extension points.
- Parsing remains a faithful Terraform JSON model; report-specific aggregation stays
  downstream.
