# Tasks: Summary Resource Type Breakdown

## Overview

This feature enhances the summary table in the generated markdown report by adding a "Resource Types" column. This column provides a breakdown of resource types for each action (Add, Change, Replace, Destroy), sorted alphabetically and formatted as a multi-line list.

Reference:
- [Feature Specification](specification.md)
- [Architecture](architecture.md)

## Tasks

### Task 1: Update Data Model

**Priority:** High

**Description:**
Refactor `ReportModel.cs` to introduce the new data structures required for the resource type breakdown.

**Acceptance Criteria:**
- [ ] `ResourceTypeBreakdown` record created with `string Type` and `int Count`.
- [ ] `ActionSummary` record created with `int Count` and `IReadOnlyList<ResourceTypeBreakdown> Breakdown`.
- [ ] `SummaryModel` updated to use `ActionSummary` for `ToAdd`, `ToChange`, `ToDestroy`, `ToReplace`, and `NoOp`.
- [ ] `SummaryModel` properties are marked as `required` where appropriate.
- [ ] Project compiles successfully.

**Dependencies:** None

---

### Task 2: Update ReportModelBuilder Logic

**Priority:** High

**Description:**
Update the `ReportModelBuilder` to calculate the resource type breakdown for each action.

**Acceptance Criteria:**
- [ ] Implement a helper method (e.g., `BuildActionSummary`) to group changes by type and count them.
- [ ] Resource types within each breakdown are sorted alphabetically by their type name.
- [ ] `SummaryModel` is populated using the new `ActionSummary` structure.
- [ ] The logic correctly handles cases with zero changes for an action (empty breakdown list).

**Dependencies:** Task 1

---

### Task 3: Update Default Template

**Priority:** High

**Description:**
Update the default Scriban template to include the new column and reflect the data model changes.

**Acceptance Criteria:**
- [ ] `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn` updated.
- [ ] Summary table header includes a third column: `| Resource Types |`.
- [ ] Action rows use `summary.<action>.count` for the Count column.
- [ ] Action rows use `summary.<action>.breakdown` to render the breakdown in the Resource Types column.
- [ ] Each item in the breakdown is formatted as `<count> <type><br/>`.
- [ ] The Total row remains unchanged (no breakdown column content).

**Dependencies:** Task 1, Task 2

---

### Task 4: Update and Add Tests

**Priority:** High

**Description:**
Update existing tests to accommodate the breaking change in the data model and add new tests to verify the breakdown logic and rendering.

**Acceptance Criteria:**
- [ ] `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ReportModelBuilderTests.cs` updated to verify `ActionSummary` and `Breakdown` content.
- [ ] `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererTests.cs` updated to verify the new column is rendered correctly in the markdown output.
- [ ] New test cases added for multiple resource types of the same action to verify sorting and multi-line formatting.
- [ ] All tests pass.

**Dependencies:** Task 1, Task 2, Task 3

## Implementation Order

Recommended sequence for implementation:
1. **Task 1**: Foundational work to define the new data structure.
2. **Task 2**: Core logic to populate the new data structure.
3. **Task 3**: Update the visual representation in the report.
4. **Task 4**: Ensure everything works as expected and prevent regressions.

## Open Questions

None.
