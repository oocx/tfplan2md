# Critical: Fixed JSON parsing crash with Azure resources (v1.16.2)

This patch release fixes a critical crash introduced in v1.16.0/v1.16.1 that affected users processing Terraform plans with certain Azure resources.

## 🐛 Bug fixes

- **Fixed crash with Object-typed references in Azure resources** - v1.16.0 and v1.16.1 would crash with `JsonElementHasWrongType, Object, Array` when processing Terraform plans containing Azure Storage containers, role assignments, or other resources where the configuration's `references` field was structured as an Object instead of an Array. The tool now handles both Array and Object structures gracefully.

**Impact:** Users of v1.16.0 or v1.16.1 who process plans with `azurerm_storage_container`, `azurerm_role_assignment`, or similar resources should upgrade immediately.

**Technical details:** The new configuration reference resolver (introduced in v1.16.0) attempted to enumerate JSON array elements without properly validating the `ValueKind` in all code paths. This release adds explicit defensive checks before calling `.EnumerateArray()` in both `ConfigurationReferenceResolver` and `ReportModelBuilder.ParentChildMerging`.

## 🔗 Commits

- [`fd04c22`](https://github.com/oocx/tfplan2md/commit/fd04c22) test: add edge case tests for JSON parsing with non-array types
- [`c08eac4`](https://github.com/oocx/tfplan2md/commit/c08eac4) fix: enhance defensive checks for JSON array enumeration

## 📋 Analysis & Review

- **Root cause analysis:** [docs/issues/071-json-parsing-error-azurerm-resources/analysis.md](https://github.com/oocx/tfplan2md/blob/main/docs/issues/071-json-parsing-error-azurerm-resources/analysis.md)
- **Code review:** [docs/issues/071-json-parsing-error-azurerm-resources/code-review.md](https://github.com/oocx/tfplan2md/blob/main/docs/issues/071-json-parsing-error-azurerm-resources/code-review.md)
- **Test coverage:** 8 new edge case tests covering Object, null, and String types for both affected components
- **All tests:** 977/978 passing (1 unrelated Docker timeout)
