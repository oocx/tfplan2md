# Test Plan: Generic Resource Review Clarity

## Overview

Use TUnit unit tests for selection/parsing, model tests for counts/filtering, and
rendered acceptance tests for complete Markdown. Name tests
`MethodName_Scenario_ExpectedResult`. The Developer implements tests and fixtures.
SC numbers follow the 17 success criteria in specification order.

Use a synthetic unmapped `review_object` resource for generic fallback tests,
including a provider with fallback keys. Existing `msgraph_resource`,
`msgraph_update_resource`, and `azapi_resource` mappings remain mapped;
they cannot substitute for unmapped fallback coverage.

## Test Coverage Matrix

| Criterion | Test cases | Type |
| --- | --- | --- |
| SC1: ordered generic candidates | T01, T02 | Unit, acceptance |
| SC2: unusable candidates and valid fallback | T01, T02 | Unit, acceptance |
| SC3: sensitivity and unknown identity policy | T03, T04 | Unit, model, acceptance |
| SC4: replacement override and validation | T05, T06 | CLI, model, acceptance |
| SC5: mapped summary compatibility | T07 | Model, acceptance |
| SC6: exact suffix on every action | T08 | Unit, acceptance |
| SC7: pseudo-only hints | T09 | Unit, acceptance |
| SC8: useful hints and computed masking | T09, T10 | Unit, acceptance |
| SC9: full-address import/move labels | T11 | Model, acceptance |
| SC10: empty/meaningful import context | T12 | Unit, acceptance |
| SC11: accessible collapsed settings | T13 | Model, acceptance, UAT |
| SC12: create/import settings and changed counts | T13, T14 | Model, acceptance |
| SC13: unchanged-values mode | T13, T14 | Model, acceptance |
| SC14: root/module output filtering | T15, T16 | CLI, model, acceptance |
| SC15: compatible defaults | T07, T17 | Regression, acceptance |
| SC16: automated acceptance breadth | T02–T17 | Acceptance |
| SC17: supported target legibility | T18 | Acceptance, UAT |

## Test Cases

### T01: candidate order and path semantics (SC1–SC2)

Resolve an unmapped resource with distinct values at all six default paths; expect
name. Parameterize preceding candidates as missing, null, empty string, empty or
non-empty object/array, expecting the next default in documented order. Zero and
false are usable. Give state both a literal body.displayName property and nested
body.displayName with different values; expect the nested value, and no match
when only the literal key exists. A string-valued JSON body is not parsed as
nested state. No usable candidate returns no identity.

### T02: action state and rendered fallback (SC1–SC2, SC16)

Render create, update, delete, and replace generic resources with distinct old/new
identities. Expect desired identity for create/update/replace and prior identity
for delete. Try a usable other side at the same path before the next candidate.
With no candidate, retain valid local-name summary and suffix. Include HTML,
quotes, backticks, pipes, and ampersands in identity text; assert appropriate
escaping at each boundary without injected markup or broken tables.

### T03: sensitive identity policy (SC3)

Mark a candidate sensitive at leaf, parent, and whole-resource root, on either
before/after side. With default masking, assert secrets absent from resource and
refactoring summaries and selection continuing to a safe later candidate. Honor
existing conservative sensitivity union. With the existing sensitive-value
option, selection follows that option, including normal escaping. With no safe
candidate, expect no identity rather than a masked placeholder as a name.

### T04: unknown identity and prior fallback (SC3)

Mark desired identity unknown at leaf, parent, and whole-resource root; expect
known policy-visible prior identity at that path. If both states are unusable,
try later paths or return no identity. For create with unknown name and known
later displayName, select displayName. Unknown/computed markers cannot become
identities. Verify sensitivity precedence when sensitive and unknown combine.

### T05: ordered CLI override (SC4)

Parse --summary-name-attributes body.title,body.properties.title and render an
unmapped resource with both overrides and defaults populated. Expect body.title,
then body.properties.title when absent; do not revert to defaults when neither
exists. Trim entry whitespace. A nonexistent path is valid and yields no identity.
Assert CLI values reach composition/model construction.

### T06: CLI errors (SC4)

Parse missing value, empty string, whitespace-only list, leading/trailing comma,
and consecutive commas. Expect existing CLI error behavior with a clear message
naming the option and invalid empty list/entry. Verify help documents both new
options and their replacement/opt-in behavior.

### T07: mapped summary compatibility (SC5, SC15)

