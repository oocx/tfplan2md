# Feature: Generic Resource Review Clarity

## Overview

Generic resources such as `msgraph_resource`, `msgraph_update_resource`, and
`azapi_resource` can represent many different API objects through the same Terraform
resource type. Their current report summaries often repeat the same local resource name,
omit the `for_each` or `count` instance, and make reviewers expand every resource card to
learn what it represents. The same reports can also contain unhelpful computed-value
hints, repetitive provider settings, ambiguous imported state, inconsistent refactoring
labels, and unchanged outputs that distract from the proposed change.

This feature gives generic resources stable, human-readable identities throughout the
report and reduces secondary detail while preserving access to every review-relevant
change. Its defaults remain compatible with existing reports except for the intended
clarity improvements to resource identity, pseudo-reference hints, and unchanged
provider metadata.

Source request: [GitHub issue #674](https://github.com/oocx/tfplan2md/issues/674).

## User Goals

- Distinguish instances of a generic resource without opening each resource card.
- See a useful object name in a generic resource summary when its planned or prior state
  contains one.
- Recognize the exact Terraform instance involved in a change or refactoring operation.
- See a plain `(known after apply)` value instead of a misleading `each.value`,
  `each.key`, `count.index`, or `self` hint.
- Understand when an import has no useful prior state and therefore resembles a create.
- Keep repeated provider settings available without letting them dominate each resource
  diff, while still seeing any change to those settings normally.
- Optionally omit unchanged outputs from reports whose output sections are otherwise
  too noisy.

## Scope

### In Scope

#### Generic display identity

- For a resource type that has no resource-specific summary mapping, select the first
  usable scalar value from this ordered default list:
  `name`, `display_name`, `displayName`, `body.displayName`,
  `body.properties.displayName`, `body.name`.
- A usable value is a non-null, non-empty scalar. Missing values and object or collection
  values do not become display identities; selection continues with the next candidate.
- A candidate that remains masked under the invocation's existing sensitive-value
  policy is not usable as a display identity. If existing options authorize the actual
  sensitive value to be shown, that visible value may be selected under the same policy;
  generic identity selection must not create a separate path that bypasses or tightens
  the active sensitivity setting. A candidate whose selected state is unknown or
  computed is also not usable; `(known after apply)` and similar placeholders must not
  become names. Selection continues with the next candidate without revealing a value
  that remains masked.
- Read nested candidates as attribute paths. A literal attribute name that contains a
  dot is not implied by a dotted candidate.
- Prefer the value from the state appropriate to the action: the desired value for a
  create or update, the prior value for a delete, and the available value when only one
  side contains the candidate. When an update's desired candidate is unknown, a known,
  non-sensitive prior value at the same path may provide the identity.
- Show the selected identity after the Terraform local name and instance suffix. Escape
  it using the existing rules for data shown in resource summaries.
- Add an ordered `--summary-name-attributes <path1,path2,...>` option. When supplied, its
  list replaces the built-in generic candidate order for that invocation. It affects
  generic fallback naming only; existing resource-specific summary mappings retain
  their documented behavior.
- Reject an empty list or an empty path entry with a clear command-line error.

#### Resource instance identity

- Preserve the local resource name style already used by summaries, including existing
  module grouping, and always include the instance suffix from the Terraform resource
  address when one exists.
- Preserve string keys exactly as Terraform identifies them, including quotation marks,
  and preserve numeric indices, for example
  `conditional_access_policies["block_legacy_auth"]` and `servers[0]`.
- Apply the same instance-label rule to create, update, replace, delete, import, and
  move-related resource summaries.

#### Meaningful known-after-apply hints

- Keep existing useful computed-value hints, including static resource references,
  variable and local references, and specific `each.value.<attribute>` references.
- Treat bare pseudo references as non-informative even when a module prefix or similar
  configuration-address context precedes them. This includes references whose useful
  portion is only `each.key`, `each.value`, `count.index`, or `self`.
- When no meaningful reference remains, render the existing plain
  `(known after apply)` marker without a colon or hint.
- Do not trace a pseudo reference through a local value, collection, or expression to
  infer a deeper source reference in this feature.
- Preserve the existing masking for values that are both sensitive and computed.

#### Refactoring Summary labels

- Label every import and move in the Refactoring Summary with its full Terraform
  resource address.
- When generic display identity selection finds a value, append it as secondary context
  after the address. The address remains present even when a display identity exists.
- Use the same rule for all operations in one Refactoring Summary so one entry cannot be
  labelled only by `displayName` while another is labelled only by `url`.

#### Empty imported prior state

- When Terraform marks a resource as imported and supplies no meaningful prior
  attribute state, show a short note before its desired-state attributes explaining
  that the imported prior state is empty and the displayed values are the full desired
  state.
- For this note, prior state is meaningful only when it contains at least one non-null,
  non-empty value outside housekeeping identity/provider paths. The paths `id`,
  `api_version`, `url`, and `ignore_missing_property` do not make prior state meaningful,
  and an empty object or collection does not contain a meaningful value. Therefore an
  imported msgraph resource with only those housekeeping values and `body: {}` shows
  the note. A non-empty value nested under `body`, or another substantive attribute,
  suppresses the note.
- Do not describe the resource as newly created solely because every desired attribute
  lacks a prior value.
- Omit the note when meaningful prior attribute state is available.

#### De-emphasized provider metadata

- For `msgraph_resource` and `msgraph_update_resource`, place unchanged `api_version`,
  `url`, and `ignore_missing_property` values in a collapsed, clearly labelled
  provider-settings area within the resource card.
- Keep those values available to a reviewer; de-emphasis must not discard them.
- For a newly created resource or an import with no meaningful prior state, keep these
  desired settings in the collapsed provider-settings area alongside the other full
  desired-state values.
- `--show-unchanged-values` does not promote unchanged `api_version`, `url`, or
  `ignore_missing_property` values back into the primary attribute table. They remain
  present in the collapsed provider-settings area.
- If meaningful prior state shows one of these attributes being added, removed, or
  changed, show it with ordinary changed attributes and count it in the resource's
  changed-attribute summary, including when `--show-unchanged-values` is enabled.
- Do not let the provider-settings presentation replace the generic display identity.

#### Opt-in changed-output filtering

- Add `--hide-unchanged-outputs` as an opt-in command-line flag.
- When enabled, omit outputs whose Terraform action is no-op from both root and module
  output sections.
- Continue to show created, changed, and deleted outputs, including their existing
  sensitive-value masking and known-after-apply presentation.
- Omit an output section when filtering leaves it empty.
- When the flag is absent, preserve the existing output selection behavior.

### Out of Scope

- GUID resolution, well-known Microsoft GUID names, and principal-mapping extensions
  from source ideas 3 and 4. These belong to the separate GUID display-name feature.
- Natural list-index sorting, primitive-list compaction, set-style list diffs, and drift
  list diffs from source ideas 5, 6, and 8. These belong to the separate collection
  readability feature.
- Following `each.*` or `count.*` expressions through locals, maps, modules, or other
  configuration expressions to discover an underlying resource reference.
- Changing existing resource-specific summary mappings or their identifying-attribute
  choices.
- Hiding provider metadata that changed or making provider settings unavailable for
  review.
- Changing the default visibility of unchanged outputs.
- Investigating or fixing the four rendering defects reported in source idea 9:
  version-command stderr in the header, double-escaped workspace names, a stray summary
  separator, and trailing-dot references. They should be handled together in a separate
  grouped bug-fix work item with failing reproductions.

## User Experience

### Generic resource summaries

Given several `for_each` instances with `body.displayName` values, a reviewer sees both
the Terraform identity and the human name without expanding the cards:

```markdown
<summary>➕ msgraph_resource <b><code>conditional_access_policies[&quot;block_legacy_auth&quot;]</code></b> — <code>Block legacy authentication</code></summary>

<summary>🔄 msgraph_resource <b><code>conditional_access_policies[&quot;require_mfa_admins&quot;]</code></b> — <code>Require MFA for admins</code> | 5 🔧 body.conditions.clientAppTypes[0], …</summary>
```

For a project that uses a different identifying path:

```text
tfplan2md --summary-name-attributes body.title,body.properties.title plan.json
```

Resources with an existing resource-specific summary retain that summary behavior. A
generic resource for which none of the configured candidates yields a usable value
still shows its local name and complete instance suffix.

### Known-after-apply hints

A computed value whose only reported reference is `module.x.each.value` renders as:

```text
(known after apply)
```

A computed value with `each.value.group_object_id` retains the more specific existing
form:

```text
(known after apply: each.value.group_object_id)
```

### Refactoring Summary

Imports use the address as their stable label and append a display identity when one is
available:

```markdown
- 📥 `module.identity.msgraph_resource.conditional_access_policies["require_mfa_admins"]` — Require MFA for admins
- 📥 `module.identity.msgraph_resource.partner_policy["contoso"]` — Contoso cross-tenant policy
```

### Import context and provider settings

An imported resource with no useful prior `body` state shows a note such as:

```markdown
> 📥 Prior state from import is empty; the values below show the full desired state.
```

Unchanged `api_version`, `url`, and `ignore_missing_property` remain available in a
collapsed `Provider settings` area. If `url` changes, it appears in the ordinary diff
table and contributes to the changed-attribute count instead.

### Changed outputs only

```text
tfplan2md --hide-unchanged-outputs plan.json
```

With the flag, output sections contain only create, update, and delete actions. Without
it, reports continue to include unchanged outputs as they do today.

## Success Criteria

- [ ] A generic resource without a resource-specific mapping uses the first usable
  value from `name`, `display_name`, `displayName`, `body.displayName`,
  `body.properties.displayName`, and `body.name`, in that order.
- [ ] Missing, null, empty, object, and collection candidates are skipped, and a generic
  resource with no usable candidate still renders a valid summary.
- [ ] A candidate that remains masked under the existing sensitive-value policy never
  appears in resource summaries or Refactoring Summary labels; a sensitive value that
  existing invocation options allow to be shown may be selected without bypassing or
  tightening that policy. Unknown/computed candidates never become display names, and
  selection safely continues to another usable candidate or prior value.
- [ ] `--summary-name-attributes` replaces the generic fallback order for that run,
  supports nested paths, and rejects empty lists or entries with a clear error.
- [ ] Existing resource-specific summary mappings produce the same identifying content
  when `--summary-name-attributes` is absent or present.
- [ ] Every indexed resource summary includes its exact string-key or numeric-index
  suffix for create, update, replace, delete, import, and move scenarios.
- [ ] A computed value referenced only by bare or module-qualified `each.key`,
  `each.value`, `count.index`, or `self` renders without a reference hint.
- [ ] Existing meaningful hints, including `each.value.<attribute>`, static resource,
  variable, and local references, remain visible; sensitive computed values remain
  masked.
- [ ] Every Refactoring Summary import and move shows the full Terraform address, with
  generic display identity appended when available.
- [ ] An imported resource with empty prior attribute state includes clear full-desired-
  state context, while an import with meaningful prior state does not show that note;
  an msgraph prior state containing only `id`, `api_version`, `url`,
  `ignore_missing_property`, and `body: {}` counts as empty for this purpose.
- [ ] Unchanged `api_version`, `url`, and `ignore_missing_property` on msgraph resources
  remain accessible in a collapsed provider-settings area.
- [ ] On a create or an import with empty prior state, desired provider metadata remains
  available in the collapsed provider-settings area; when meaningful prior state shows
  provider metadata being added, removed, or changed, it appears in the ordinary
  resource diff and changed-attribute count.
- [ ] With `--show-unchanged-values`, unchanged msgraph provider metadata remains in the
  collapsed provider-settings area and changed provider metadata remains in the primary
  diff and changed-attribute count.
- [ ] `--hide-unchanged-outputs` removes only no-op outputs from root and module output
  sections, omits sections left empty, and preserves changed output rendering and
  sensitive-value protection.
- [ ] Reports generated without either new command-line option preserve existing output
  selection and resource-specific summaries, apart from the specified instance suffix,
  generic identity, pseudo-hint, refactoring-label, import-context, and provider-metadata
  presentation changes.
- [ ] Automated acceptance coverage includes generic create/update/delete resources,
  string and numeric instances, candidate fallback and override order, meaningful and
  pseudo computed references, import/move labels, empty and populated imported prior
  state, unchanged and changed provider metadata, and root/module output filtering.
- [ ] The resulting Markdown renders legibly on all currently supported render targets.

## Source-Idea Traceability

| Source idea from issue #674 | Requirement in this feature |
| --- | --- |
| 1. Generic `displayName` and similar summary identity | Generic display identity and ordered override |
| 2. `for_each` / `count` instance key | Resource instance identity |
| 7. Better known-after-apply hints | Suppress pseudo-only hints while preserving specific existing hints |
| 9. Consistent Refactoring Summary labels | Full address plus optional generic display identity |
| 10a. Imports with empty prior state | Empty imported prior-state note |
| 10b. Repetitive provider meta-attributes | Collapsed unchanged msgraph provider settings; ordinary rendering when changed |
| 10c. Unchanged outputs | Opt-in `--hide-unchanged-outputs` filtering |

Source ideas 3 and 4 are assigned to work item 147,
`docs/features/147-guid-display-name-resolution/specification.md`. Source ideas 5, 6,
and 8 are assigned to work item 148,
`docs/features/148-compact-collection-diffs/specification.md`. The four rendering
defects listed under source idea 9 are assigned to a suggested grouped bug-fix
follow-up.

## Open Questions and Assumptions

1. **Override composition:** Does `--summary-name-attributes` replace or extend the
   built-in list? This specification assumes it replaces the list, giving callers a
   deterministic order they fully control.
2. **Pseudo-reference traversal:** Should tfplan2md follow `each.value`, `each.key`, or
   `count.index` through configuration expressions? This specification assumes no;
   pseudo-only hints are omitted, while existing specific hints such as
   `each.value.group_object_id` remain useful and visible.
3. **Refactoring label format:** This specification assumes the full address is always
   the stable label and a resolved display identity is secondary context.
4. **Provider metadata:** This specification assumes unchanged `api_version`, `url`,
   and `ignore_missing_property` are collapsed for msgraph resources, while changed
   values stay in the primary diff and counts.
5. **Output scope:** This specification assumes changed-output filtering applies to both
   root and module output sections and filters only Terraform no-op actions.
6. **Empty import trigger:** This specification assumes housekeeping-only prior state is
   equivalent to empty prior state for the explanatory import note. Specifically,
   `id`, `api_version`, `url`, and `ignore_missing_property`, plus empty objects or
   collections such as `body: {}`, do not suppress the note; any non-null, non-empty
   substantive value outside those paths does.
