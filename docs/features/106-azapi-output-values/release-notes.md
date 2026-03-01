# azapi Output Values (Feature 106)

`azapi_resource` and `azapi_update_resource` now render a dedicated **Output Values** section
in Terraform plan reports, surfacing the Azure REST API response (`output` attribute) alongside
the body (input) attributes you configure. This makes it easier to review what values Azure
will return — resource IDs, connection strings, computed SKUs — without having to cross-reference
the Terraform state.

## ✨ Features

- **Dedicated Output Values section**: Both `azapi_resource` and `azapi_update_resource`
  now render an `#### Output Values` section after the `#### Body` / `#### Body Changes` section,
  containing all values returned by the Azure REST API (`output` attribute). All change actions
  are covered: create, update, delete, and replace.

- **Section suppression on create**: When all output values will be unknown after apply (the
  common case for a fresh `azapi_resource` create), the Output Values section is suppressed
  entirely — no empty placeholder or notice is shown, keeping the report clean.

- **Feature 034 grouping**: Complex `output` sub-objects (e.g., a `sku` object with multiple
  fields) are rendered as grouped `###### \`sku\`` sub-sections with their own Property table,
  consistent with how body attributes with sub-objects are rendered.

- **Sensitivity masking**: Sensitive output fields are shown as `(sensitive)` — no actual values
  are exposed in the report.

- **Display name mapping**: Azure resource IDs in output values are resolved to human-readable
  names via the `--principals` mapping file (e.g., a workspace ID becomes
  `Log Analytics Workspace 🆔 log-workspace-new in resource group 📁 rg-logs`).

- **Large-value folding**: Output values exceeding the 200-character threshold are collapsed
  into `<details>` blocks, consistent with body attribute handling.

## 📸 Screenshots

### Update operation — Output Values section with grouped `sku` sub-table and display name mapping

![Update resource with Output Values section](https://raw.githubusercontent.com/oocx/tfplan2md/v1.32.0/docs/features/106-azapi-output-values/azapi-output-update.png)

### Delete operation — Output Values with sensitive field masking

![Delete resource with Output Values (sensitive)](https://raw.githubusercontent.com/oocx/tfplan2md/v1.32.0/docs/features/106-azapi-output-values/azapi-output-delete.png)

## 🔗 Commits

- [`cd6faf5`](https://github.com/oocx/tfplan2md/commit/cd6faf5aab39e1079fa07fe63e4627f1ec75adac) feat: expose after_unknown to Scriban templates for output-unknown detection
- [`1af40bb`](https://github.com/oocx/tfplan2md/commit/1af40bbef06790b33f8cf8eadd04242d0525233f) fix: add Output Values heading for known-after-apply and replace-before cases
- [`8102733`](https://github.com/oocx/tfplan2md/commit/81027331cd6b84f0f0550ef844f38c11ecf06583) fix: remove output values section when all outputs unknown; add display name mapping to UAT