Render azapi_resource, both msgraph types, azurerm_resource_group, and a
provider-owned custom summary such as azuredevops_variable_group with and without
an override pointing to competing identity. Expect unchanged mapped identifying
content across invocations, allowing specified suffix/settings changes. For an
unmapped type whose provider offers fallback keys, expect generic default order.

### T08: instance identity (SC6)

Parameterize create, update, both replace orders, delete, import, and move with
string and numeric indices. Include module instances and keys containing dots,
brackets, escaped quotes, and backslashes. Preserve the final resource suffix
exactly as Terraform identifies it, allowing only output escaping; retain module
grouping and add no suffix to unindexed resources. Exercise default cards and
provider-owned summary headers from T07.

### T09: computed reference selection (SC7–SC8)

Render each.key, each.value, count.index, and self in bare and nested-module-
qualified forms. Expect the plain known-after-apply marker without colon/hint.
Add meaningful references to mixed lists: static resource, variable, local, and
each.value.group_object_id. Preserve existing selection priorities and hints.
Removing module context must not classify pseudo references as static resources.
Pseudo-only expressions must not trigger underlying source tracing.

### T10: sensitive computed values (SC8)

Render sensitive computed attributes with pseudo-only and meaningful references.
Expect existing default masking with no revealed secret or disallowed computed
hint. Compare authorized sensitive mode to existing policy rather than adding a
new rule.

### T11: refactoring labels (SC9)

Render imports and moves for unmapped resources with/without names and mapped
msgraph/azapi resources. Every label retains its full module-qualified Terraform
address and exact suffix, appending selected visible context where available.
No label consists only of displayName or url. If existing input supports combined
import/move metadata, assert both operations use stable addresses. Reuse T03
masking and T02 escaping assertions.

### T12: meaningful imported prior state (SC10)

Render before null, empty object, housekeeping-only values, body:{}, and nested
null/empty strings/empty arrays. Expect the full-desired-state note immediately
before desired attributes and no newly-created claim based solely on missing
prior values. With substantive root/nested values, including zero, false,
non-empty arrays, or body.id, expect no note. Root exclusions do not suppress
nested same-named data. Exercise default and provider-owned renderers. Ordinary
creates have no import note; sensitive desired values retain masking.

### T13: secondary msgraph settings (SC11–SC13)

For both msgraph types, render meaningful-state updates with unchanged api_version,
url, ignore_missing_property; creates and empty-state imports with desired values.
Run with show-unchanged-values off/on. Expect available settings in a collapsed
Provider settings area inside the card, absent from primary rows/changed count.
Missing settings produce no phantom rows. Preserve masking/computed presentation
in secondary rows and friendly mapped context in summaries.

### T14: changed settings and counts (SC12–SC13)

Against substantive prior state, parameterize addition, removal, and update of
each setting, with unchanged-value mode off/on. Expect changed settings in primary
diffs and counted once; unchanged companions stay secondary. Repeat with populated
imported prior state. Non-msgraph types retain ordinary presentation for these
same attribute names.

### T15: output filtering (SC14)

Render root/module no-op, create, update, delete, sensitive changed, and unknown
changed outputs. With hide-unchanged-outputs, omit only effective no-ops. Preserve
remaining action markers, before/after values, masking, and computed format.
Verify option propagation and existing absent-action handling before filtering.

### T16: empty output sections (SC14)

Filter root-only no-ops, module-only no-ops, and mixed cases. Expect no empty
Outputs section or output-only module heading. Modules retaining resources still
appear; non-empty output sections retain order. No-output plans still render.

### T17: default compatibility (SC15–SC16)

Render existing fixtures without either new option and compare output selection,
mapped identifiers, custom provider sections, annotations, inline actions, and
large-value formatting. Limit snapshot changes to specified clarity improvements.
Add full-render acceptance fixtures for new behaviors and regenerate intentional
snapshots with the repository skill.

### T18: supported targets (SC17)

Render feature fixtures for github, azuredevops, and bitbucket. Assert escaped
labels, import-note position, settings accessibility, primary counts/diffs, and
output selection. Verify each target's existing collapsible presentation or
fallback conventions. Use UAT for visual legibility; automated assertions remain
deterministic.

## Edge Cases and Error Conditions

T01–T06 cover scalar boundaries, invalid CLI inputs, dotted paths, inherited
sensitivity, and unknown fallback. T08–T12 cover escaped addresses, mixed references,
and housekeeping-only imports. T13–T16 cover metadata removals, both unchanged-value
modes, and empty sections. Automated acceptance requires no live provider/network.
