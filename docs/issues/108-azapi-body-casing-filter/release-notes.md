# Bug fix: suppress azapi body changes that differ only in Azure resource ID casing

This release fixes a misleading diff for `azapi_update_resource` and `azapi_resource` plans:
body properties whose before/after values are identical Azure resource IDs except for letter
casing are no longer reported as real changes.

## 🐛 Bug fixes

- **azapi body casing-only changes suppressed** (`--ignore-azure-id-case-changes`): when an
  `azapi_update_resource` or `azapi_resource` has a body property that changes only in the
  capitalisation of an Azure resource ID segment (e.g. `APP-RG-GWC` vs `app-rg-gwc`),
  tfplan2md was incorrectly displaying that property as a real infrastructure change. Azure's
  ARM API is known to return resource IDs with inconsistent casing on successive reads, so
  these differences carry no infrastructure significance and should be suppressed.

  The fix threads the `ignoreAzureIdCaseChanges` flag through `AzApiBodyRenderer.AreEqual()`
  and introduces a new `AzApiResourceIdCaseChangeFilter` that removes casing-only body-change
  rows before they reach the Markdown output. The equivalent suppression for `azurerm`
  resources has been in place since v1.17.0; this release extends the same behaviour to
  `azapi` providers. The `--ignore-azure-id-case-changes` flag (enabled by default) now
  consistently covers both provider families.

## 🔗 Commits

- [`bc468c2`](https://github.com/oocx/tfplan2md/commit/bc468c2) fix: reimplement azapi casing filter on post-Scriban C# rendering pipeline
- [`cc9ed44`](https://github.com/oocx/tfplan2md/commit/cc9ed44) fix: filter out casing-only Azure ID changes in azapi body comparison
