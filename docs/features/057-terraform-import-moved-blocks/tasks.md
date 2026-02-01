# Tasks: Terraform Import and Moved Blocks

## Overview

Implement visibility for Terraform `import` and `moved` blocks in generated markdown reports. This includes adding inline annotations to resource summary lines and a consolidated "Refactoring Summary" table at the end of the report.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)

## Tasks

### Task 1: Extend Parsing Models

**Priority:** High

**Description:**
Update the Terraform plan parsing models to capture `previous_address` and `importing.id` from the plan JSON.

**Acceptance Criteria:**
- [ ] `TerraformPlan.cs` includes `PreviousAddress` (string?) and `Importing` (with `Id` string?).
- [ ] Properties are correctly mapped from `previous_address` and `change.importing.id`.
- [ ] Unit tests (TC-01, TC-02) verify parsing with sample JSON.
- [ ] Source generation (`TfPlanJsonContext`) is updated/verified to support new fields.

**Dependencies:** None

---

### Task 2: Update Scriban Loop Limit (ADR-005)

**Priority:** High

**Description:**
Increase the Scriban template loop limit to 10000 to improve reliability for large plans and refactoring sections.

**Acceptance Criteria:**
- [ ] `MarkdownRenderer.cs` sets `TemplateContext.LoopLimit = 10000`.
- [ ] Existing filtering for `no-op` resources is maintained (except for refactoring resources).

**Dependencies:** None

---

### Task 3: Enrich Report Models

**Priority:** High

**Description:**
Update the report models to store refactoring metadata and implement logic to populate it.

**Acceptance Criteria:**
- [ ] `ResourceChangeModel` has `ImportId`, `MovedFromAddress`, and `IsRefactoringAlreadyApplied`.
- [ ] `ReportModel` has a `RefactoringOperations` list.
- [ ] `ReportModelBuilder` correctly identifies refactoring resources.
- [ ] `no-op` resources are selectively RETAINED if they contain refactoring metadata.
- [ ] `RefactoringOperations` list is sorted correctly: Imports first, then Moves. Within groups: AlreadyApplied first, then alphabetical by address.
- [ ] Unit tests (TC-03, TC-06) verify model building.

**Dependencies:** Task 1

---

### Task 4: Update Summary HTML Builder

**Priority:** Medium

**Description:**
Update `ResourceSummaryHtmlBuilder` to prepend refactoring icons and labels to the resource summary lines.

**Acceptance Criteria:**
- [ ] Summary HTML includes 📥 *Imported* for imports.
- [ ] Summary HTML includes 🔀 *Moved from* `address` for moves.
- [ ] Unnecessary refactorings include (⚠️ *already applied*) warning.
- [ ] Non-breaking spaces used for icon+label (TC-08).
- [ ] Formatting uses `<code>` and `<i>` per style guide.
- [ ] Unit tests (TC-04, TC-05) verify output.

**Dependencies:** Task 3

---

### Task 5: Implement Templates & Snapshot Tests

**Priority:** Medium

**Description:**
Update the Scriban templates to render the "Refactoring Summary" table and verify rendering with snapshot tests.

**Acceptance Criteria:**
- [ ] `default.sbn` renders the Refactoring Summary table at the end (if operations exist).
- [ ] Table follows formatting from spec (icons, labels, code formatting).
- [ ] `summary.sbn` is reviewed/updated if necessary.
- [ ] Snapshot tests (TC-07, TC-09) verify rendering and ensure no regressions for standard plans.

**Dependencies:** Task 3, Task 4

---

### Task 6: Final Verification & UAT

**Priority:** Medium

**Description:**
Generate comprehensive test data and run UAT to verify visual rendering on GitHub and Azure DevOps.

**Acceptance Criteria:**
- [ ] `refactoring-comprehensive.json` test data created.
- [ ] UAT scenarios 1 & 2 pass.
- [ ] Documentation (if any) is updated to reflect the new report section.

**Dependencies:** Task 5

## Implementation Order

1. **Task 1 (Parsing)** - Foundational for data access.
2. **Task 2 (Loop Limit)** - Low risk, improves reliability for later tasks.
3. **Task 3 (Report Model)** - Core logic for data transformation.
4. **Task 4 (Summary HTML)** - Component-level rendering logic.
5. **Task 5 (Templates & Snapshots)** - Final assembly and automated verification.
6. **Task 6 (UAT)** - Final quality gate.

## Open Questions

None.
