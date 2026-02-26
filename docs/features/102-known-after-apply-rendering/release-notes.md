# Known-After-Apply Rendering

Terraform plans frequently contain resources where computed values — IDs, generated keys, or
anything that depends on a resource not yet created — are not resolved until after apply.
Previously, tfplan2md silently dropped these attributes from the report, which made plan reviews
misleading: a resource with a dozen computed attributes would appear to have no attribute changes
at all. This release fixes that.

## ✨ Features

- **Computed attributes are now visible in tables.** Any attribute where Terraform reports
  `after_unknown: true` now appears in the attribute table with a `(known after apply)` value
  instead of being silently omitted. Only attributes explicitly present in the plan's `after`
  object are included — purely server-assigned outputs (e.g., bare `id` on most `azurerm_*`
  resources) are not fabricated.

- **Configuration reference labels.** When a computed attribute's value comes from a static
  Terraform configuration reference (e.g., `group_object_id = azuread_group.admins.object_id`),
  the table shows the source resource as context:
  `(known after apply: azuread_group.platform_engineers)` instead of the generic placeholder.
  This applies both to attribute table values and to AzureAD group member summary lines.

- **Sensitive + computed renders correctly.** When an attribute is both sensitive and
  computed — for example, a primary access key being rotated — the After column shows
  `🔒(known after apply)` and the Before column continues to show `(sensitive)`, never the
  actual secret.

- **Whole-resource unknown shows an explanatory note.** For newly created resources where
  every attribute is computed (Terraform reports `after_unknown: true` on the whole resource), 
  the report now shows `_(all values known after apply)_` instead of `_No attribute changes._`,
  which was factually incorrect.

## 🐛 Bug fixes

- **AzureAD group member summary lines were blank when IDs are computed.** Adding a
  `azuread_group_member` resource where both `group_object_id` and `member_object_id` reference
  resources not yet created resulted in an empty summary (`—`). The summary now shows either the
  configuration reference label (e.g., `azuread_group.platform_engineers → azuread_user.admin`)
  or the generic `(known after apply)` placeholder, giving reviewers enough context to understand
  the change.

## 🔗 Commits

- [`baed762b`](https://github.com/oocx/tfplan2md/commit/baed762b) feat: implement known-after-apply rendering scenarios
