# Fix: azuread_group_member Renders with Empty Summary and No Details Table

This release fixes a rendering bug where `azuread_group_member` resources with computed
(`group_object_id` / `member_object_id` unknown at plan time) would produce an empty summary
line and no attributes table.

## 🐛 Bug fixes

- **Empty summary line**: When `group_object_id` or `member_object_id` are null at plan time
  (because they reference IDs that will only be known after a dependent resource is created),
  the summary line now shows meaningful context instead of an empty code span. The context is
  resolved in this priority order:

  1. **Static resource reference** from the Terraform configuration block — e.g.,
     `azuread_group.platform_engineers` when the attribute is
     `group_object_id = azuread_group.platform_engineers.object_id`.
  2. **Indexed resource reference** — when the resource uses `count` and the attribute
     references a list, e.g., `azuread_user.users[0]`.
  3. **for_each instance key** — when the resource uses `for_each` with a map and the
     references are dynamic (`each.value.*`), the instance key is shown
     (e.g., `"team-example - user@example.de"`). The key often encodes the group name and
     user identifier, providing useful context at a glance.
  4. **`(known after apply)`** — fallback when no additional context is available.

- **Missing attributes table**: Attributes listed in `after_unknown` (where both `before` and
  `after` values are `null`) were previously treated as unchanged and silently omitted from the
  details table. They now appear with the value `(known after apply)`.
