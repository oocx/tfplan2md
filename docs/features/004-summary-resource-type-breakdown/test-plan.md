# Test Plan: Summary Resource Type Breakdown

## Overview

This test plan covers the validation of the "Summary Resource Type Breakdown" feature. The goal is to ensure that the summary table in the generated markdown report correctly displays a breakdown of resource types for each action (Add, Change, Replace, Destroy), sorted alphabetically and formatted correctly.

Reference: [Feature Specification](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Summary table includes "Resource Types" column | TC-04 | Integration |
| Action row displays correct count and type name | TC-01, TC-04 | Unit, Integration |
| Resource types are sorted alphabetically | TC-02 | Unit |
| Multi-line display works correctly (`<br/>`) | TC-04 | Integration |
| Empty cells display correctly for 0 count | TC-03, TC-05 | Unit, Integration |
| Total row's Resource Types column remains empty | TC-04 | Integration |

## Test Cases

### TC-01: Calculate Breakdown Counts

**Type:** Unit (ReportModelBuilderTests)

**Description:**
Verify that the `ReportModelBuilder` correctly groups resources by type and calculates the counts for each action.

**Preconditions:**
- A `TerraformPlan` object with multiple resources of the same type and different types for a specific action (e.g., "create").

**Test Steps:**
1. Create a `TerraformPlan` with:
   - 2 resources of type `azurerm_storage_account` (action: create)
   - 1 resource of type `azurerm_resource_group` (action: create)
2. Call `ReportModelBuilder.Build(plan)`.
3. Inspect `result.Summary.ToAdd`.

**Expected Result:**
- `result.Summary.ToAdd.Count` should be 3.
- `result.Summary.ToAdd.Breakdown` should contain 2 items.
- One item should be `{ Type = "azurerm_storage_account", Count = 2 }`.
- One item should be `{ Type = "azurerm_resource_group", Count = 1 }`.

---

### TC-02: Sort Breakdown Alphabetically

**Type:** Unit (ReportModelBuilderTests)

**Description:**
Verify that the resource types in the breakdown list are sorted alphabetically by type name.

**Preconditions:**
- A `TerraformPlan` object with resource types that are not in alphabetical order when added.

**Test Steps:**
1. Create a `TerraformPlan` with:
   - 1 `type_b` (action: update)
   - 1 `type_a` (action: update)
   - 1 `type_c` (action: update)
2. Call `ReportModelBuilder.Build(plan)`.
3. Inspect `result.Summary.ToChange.Breakdown`.

**Expected Result:**
- The breakdown list should be ordered: `type_a`, `type_b`, `type_c`.

---

### TC-03: Handle Empty Actions

**Type:** Unit (ReportModelBuilderTests)

**Description:**
Verify that actions with no resources result in an empty breakdown list.

**Preconditions:**
- A `TerraformPlan` with no resources marked for destruction.

**Test Steps:**
1. Create a `TerraformPlan` with only "create" actions.
2. Call `ReportModelBuilder.Build(plan)`.
3. Inspect `result.Summary.ToDestroy`.

**Expected Result:**
- `result.Summary.ToDestroy.Count` should be 0.
- `result.Summary.ToDestroy.Breakdown` should be empty.

---

### TC-04: Render Summary Table with Breakdown

**Type:** Integration (MarkdownRendererTests)

**Description:**
Verify that the default template renders the summary table with the new column and correctly formats the breakdown data.

**Preconditions:**
- A `ReportModel` populated with known breakdown data.

**Test Steps:**
1. Create a `ReportModel` with:
   - `Summary.ToAdd`: Count 3, Breakdown: `[{ Type: "type_a", Count: 2 }, { Type: "type_b", Count: 1 }]`
2. Render the report using `MarkdownRenderer`.
3. Inspect the generated markdown string.

**Expected Result:**
- The table header should contain `| Resource Types |`.
- The "Add" row should contain a cell with: `2 type_a<br/>1 type_b`.
- The "Total" row should have an empty cell for the last column.

---

### TC-05: Render Empty Breakdown Cells

**Type:** Integration (MarkdownRendererTests)

**Description:**
Verify that empty breakdown lists render as empty table cells.

**Preconditions:**
- A `ReportModel` with 0 changes for a specific action.

**Test Steps:**
1. Create a `ReportModel` where `Summary.ToDestroy.Count` is 0 and Breakdown is empty.
2. Render the report.
3. Inspect the "Destroy" row in the summary table.

**Expected Result:**
- The "Resource Types" cell for the "Destroy" row should be empty (e.g., `| |` or `|  |`).

## Test Data Requirements

No new external test data files are required. Tests will use inline object construction or existing patterns for creating `TerraformPlan` and `ReportModel` objects in code.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| No changes in plan | All counts 0, all breakdowns empty | TC-03 |
| Single resource type | Breakdown has 1 item, no `<br/>` needed (but template loop handles it) | TC-01 |
| Very long type names | Rendered as is (layout handling is up to the markdown viewer) | N/A (Visual check if needed) |

## Non-Functional Tests

- **Performance:** Ensure that sorting and grouping does not significantly impact rendering time for large plans (implicit check, no specific benchmark required for this feature).

## Open Questions

None.
