# Critical: Fixed JSON parsing crash with Azure resources (v1.16.2)

This patch release fixes a critical crash introduced in v1.16.0/v1.16.1 that affected users processing Terraform plans with certain Azure resources.

## 🐛 Bug fixes

- **Fixed crash with Object-typed references in Azure resources (Issue #071)** - v1.16.0 and v1.16.1 would crash with `JsonElementHasWrongType, Object, Array` when processing Terraform plans containing Azure Storage containers, role assignments, or other resources where the configuration's `references` field was structured as an Object instead of an Array. The tool now handles both Array and Object structures gracefully.

- **Fixed crash with Array-typed expression properties (Issue #464)** - This is a **second instance** of the `JsonElementHasWrongType` error. While Issue #071 fixed the case where the `references` property had the wrong type (Object instead of Array), this fix addresses a different scenario: when **expression property values themselves** are Arrays representing nested Terraform blocks. For example, `azurerm_data_factory_trigger_schedule` resources can have nested `pipeline` blocks that appear as Array-valued expression properties like `authentication_credentials: [{ credential_id: "test" }]`. The code must check `ValueKind` BEFORE calling `TryGetProperty` to prevent crashes when processing such configurations.

**Impact:** Users of v1.16.0 or v1.16.1 who process plans with:
- Azure Storage containers, role assignments (Issue #071)
- Azure Data Factory resources with nested blocks (Issue #464)
- Any other resources with non-standard JSON structures in their configuration

should upgrade immediately.

**Technical details:** The new configuration reference resolver (introduced in v1.16.0) attempted to enumerate JSON array elements without properly validating the `ValueKind` in all code paths. This release adds explicit defensive checks before calling `.EnumerateArray()` in both `ConfigurationReferenceResolver` and `ReportModelBuilder.ParentChildMerging`. Additionally, it adds checks before calling `.TryGetProperty()` on expression property values to handle Array-typed nested blocks.

## 🔗 Commits

### Issue #071 (Object-typed references)
- [`fd04c22`](https://github.com/oocx/tfplan2md/commit/fd04c22) test: add edge case tests for JSON parsing with non-array types
- [`c08eac4`](https://github.com/oocx/tfplan2md/commit/c08eac4) fix: enhance defensive checks for JSON array enumeration

### Issue #464 (Array-typed expression properties)
- [`b8b17fd`](https://github.com/oocx/tfplan2md/commit/b8b17fd) fix: handle array-typed expression properties in ConfigurationReferenceResolver
- [`aacbe2b`](https://github.com/oocx/tfplan2md/commit/aacbe2b) docs: add code review for JsonElementHasWrongType bug fix
- [`122affc`](https://github.com/oocx/tfplan2md/commit/122affc) chore: push Code Reviewer's commit (review approved)
- [`002352d`](https://github.com/oocx/tfplan2md/commit/002352d) chore: push Release Manager's commits (release notes ready)

## 📋 Analysis & Review

- **Root cause analysis:** [docs/issues/071-json-parsing-error-azurerm-resources/analysis.md](https://github.com/oocx/tfplan2md/blob/main/docs/issues/071-json-parsing-error-azurerm-resources/analysis.md)
- **Code review:** [docs/issues/071-json-parsing-error-azurerm-resources/code-review.md](https://github.com/oocx/tfplan2md/blob/main/docs/issues/071-json-parsing-error-azurerm-resources/code-review.md)
- **Test coverage:** 
  - 8 edge case tests covering Object, null, and String types for both affected components (Issue #071)
  - 1 synthetic test for Array-typed expression properties (Issue #464)
  - 1 integration test using the exact real-world plan.json from Issue #464
- **All tests:** Passing (full test suite verified)
