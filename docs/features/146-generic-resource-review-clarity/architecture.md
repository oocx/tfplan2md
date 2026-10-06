# Architecture: Generic Resource Review Clarity

## Decision

Extend existing model-building stages and provider registrations. Resolve generic
identity once, carry it as unformatted model data, and format it at output boundaries.
Implement msgraph presentation in `Providers/MsGraph/` through existing resource
factory and renderer interfaces. Filter outputs before report assembly groups them.

This choice is **not contested**: established extension points cover the change and
avoid a second rendering pipeline. No global ADR is needed; these decisions apply to
feature 146 and preserve the existing provider boundary.

## Existing implementation

- `ResourceChangeStage` builds masked rows, calls provider factories, then constructs
  fallback summaries and changed-attribute counts.
- `ResourceSummaryBuilder`, `ResourceSummaryHtmlBuilder`, and
  `ReportAssemblyStage.ResolveRefactoringResourceName` independently choose names.
  Their flattened-state searches do not consistently check sensitivity or unknowns.
  Replace their generic selection with a shared decision, rather than another list.
- `ResourceSummaryMappings.ResourceMappings` explicitly maps `azapi_resource`,
  `msgraph_resource`, and `msgraph_update_resource`, among others. Provider fallback
  mappings also exist. Generic provider does not imply unmapped resource type.
- Default HTML summaries prefer `model.Name`, omitting the instance suffix; the
  last-dot address fallback can split dots inside quoted keys.
- `ReferenceSelector` excludes pseudo prefixes before removing module prefixes,
  permitting module-qualified pseudo references to resemble resource references.
- `BuildOutputModels` determines output actions and masks values; `ReportAssemblyStage`
  groups those models into root and module sections.
- MsGraph has no provider module. Existing factories, renderer registration, and
  `ProviderContributionSet` support one without introducing another provider system.

## Options considered

| Approach | Advantages | Costs |
| --- | --- | --- |
| Extend model construction and provider registrations (selected) | Shared identity, existing masking and formatting, provider isolation, accurate counts | Small model additions and a MsGraph module; existing paths must consume shared data |
| Resolve identities and filter metadata separately in each renderer | Fewer initial model changes | Duplicate sensitivity rules, counts diverge from visible changes, inconsistent root/module filtering |
| Introduce a separate generic-resource pipeline and configurable presentation registry | Flexible future rules | Speculative abstraction, duplicate pipeline, harder mapped-summary compatibility |

The first approach is clearly preferable. Presentation-only filtering cannot satisfy
count and masking requirements reliably; existing interfaces already cover the feature.

## Generic identity

Add an internal resolver in `MarkdownGeneration/Summaries/`, accepting state, sensitivity
and unknown markers, action, invocation sensitivity policy, and ordered candidate paths.
Return an optional raw scalar identity and its path. Store the result on
`ResourceChangeModel` so fallback HTML/text summaries and refactoring operations share
one decision. Do not store HTML in this field.

Use the ordered defaults from the specification. The override replaces that list only
for generic fallback naming. Eligibility uses explicit resource-mapping membership,
not whether `ResolveKeys` returned some list. Preserve mapped identifying content and
provider view-model summaries. Provider fallback keys must not preempt the new generic
order for unmapped resources. In particular, existing azapi/msgraph explicit mappings
remain mapped when an override is supplied; do not remove or reclassify them.

Walk JSON objects by path segment rather than using flattened dictionary lookup:
the literal key `body.displayName` must not satisfy nested `body.displayName`.
Accept non-empty strings, numbers, and booleans; reject null, arrays, and objects.
Do not add JSON-string body parsing or a new path-expression language.

For each candidate, examine action-preferred state first (before for delete, after for
create/update/replace and ordinary import/move). If unusable, try the other available
state at the same path before advancing to the next candidate. Known prior values may
supply update identity when desired values are unknown. Inspect root and ancestor unknown
markers, including whole-resource unknown after-state, rather than comparing strings
to placeholder text. Reuse `SensitivityHelper` with the invocation's `ShowSensitive`
policy, including its conservative before/after sensitivity union. Skip masked values
without exposing them; do not create another sensitivity policy.

Existing explicit mappings should supply their selected identifying value through the
same model field where possible, preserving their identifying choices. This permits
mapped msgraph `body.displayName` context in resource/refactoring summaries without
making the CLI override replace mapped choices. Mapping-driven context and generic
fallback eligibility remain distinct decisions.

## Instance and refactoring labels

