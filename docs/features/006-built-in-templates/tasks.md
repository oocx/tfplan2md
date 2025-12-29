# Tasks: Built-in Templates

## Status

✅ **All tasks completed** - Feature fully implemented with comprehensive test coverage.

## Overview

Implement support for multiple built-in templates, starting with a new "summary" template. This includes updating the data models to support plan timestamps, implementing a template resolution strategy that prioritizes built-in templates, and updating the CLI to provide better feedback and documentation for these templates.

Reference: [specification.md](specification.md) and [architecture.md](architecture.md).

## Tasks

### Task 1: Update Data Models for Timestamp Support

**Priority:** High

**Description:**
Add support for the `timestamp` field from the Terraform plan JSON to the internal data models so it can be used in templates.

**Acceptance Criteria:**
- [x] `TerraformPlan` record in [src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs](src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs) has a `Timestamp` property with `[JsonPropertyName("timestamp")]`.
- [x] `ReportModel` in [src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs](src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs) has a `Timestamp` property.
- [x] `MarkdownRenderer` correctly maps the timestamp from `TerraformPlan` to `ReportModel`.
- [x] Unit tests verify that the timestamp is correctly parsed from JSON.

**Dependencies:** None

---

### Task 2: Implement Built-in Template Registry and Resolution

**Priority:** High

**Description:**
Enhance `MarkdownRenderer` to manage a registry of built-in templates and implement the resolution logic for the `--template` option.

**Acceptance Criteria:**
- [x] `MarkdownRenderer` has a registry of built-in templates (e.g., "default", "summary").
- [x] A new method `ResolveTemplate(string templateNameOrPath)` is implemented in `MarkdownRenderer`.
- [x] Resolution logic:
    1. Check if `templateNameOrPath` matches a built-in template name.
    2. If not, check if it's a valid file path.
    3. If neither, throw a descriptive exception listing available built-in templates.
- [x] `MarkdownRenderer.Render` methods are updated to use the resolved template.
- [x] Unit tests verify the resolution logic for built-in names, file paths, and error cases.

**Dependencies:** Task 1

---

### Task 3: Create the Summary Template

**Priority:** Medium

**Description:**
Create the new "summary" Scriban template as an embedded resource.

**Acceptance Criteria:**
- [x] New template file created at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/summary.sbn`.
- [x] Template includes:
    - Terraform Version
    - Plan Timestamp
    - Summary Table (Action, Count, Resource Types)
- [x] Template is configured as an `EmbeddedResource` in the `.csproj` file.
- [x] `MarkdownRenderer` includes "summary" in its built-in template registry.

**Dependencies:** Task 2

---

### Task 4: Update CLI and Help Text

**Priority:** Medium

**Description:**
Update the CLI to support the new template resolution and provide helpful information in the help output.

**Acceptance Criteria:**
- [x] `HelpTextProvider.cs` is updated to list available built-in templates and provide examples.
- [x] `Program.cs` is updated to handle the template resolution and display the error message from `MarkdownRenderer` if resolution fails.
- [x] CLI help output (`--help`) correctly displays the new information.

**Dependencies:** Task 3

---

### Task 5: Update Documentation

**Priority:** Low

**Description:**
Update the project documentation to reflect the new built-in templates feature.

**Acceptance Criteria:**
- [x] `README.md` is updated to describe the `--template` option's support for built-in names.
- [x] `README.md` includes an example of using the `summary` template.
- [x] `docs/features.md` is updated to document the built-in templates feature.

**Dependencies:** Task 4

---

## Implementation Order

1. **Task 1** - Foundational work to make the timestamp available.
2. **Task 2** - Core logic for template resolution.
3. **Task 3** - Implementation of the requested feature (summary template).
4. **Task 4** - Exposing the feature to the user via CLI.
5. **Task 5** - Finalizing documentation.

## Open Questions

None.
