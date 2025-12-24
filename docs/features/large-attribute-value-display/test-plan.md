# Test Plan: Large Attribute Value Display

## Overview

This test plan defines the verification strategy for the "Large Attribute Value Display" feature. The feature introduces alternative rendering for large attribute values (multi-line or >100 chars) to prevent table layout breakage and improve readability.

Reference:
- Specification: [specification.md](specification.md)
- Architecture: [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Detection of multi-line values as large | TC-01 | Unit |
| Detection of long single-line values (>100 chars) as large | TC-02 | Unit |
| Small attributes remain in tables | TC-03, TC-10 | Integration |
| Large attributes grouped in `<details>` section | TC-10 | Integration |
| Summary line format and accuracy | TC-09 | Unit |
| `--large-value-format inline-diff` output format | TC-07, TC-08 | Unit |
| `--large-value-format standard-diff` output format | TC-06 | Unit |
| Default format is `inline-diff` | TC-11 | Unit |
| Create/Delete operations show single value | TC-04, TC-05 | Unit |
| Update/Replace operations show comparison | TC-06, TC-07, TC-08 | Unit |
| Sensitive values are masked | TC-12 | Unit |
| Complete replacement logic (no common lines) | TC-08 | Unit |

## User Acceptance Scenarios

> **Purpose**: These scenarios are for manual Maintainer review using the generated markdown to verify rendering quality on different platforms.

### Scenario 1: Azure DevOps Inline Diff

**User Goal**: Verify that large policy changes are readable and correctly highlighted in Azure DevOps.

**Steps**:
1. Setup: Create a Terraform plan with a multi-line `azurerm_api_management_policy` change.
2. Execute: `tfplan2md plan.json --large-value-format inline-diff > report.md`
3. Inspect: Open `report.md` in an Azure DevOps Wiki or Pull Request comment.

**Expected Output**:
- Large policy is in a `<details>` section.
- Summary shows line counts.
- Diff uses `<pre>` with background colors and colored left borders.
- Text is readable in both Light and Dark modes (due to explicit `color: #24292e`).
- Character-level changes are highlighted with darker backgrounds.

**Success Criteria**:
- [ ] Colors match the specification.
- [ ] Layout is clean and doesn't break the page width.
- [ ] Dark mode contrast is sufficient.

---

### Scenario 2: GitHub Standard Diff

**User Goal**: Verify that the report is fully functional and readable on GitHub.

**Steps**:
1. Setup: Use the same plan as Scenario 1.
2. Execute: `tfplan2md plan.json --large-value-format standard-diff > report.md`
3. Inspect: Open `report.md` in a GitHub Gist or Pull Request.

**Expected Output**:
- Large policy is in a `<details>` section.
- Diff uses standard ` ```diff ` code fences.
- Deleted lines start with `-`, added lines with `+`.

**Success Criteria**:
- [ ] GitHub applies red/green syntax highlighting to the diff block.
- [ ] No HTML rendering issues.

---

### Scenario 3: Mixed Attribute Sizes

**User Goal**: Verify that a resource with both small and large attributes displays correctly.

**Steps**:
1. Setup: Create a resource (e.g., `azurerm_linux_virtual_machine`) with small changes (size, name) and a large change (`custom_data`).
2. Execute: `tfplan2md plan.json > report.md`
3. Inspect: View the report.

**Expected Output**:
- `name` and `size` are in the main table.
- `custom_data` is NOT in the table.
- A `<details>` section exists below the table containing the `custom_data` diff.

**Success Criteria**:
- [ ] Table remains compact.
- [ ] Large value is easily accessible but doesn't dominate the view.

## Test Cases

### TC-01: IsLargeValue_WithNewlines_ReturnsTrue

**Type:** Unit

**Description:** Verifies that any string containing a newline character is identified as a large value.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.IsLargeValue` with `"line1\nline2"`.
2. Call `ScribanHelpers.IsLargeValue` with `"line1\r\nline2"`.

**Expected Result:** Both calls return `true`.

---

### TC-02: IsLargeValue_LongSingleLine_ReturnsTrue

**Type:** Unit

**Description:** Verifies that a single-line string longer than 100 characters is identified as a large value.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.IsLargeValue` with a string of 101 'a' characters.

**Expected Result:** Returns `true`.

---

### TC-03: IsLargeValue_ShortSingleLine_ReturnsFalse

**Type:** Unit

**Description:** Verifies that short single-line strings are NOT identified as large values.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.IsLargeValue` with `"short value"`.
2. Call `ScribanHelpers.IsLargeValue` with a string of 100 'a' characters.

**Expected Result:** Both calls return `false`.

---

### TC-04: FormatLargeValue_CreateOperation_ShowsSingleCodeBlock

**Type:** Unit

**Description:** Verifies that for a "create" operation (before is null), only the "after" value is shown in a standard code block.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue(null, "large value content", "inline-diff")`.

**Expected Result:** Returns a markdown string containing a code block (```) with the content, and no diff markers or HTML.