Derive the instance suffix from the final resource segment of `Address` using a small
bracket/quote-aware scan that respects escapes. Append the exact suffix substring to
`Name`; preserve local-name styling and module grouping. Do not reconstruct string keys
or split blindly on dots. Use the label in default summaries and audit provider-owned
summary headers embedding local names, so all actions retain the suffix without changing
mapped identifying content. Keep Terraform identity separate from friendly identity.

Escape identity with existing helpers at each output boundary. Refactoring operations
use full `Address` as their stable label and carry optional identifying context separately.
Render all imports/moves from the address and append available identity; remove report
assembly's independent name/url precedence. Mapped identifiers retain their existing
choice, while unmapped resources use the resolved generic identity.

## Computed references

In `ReferenceSelector`, classify the useful portion after recognized module context is
consumed. Apply non-resource and bare pseudo exclusions there before static-resource
selection. Preserve useful-reference priorities and specific `each.value.<attribute>`,
variable/local, and static-resource references. Do not chase expressions or turn
`ConfigurationReferenceResolver` into a graph walker. Null selection flows through
existing plain known-after-apply formatting and sensitive-computed masking precedence.

## Empty imported prior state

Add a provider-neutral model flag for an import with empty meaningful prior state.
A recursive walker ignores null, empty strings, empty objects, and empty collections;
zero and false are meaningful. Exclude the specified housekeeping paths `id`,
`api_version`, `url`, and `ignore_missing_property` at the root, rather than ignoring
all nested fields sharing those names.

Keep MsGraph-specific classification and path declarations in its provider factory.
The reusable meaningful-state walker takes excluded paths and knows no provider types.
Other providers can supply their housekeeping exclusions through existing factories;
ensure every import scenario covered by the specification uses the specified exclusions.
Place the note immediately before desired attributes when the flag is true. Provider
renderers must consume this flag too; the note cannot be limited to the default renderer.

## MsGraph provider settings

Create `Providers/MsGraph/MsGraphModule`; register its factories/renderers through
composition, following existing providers. Keep resource selection and setting paths
`api_version`, `url`, and `ignore_missing_property` in that directory. Classify rows
from raw before/after states, independently of whether `--show-unchanged-values` added
unchanged rows to `AttributeChanges`.

Move desired settings on create or import with empty prior state, and unchanged settings
on other actions, into a provider view model. Remove these from primary rows before
summaries and counts are built. When meaningful prior state proves a setting added,
removed, or changed, retain it in primary rows and normal counts. Preserve existing
masking and unknown handling in secondary rows. `--show-unchanged-values` must not
promote secondary settings into primary rows.

Use a MsGraph renderer sharing existing default resource rendering operations, inserting
a collapsed `Provider settings` table inside the card. If necessary, extract a small
provider-neutral rendering hook from the default renderer for secondary-section placement.
Do not copy the renderer or put MsGraph decisions in core. Preserve annotations, large
values, inline actions, and render-target handling on existing paths. A suppression
filter alone is insufficient because settings must remain available in the report.

## CLI and outputs

Thread ordered name paths and `HideUnchangedOutputs` through CLI options, composition,
and `ReportModelBuilderOptions`, with immutable lists and compatible defaults. Validate
empty lists and empty comma-separated entries during parsing; trim surrounding whitespace
and use existing CLI error/help conventions. Paths need not exist in any resource.

Filter effective no-op outputs in `BuildOutputModels` after action determination and
before adding models. Root/module grouping then shares the filtered input. Preserve
existing absent-action handling, value selection, masking, and computed formatting.
Existing section renderers omit empty collections; ensure output-only module groups do
not leave empty headings. Without the flag, preserve current selection.

## Verification and constraints

Quality Engineer owns detailed tests. Cover nested versus literal dotted keys, zero/false,
root/parent sensitivity and unknowns, update fallback, mapped summaries with overrides,
instance keys with dots/brackets/escaped quotes, every action/refactoring form,
housekeeping-only versus meaningful imports, changed versus secondary settings with both
unchanged-value modes, and root/module sections emptied by filtering. Include rendered
Markdown acceptance coverage on supported targets and intentional snapshot regeneration.

Use focused internal helpers and split touched files exceeding the size guideline.
Remove replaced generic identity/refactoring paths. No external dependency, expression
tracing, GUID resolution, or collection-diff work is required. The mapping dictionary
contains legacy provider knowledge; this feature must not add more provider rules there
or elsewhere in core.
