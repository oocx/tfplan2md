# Security: Fix Sensitive Value Exposure in Generated Reports

This release fixes a security issue in which `tfplan2md` could include plaintext Terraform sensitive values in generated Markdown reports. The bug affected several distinct rendering paths: AzApi resource body properties, AzureDevOps variable groups, Scriban template context variables, and hierarchical sensitivity detection. After this fix, any value marked sensitive in your Terraform plan is rendered as `` `(sensitive)` `` in the output — regardless of which provider or rendering path is used.

## 🐛 Bug fixes

- **AzApi create/delete/replace body**: Properties marked sensitive in `after_sensitive.body` or `before_sensitive.body` (e.g. `administratorLoginPassword`, `clientSecret`) are now masked in the Body table. Previously, values from `after.body` (or `before.body`) were rendered verbatim.

- **AzApi update body**: The `is_sensitive` flag computed from the sensitivity structure was correctly threaded into the update renderers but never applied. Sensitive properties in Body Changes tables now display `` `(sensitive)` `` in both Before and After columns.

- **Scriban template context (defense in depth)**: The `before_json` and `after_json` ScriptObjects exposed to custom Scriban templates are now masked by default when `--show-sensitive` is not set. Sensitive leaf paths are replaced with `"(sensitive)"`. The raw structures remain accessible via `before_sensitive` and `after_sensitive` for templates that need to make layout decisions.

- **AzureDevOps Variable Group secret transitions**: A variable that transitions from `is_secret: true` to `is_secret: false` now shows `` `(sensitive / hidden)` `` in the Before column (matching the long-standing behavior for the reverse direction). Previously, the old plaintext value was revealed.

- **Root boolean sensitivity** (`after_sensitive: true` at the resource level): All attributes are now correctly hidden when the entire resource is marked sensitive via an empty-string key in the sensitivity map.

- **Top-level array parent sensitivity** (`secrets: true` in `after_sensitive`): Array elements such as `secrets[0].value` are now covered by the parent `secrets` sensitivity marker. Previously, only dotted paths were traced upward; bare array keys like `secrets[0]` were not linked to their parent.

- **Duplicate hierarchical path detection**: `GetHierarchicalPaths` no longer yields duplicate paths for multi-level indexed keys (e.g., `a[0].b[1]`), and now correctly strips only the innermost index when resolving the parent.

The `--show-sensitive` flag continues to work across all paths: passing it reveals the actual values in all output sections.

## 📸 Screenshots

### AzApi create: `administratorLoginPassword` masked

![AzApi create body with sensitive masking](https://raw.githubusercontent.com/oocx/tfplan2md/v1.27.0/docs/issues/098-sensitive-info-exposure/098-azapi-create.png)

### Variable Group: secret-to-public transition masked

![Variable Group sensitive/hidden masking](https://raw.githubusercontent.com/oocx/tfplan2md/v1.27.0/docs/issues/098-sensitive-info-exposure/098-vargroup.png)

## 🔗 Commits

- [`1d44017e`](https://github.com/oocx/tfplan2md/commit/1d44017e) fix: handle root boolean and top-level array parent sensitivity
- [`b5820a62`](https://github.com/oocx/tfplan2md/commit/b5820a62) fix: mask Variable Group values when either side is secret
- [`712c3f6f`](https://github.com/oocx/tfplan2md/commit/712c3f6f) fix: mask sensitive values in AzApi create/delete/replace body rendering
- [`ca0e215f`](https://github.com/oocx/tfplan2md/commit/ca0e215f) fix: mask sensitive values in AzApi update body rendering
- [`b13f7683`](https://github.com/oocx/tfplan2md/commit/b13f7683) fix: mask before_json/after_json at mapper level + fix AzApi comparison
- [`81e98ed7`](https://github.com/oocx/tfplan2md/commit/81e98ed7) fix: deduplicate GetHierarchicalPaths for multi-level indexed keys
- [`246fb13c`](https://github.com/oocx/tfplan2md/commit/246fb13c) fix: handle ScriptArray per-element sensitivity in MaskSensitiveLeaves
