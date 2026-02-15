# Tasks: Universal Azure Resource ID Formatting

## Overview

Implement universal Azure resource ID formatting for all `azurerm` resources. This involves updating the detection logic for large values, adding a new formatting helper, and updating the default template to use these new capabilities.

Reference: [Specification](specification.md), [Architecture](architecture.md), [Test Plan](test-plan.md)

## Tasks

### Task 1: Enhance `AzureScopeParser` with Detection Logic

**Priority:** High

**Description:**
Add a method to `AzureScopeParser` to detect if a string matches an Azure resource ID pattern.

**Acceptance Criteria:**
- [x] `public static bool IsAzureResourceId(string? scope)` method added to `AzureScopeParser`.
- [x] Method returns `true` if `Parse(scope).Level != ScopeLevel.Unknown`.
- [x] Unit tests (TC-01, TC-02) verify detection for Subscriptions, Resource Groups, Resources, and Management Groups.
- [x] Unit tests verify that invalid formats return `false`.

**Dependencies:** None

---

### Task 2: Update `ScribanHelpers` for Azure ID Handling

**Priority:** High

**Description:**
Update `ScribanHelpers` to exempt Azure IDs from large value classification and provide a universal formatting helper.

**Acceptance Criteria:**
- [x] `IsLargeValue` signature updated to `IsLargeValue(string? input, string? providerName = null)`.
- [x] `IsLargeValue` logic updated: returns `false` if `providerName` is `azurerm` and `AzureScopeParser.IsAzureResourceId(input)` is true (unless it contains newlines).
- [x] `FormatValue(string? value, string? providerName)` helper added.
- [x] `FormatValue` uses `AzureScopeParser.ParseScope(value)` for Azure IDs in `azurerm` resources.
- [x] `FormatValue` returns backticked and escaped value for other strings.
- [x] New helpers registered in `RegisterHelpers`.
- [x] Unit tests (TC-03, TC-04, TC-06) verify the new logic.

**Dependencies:** Task 1

---

### Task 3: Update Default Template to Use Universal Formatting

**Priority:** High

**Description:**
Update the C# model to precompute `IsLarge` status (exempting Azure IDs) and modify `default.sbn` to use this property.

**Acceptance Criteria:**
- [x] `ReportModel` updated to compute `IsLarge` for each attribute, passing provider name to `IsLargeValue`.
- [x] `default.sbn` updated to use `attr.is_large` instead of calling the helper function.
- [x] Attribute value rendering in tables updated to use `format_value(attr.value, change.provider_name)`.
- [x] Manual backtick wrapping removed where `format_value` is used.
- [x] Template continues to render correctly for non-Azure resources.

**Dependencies:** Task 2

---

### Task 4: Implement Integration Tests and Test Data

**Priority:** Medium

**Description:**
Create test data and integration tests to verify the end-to-end behavior.

**Acceptance Criteria:**
- [x] `azure-resource-ids.json` test data created with various `azurerm` resources and long IDs.
- [x] Integration tests (TC-05, TC-08) verify that long Azure IDs stay in the table and are formatted.
- [x] Regression tests (TC-07) verify that role assignment formatting is unchanged.
- [x] Snapshot tests updated if necessary.

**Dependencies:** Task 3

---

### Task 5: Update Documentation

**Priority:** Low

**Description:**
Update the main features documentation to reflect the universal Azure ID formatting.

**Acceptance Criteria:**
- [x] `docs/features.md` updated with a section on Universal Azure Resource ID Formatting.
- [x] Documentation includes examples of before/after formatting.

**Dependencies:** Task 4

## Implementation Order

1. **Task 1** - Foundational logic for detection.
2. **Task 2** - Helper logic for template integration.
3. **Task 3** - UI/Template changes.
4. **Task 4** - Verification and regression testing.
5. **Task 5** - Final documentation.

## Open Questions

None.
