# Feature: Known-After-Apply Rendering

## Overview

Terraform reports two categories of computed values in its plan JSON:

- **`after_unknown: true` on a specific attribute** — the attribute is user-configured, but its final value depends on something not yet resolved (e.g., a reference to a resource that does not exist yet).
- **`after_unknown: true` on a whole resource** — every attribute of a new resource will only be known after apply.

Currently, tfplan2md silently drops all of these from reports:

1. **Attribute tables show `_No attribute changes._`** for any resource where all `after` values are `null` — because `null == null` is treated as "unchanged". The `after_unknown` field is never read during resource rendering.
2. **AzureAD group member summary lines are blank** (just `—`) when both `group_object_id` and `member_object_id` are computed, because `FormatCodeSummary("")` silently returns an empty string.

This feature fixes both problems and introduces a way to surface configuration-level source references (e.g., `azuread_group.platform_engineers`) as useful context wherever the actual value is not yet known.

---

## User Goals

- Reviewers can see which attributes are computed (and therefore pending) rather than silently omitted.
- For AzureAD group members, the summary line shows enough context to know which group and which member are involved, even when IDs are not yet resolved.
- When the Terraform configuration contains a static resource reference (e.g., `group_object_id = azuread_group.admins.object_id`), reports use that reference as a meaningful label instead of the generic `(known after apply)` placeholder.
- The fix applies universally to all resource types, not just AzureAD.

## Scope

### In Scope

- Show computed attributes in attribute tables with `(known after apply)` or a configuration reference label.
- Fix empty `azuread_group_member` summary lines when IDs are computed.
- Use configuration-block `references` arrays to show the source resource/variable name when available.
- Handle all known plan shapes:
  - `after: { attr: null }` + `after_unknown: { attr: true }` (attribute-level computed)
  - `after: null` + `after_unknown: true` (whole-resource computed)
  - Mixed: some attributes known, some computed

### Out of Scope

- Showing computed attributes that do not appear in the plan's `after` or `after_unknown` at all (e.g., attributes that are simply absent from the plan).
- Displaying configuration references for non-computed attributes.
- Changes to the `--show-unchanged` flag behavior.

---

## Design Decisions

### A — Which computed attributes appear in tables

Only attributes that are **explicitly present in the `after` object as JSON `null`** (with a corresponding `after_unknown: true` flag) are included. Attributes absent from `after` entirely — such as server-assigned `id` on most `azurerm_*` resources — are not added to the table.

This relies on the Terraform CLI convention: user-configured computed references (e.g., `group_object_id = azuread_group.admins.object_id`) appear as `null` in `after`; purely server-assigned outputs (`id`, `etag`) are absent from `after` unless the provider explicitly writes them there.

### B — Attribute table value format for computed attributes

When a computed attribute has a configuration reference available, the table shows:

```
(known after apply: reference)
```

The reference shown is the **most specific useful reference** from the `expressions.references` array, using this priority order:

1. **Static resource reference** — `type.name` (2 parts) or `module.name.type.name` (4 parts), e.g., `azuread_group.platform_engineers`
2. **`each.value.attribute`** — conveys the attribute name being sourced, e.g., `each.value.group_object_id`
3. **`var.something` or `local.something`** — identifies the variable source
4. **`(known after apply)`** — used when no reference is found or the configuration block is absent

Useless bare meta-arguments (`each.key`, `each.value`, `count.index`, `self`) are skipped.

The **summary line** for AzureAD group members uses the stripped resource-level reference (e.g., `azuread_group.platform_engineers`, not `azuread_group.platform_engineers.object_id`).

### C — Sensitive + computed attribute

When an attribute is **both sensitive and computed** (present in `before_sensitive` and `after_unknown`), the After column shows:

```
🔒(known after apply)
```

The lock icon signals that the before value was sensitive. The transition is deliberately visible so reviewers cannot miss a key rotation or secret regeneration.

---

## Scenarios and Expected Output

All rendered summaries follow the [report style guide](../../report-style-guide.md): values in `<code>` tags inside `<summary>`, values in backtick inline-code in tables.

---

### Scenario 1 — AzureAD group member, no configuration block, all IDs unknown

**Plan shape:**
```json
{
  "address": "azuread_group_member.all_unknown",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": null, "id": null },
    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true }
  }
}
```
No `configuration` block is present.

