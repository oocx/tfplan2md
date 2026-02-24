# azuread_group_member and all-unknown resources now render attribute tables

This patch fixes a bug where resources with all attributes marked `(known after apply)` — such as `azuread_group_member` — rendered with a completely empty attribute section instead of showing the expected `(known after apply)` values.

## 🐛 Bug fixes

### Fixed empty attribute table for resources where all values are "known after apply"

**Problem:** When a Terraform plan contained a resource where every attribute value was computed from another planned resource (i.e., all attributes appeared in `after_unknown` and `after` was `null`), the tfplan2md report rendered the resource with no attribute table at all — just the resource header line with no details.

This was particularly common with `azuread_group_member` resources where `group_object_id` and `member_object_id` are derived from `azuread_group` or `azuread_user` resources created in the same plan:

```hcl
resource "azuread_group_member" "user_groups" {
  for_each         = { for v in local.user_groups_members : "${v.group_name} - ${v.user_name}" => v }
  group_object_id  = each.value.group_object_id
  member_object_id = each.value.user_object_id
}
```

**Symptom:** The report showed the resource header but no attribute details:

```markdown
➕ azuread_group_member user_groups —

➕ azuread_group_member user_groups —
```

**Root cause:** The `BuildAttributeChanges` method in the report model builder only consulted `change.Before` and `change.After` to build the set of attribute keys to render. When both are `null` (as is the case for a create action where all values are computed), no keys were found, and the attribute table was empty.

**Fix:** `BuildAttributeChanges` now also consults `change.AfterUnknown`. Attributes present in `after_unknown` are included in the table with `(known after apply)` as their value. This matches Terraform's own `terraform show` output format.

**After the fix**, the same resources render correctly:

```markdown
➕ azuread_group_member user_groups —

| Attribute        | Value                 |
| ---------------- | --------------------- |
| group_object_id  | `(known after apply)` |
| id               | `(known after apply)` |
| member_object_id | `(known after apply)` |
```

### Impact

This fix affects any resource type where ALL attribute values are computed at apply time, not just `azuread_group_member`. Any resource whose Terraform plan JSON has `"after": null` alongside a populated `"after_unknown"` object will now render correctly.

**Also fixed:** Attributes where Terraform emits both `"after": {"attr": null}` and `"after_unknown": {"attr": true}` (a partially-known resource) previously disappeared from the table because `null == null` was treated as "unchanged". These are now correctly shown as `(known after apply)`.

## 🔗 Commits

- [`c27a127`](https://github.com/oocx/tfplan2md/commit/c27a1270) fix: show (known after apply) for attributes with after_unknown=true

## 🧪 Test coverage

Added a new test fixture and snapshot test covering the all-unknown case:

- **`azuread-group-member-all-unknown-plan.json`** — plan fixture with `azuread_group_member` where `"after": null` and all attributes in `"after_unknown"`
- **`azuread-group-member-all-unknown.md`** — snapshot baseline showing all three attributes rendered as `(known after apply)`
- **`AzureAdSnapshotTests.Snapshot_AzureAd_GroupMemberAllUnknown_MatchesBaseline`** — new snapshot test

Multiple existing snapshots were updated to include `(known after apply)` rows for attributes that were previously silently dropped (e.g., `null_resource` computed `id` attributes in ephemeral resource tests).

All 1233 tests passing.
