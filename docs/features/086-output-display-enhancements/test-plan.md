# Test Plan: Output Display Enhancements

## Overview

This test plan defines the testing strategy for Feature 086: Output Display Enhancements. This feature introduces two improvements to tfplan2md markdown output:

1. **Collapsible Debug Section**: Debug information is wrapped in a collapsed `<details>` block by default
2. **No-Changes Summary**: Plans with zero changes show "No changes" text instead of empty summary tables, and omit the redundant Resource Changes section

**Reference:** `docs/features/086-output-display-enhancements/specification.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Debug section is wrapped in `<details>` block with `<summary>🐛 Debug Information</summary>` | TC-01, TC-02, TC-03 | Unit |
| Debug section is collapsed by default (no `open` attribute) | TC-01, TC-02 | Unit |
| Debug section includes `<br>` spacing after summary tag | TC-01, TC-02 | Unit |
| All existing debug content is preserved and visible when expanded | TC-02, TC-03, TC-04 | Unit |
| Plans with zero changes show `No changes` in Summary section | TC-05, TC-06 | Unit, Integration |
| Plans with zero changes do NOT render Resource Changes section | TC-05, TC-06 | Unit, Integration |
| Plans with changes continue to show full summary table | TC-07 | Integration |
| Debug and no-changes features render correctly in GitHub and Azure DevOps | UAT-01, UAT-02 | UAT |
| Existing tests updated to match new format | TC-08, TC-09 | Regression |

## Test Cases

### TC-01: Debug Section Collapsed Structure (Empty Diagnostics)

**Type:** Unit

**Description:**
Verify that when debug diagnostics are empty (no principal mapping, no template resolutions), the debug section still renders as a collapsible `<details>` block with proper structure.

**Preconditions:**
- DiagnosticContext is created but has no diagnostics collected

**Test Steps:**
1. Create an empty DiagnosticContext instance
2. Call `GenerateMarkdownSection()`
3. Verify output structure

**Expected Result:**
The markdown output should contain:
- Opening `<details>` tag (no `open` attribute)
- `<summary>🐛 Debug Information</summary>` (with non-breaking space U+00A0 between emoji and text)
- `<br>` tag immediately after `</summary>`
- "No diagnostics collected." message
- Closing `</details>` tag

**Test Data:**
None (in-memory test)

**Coverage:**
- Acceptance criteria: Collapsed by default, proper summary tag, `<br>` spacing
- Edge case: Empty debug context

---

### TC-02: Debug Section Collapsed Structure (With Diagnostics)

**Type:** Unit

**Description:**
Verify that when debug diagnostics contain content (principal mapping, template resolutions), the entire debug section is wrapped in a collapsible `<details>` block while preserving all subsection headings and content.

**Preconditions:**
- DiagnosticContext has principal mapping diagnostics
- DiagnosticContext has template resolution diagnostics
- DiagnosticContext has failed resolution diagnostics

**Test Steps:**
1. Create DiagnosticContext with:
   - Principal mapping file loaded successfully
   - Principal type counts (users, groups, service principals)
   - Template resolutions for multiple resource types
   - Failed principal resolutions
2. Call `GenerateMarkdownSection()`
3. Verify output structure and content

**Expected Result:**
The markdown output should contain:
- Opening `<details>` tag (no `open` attribute)
- `<summary>🐛 Debug Information</summary>` (with non-breaking space U+00A0)
- `<br>` tag after `</summary>`
- `### Principal Mapping` subsection with:
  - "Loaded successfully from 'principals.json'"
  - Principal type counts
  - Failed resolutions list
- `### Template Resolution` subsection with:
  - List of resource type template mappings
- Closing `</details>` tag
- **NOT** contain `## Debug Information` (H2 heading replaced by summary)

**Test Data:**
None (in-memory test with predefined DiagnosticContext state)

**Coverage:**
- Acceptance criteria: All debug content preserved, collapsed by default, proper structure
- Existing debug formatting maintained

