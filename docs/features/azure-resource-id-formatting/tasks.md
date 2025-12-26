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
- [ ] `public static bool IsAzureResourceId(string? scope)` method added to `AzureScopeParser`.
- [ ] Method returns `true` if `Parse(scope).Level != ScopeLevel.Unknown`.
- [ ] Unit tests (TC-01, TC-02) verify detection for Subscriptions, Resource Groups, Resources, and Management Groups.
- [ ] Unit tests verify that invalid formats return `false`.

**Dependencies:** None

---

### Task 2: Update `ScribanHelpers` for Azure ID Handling

**Priority:** High

**Description:**
Update `ScribanHelpers` to exempt Azure IDs from large value classification and provide a universal formatting helper.

**Acceptance Criteria:**
- [ ] `IsLargeValue` signature updated to `IsLargeValue(string? input, string? providerName = null)`.
- [ ] `IsLargeValue` logic updated: returns `false` if `providerName` is `azurerm` and `AzureScopeParser.IsAzureResourceId(input)` is true (unless it contains newlines).
- [ ] `FormatValue(string? value, string? providerName)` helper added.
- [ ] `FormatValue` uses `AzureScopeParser.ParseScope(value)` for Azure IDs in `azurerm` resources.
- [ ] `FormatValue` returns backticked and escaped value for other strings.
- [ ] New helpers registered in `RegisterHelpers`.
- [ ] Unit tests (TC-03, TC-04, TC-06) verify the new logic.

**Dependencies:** Task 1

---

### Task 3: Update Default Template to Use Universal Formatting

**Priority:** High

**Description:**
Modify `default.sbn` to use the new `format_value` helper and pass provider information to `is_large_value`.

**Acceptance Criteria:**
- [ ] `is_large_value` calls in `default.sbn` updated to pass `change.provider_name`.
- [ ] Attribute value rendering in tables (Create, Delete, Update/Replace) updated to use `format_value(attr.value, change.provider_name)`.
- [ ] Manual backtick wrapping removed where `format_value` is used.
- [ ] Template continues to render correctly for non-Azure resources.

**Dependencies:** Task 2

---

### Task 4: Implement Integration Tests and Test Data

**Priority:** Medium

**Description:**
Create test data and integration tests to verify the end-to-end behavior.

**Acceptance Criteria:**
- [ ] `azure-resource-ids.json` test data created with various `azurerm` resources and long IDs.
- [ ] Integration tests (TC-05, TC-08) verify that long Azure IDs stay in the table and are formatted.
- [ ] Regression tests (TC-07) verify that role assignment formatting is unchanged.
- [ ] Snapshot tests updated if necessary.

**Dependencies:** Task 3

---

### Task 5: Update Documentation

**Priority:** Low

**Description:**
Update the main features documentation to reflect the universal Azure ID formatting.

**Acceptance Criteria:**
- [ ] `docs/features.md` updated with a section on Universal Azure Resource ID Formatting.
- [ ] Documentation includes examples of before/after formatting.

**Dependencies:** Task 4

## Implementation Order

1. **Task 1** - Foundational logic for detection.
2. **Task 2** - Helper logic for template integration.
3. **Task 3** - UI/Template changes.
4. **Task 4** - Verification and regression testing.
5. **Task 5** - Final documentation.

## Open Questions

None.
