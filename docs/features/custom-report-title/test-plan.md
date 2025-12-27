# Test Plan: Custom Report Title

## Overview

This test plan covers the "Custom Report Title" feature, which allows users to provide a custom title for generated reports via the `--report-title` CLI option.

Reference:
- [Specification](../../features/custom-report-title/specification.md)
- [Architecture](../../features/custom-report-title/architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `--report-title` option is recognized by the CLI parser | TC-01 | Unit |
| User-provided title replaces the default heading in built-in templates | TC-06, TC-07 | Integration |
| Custom templates can access the title via the `report_title` variable | TC-08 | Integration |
| Markdown special characters are automatically escaped | TC-04, TC-05 | Unit |
| The `report_title` variable contains only the text (no `#`) | TC-04, TC-06 | Unit/Integration |
| Empty string title shows error message and help output | TC-02 | Unit |
| Title with newlines fails with clear error message | TC-03 | Unit |
| When `--report-title` is not provided, `report_title` is null | TC-05 | Unit |
| Generated reports with custom titles pass markdown linting | TC-09 | Integration |
| Documentation and help text include the new option | TC-10 | Unit |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: Custom Title with Special Characters

**User Goal**: Verify that a custom title with special characters renders correctly as a level-1 heading.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- A level-1 heading with the text: `Drift Detection # Results [Production]`
- The `#` and `[` `]` characters should be rendered literally, not as markdown syntax.

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Information is accurate and complete

---

### Scenario 2: Default Title (No Option)

**User Goal**: Verify that the default title is still used when the option is omitted.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- The default level-1 heading: `Terraform Plan Summary`

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown

## Test Cases

### TC-01: Parse_ReportTitleFlag_SetsReportTitle

**Type:** Unit
**Class:** `CliParserTests`

**Description:**
Verify that the `--report-title` flag correctly populates the `ReportTitle` property in `CliOptions`.

**Preconditions:**
- None

**Test Steps:**
1. Call `CliParser.Parse(new[] { "--report-title", "My Custom Title" })`.

**Expected Result:**
`options.ReportTitle` should be `"My Custom Title"`.

---

### TC-02: Parse_ReportTitleEmpty_ThrowsCliParseException

**Type:** Unit
**Class:** `CliParserTests`

**Description:**
Verify that providing an empty string to `--report-title` throws a `CliParseException`.

**Preconditions:**
- None

**Test Steps:**
1. Call `CliParser.Parse(new[] { "--report-title", "" })`.

**Expected Result:**
Throws `CliParseException` with message containing "cannot be empty".

---

### TC-03: Parse_ReportTitleWithNewlines_ThrowsCliParseException

**Type:** Unit
**Class:** `CliParserTests`

**Description:**
Verify that providing a title with newlines throws a `CliParseException`.

**Preconditions:**
- None

**Test Steps:**
1. Call `CliParser.Parse(new[] { "--report-title", "Line 1\nLine 2" })`.

**Expected Result:**
Throws `CliParseException` with message containing "cannot contain newlines".

---

### TC-04: Build_WithReportTitle_EscapesMarkdownCharacters

**Type:** Unit
**Class:** `ReportModelBuilderTests`

**Description:**
Verify that the `ReportModelBuilder` escapes markdown special characters in the title.

**Preconditions:**
- None

**Test Steps:**
1. Create `ReportModelBuilder` with `reportTitle: "My # Title [Draft]"`.
2. Call `builder.Build(plan)`.

**Expected Result:**
`model.ReportTitle` should be `"My \# Title \[Draft\]"`.

---

### TC-05: Build_WithoutReportTitle_SetsReportTitleToNull

**Type:** Unit
**Class:** `ReportModelBuilderTests`

**Description:**
Verify that `ReportTitle` is null in the model when not provided.

**Preconditions:**
- None

**Test Steps:**
1. Create `ReportModelBuilder` without providing `reportTitle`.
2. Call `builder.Build(plan)`.

**Expected Result:**
`model.ReportTitle` should be `null`.

---

### TC-06: Render_WithReportTitle_UsesCustomTitleInDefaultTemplate

**Type:** Integration
**Class:** `MarkdownRendererTests`

**Description:**
Verify that the default template renders the custom title correctly.

**Preconditions:**
- A valid `ReportModel` with `ReportTitle = "Custom Title"`.

**Test Steps:**
1. Call `renderer.Render(model)`.

**Expected Result:**
The output should start with `# Custom Title`.

---

### TC-07: Render_WithReportTitle_UsesCustomTitleInSummaryTemplate

**Type:** Integration
**Class:** `MarkdownRendererSummaryTests`

**Description:**
Verify that the summary template renders the custom title correctly.

**Preconditions:**
- A valid `ReportModel` with `ReportTitle = "Summary Title"`.

**Test Steps:**
1. Call `renderer.Render(model, "summary")`.

**Expected Result:**
The output should start with `# Summary Title`.

---

### TC-08: Render_WithCustomTemplate_CanAccessReportTitle

**Type:** Integration
**Class:** `MarkdownRendererTests`

**Description:**
Verify that a custom template can access the `report_title` variable.

**Preconditions:**
- A custom template: `# {{ report_title ?? "Default" }}`.
- A `ReportModel` with `ReportTitle = "Custom"`.

**Test Steps:**
1. Call `renderer.Render(model, customTemplatePath)`.

**Expected Result:**
The output should be `# Custom`.

---

### TC-09: Render_WithCustomTitle_PassesMarkdownLint

**Type:** Integration
**Class:** `MarkdownLintIntegrationTests`

**Description:**
Verify that reports generated with custom titles pass markdown linting.

**Preconditions:**
- `markdownlint-cli2` available.

**Test Steps:**
1. Generate a report with a custom title containing special characters.
2. Run `markdownlint-cli2` on the output.

**Expected Result:**
No linting errors.

---

### TC-10: HelpText_IncludesReportTitleOption

**Type:** Unit
**Class:** `HelpTextProviderTests`

**Description:**
Verify that the help text includes the `--report-title` option.

**Preconditions:**
- None

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()`.

**Expected Result:**
Help text contains `--report-title`.

## Test Data Requirements

No new JSON test data files are strictly required, as existing plans can be used. However, for TC-09, we should ensure we test with a variety of special characters.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Title with only spaces | Treated as empty, throws error | TC-02 |
| Title with markdown syntax | Escaped and rendered literally | TC-04 |
| Title with HTML tags | Escaped (if applicable) or rendered literally | TC-04 |
| Very long title | Renders correctly (no length limit) | TC-06 |

## Non-Functional Tests

- **Performance**: No significant impact expected.
- **Error Handling**: Covered by TC-02 and TC-03.

## Open Questions

None.