---

### TC-03: Debug Section Does Not Have `open` Attribute

**Type:** Unit

**Description:**
Verify that the `<details>` tag does not include the `open` attribute, ensuring the debug section is collapsed by default.

**Preconditions:**
- DiagnosticContext with content

**Test Steps:**
1. Create DiagnosticContext with diagnostics
2. Call `GenerateMarkdownSection()`
3. Parse the opening `<details>` tag

**Expected Result:**
- `<details>` tag exists
- `<details>` tag does NOT contain `open` attribute
- Format should be `<details>` not `<details open>`

**Test Data:**
None (in-memory test)

**Coverage:**
- Acceptance criteria: Collapsed by default

---

### TC-04: Debug Section Content Preserved (Regression)

**Type:** Unit

**Description:**
Verify that all existing debug section content (principal mapping, template resolution, failed resolutions) is preserved exactly as before, only the wrapping structure changes.

**Preconditions:**
- DiagnosticContext with comprehensive diagnostics

**Test Steps:**
1. Create DiagnosticContext with:
   - Principal mapping load success/failure scenarios
   - Principal type counts
   - Failed principal and role definition resolutions
   - Template resolutions (built-in, default, custom templates)
2. Call `GenerateMarkdownSection()`
3. Extract content between `<details>` and `</details>` tags
4. Verify content matches existing diagnostic output expectations

**Expected Result:**
The content between `<details>` tags should include:
- All subsection headings (`###`)
- Principal mapping diagnostics with counts
- Failed resolution details with resource context
- Template resolution list with resource types
- All formatting (backticks, list markers, etc.) unchanged

**Test Data:**
None (in-memory test)

**Coverage:**
- Acceptance criteria: All existing debug content preserved
- Regression: Existing diagnostic tests remain valid (content-wise)

---

### TC-05: No-Changes Summary Shows Simple Message

**Type:** Unit

**Description:**
Verify that when a Terraform plan has zero resource changes (all action counts are 0), the Summary section renders "No changes" instead of an empty summary table.

**Preconditions:**
- Terraform plan JSON with zero resource changes
- Summary model with `total == 0`

**Test Steps:**
1. Parse a Terraform plan with no changes (only no-op actions)
2. Build ReportModel
3. Render markdown using default template
4. Extract Summary section

**Expected Result:**
The Summary section should:
- Contain heading `## Summary`
- Contain text `No changes` (plain text, not in a table)
- **NOT** contain summary table with columns "Action | Count | Resource Types"
- **NOT** contain rows for Add, Change, Replace, Destroy actions

**Test Data:**
- `TestData/no-op-plan.json` (existing test data with only no-op resources)
- OR create minimal plan with `resource_changes: []`

**Coverage:**
- Acceptance criteria: No-changes summary format
- Summary template conditional logic (`summary.total == 0`)

---

### TC-06: No-Changes Plan Omits Resource Changes Section

**Type:** Integration

**Description:**
Verify that when a Terraform plan has zero resource changes, the Resource Changes section is completely omitted from the output (not rendered at all).

**Preconditions:**
- Terraform plan JSON with zero resource changes
- ReportModel with empty `module_changes` list

**Test Steps:**
1. Parse a Terraform plan with no changes
2. Build ReportModel (should have `module_changes.size == 0`)
3. Render markdown using default template
4. Check for Resource Changes section

**Expected Result:**
The markdown output should:
- **NOT** contain `## Resource Changes` heading
- **NOT** contain "No changes" in a separate Resource Changes section
- Only show "No changes" in the Summary section
- Still render other sections (Code Analysis Summary, if present)

**Test Data:**
- `TestData/no-op-plan.json`

**Coverage:**
- Acceptance criteria: Resource Changes section omitted for no-changes plans
- Template conditional logic (`module_changes.size > 0`)
- Avoids redundant "No changes" messages

---

### TC-07: Plans With Changes Show Full Summary Table (Regression)

