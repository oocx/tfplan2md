# Test Plan: Unchanged Values CLI Option

## Overview

This test plan covers the verification of the new CLI option `--show-unchanged-values`. This feature allows users to control whether unchanged values (where `before == after`) are displayed in attribute change tables. The default behavior is to hide unchanged values to reduce noise.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Default behavior hides unchanged values | TC-01 | Unit |
| Flag `--show-unchanged-values` enables display of all values | TC-02 | Unit |
| Filtering works for different data types | TC-01, TC-02 | Unit |
| CLI parser correctly handles the flag | TC-03 | Unit |

## Test Cases

### TC-01: ReportModelBuilder_Build_Default_HidesUnchangedValues

**Type:** Unit

**Description:**
Verify that when `showUnchangedValues` is false (default), attributes with identical before and after values are excluded from the `AttributeChanges` list.

**Preconditions:**
- Use `TestData/azurerm-azuredevops-plan.json` which contains an update for `azurerm_key_vault.main`.
- This resource has:
    - Unchanged attributes: `name`, `location`
    - Changed attributes: `sku_name`, `soft_delete_retention_days`

**Test Steps:**
1. Parse the test plan JSON.
2. Create `ReportModelBuilder` with `showUnchangedValues: false`.
3. Build the report model.
4. Retrieve the `azurerm_key_vault.main` resource change.

**Expected Result:**
- The `AttributeChanges` list for the key vault should contain exactly 2 items.
- `sku_name` and `soft_delete_retention_days` should be present.
- `name` and `location` should NOT be present.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-02: ReportModelBuilder_Build_WithShowUnchangedValues_ShowsAllValues

**Type:** Unit

**Description:**
Verify that when `showUnchangedValues` is true, all attributes are included in the `AttributeChanges` list, even if they haven't changed.

**Preconditions:**
- Use `TestData/azurerm-azuredevops-plan.json`.

**Test Steps:**
1. Parse the test plan JSON.
2. Create `ReportModelBuilder` with `showUnchangedValues: true`.
3. Build the report model.
4. Retrieve the `azurerm_key_vault.main` resource change.

**Expected Result:**
- The `AttributeChanges` list for the key vault should contain 4 items.
- `sku_name`, `soft_delete_retention_days`, `name`, and `location` should all be present.

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`

---

### TC-03: CliParser_Parse_ShowUnchangedValuesFlag

**Type:** Unit

**Description:**
Verify that the CLI parser correctly sets the `ShowUnchangedValues` property when the flag is provided.

**Test Steps:**
1. Call `CliParser.Parse` with `["--show-unchanged-values"]`.
2. Call `CliParser.Parse` with empty args `[]`.

**Expected Result:**
- Case 1: `options.ShowUnchangedValues` should be `true`.
- Case 2: `options.ShowUnchangedValues` should be `false`.

## Test Data Requirements

No new test data files are required. We can reuse `TestData/azurerm-azuredevops-plan.json` as it contains a resource update with both changed and unchanged attributes.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Resource with NO changes (no-op) | Should be filtered out at resource level (existing behavior) | N/A (Existing tests cover this) |
| Resource with all attributes unchanged but action is update | `AttributeChanges` list should be empty if flag is false | Covered by logic in TC-01 |
| Null values | `null` == `null` should be treated as unchanged | Covered by logic in TC-01 |

## Open Questions

None.