**Expected summary line:**
```
➕ azuread_group_member all_unknown — (known after apply) → (known after apply)
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply)` |
| id | `(known after apply)` |
| member_object_id | `(known after apply)` |
```

All three attributes are present in `after` as `null`, so all three appear. No configuration references are available, so plain `(known after apply)` is used.

---

### Scenario 2 — AzureAD group member, static resource references in configuration

**Plan shape:**
```json
{
  "address": "azuread_group_member.platform_admin_member",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": null, "id": null },
    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true }
  }
}
```
**Configuration block:**
```json
{
  "address": "azuread_group_member.platform_admin_member",
  "expressions": {
    "group_object_id": {
      "references": ["azuread_group.platform_engineers.object_id", "azuread_group.platform_engineers"]
    },
    "member_object_id": {
      "references": ["azuread_user.admin.object_id", "azuread_user.admin"]
    }
  }
}
```

**Expected summary line** (uses the stripped resource-level reference):
```
➕ azuread_group_member platform_admin_member — azuread_group.platform_engineers → azuread_user.admin
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply: azuread_group.platform_engineers)` |
| id | `(known after apply)` |
| member_object_id | `(known after apply: azuread_user.admin)` |
```

`id` has no configuration reference, so it shows plain `(known after apply)`.

---

### Scenario 3 — AzureAD group member, for_each with `each.value` references, string instance key

**Plan shape:**
```json
{
  "address": "azuread_group_member.user_groups[\"team-example - user@example.de\"]",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": null, "id": null },
    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true }
  }
}
```
**Configuration block:**
```json
{
  "address": "azuread_group_member.user_groups",
  "expressions": {
    "group_object_id": { "references": ["each.value.group_object_id", "each.value"] },
    "member_object_id": { "references": ["each.value.user_object_id", "each.value"] }
  }
}
```

No static resource reference is available. `each.value.group_object_id` is the best useful reference (priority 2).

**Expected summary line** (falls back to the for_each string instance key as context — no static resource reference exists):
```
➕ azuread_group_member user_groups — "team-example - user@example.de" → "team-example - user@example.de"
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply: each.value.group_object_id)` |
| id | `(known after apply)` |
| member_object_id | `(known after apply: each.value.user_object_id)` |
```

> The summary uses the for_each string instance key (it encodes group+user in the real-world case). The table uses `each.value.group_object_id` / `each.value.user_object_id` which are the best available references (priority 2).

---

### Scenario 4 — AzureAD group member, mixed: group unknown, member known

**Plan shape:**
```json
{
  "address": "azuread_group_member.platform_admin_member",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": "user-200", "id": null },
    "after_unknown": { "group_object_id": true, "id": true }
  }
}
```
No configuration block (or only dynamic references for `group_object_id`).

**Expected summary line:**
```
➕ azuread_group_member platform_admin_member — (known after apply) → user-200
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply)` |
| id | `(known after apply)` |
| member_object_id | `user-200` |
```

`member_object_id` has a concrete known value; `group_object_id` and `id` are computed with no config reference.

---

### Scenario 5 — AzureAD group member, for_each numeric instance key (count-based), static group reference, var member reference

**Plan shape:**
```json
{
  "address": "azuread_group_member.members[0]",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": null, "id": null },
    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true }
  }
}
```
**Configuration block:**
```json
{
  "address": "azuread_group_member.members",
  "expressions": {
    "group_object_id": {
      "references": ["azuread_group.admins.object_id", "azuread_group.admins"]
    },
    "member_object_id": {
      "references": ["count.index", "var.users"]
    }
  }
}
```

- `group_object_id`: static resource reference `azuread_group.admins` (priority 1). Instance key `0` is numeric → combined as `azuread_group.admins[0]` in summary.
- `member_object_id`: `count.index` is useless; `var.users` is priority 3.
- `id`: no configuration reference.

**Expected summary line:**
```
➕ azuread_group_member members — azuread_group.admins[0] → (known after apply)
```