**Type:** Integration

**Description:**
Verify that plans with actual resource changes continue to render the full summary table with action counts and resource types, and the Resource Changes section appears normally.

**Preconditions:**
- Terraform plan JSON with resource changes (add, change, destroy)
- Summary model with `total > 0`

**Test Steps:**
1. Parse a Terraform plan with resource changes
2. Build ReportModel
3. Render markdown using default template
4. Verify Summary and Resource Changes sections

**Expected Result:**
The markdown output should:
- Contain `## Summary` heading
- Contain full summary table with:
  - Headers: "Action | Count | Resource Types"
  - Rows for Add, Change, Replace, Destroy with non-zero counts
  - Total row
- Contain `## Resource Changes` heading
- Contain module and resource details
- **NOT** contain "No changes" text

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json` (existing test data with changes)
- `TestData/firewall-rule-changes.json`

**Coverage:**
- Acceptance criteria: Plans with changes show full summary table (no regression)
- Ensures conditional logic only affects no-changes scenarios

---

### TC-08: Existing Debug Output Tests Updated

**Type:** Regression

**Description:**
Verify that existing tests for debug output functionality are updated to expect the new `<details>` wrapper format while maintaining all content assertions.

**Preconditions:**
- Existing test files:
  - `src/tests/Oocx.TfPlan2Md.TUnit/Diagnostics/DiagnosticContextTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/EndToEnd/DebugOutputIntegrationTests.cs`

**Test Steps:**
1. Update test assertions to expect `<details>` wrapper
2. Update assertions to look for `<summary>🐛 Debug Information</summary>` instead of `## Debug Information`
3. Verify all content assertions still pass (principal mapping, template resolution, etc.)
4. Run all diagnostic tests

**Expected Result:**
- All existing debug tests pass with updated assertions
- Test coverage remains at 100% for DiagnosticContext
- No test regressions

**Test Data:**
N/A (update to existing tests)

**Coverage:**
- Acceptance criteria: Existing tests updated
- Regression prevention

---

### TC-09: Existing No-Changes Tests Updated

**Type:** Regression

**Description:**
Verify that existing tests for no-changes scenarios are updated to expect "No changes" text in Summary instead of empty table, and no Resource Changes section.

**Preconditions:**
- Existing test in `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownRendererTests.cs`
  - Test: `Render_NoOpPlan_ProducesValidMarkdown`
  - Test: `Render_EmptyPlan_ShowsNoChangesMessage`

**Test Steps:**
1. Update test assertions to expect:
   - "No changes" in Summary section (not table)
   - NO `## Resource Changes` heading for zero-changes plans
2. Run all MarkdownRenderer tests
3. Verify snapshot tests are updated if they exist

**Expected Result:**
- All existing no-changes tests pass with updated assertions
- Tests correctly validate the new "No changes" format
- No regression in other renderer tests

**Test Data:**
N/A (update to existing tests)

**Coverage:**
- Acceptance criteria: Existing tests updated
- Regression prevention

---

### TC-10: Debug Section in End-to-End Rendering

**Type:** Integration

**Description:**
Verify that the debug section is correctly appended to the full report output and renders as collapsible when `--debug` flag is enabled.

**Preconditions:**
- Terraform plan JSON
- DiagnosticContext enabled (simulating `--debug` flag)

**Test Steps:**
1. Parse a Terraform plan
2. Create DiagnosticContext and components with diagnostics enabled
3. Render markdown report
4. Append debug section (as done in `ProgramEntry.cs`)
5. Verify full output structure

**Expected Result:**
The full markdown output should:
- Contain main report sections (Summary, Resource Changes, etc.)
- End with collapsed debug section in `<details>` block
- Debug section positioned after all main report content
- Proper separation (blank lines) between main report and debug section

**Test Data:**
- `TestData/azurerm-azuredevops-plan.json`
- `TestData/principal-mapping.json`

