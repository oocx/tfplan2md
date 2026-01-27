# Tasks: Display Enhancements

## Overview

This feature introduces focused display improvements to enhance readability and context:
1. Syntax highlighting for large JSON/XML values (with pretty-printing)
2. Enriched API Management subresource summaries
3. Named values sensitivity override when `secret=false`
4. Subscription attributes emoji (🔑)

**Note:** The architecture was updated during implementation to enforce strict separation between generic `MarkdownGeneration` and provider-specific logic. Tasks 2 and 3 of the original plan were implemented incorrectly in generic code and must be reverted and re-implemented using `IResourceViewModelFactory` in `Providers/AzureRM/`.

Reference documents:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)
- [UAT Test Plan](uat-test-plan.md)

## Tasks

### Task 1: Subscription Attribute Emoji (🔑)

**Priority:** High

**Description:**
Add the 🔑 emoji prefix to `subscription_id` and `subscription` attribute values globally.

**Acceptance Criteria:**
- [x] `ScribanHelpers.FormatAttributeValueTable`, `FormatAttributeValueSummary`, and `FormatAttributeValuePlain` are updated to detect `subscription_id` and `subscription` (case-insensitive).
- [x] Detected attributes are prefixed with `🔑` and a non-breaking space.
- [x] Unit tests (TC-09) verify the emoji prefix in all three formatting contexts.

**Dependencies:** None

---

### Task 2: Syntax Highlighting for Large Values

**Priority:** Medium (Complex)

**Description:**
Implement JSON/XML detection and pretty-printing for large values, adding language-specific code fences in generic code.

**Acceptance Criteria:**
- [x] `ScribanHelpers.FormatLargeValue` handles JSON/XML detection and formatting.
- [x] JSON is detected via `System.Text.Json` and pretty-printed.
- [x] XML is detected via `System.Xml.Linq` and pretty-printed.
- [x] Large values are wrapped in fenced code blocks with language markers (```json, ```xml).
- [x] Resource updates (simple-diff/inline-diff) pretty-print the content *before* diffing to ensure the diff is readable.
- [x] Already-formatted content is preserved (TC-03).
- [x] Unit tests (TC-01, TC-02, TC-03, TC-04) pass.

**Dependencies:** None

---

### Task 3: Architecture Correction - Revert Provider Logic and Fix Pipeline

**Priority:** High

**Description:**
Remove provider-specific resource type checks and attribute semantics (`azurerm_api_management_*`) from `MarkdownGeneration` and fix the core pipeline to respect provider overrides.

**Acceptance Criteria:**
- [ ] No `azurerm` resource type logic remains in `ResourceSummaryHtmlBuilder.cs`.
- [ ] `BuildAttributeChanges` and `IsNamedValueNonSecret` are removed from `ReportModelBuilder.ResourceChanges.cs`.
- [ ] `ReportModelBuilder.BuildResourceChangeModel` is updated to check if `SummaryHtml` is already set (non-null/non-empty) before calling `BuildSummaryHtml(model)`.
- [ ] Unit test TC-11 verifies that provider-set summaries are preserved.
- [ ] Existing generic tests continue to pass.

**Dependencies:** None

---

### Task 4: AzureRM: API Management Summary Enrichment

**Priority:** High

**Description:**
Implement provider-specific summary enrichment for APIM subresources using `IResourceViewModelFactory` and register them in `AzureRMModule.cs`.

**Acceptance Criteria:**
- [ ] `AzureRMApimApiOperationFactory` created and registered in `AzureRMModule.cs` for `azurerm_api_management_api_operation`.
- [ ] `AzureRMApimSubresourceFactory` created and registered for common APIM subresources (e.g., policy, product, etc.).
- [ ] Summaries include `display_name`, `operation_id`, `api_name`, and `api_management_name` as per spec.
- [ ] Unit tests (TC-05, TC-06) verify the enriched summary at the provider layer.

**Dependencies:** Task 3

---

### Task 5: AzureRM: Named Values Sensitivity Override

**Priority:** High

**Description:**
Implement provider-specific sensitivity override for `azurerm_api_management_named_value.value` when `secret` is `false`.

**Acceptance Criteria:**
- [ ] `AzureRMApimNamedValueFactory` created and registered.
- [ ] Factory overrides `model.AttributeChanges` for the `value` key when the resource's `secret` attribute is `false`.
- [ ] Unit tests (TC-07, TC-08) verify behavior at the provider layer.

**Dependencies:** Task 3

---

### Task 6: Integration & UAT

**Priority:** High

**Description:**
Create integrated test data and run the UAT workflow to verify rendering on GitHub and Azure DevOps.

**Acceptance Criteria:**
- [ ] `examples/apim-display-enhancements.json` is completed with test data for all improvements.
- [ ] `artifacts/apim-display-enhancements-demo.md` is generated and committed.
- [ ] UAT simulation is run and passes (TC-10).
- [ ] Documentation is updated if necessary.

**Dependencies:** Task 1, Task 2, Task 4, Task 5

## Implementation Order

1. **Task 3: Architecture Correction** - Revert incorrect implementation and enable factory overrides.
2. **Task 4: AzureRM Summary Enrichment** - Re-implement APIM summaries in the correct layer.
3. **Task 5: AzureRM Sensitivity Override** - Re-implement named value fix in the correct layer.
4. **Task 6: Integration & UAT** - Final verification.

Note: Tasks 1 and 2 are already completed correctly in generic code.

## Open Questions

None.