> Numeric instance key is appended to the static resource ref in the summary to distinguish individual count instances. It is **not** used alone as a label.

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| group_object_id | `(known after apply: azuread_group.admins)` |
| id | `(known after apply)` |
| member_object_id | `(known after apply: var.users)` |
```

---

### Scenario 6 — Non-AzureAD resource, computed attribute present in `after`

This covers any resource where some attributes resolve to `null` in `after` with `after_unknown: true`. The most common case where `id` appears in `after` is provider-specific (some providers include it, others don't).

**Plan shape (azurerm_resource_group where provider writes `id` into `after`):**
```json
{
  "address": "azurerm_resource_group.demo",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "id": null, "location": "eastus", "name": "rg-demo" },
    "after_unknown": { "id": true }
  }
}
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| id | `(known after apply)` |
| location | `🌍 eastus` |
| name | `🆔 rg-demo` |
```

`id` appears because it is present in `after` as `null`. No configuration reference exists for `id`, so plain `(known after apply)` is used.

**Plan shape (azurerm_resource_group where provider does NOT write `id` into `after`):**
```json
{
  "address": "azurerm_resource_group.demo",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "location": "eastus", "name": "rg-demo" },
    "after_unknown": { "id": true }
  }
}
```

**Expected attribute table:**
```markdown
| Attribute | Value |
| ----------- | ------- |
| location | `🌍 eastus` |
| name | `🆔 rg-demo` |
```

`id` does not appear because it is absent from `after`. Only the keys present in `after` are iterated (Decision A1).

---

### Scenario 7 — Update resource, attribute transitions from sensitive-known to computed

An attribute that previously had a real (sensitive) value becomes computed on the next apply — for example, a key rotation.

**Plan shape:**
```json
{
  "address": "azurerm_storage_account.data",
  "change": {
    "actions": ["update"],
    "before": { "account_replication_type": "LRS", "primary_access_key": "abc123" },
    "after": { "account_replication_type": "GRS", "primary_access_key": null },
    "after_unknown": { "primary_access_key": true },
    "before_sensitive": { "primary_access_key": true },
    "after_sensitive": {}
  }
}
```

`primary_access_key` is in `after` as `null` AND in `before_sensitive` → both sensitive and computed.

**Expected summary (change count includes the computed attribute):**
```
🔄 azurerm_storage_account data — stdata | 2 🔧 account_replication_type, primary_access_key
```

**Expected attribute table (diff view for update actions):**
```markdown
| Attribute | Before | After |
| ----------- | -------- | ------- |
| account_replication_type | `LRS` | `GRS` |
| primary_access_key | `(sensitive)` | `🔒(known after apply)` |
```

The lock icon signals the "was sensitive" origin. No actual value is revealed. The change is visible so a key rotation cannot silently disappear from a review.

---

### Scenario 8 — Whole-resource unknown (`after_unknown: true` as literal boolean)

Some resources use the compact form where `after` is `null` and `after_unknown` is the JSON boolean `true` (not an object).

**Plan shape:**
```json
{
  "address": "null_resource.app_config",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": null,
    "after_unknown": true,
    "before_sensitive": false,
    "after_sensitive": false
  }
}
```

`after` is entirely `null`, so no keys are present in either `after` or `before`. The result is an empty key set → no attribute table rows. However, `_No attribute changes._` must not be shown; instead render an empty table or omit the table entirely with a note.

**Expected output:**
The resource block renders without an attribute table body (no rows to show). The `_No attribute changes._` placeholder is **not** used.

> This edge case is intentionally minimal — the plan provides no attribute-level data. Future iterations may add a `(all values known after apply)` note if desired.

---

### Scenario 9 — Child resource with computed `ChildReferenceAttribute`

When a child resource (e.g., `azurerm_subnet`) matches a registered parent-child relationship, the system matches it to its parent by checking the `ChildReferenceAttribute` (e.g., `virtual_network_name`) against the parent's `ParentIdAttribute` (`name`). If the child's `ChildReferenceAttribute` is `null` / computed, the match cannot be made, and the child renders as a standalone resource.

**Plan shape:**
```json
{
  "address": "azurerm_subnet.app",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "name": "app-subnet", "virtual_network_name": null, "address_prefixes": ["10.0.1.0/24"] },
    "after_unknown": { "virtual_network_name": true }
  }
}
```

`virtual_network_name` is computed — the subnet cannot be matched to any `azurerm_virtual_network` parent.

**Expected output:**

The subnet renders as a standalone create entry (not nested under a VNet block):

```
➕ azurerm_subnet app

