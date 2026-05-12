# Test Plan: Terraform Import and Moved Blocks

## Overview

This test plan covers the "Terraform Import and Moved Blocks" feature, which adds visibility for `import` and `moved` blocks in generated reports. It includes unit tests for parsing, model building, and rendering, as well as UAT scenarios for visual verification in GitHub and Azure DevOps.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Refactoring Summary table appears when import or moved operations exist | TC-01, TC-02 | Unit / Snapshot |
| Refactoring Summary does not appear when no import/moved operations exist | TC-03 | Unit / Snapshot |
| Each imported resource shows "📥 Imported" annotation in its summary line | TC-04 | Unit / Snapshot |
| Each moved resource shows "🔀 Moved from `<previous_address>`" in its summary line | TC-05 | Unit / Snapshot |
| Import operations show the import ID in the Refactoring Summary | TC-01 | Unit / Snapshot |
| Move operations show the previous address in the Refactoring Summary | TC-02 | Unit / Snapshot |
| Unnecessary import/moved blocks (no-op resources) show warning status | TC-06 | Unit / Snapshot |
| Warnings clearly indicate blocks can be removed from configuration | TC-06 | Unit / Snapshot |
| All code values follow report style guide formatting | TC-07 | Snapshot |
| Icons use non-breaking spaces to prevent line wrapping | TC-08 | Unit |
| Resource summary lines without annotations render exactly as before | TC-09 | Snapshot |

## User Acceptance Scenarios

> **Purpose**: Verify rendering in real pull request environments (GitHub and Azure DevOps) to ensure icons, layout, and formatting meet user expectations.

### Scenario 1: Refactoring with Active Changes

**User Goal**: Review a plan that contains resources being imported and moved as part of an infrastructure update.

**Test PR Context**:
- **GitHub & Azure DevOps**: Verify the Refactoring Summary table at the end of the report and annotations in the individual resource summary lines.

**Expected Output**:
- "Refactoring Summary" section exists.
- Table shows 📥 Import and 🔀 Move operations with ✅ Ready status.
- Summary lines for affected resources include 📥 *Imported* or 🔀 *Moved from*.

**Success Criteria**:
- [ ] Icons and labels are properly aligned.
- [ ] `<code>` and `<i>` tags are used correctly in summary lines.
- [ ] Non-breaking spaces prevent icons from separating from labels.

---

### Scenario 2: Refactoring Hygiene (Unnecessary Blocks)

**User Goal**: Identify refactoring blocks that have already been applied and can be safely removed from the configuration.

**Test PR Context**:
- **GitHub & Azure DevOps**: Verify that already-applied moves show warning status (⚠️ Already moved) and that pending imports remain `✅ Ready` without an inline warning.

**Expected Output**:
- Refactoring Summary table shows ⚠️ status for already-applied moved resources.
- Resource summary line keeps `📥 *Imported*` without `(⚠️ *already imported*)` for pending no-op imports.

**Success Criteria**:
- [ ] Warnings are clearly visible.
- [ ] No-op resources with refactoring metadata are NOT filtered out of the report.

## Test Cases

### TC-01: Parse_ImportBlock_ReturnsImportId

**Type:** Unit

**Description:**
Verify that the Terraform plan parser correctly captures the `importing.id` field from the plan JSON.

**Preconditions:**
- Plan JSON with an `import` block.

**Test Steps:**
1. Parse the plan JSON.
2. Inspect the `ResourceChange.Change.Importing.Id` property.

**Expected Result:**
The property contains the correct import ID string.

**Test Data:**
`import-resource.json`

---

### TC-02: Parse_MovedBlock_ReturnsPreviousAddress

**Type:** Unit

**Description:**
Verify that the Terraform plan parser correctly captures the `previous_address` field from the plan JSON.

**Preconditions:**
- Plan JSON with a `moved` block.

**Test Steps:**
1. Parse the plan JSON.
2. Inspect the `ResourceChange.PreviousAddress` property.

**Expected Result:**
The property contains the correct previous address string.

**Test Data:**
`moved-resource.json`

---

### TC-03: ReportModel_NoRefactoring_RefactoringOperationsIsEmpty

**Type:** Unit

**Description:**
Verify that the `ReportModel` does not contain any refactoring operations when none are present in the plan.

**Preconditions:**
- Plan JSON without `import` or `moved` blocks (e.g., `minimal-plan.json`).

