# Feature: Show Terraform Expression References for "Known After Apply" Attributes

## Overview

When a resource attribute is "known after apply", tfplan2md currently shows the literal text
`(known after apply)`. This tells reviewers that a value will be set at apply time, but not
_what_ it will be set to.

The Terraform plan JSON `configuration` block records the source expressions used for each
attribute — e.g., `each.value.group_object_id`, `azuread_group.my_group.object_id`, or
`var.group_id`. By reading those expressions, tfplan2md can show the actual Terraform reference
that will produce the value, giving reviewers meaningful context rather than a generic placeholder.

## User Story

> As a user reviewing a Terraform plan that contains resources with computed attributes,  
> I want to see the actual source reference (e.g. `each.value.group_object_id`) instead of just
> `(known after apply)`,  
> so that I can understand where the value will come from at apply time and verify that the
> correct source is wired up.

## User Goals

- **Understand data flow**: Know where computed attribute values originate — which variable,
  output, or resource attribute feeds into the planned resource.
- **Catch wiring mistakes early**: Spot errors like a wrong `each.value.*` key or a missing
  module output before running `terraform apply`.
- **Replace meaningless placeholders**: Move beyond `(known after apply)` to something that
  carries real review value.

## Scope

### In Scope

#### 1. Configuration Expression Parsing

Parse the `configuration` block in the Terraform plan JSON to extract the `references` array
for each resource attribute.

The configuration block structure for root-module resources:

```json
"configuration": {
  "root_module": {
    "resources": [
      {
        "address": "azuread_group_member.user_groups",
        "expressions": {
          "group_object_id": {
            "references": ["each.value.group_object_id", "each.key", "var.user_groups_members"]
          },
          "member_object_id": {
            "references": ["each.value.user_object_id", "each.key", "var.user_groups_members"]
          }
        }
      }
    ]
  }
}
```

- Parse the `references` array for each attribute in `expressions`.
- Match configuration resources to plan resource changes by **base address** (the address without
  the instance key, e.g., `azuread_group_member.user_groups` matches
  `azuread_group_member.user_groups["team-example - user@example.de"]`).
- The `configuration` block is already present as a raw `JsonElement?` field in `TerraformPlan`.
  It needs structured parsing to access `expressions`.

#### 2. Reference Selection Logic

When the `references` array for an attribute contains multiple entries, select the single most
informative one using this priority order:

1. **Full resource/data-source attribute reference** — a reference that contains a dot-separated
   path starting with a resource type (e.g., `azuread_group.my_group.object_id`,
   `data.azuread_user.admin.object_id`, `module.networking.subnet_id`).
2. **`each.value.<attribute_name>`** — conveys the attribute name being read from `each.value`,
   e.g., `each.value.group_object_id`.
3. **`var.<name>`** — a variable reference.
4. **Any other non-trivial reference** — anything not in the skip list below.
5. **Skip** — entries to always ignore: bare `each.key`, bare `each.value` (without an attribute
   path suffix).

When multiple entries at the same priority level are present, use the **first** qualifying entry.

#### 3. Display Format

Show the selected reference with a `→` prefix to visually distinguish it from literal values:

```
→ azuread_group.my_group.object_id
```

The display value replaces the `(known after apply)` text for that attribute.

Example attribute table after this feature:

| Attribute         | Value                                    |
| ----------------- | ---------------------------------------- |
| group_object_id   | → each.value.group_object_id             |
| id                | (known after apply)                      |
| member_object_id  | → each.value.user_object_id              |

> Note: `id` has no matching `expressions` entry (it is provider-computed, not user-supplied), so
> it falls back to `(known after apply)`.

#### 4. Fallback Behaviour

If any of the following conditions hold, fall back to displaying `(known after apply)` unchanged:

- The plan `configuration` block is absent from the plan JSON.
- No resource entry matching the base address is found in the configuration resources list.
- The attribute has no `expressions` entry in the configuration resource.
- The `references` array is absent or empty for the attribute.
- All entries in `references` are in the skip list (bare `each.key`, bare `each.value`).

#### 5. Name Mapping Integration (optional enhancement)

When `--name-mapping` is configured and a selected reference is a **full resource attribute
reference** (priority 1 above, e.g., `azuread_group.my_group.object_id`), attempt to resolve
the resource address portion (`azuread_group.my_group`) via the existing principal/display-name
mapping infrastructure.

- If a mapped display name is found for the referenced resource, show both: e.g.,
  `→ Engineering Team [azuread_group.my_group.object_id]`.
- If no mapping is found, show the reference string unchanged: `→ azuread_group.my_group.object_id`.
- This enhancement is **optional**: if it proves complex to wire up cleanly, it can be deferred
  to a follow-on feature without blocking the core reference display.

#### 6. Root Module Only (initial scope)

For the initial implementation, parse expressions only for resources in the `configuration.root_module.resources` list.

Resources inside child modules have their configuration nested under
`configuration.root_module.module_calls.<module_name>.module.resources`. Their `resource_changes`
entries have addresses like `module.azure.azuread_group_member.user_groups`.
Module-level configuration parsing is **out of scope** for this feature (see below).

### Out of Scope

- **Module-level expression parsing**: Resources whose `module_address` is non-null (i.e., inside
  a child module) will continue to show `(known after apply)` for computed attributes. A follow-on
  feature can extend expression parsing to `module_calls` nesting.
- **Output expression references**: `configuration.root_module.outputs[*].expression.references`
  is not part of this feature.
