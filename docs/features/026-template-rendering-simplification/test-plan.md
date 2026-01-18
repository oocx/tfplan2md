# Test Plan: Template Rendering Simplification

## Overview

This test plan verifies the refactoring of the template rendering system from a two-pass "render-then-replace" mechanism to a single-pass "direct dispatch" system. The primary goal is to ensure architectural simplification while maintaining 100% output equivalence for existing features.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Zero HTML anchor comments in templates | TC-01 | Static Analysis / Unit |
| Zero `func` definitions in resource templates | TC-02 | Static Analysis / Unit |
| Output equivalence (byte-identical or whitespace-equivalent) | TC-03, TC-04 | Integration (Snapshot) |
| Regex workarounds in renderer eliminated | TC-05 | Static Analysis / Unit |
| Template files under 100 lines | TC-06 | Static Analysis |
| Single-pass rendering (no wasted computation) | TC-07 | Unit |
| Resource-specific template resolution | TC-08 | Unit |
| Custom template directory support | TC-09 | Integration |
| Helper functions split into focused files | TC-10 | Unit |

## User Acceptance Scenarios

> **Purpose**: Verify that the refactoring does not break the visual rendering of complex resources in GitHub and Azure DevOps.

### Scenario 1: Comprehensive Demo Rendering

**User Goal**: Ensure the "Comprehensive Demo" report renders exactly as before, including all resource-specific formatting (NSG, Firewall, Role Assignments).

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- All icons (✅, ❌, 📝, etc.) appear correctly.
- Tables for NSG rules and Firewall rules are formatted correctly.
- Role assignment readable display is preserved.
- No visible HTML anchor comments.
- No broken table formatting (no literal `<br>` or alignment issues).

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Information is accurate and complete
- [ ] No regressions compared to `artifacts/comprehensive-demo.md`

---

### Scenario 2: Custom Template Override

**User Goal**: Verify that users can still provide custom templates in a local directory and they take precedence over built-in ones.

**Test PR Context**:
- **GitHub**: Verify that a custom template for a specific resource type is used.

**Expected Output**:
- The specific resource uses the custom layout defined in the local template file.
- Other resources use built-in templates.

**Success Criteria**:
- [ ] Custom template is correctly resolved and rendered.
- [ ] No errors during template loading.

## Test Cases

### TC-01: Verify Absence of Anchor Comments

**Type:** Static Analysis / Unit

**Description:**
Verify that no template files (`.sbn`) contain the string `tfplan2md:resource-start` or `tfplan2md:resource-end`.

**Preconditions:**
- Refactoring complete.

**Test Steps:**
1. Scan all files in `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/`.
2. Assert that no file contains anchor comment strings.

**Expected Result:**
Zero occurrences found.

---

### TC-02: Verify Absence of Template `func` Definitions

**Type:** Static Analysis / Unit

**Description:**
Verify that resource-specific templates do not contain `func` definitions, as logic should have moved to C#.

**Preconditions:**
- Refactoring complete.

**Test Steps:**
1. Scan all `.sbn` files in `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/`.
2. Assert that no file contains the `func` keyword.

**Expected Result:**
Zero occurrences found.

---

### TC-03: Snapshot Regression Testing (Standard)

**Type:** Integration (Snapshot)

**Description:**
Run existing xUnit snapshot tests to ensure output remains equivalent.

**Preconditions:**
- Baseline snapshots captured before refactoring.

**Test Steps:**
1. Run `dotnet test` for `Oocx.TfPlan2Md.Tests`.
2. Compare generated output with `.verified.md` files.

**Expected Result:**
All tests pass. Any differences are whitespace-only or documented intentional improvements.

---

### TC-04: Artifact Equivalence (UAT)

**Type:** Integration

**Description:**
Compare generated demo artifacts with baselines.

**Preconditions:**
- `artifacts/comprehensive-demo.md` (and others) captured before refactoring.

**Test Steps:**
1. Run `scripts/generate-demo-artifacts.sh`.
2. Run `git diff artifacts/`.

**Expected Result:**
No functional differences. Whitespace differences are acceptable if they don't affect rendering.

---

### TC-05: Verify Removal of Regex Hacks

**Type:** Static Analysis / Unit

**Description:**
Verify that `MarkdownRenderer.cs` no longer contains the 6 identified Regex workarounds.

**Preconditions:**
- Refactoring complete.

**Test Steps:**
1. Inspect `MarkdownRenderer.cs`.
2. Assert that `Regex.Replace` calls for anchors, blank lines in tables, and heading spacing are removed.

**Expected Result:**
Identified Regex calls are gone.

---

### TC-06: Template Line Count Validation

**Type:** Static Analysis

**Description:**
Verify that all template files are under 100 lines (target is <60 for most).

**Preconditions:**
- Refactoring complete.

**Test Steps:**
1. Count lines in all `.sbn` files.
2. Assert all are < 100 lines.

**Expected Result:**
All templates meet the size constraint.

---

### TC-07: ITemplateLoader Unit Tests

**Type:** Unit

**Description:**
Verify the new `ITemplateLoader` correctly loads partials and resource templates.

**Test Steps:**
1. Test loading `_header` (partial).
2. Test loading `azurerm/role_assignment` (resource template).
3. Test loading from custom directory.
4. Test fallback to default when resource template is missing.

**Expected Result:**
Correct template content is returned for each scenario.

---

### TC-08: Template Resolution Logic

**Type:** Unit

**Description:**
Verify `resolve_template` helper correctly maps resource types to template paths.

**Test Steps:**
1. Input `azurerm_network_security_group`, expect `azurerm/network_security_group`.
2. Input unknown type, expect `_resource` (default).

**Expected Result:**
Correct mapping for all supported types.

---

### TC-09: FormattedValue Pattern Verification

**Type:** Unit

**Description:**
Verify that `FormattedValue<T>` and `FormattedList<T>` correctly provide both raw and formatted data to Scriban.

**Test Steps:**
1. Create a ViewModel with `FormattedValue`.
2. Render a simple template accessing `.raw` and `.formatted`.
3. Assert output matches expectations.

**Expected Result:**
Both properties are accessible and correct.

---

### TC-10: Helper Registry Verification

**Type:** Unit

**Description:**
Verify that all split helper classes are correctly registered and available in the Scriban context.

**Test Steps:**
1. Render a template using a helper from each new file (e.g., `SemanticIcons`, `AzureFormatting`).
2. Assert all helpers execute correctly.

**Expected Result:**
No "function not found" errors; correct output from all helpers.

## Test Data Requirements

- Existing test data in `src/tests/Oocx.TfPlan2Md.Tests/TestData/`:
  - `comprehensive-demo.json`
  - `nsg-plan.json`
  - `firewall-plan.json`
  - `role-assignment-plan.json`
- No new JSON files required, but existing ones must be used to exercise all migrated templates.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Missing resource template | Fallback to `_resource.sbn` (default) | TC-07, TC-08 |
| Template rendering error | Log error and continue or fail gracefully (no partial/broken output) | TC-07 |
| Resource address with special chars | Correctly rendered without regex replacement issues | TC-03 |
| Empty plan (no changes) | Renders "No changes" via `_header` and `_footer` | TC-03 |

## Non-Functional Tests

- **Maintainability**: Verified by TC-01, TC-02, TC-05, TC-06 (metrics-based).
- **Performance**: Ensure rendering time does not regress significantly (though not a primary goal).

## Open Questions

- Should we automate the line count and "no func" checks as part of the build/test suite? **Yes, these will be implemented as unit tests that scan the template directory.**
