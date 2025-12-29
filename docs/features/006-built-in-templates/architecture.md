# ADR-004: Built-in Templates

## Status

Proposed

## Context

Users need a way to generate different types of reports (e.g., a summary-only report for notifications) without having to manage and provide external template files. The current implementation only supports a default embedded template or a custom external template file.

The user specifically requested a "summary" template that includes the Terraform version, plan timestamp, and a summary table of changes.

## Options Considered

### Option 1: Separate CLI Options for Built-in Templates
Introduce a new flag like `--builtin-template <name>` or specific flags like `--summary`.
- **Pros**: Clear distinction between built-in and custom templates.
- **Cons**: Increases CLI surface area. Confusing if both `--template` and `--builtin-template` are provided.

### Option 2: Reuse `--template` Option (Selected)
Allow the `--template` option to accept either a file path or a built-in template name.
- **Pros**: Simple, unified interface. Consistent with other tools (e.g., `dotnet new`).
- **Cons**: Potential ambiguity if a file is named "summary" (though unlikely to be an issue in practice).

## Decision

We will reuse the existing `--template` (and `-t`) option. The resolution logic will be:
1. Check if the value matches a known built-in template name (e.g., "default", "summary").
2. If not a built-in name, treat it as a file path and attempt to load it.
3. If the file does not exist, fail with an error message listing available built-in templates.

We will also add support for parsing the `timestamp` field from the Terraform plan JSON to include it in the summary template.

## Rationale

Reusing the existing option keeps the CLI simple and intuitive. The resolution order prioritizes built-in templates to ensure consistent behavior across environments, but still allows for custom templates.

## Consequences

### Positive
- Users can easily generate summary reports with `tfplan2md --template summary`.
- No need to distribute separate template files for common use cases.
- Extensible pattern for adding more built-in templates in the future.

### Negative
- A user cannot have a custom template file named exactly "summary" (without a path prefix like `./summary`) if they want to use the file instead of the built-in template. This is a minor edge case.

## Implementation Notes

### Components to Modify

1.  **`Oocx.TfPlan2Md.Parsing.TerraformPlan`**:
    -   Add `Timestamp` property to the record.

2.  **`Oocx.TfPlan2Md.MarkdownGeneration.ReportModel`**:
    -   Add `Timestamp` property.

3.  **`Oocx.TfPlan2Md.MarkdownGeneration.MarkdownRenderer`**:
    -   Add a registry of built-in templates (Dictionary mapping name to resource name).
    -   Implement `ResolveTemplate` method.
    -   Add the new `summary.sbn` embedded resource.

4.  **`Oocx.TfPlan2Md.CLI.HelpTextProvider`**:
    -   Update help text to list built-in templates.

5.  **`Oocx.TfPlan2Md.Program`**:
    -   Ensure the template option is passed correctly to the renderer.

### Template Content
The `summary.sbn` template will contain:
-   Terraform Version
-   Plan Timestamp
-   Summary Table (Action, Count, Resource Types)
