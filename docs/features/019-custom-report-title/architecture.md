# ADR-005: Custom Report Title

## Status

Proposed

## Context

Users need to customize the report title to provide context (e.g., repository name, pipeline type) when sharing reports. Currently, the title is hardcoded in the templates (e.g., "# Terraform Plan Summary").
The Feature Specification defines the requirements:
- CLI option `--report-title`.
- Automatic markdown escaping.
- Templates control the default title.
- Templates add the `#` heading markup.

## Options Considered

### Option 1: Raw Title in Model, Escape in Template
Pass the raw title to the template and use a Scriban function to escape it.
- **Pros:** Maximum flexibility for templates.
- **Cons:** Requires template authors to remember to escape the title. Risk of broken markdown if forgotten.

### Option 2: Escape in CLI Parser
Escape the title immediately when parsing arguments.
- **Pros:** Early validation/sanitization.
- **Cons:** CLI Parser should focus on parsing, not business logic or rendering concerns.

### Option 3: Escape in ReportModelBuilder (Selected)
Pass the raw title to `ReportModelBuilder`, which escapes it and populates the `ReportModel`.
- **Pros:** Centralizes logic in the builder which prepares data for the view. `ReportModel` contains "ready-to-render" data.
- **Cons:** None significant.

## Decision

We will implement Option 3.
1.  Add `ReportTitle` to `CliOptions` and `ReportModel`.
2.  Update `CliParser` to parse and validate the option (reject empty/newlines).
3.  Update `ReportModelBuilder` to accept the title and escape it using `ScribanHelpers.EscapeMarkdown`.
4.  Update built-in templates to use `{{ report_title ?? "Default Title" }}`.

## Rationale

This approach ensures that the data passed to the template is safe to render, while keeping the CLI parsing logic clean. It aligns with the existing pattern where `ReportModel` serves as a view model for the template.

## Consequences

### Positive
- Users can customize titles easily.
- Markdown injection is prevented by automatic escaping.
- Templates remain simple and declarative.

### Negative
- None.

## Implementation Notes

- **Components to modify:**
    - `src/Oocx.TfPlan2Md/CLI/CliParser.cs`: Add option parsing and validation.
    - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`: Add property to `ReportModel` and `ReportModelBuilder`.
    - `src/Oocx.TfPlan2Md/Program.cs`: Pass title from options to builder.
    - `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`: Update headline.
    - `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/summary.sbn`: Update headline.

- **Validation:**
    - `string.IsNullOrWhiteSpace` check in `CliParser`.
    - `Contains('\n')` check in `CliParser`.

- **Escaping:**
    - Use `ScribanHelpers.EscapeMarkdown`.