- **Non-unknown attributes**: Expression references are only surfaced for attributes where
  `after_unknown` is `true`. Known (computed) values are shown as-is.
- **Showing multiple references**: Only one reference is displayed per attribute (the best match
  per the selection logic). Displaying the full array is not in scope.
- **Expression constant values**: When a configuration attribute has a `constant_value` instead of
  `references` (e.g., a hard-coded string), this feature does not apply — the known `after` value
  is already shown.
- **Diff (terraform-show) renderer**: The `--format diff` renderer (`TerraformShowRenderer`)
  already has its own `after_unknown` handling. This feature applies to the markdown table renderer
  only.

## User Experience

### Example: `azuread_group_member` with `for_each`

Terraform configuration:
```hcl
resource "azuread_group_member" "user_groups" {
  for_each         = { for v in local.user_groups_members : "${v.group_name} - ${v.user_name}" => v }
  group_object_id  = each.value.group_object_id
  member_object_id = each.value.user_object_id
}
```

**Before this feature:**

```
➕ azuread_group_member user_groups

| Attribute         | Value                 |
| ----------------- | --------------------- |
| group_object_id   | (known after apply)   |
| id                | (known after apply)   |
| member_object_id  | (known after apply)   |
```

**After this feature:**

```
➕ azuread_group_member user_groups

| Attribute         | Value                              |
| ----------------- | ---------------------------------- |
| group_object_id   | → each.value.group_object_id       |
| id                | (known after apply)                |
| member_object_id  | → each.value.user_object_id        |
```

### Example: Resource-to-resource reference

Terraform configuration:
```hcl
resource "azuread_group_member" "admins" {
  group_object_id  = azuread_group.admins.object_id
  member_object_id = azuread_user.jane.object_id
}
```

**After this feature:**

```
➕ azuread_group_member admins

| Attribute         | Value                                    |
| ----------------- | ---------------------------------------- |
| group_object_id   | → azuread_group.admins.object_id         |
| id                | (known after apply)                      |
| member_object_id  | → azuread_user.jane.object_id            |
```

### Example: With name mapping applied (optional enhancement)

If `azuread_group.admins` is mapped to "Engineering Admins" in the name-mapping file:

```
| group_object_id   | → Engineering Admins [azuread_group.admins.object_id] |
```

### Example: No configuration block (fallback)

For plan files that do not include the `configuration` block (e.g., older `terraform show -json`
output), all attributes continue to show `(known after apply)` unchanged.

## Success Criteria

- [ ] When an attribute is in `after_unknown` AND the plan `configuration` block contains an
  `expressions` entry with a non-empty `references` array for that attribute, the selected
  reference is displayed as `→ <reference>` instead of `(known after apply)`.
- [ ] The reference selection logic is applied: resource attribute references (priority 1) are
  preferred over `each.value.*` (priority 2) over `var.*` (priority 3) over other entries (priority 4).
- [ ] Bare `each.key` and bare `each.value` entries are skipped and never displayed.
- [ ] When all entries in `references` are in the skip list, the attribute falls back to
  `(known after apply)`.
- [ ] When the `configuration` block is absent or no matching configuration resource is found,
  the attribute falls back to `(known after apply)` — no error, no output change.
- [ ] Attributes that have a `references` array entry but are **not** in `after_unknown` are
  unaffected (i.e., existing known values continue to render normally).
- [ ] Resources inside child modules (`module_address` is non-null) are unaffected and continue
  to show `(known after apply)`.
- [ ] When `--name-mapping` is configured and the selected reference is a resource attribute
  reference whose resource address resolves in the name-mapping data, the mapped name is shown
  alongside the reference.
- [ ] When `--name-mapping` is configured but no mapping is found, the raw reference string is
  shown unchanged.
- [ ] Tests cover:
  - [ ] Single reference in `references` array — reference is displayed.
  - [ ] Multiple references — correct priority selection is applied.
  - [ ] Attribute with only skippable references (`each.key`, `each.value`) — fallback to
    `(known after apply)`.
  - [ ] Attribute without an `expressions` entry — fallback to `(known after apply)`.
  - [ ] Plan with no `configuration` block — all unknown attributes fall back to
    `(known after apply)`.
  - [ ] Resource inside a child module — fallback to `(known after apply)` (module parsing is
    out of scope).
  - [ ] Name mapping applied to a full resource reference (if the name-mapping enhancement is
    in scope for this iteration).
- [ ] Existing snapshots for resources with known `after` values are unchanged.
- [ ] The `azuread-group-member-all-unknown` snapshot is updated to reflect the new `→` reference
  display (or a new fixture with `expressions` is added alongside the existing fixture).

## Open Questions

### For Architect

1. **Parsing strategy**: The `configuration` field in `TerraformPlan` is currently a raw
   `JsonElement?`. Should expressions be parsed eagerly into a typed model during plan
   deserialization, or resolved lazily on-demand in `ReportModelBuilder`?

2. **Address matching**: Plan resource change addresses include instance keys
   (e.g., `azuread_group_member.user_groups["team-example - user@example.de"]`), while
   configuration resource addresses do not. What is the canonical approach for stripping instance
   keys to match the two?

3. **Name-mapping integration**: The existing `PrincipalMapper` maps Azure AD principal GUIDs
   to display names. Is there an existing lookup path for resource addresses
   (`azuread_group.my_group`), or would this require a new mapping abstraction?

4. **Module-level expressions**: Is there a recommended phasing strategy for eventually extending
   expression parsing to child modules, so the initial implementation does not paint us into a
   corner?
