# Tasks: Custom Report Title

## Overview

This feature allows users to provide a custom title for generated reports via the `--report-title` CLI option. The title is automatically escaped for markdown and made available to templates.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)

## Tasks

### Task 1: Update CLI Options and Report Model

**Priority:** High

**Description:**
Add the `ReportTitle` property to `CliOptions` and `ReportModel` to support passing the custom title through the application.

**Acceptance Criteria:**
- [x] `CliOptions` has a `string? ReportTitle` property.
- [x] `ReportModel` has a `string? ReportTitle` property.
- [x] `ReportModelBuilder` constructor accepts an optional `reportTitle` parameter.
- [x] `ReportModelBuilder` stores the `reportTitle` for use during model building.

**Dependencies:** None

---

### Task 2: Implement CLI Parsing and Validation

**Priority:** High

**Description:**
Update `CliParser` to handle the `--report-title` option and implement validation rules.

**Acceptance Criteria:**
- [x] `CliParser` recognizes `--report-title`.
- [x] `CliParser` throws `CliParseException` if `--report-title` is provided without a value.
- [x] `CliParser` throws `CliParseException` if the title is empty or only whitespace.
- [x] `CliParser` throws `CliParseException` if the title contains newlines.
- [x] Unit tests in `CliParserTests.cs` verify these behaviors (TC-01, TC-02, TC-03).

**Dependencies:** Task 1

---

### Task 3: Implement Report Model Building with Escaping

**Priority:** High

**Description:**
Update `ReportModelBuilder` to escape markdown special characters in the report title before populating the `ReportModel`.

**Acceptance Criteria:**
- [x] `ReportModelBuilder.Build` populates `ReportModel.ReportTitle`.
- [x] If a title is provided, it is escaped using `ScribanHelpers.EscapeMarkdownHeading`.
- [x] If no title is provided, `ReportModel.ReportTitle` is `null`.
- [x] Unit tests in `ReportModelBuilderTests.cs` verify escaping and null handling (TC-04, TC-05).

**Dependencies:** Task 1

---

### Task 4: Update Program.cs and Help Text

**Priority:** Medium

**Description:**
Integrate the new option into the main application flow and update the help text.

**Acceptance Criteria:**
- [x] `Program.cs` passes `options.ReportTitle` to the `ReportModelBuilder`.
- [x] `HelpTextProvider.cs` includes the `--report-title` option in the help output.
- [x] Unit tests in `HelpTextProviderTests.cs` verify the help text update (TC-10).

**Dependencies:** Task 2, Task 3

---

### Task 5: Update Built-in Templates

**Priority:** Medium

**Description:**
Update the `default` and `summary` templates to use the `report_title` variable with a fallback to the default title.

**Acceptance Criteria:**
- [x] `default.sbn` uses `# {{ report_title ?? "Terraform Plan Report" }}`.
- [x] `summary.sbn` uses `# {{ report_title ?? "Terraform Plan Summary" }}`.
- [x] Integration tests in `MarkdownRendererTests.cs` and `MarkdownRendererSummaryTests.cs` verify the rendered output (TC-06, TC-07).
- [x] Integration test verifies custom template access (TC-08).

**Dependencies:** Task 3

---

### Task 6: Final Validation and Documentation

**Priority:** Low

**Description:**
Perform final validation of the generated markdown and update user-facing documentation.

**Acceptance Criteria:**
- [x] Generated reports with custom titles pass markdown linting (TC-09).
- [x] `README.md` is updated to include the `--report-title` option in the CLI options table.
- [x] `docs/features.md` is updated to mention the custom title feature.

**Dependencies:** Task 4, Task 5

## Implementation Order

1. **Task 1 & 2**: Foundation and CLI parsing. This allows testing the CLI interface early.
2. **Task 3**: Core logic for escaping and model building.
3. **Task 4**: Integration into the main program.
4. **Task 5**: Template updates to actually show the custom title.
5. **Task 6**: Documentation and final quality checks.

## Open Questions

None.
