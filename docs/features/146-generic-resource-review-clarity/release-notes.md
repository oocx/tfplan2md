# ✨ Generic resource review clarity

Generic resources now show a useful object name beside the exact Terraform instance
key or index, making similar resources easier to distinguish in a plan review.
Fallback identities use `name`, `display_name`, `displayName`, `body.displayName`,
`body.properties.displayName`, then `body.name`. Missing, empty, non-scalar,
unknown, and masked values are skipped. Existing resource-specific summary mappings
and the invocation’s sensitive-value policy continue to apply.

- Import and move entries in the Refactoring Summary show the full Terraform address,
  with a generic display name when available.
- Bare `each.key`, `each.value`, `count.index`, and `self` hints now show plain
  `(known after apply)`. Specific attribute, resource, variable, and local references
  retain their useful hints.
- Imports with no substantive prior state include a note explaining that the report
  shows the full desired state. Housekeeping-only state counts as empty.
- Unchanged Microsoft Graph `api_version`, `url`, and `ignore_missing_property`
  values are available in a collapsed **Provider settings** area. Changed settings
  remain in the primary diff and changed-attribute count.

## ▶️ Getting started

Override the ordered generic fallback paths for one invocation:

```sh
tfplan2md --summary-name-attributes body.title,body.properties.title plan.json
```

To omit unchanged root and module outputs while retaining created, updated, and
deleted outputs:

```sh
tfplan2md --hide-unchanged-outputs plan.json
```

Both options are optional; unchanged outputs remain visible by default.

## 📸 Screenshots

Exact Terraform instance identity with a safe generic display name:

<!-- release-screenshot: selector="summary:has-text('defaults')"; focus="Shows the exact string instance key and selected generic display name" -->
![Generic instance and display identity](https://raw.githubusercontent.com/oocx/tfplan2md/v1.47.0/docs/features/146-generic-resource-review-clarity/generic-identity-crop.png)

Empty imported prior state is explained before the desired values, with repetitive
provider metadata available in the collapsed Provider settings area:

<!-- release-screenshot: target-resource-id="msgraph_update_resource.import_empty"; focus="Shows the empty-import note and collapsed Provider settings inside the expanded resource card" -->
![Import context and collapsed provider settings](https://raw.githubusercontent.com/oocx/tfplan2md/v1.47.0/docs/features/146-generic-resource-review-clarity/import-provider-settings-crop.png)

## 🔗 Commits

- [Generic identity and instance labels](https://github.com/oocx/tfplan2md/commit/633e05ef)
- [Review and refactoring labels](https://github.com/oocx/tfplan2md/commit/46f51860)
- [Computed reference hints](https://github.com/oocx/tfplan2md/commit/40023506)
- [Empty import context](https://github.com/oocx/tfplan2md/commit/4038b69e)
- [Provider settings presentation](https://github.com/oocx/tfplan2md/commit/4b70a22d)
- [Optional unchanged output filtering](https://github.com/oocx/tfplan2md/commit/b68cfdb1)
- [Consistent imported housekeeping state](https://github.com/oocx/tfplan2md/commit/ba84aa61)