| Attribute | Value |
| ----------- | ------- |
| address_prefixes | `["10.0.1.0/24"]` |
| name | `app-subnet` |
| virtual_network_name | `(known after apply)` |
```

The `virtual_network_name` row appears in the table because it is `null` in `after` with `after_unknown: true` (Decision A1 applies). No parent merge occurs.

---

## Invariants

1. **`_No attribute changes._` must never appear for a resource with computed attributes.** If any computed attribute is included in the table, the table must be shown.
2. **The AzureAD group member summary line must never be blank when IDs are computed.** It must show at minimum `(known after apply) → (known after apply)`.
3. **Reference priority for AzureAD group member summary lines:**
   - Static resource reference (`type.name`) — highest priority
   - For_each string instance key — when only `each.value.*` refs exist
   - `(known after apply)` — fallback when no configuration block or no useful references
4. **Numeric for_each instance keys are never used alone as context labels** in summaries; they are only appended to a static resource reference (e.g., `azuread_group.admins[0]`).
5. **The `after_unknown: true` whole-resource boolean pattern** is handled the same as the attribute-level pattern for filtering purposes (no keys to show = no table rows).
6. **Reference selection for table values** strips the trailing `.attribute` segment from `type.name.attribute` references to yield `type.name` as the display label.
7. **Summary counts for create resources**: Computed attributes on newly created resources do not contribute to a separate attribute change count. A "create" resource is counted at the resource level only (➕ count); no per-attribute count is shown next to the summary line.
8. **Summary counts for update resources**: Computed attributes that appear in `after` as `null` with `after_unknown: true` ARE counted in the attribute change count shown in the summary line and ARE listed by name alongside other changed attributes (as in Scenario 7 — `2 🔧 account_replication_type, primary_access_key`).
9. **Parent/child merging with a computed reference attribute**: If a child resource's `ChildReferenceAttribute` value is `null` (computed / known after apply), the child cannot be matched to a parent resource and renders standalone. The `(known after apply)` value is shown in the attribute table for that attribute (Scenario 9).
10. **Sensitive value protection via references**: Configuration reference strings (sourced from the plan's `expressions.references` array) are Terraform expression paths (e.g., `azuread_group.admins.object_id`) — never actual values. They are always safe to display. The `before` value of a sensitive attribute is never surfaced; it always renders as `(sensitive)`, regardless of whether the attribute is also computed.

---

## Success Criteria

- [ ] AzureAD group member resources with all-unknown IDs show a non-empty summary line (Scenario 1).
- [ ] AzureAD group member resources with static config references show those references in both the summary and the attribute table values (Scenarios 2, 5).
- [ ] AzureAD group member resources with for_each string keys show the instance key in the summary and `each.value.*` references in the table (Scenario 3).
- [ ] Mixed resources (some attributes known, some computed) render correctly without omitting either (Scenario 4).
- [ ] Attribute tables include computed attributes with `(known after apply)` or `(known after apply: reference)` instead of `_No attribute changes._`.
- [ ] Attributes absent from `after` entirely are not added to the table, even if present in `after_unknown` (Decision A1).
- [ ] Attributes that are both sensitive and computed show `🔒(known after apply)` in the After column (Scenario 7).
- [ ] Sensitive+computed attributes appear in the change count and diff table.
- [ ] Resources with only non-computed attributes still render without any `(known after apply)` rows.
- [ ] Existing snapshot tests for resources with only known values continue to pass unchanged.
- [ ] Both `after[attr] = null / after_unknown[attr] = true` and `after = null / after_unknown = true` (whole-resource boolean) patterns are handled (Scenarios 1–8).
- [ ] Computed attributes on create resources are NOT counted in any attribute-level change count; only the resource-level create count is affected (Invariant 7).
- [ ] Computed attributes on update resources ARE counted in the attribute change count shown in the summary line (Scenario 7, Invariant 8).
- [ ] A child resource whose `ChildReferenceAttribute` is computed renders standalone, not merged under a parent (Scenario 9).
- [ ] Configuration reference strings used as labels are always Terraform expression paths and never contain sensitive values.
- [ ] The `before` value of a sensitive attribute never appears in any rendered output, even when the attribute transitions to computed (Scenario 7, Invariant 10).