**Coverage:**
- Integration of debug section with main report
- Acceptance criteria: Debug section rendered correctly

---

### TC-11: Summary-Only Template With No Changes

**Type:** Unit

**Description:**
Verify that when using the `--template summary` option with a no-changes plan, the summary template correctly shows "No changes" instead of an empty table.

**Preconditions:**
- Terraform plan with zero changes
- Using `summary.sbn` template (includes `_summary.sbn` partial)

**Test Steps:**
1. Parse a no-changes plan
2. Build ReportModel
3. Render using summary template
4. Verify output

**Expected Result:**
The output should:
- Contain "No changes" text
- **NOT** contain summary table with zero counts
- Template reuse of `_summary.sbn` works correctly

**Test Data:**
- `TestData/no-op-plan.json`

**Coverage:**
- Summary-only template compatibility
- `_summary.sbn` partial reuse

---

### TC-12: Edge Case - No Debug, No Changes

**Type:** Integration

**Description:**
Verify that a plan with no changes and no debug flag enabled renders cleanly with just "No changes" in summary and no debug or resource changes sections.

**Preconditions:**
- Terraform plan with zero changes
- DiagnosticContext is null (no `--debug` flag)

**Test Steps:**
1. Parse a no-changes plan
2. Build ReportModel WITHOUT DiagnosticContext
3. Render markdown
4. Verify minimal output

**Expected Result:**
The output should:
- Contain `## Summary` with "No changes"
- **NOT** contain `## Resource Changes`
- **NOT** contain debug section
- Only contain Summary (and Code Analysis if applicable)

**Test Data:**
- `TestData/no-op-plan.json`

**Coverage:**
- Edge case: Minimal output scenario
- Combination of both features (no debug, no changes)

---

### TC-13: Edge Case - Debug Enabled, No Changes

**Type:** Integration

**Description:**
Verify that a plan with no changes but debug flag enabled shows "No changes" in summary, no Resource Changes section, and collapsible debug section.

**Preconditions:**
- Terraform plan with zero changes
- DiagnosticContext enabled

**Test Steps:**
1. Parse a no-changes plan
2. Build ReportModel WITH DiagnosticContext
3. Render markdown
4. Append debug section
5. Verify output structure

**Expected Result:**
The output should:
- Contain `## Summary` with "No changes"
- **NOT** contain `## Resource Changes`
- Contain collapsed debug section at the end
- Clean separation between summary and debug

**Test Data:**
- `TestData/no-op-plan.json`
- `TestData/principal-mapping.json`

**Coverage:**
- Edge case: No changes + debug enabled
- Combination of both features

---

### TC-14: Non-Breaking Space in Debug Summary

**Type:** Unit

**Description:**
Verify that the debug section summary uses a non-breaking space (U+00A0) between the bug emoji and "Debug Information" text, following the style guide convention.

**Preconditions:**
- DiagnosticContext with any content

**Test Steps:**
1. Create DiagnosticContext
2. Call `GenerateMarkdownSection()`
3. Extract summary tag content
4. Verify character between emoji and text

**Expected Result:**
- Summary tag should be: `<summary>🐛\u00A0Debug Information</summary>`
- Character between 🐛 and "Debug" should be U+00A0 (non-breaking space)
- Should **NOT** be regular space (U+0020)

**Test Data:**
None (in-memory test)

**Coverage:**
- Style guide compliance
- Consistency with existing collapsible sections

---

## Test Data Requirements

### Existing Test Data (Reuse)
- `TestData/no-op-plan.json` - Plan with only no-op resources (zero changes)
- `TestData/azurerm-azuredevops-plan.json` - Plan with resource changes
- `TestData/firewall-rule-changes.json` - Plan with updates
- `TestData/principal-mapping.json` - Principal mapping file for debug
- `TestData/partial-principal-mapping.json` - Mapping file with missing principals (for failed resolutions)