**Test Steps:**
1. Build the `ReportModel`.
2. Check `ReportModel.RefactoringOperations`.

**Expected Result:**
`RefactoringOperations` is an empty list.

**Test Data:**
`minimal-plan.json`

---

### TC-04: BuildSummaryHtml_ImportedResource_IncludesAnnotation

**Type:** Unit

**Description:**
Verify that `ResourceSummaryHtmlBuilder` prepends the "📥 Imported" annotation to the summary HTML.

**Preconditions:**
- `ResourceChangeModel` with `ImportId = "some-id"`.

**Test Steps:**
1. Call `ResourceSummaryHtmlBuilder.BuildSummaryHtml`.

**Expected Result:**
The output HTML contains `📥&nbsp;<i>Imported</i>` and the import ID in `🆔&nbsp;<code>some-id</code>`.

---

### TC-05: BuildSummaryHtml_MovedResource_IncludesAnnotation

**Type:** Unit

**Description:**
Verify that `ResourceSummaryHtmlBuilder` prepends the "🔀 Moved from" annotation to the summary HTML.

**Preconditions:**
- `ResourceChangeModel` with `MovedFromAddress = "old.address"`.

**Test Steps:**
1. Call `ResourceSummaryHtmlBuilder.BuildSummaryHtml`.

**Expected Result:**
The output HTML contains `🔀&nbsp;<i>Moved from</i> <code>old.address</code>`.

---

### TC-06: ReportModel_NoOpRefactoring_IncludesInReportWithWarning

**Type:** Unit / Integration

**Description:**
Verify that no-op resources with refactoring metadata are retained in the report, while pending imports are not mislabeled as already imported.

**Preconditions:**
- Plan JSON where a resource has `importing` metadata and `actions = ["no-op"]`.

**Test Steps:**
1. Build the `ReportModel`.
2. Verify the resource is present in `ReportModel.Changes`.
3. Verify `ResourceChangeModel.IsImportAlreadyApplied` is false.

**Expected Result:**
The resource is not filtered out and remains classified as a ready import.

**Test Data:**
`no-op-import.json`

---

### TC-07: Snapshot_DefaultReport_RefactoringSummaryTable

**Type:** Snapshot

**Description:**
Verify the rendering of the "Refactoring Summary" table in the default markdown report.

**Preconditions:**
- Plan JSON with both imports and moves.

**Test Steps:**
1. Generate the markdown report using the default template.
2. Compare with snapshot.

**Expected Result:**
Markdown matches snapshot, including table headers, icons, and non-breaking spaces.

---

### TC-08: Check_NonBreakingSpaces_InIcons

**Type:** Unit

**Description:**
Verify that all refactoring icons are followed by a non-breaking space (`\u00A0` or `&nbsp;`).

**Test Steps:**
1. Inspect the strings used in `ResourceSummaryHtmlBuilder` and the templates.
2. Search for icon characters (`📥`, `🔀`).

**Expected Result:**
Each icon is immediately followed by a non-breaking space.

---

### TC-09: Snapshot_Comparison_NoRegression

**Type:** Snapshot

**Description:**
Verify that a standard plan (no refactoring) produces the exact same output as before the feature.

**Preconditions:**
- Standard plan JSON (e.g., `azurerm-azuredevops-plan.json`).

**Test Steps:**
1. Generate the markdown report.
2. Compare with a known good snapshot from before the feature.

**Expected Result:**
Zero diff.

## Test Data Requirements

List any new test data files needed:
- `import-resource.json` - Plan with a simple resource import.
- `moved-resource.json` - Plan with a resource move.
- `no-op-import.json` - Plan with a pending import rendered as `✅ Ready` even when Terraform reports `["no-op"]`.
- `refactoring-comprehensive.json` - Plan with multiple imports and moves, including the pending-import regression coverage updated by issue 123.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Resource is both imported and moved | Should display both annotations / handle precedence gracefully (unlikely in TF, but good to cover) | TC-XX |
| Import ID is very long | Table should handle wrapping (markdown table column widths) | UAT |
| Large number of moves | Sorting should remain consistent (Group by type, then alphabetical) | TC-10 |

## Non-Functional Tests

- **Performance**: Building the refactoring list in C# should not noticeably slow down report generation.
- **Compatibility**: Ensure plans from older Terraform versions (without these fields) still parse correctly (fields will be null).

## Open Questions

None.