---

### TC-05: FormatLargeValue_DeleteOperation_ShowsSingleCodeBlock

**Type:** Unit

**Description:** Verifies that for a "delete" operation (after is null), only the "before" value is shown.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue("large value content", null, "inline-diff")`.

**Expected Result:** Returns a markdown string containing a code block with the content.

---

### TC-06: FormatLargeValue_UpdateStandardDiff_ShowsDiffCodeFence

**Type:** Unit

**Description:** Verifies that `standard-diff` format uses the `diff` language identifier.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue("old\ncontent", "new\ncontent", "standard-diff")`.

**Expected Result:** Returns a string starting with ` ```diff ` and containing `- old` and `+ new`.

---

### TC-07: FormatLargeValue_UpdateInlineDiff_WithCommonLines_ShowsHtmlPre

**Type:** Unit

**Description:** Verifies that `inline-diff` format uses HTML `<pre>` and `<span>` with specific styles when common lines exist.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue("common\nold", "common\nnew", "inline-diff")`.

**Expected Result:** 
- Contains `<pre style="font-family: monospace; line-height: 1.5;">`.
- Contains `common` without background color.
- Contains `<span style="background-color: #fff5f5; ...">` for the deleted line.
- Contains `<span style="background-color: #f0fff4; ...">` for the added line.
- Contains `color: #24292e` in all spans.

---

### TC-08: FormatLargeValue_UpdateInlineDiff_NoCommonLines_ShowsBeforeAfterBlocks

**Type:** Unit

**Description:** Verifies the "Complete Replacement" logic where no common lines exist.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue("completely\ndifferent", "nothing\nin\ncommon", "inline-diff")`.

**Expected Result:** 
- Returns a string with "**Before:**" followed by a code block.
- Followed by "**After:**" and another code block.
- No HTML `<pre>` or `<span>` tags.

---

### TC-09: LargeAttributesSummary_MultipleAttributes_ReturnsCorrectString

**Type:** Unit

**Description:** Verifies the summary line generation logic.

**Preconditions:** None

**Test Steps:**
1. Call `ScribanHelpers.LargeAttributesSummary` with a list of attributes and their before/after values.
   - attr1: 10 lines, 5 changed.
   - attr2: 1 line, 1 changed.

**Expected Result:** Returns `"Large values: attr1 (10 lines, 5 changed), attr2 (1 line, 1 changed)"`.

---

### TC-10: MarkdownRenderer_WithLargeValues_GroupsThemInDetailsSection

**Type:** Integration

**Description:** Verifies that the `MarkdownRenderer` correctly uses the helpers to split attributes between the table and the details section.

**Preconditions:** A `ReportModel` with a resource containing both small and large attributes.

**Test Steps:**
1. Render the model using `MarkdownRenderer.Render`.

**Expected Result:** 
- The `<table>` contains only small attributes.
- A `<details>` section exists with the large attributes.

---

### TC-11: CliParser_LargeValueFormatOption_ParsesCorrectly

**Type:** Unit

**Description:** Verifies CLI argument parsing for the new option.

**Preconditions:** None

**Test Steps:**
1. Parse `tfplan2md plan.json --large-value-format standard-diff`.
2. Parse `tfplan2md plan.json`.

**Expected Result:** 
1. `LargeValueFormat` is `StandardDiff`.
2. `LargeValueFormat` is `InlineDiff` (default).

---

### TC-12: FormatLargeValue_SensitiveValue_MasksCorrectly

**Type:** Unit

**Description:** Verifies that sensitive values are masked in large attribute displays.

**Preconditions:** `ReportModelBuilder` configured with `showSensitive: false`.

**Test Steps:**
1. Create a plan with a sensitive large attribute.
2. Build the model and render.

**Expected Result:** The large value display shows `(sensitive value)` or `***` instead of the actual content.

## Test Data Requirements

- `large-attribute-update.json`: A Terraform plan JSON containing an update to a multi-line attribute (e.g., `azurerm_api_management_policy`).
- `large-attribute-create.json`: A plan with a new resource having a long single-line attribute.
- `mixed-attributes.json`: A plan with a resource having both small and large attribute changes.
- `no-common-lines.json`: A plan where a large attribute is completely replaced with no overlapping lines.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty string | Treated as small value (not large) | TC-03 |
| Null values | Handled gracefully (Create/Delete logic) | TC-04, TC-05 |
| Only whitespace changes | Identified as changes in diff | TC-07 |
| Very large values (1000+ lines) | Rendered without crashing; performance remains acceptable | TC-10 |
| Special characters in values | Correctly escaped for HTML/Markdown | TC-07 |

## Non-Functional Tests

- **Performance**: Ensure that the diff algorithm (LCS) doesn't significantly slow down report generation for large plans.
- **Accessibility**: Verify that the chosen colors for `inline-diff` meet contrast requirements (WCAG AA) when combined with the explicit text color.

## Open Questions

None.