### New Test Data (If Needed)
- **Minimal no-changes plan**: If `no-op-plan.json` doesn't exist, create a minimal plan JSON with:
  ```json
  {
    "format_version": "1.2",
    "terraform_version": "1.14.0",
    "resource_changes": [],
    "configuration": { "root_module": {} }
  }
  ```

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty debug context | Debug section shows "No diagnostics collected." inside `<details>` | TC-01 |
| No changes + no debug | Only Summary with "No changes", no other sections | TC-12 |
| No changes + debug enabled | Summary "No changes" + collapsed debug section | TC-13 |
| Changes + debug enabled | Full summary table + Resource Changes + debug section | TC-10 |
| Summary-only template, no changes | "No changes" text (no table) | TC-11 |
| Debug section with failed resolutions | All failure details preserved in `<details>` block | TC-02, TC-04 |
| Single principal type | Singular form (e.g., "1 user") inside debug section | Existing test in DiagnosticContextTests |
| Azure DevOps entities in debug | Azdo counts preserved in debug section | Existing test in DiagnosticContextTests |

## Non-Functional Tests

### Style Guide Compliance
- **Test:** TC-14 (Non-breaking space in debug summary)
- **Requirement:** Debug section summary follows style guide pattern for collapsible sections
- **Validation:** Unicode character U+00A0 between emoji and text

### Markdown Compatibility
- **Test:** UAT-01, UAT-02
- **Requirement:** `<details>` tags render correctly in GitHub and Azure DevOps
- **Validation:** Visual inspection via UAT PRs
- **Note:** Both platforms support `<details>` tags (already used for resource sections)

### Template Consistency
- **Test:** TC-11
- **Requirement:** Changes to `_summary.sbn` partial work in all templates that include it
- **Validation:** Summary-only template and default template both show "No changes" correctly

### Backward Compatibility
- **Test:** TC-07, TC-08, TC-09
- **Requirement:** Plans with changes render identically to before (except debug section wrapper)
- **Validation:** Regression tests confirm no impact on existing functionality

## Open Questions

None. All testing requirements are well-defined based on specification and architecture documents.

## Testing Tools & Commands

### Run All Tests
```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

### Run Specific Test Categories
```bash
# Run only unit tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /**[Category=Unit]

# Run only integration tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /**[Category=Integration]
```

### Run Specific Test Classes
```bash
# Run DiagnosticContext tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/DiagnosticContextTests/*

# Run MarkdownRenderer tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/MarkdownRendererTests/*

# Run DebugOutputIntegration tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/DebugOutputIntegrationTests/*
```

### Snapshot Test Updates
If snapshot tests exist and need updating after intentional output changes:
```bash
# Use the update-test-snapshots skill
@developer-coding-agent use the update-test-snapshots skill to regenerate snapshots
```

## Test Execution Order

1. **Unit Tests First** (TC-01 through TC-05, TC-11, TC-14)
   - DiagnosticContext tests (debug section structure)
   - Template rendering tests (summary conditional logic)
   
2. **Integration Tests** (TC-06, TC-07, TC-10, TC-12, TC-13)
   - Full rendering pipeline
   - Combination scenarios
   
3. **Regression Tests** (TC-08, TC-09)
   - Existing test updates
   - Backward compatibility
   
4. **UAT Tests** (UAT-01, UAT-02)
   - Visual verification in GitHub and Azure DevOps
   - See `uat-test-plan.md`

## Definition of Done

Testing is complete when:
- [ ] All unit tests pass (TC-01 through TC-05, TC-11, TC-14)
- [ ] All integration tests pass (TC-06, TC-07, TC-10, TC-12, TC-13)
- [ ] All regression tests updated and passing (TC-08, TC-09)
- [ ] Snapshot tests updated (if applicable)
- [ ] UAT test plan created and ready for execution
- [ ] All tests executable via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`
- [ ] No manual testing steps required (all automated except UAT visual checks)
- [ ] Test coverage remains at or above current levels
- [ ] Code Reviewer has validated test completeness
