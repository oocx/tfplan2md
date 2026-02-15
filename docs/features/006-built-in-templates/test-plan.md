# Test Plan: Built-in Templates

## Status

✅ **All tests implemented and passing** - 145 tests passing, 0 failures.

## Overview

This test plan covers the "Built-in Templates" feature, which introduces a new "summary" template, enhances the `--template` option to support built-in template names, and adds support for parsing the plan timestamp.

Reference: [specification.md](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `TerraformPlan` has `Timestamp` property | TC-01 | Unit |
| `ReportModel` has `Timestamp` property | TC-01 | Unit |
| `MarkdownRenderer` maps timestamp | TC-01 | Unit |
| `MarkdownRenderer` resolves built-in "summary" | TC-02 | Unit |
| `MarkdownRenderer` resolves built-in "default" | TC-03 | Unit |
| `MarkdownRenderer` resolves file path | TC-04 | Unit |
| `MarkdownRenderer` throws on unknown template | TC-05 | Unit |
| Summary template includes version, timestamp, table | TC-06 | Unit |
| CLI accepts `--template summary` | TC-07 | Integration |
| CLI lists built-in templates in help | TC-08 | Unit/Integration |

## Test Cases

### TC-01: Parse Timestamp from Plan JSON

**Type:** Unit
**Class:** `Parsing/TerraformPlanParserTests.cs`

**Description:**
Verify that the `TerraformPlanParser` correctly extracts the `timestamp` field from the JSON input and that it propagates to the `ReportModel`.

**Preconditions:**
- A JSON plan string containing a `timestamp` field.

**Test Steps:**
1. Call `TerraformPlanParser.Parse` with the JSON.
2. Verify `TerraformPlan.Timestamp` matches the input.
3. Build `ReportModel` from the plan.
4. Verify `ReportModel.Timestamp` matches.

**Expected Result:**
Timestamp is correctly parsed and mapped.

---

### TC-02: Resolve Built-in "summary" Template

**Type:** Unit
**Class:** `MarkdownGeneration/MarkdownRendererTests.cs`

**Description:**
Verify that passing "summary" to the renderer uses the built-in summary template.

**Preconditions:**
- `MarkdownRenderer` is initialized.

**Test Steps:**
1. Call `MarkdownRenderer.Render` with a model and template name "summary".
2. Verify the output contains summary-specific content (e.g., "Terraform Plan Summary").

**Expected Result:**
The summary template is used.

---

### TC-03: Resolve Built-in "default" Template

**Type:** Unit
**Class:** `MarkdownGeneration/MarkdownRendererTests.cs`

**Description:**
Verify that passing "default" to the renderer uses the built-in default template.

**Preconditions:**
- `MarkdownRenderer` is initialized.

**Test Steps:**
1. Call `MarkdownRenderer.Render` with a model and template name "default".
2. Verify the output matches the default template output.

**Expected Result:**
The default template is used.

---

### TC-04: Resolve Custom Template File

**Type:** Unit
**Class:** `MarkdownGeneration/MarkdownRendererTests.cs`

**Description:**
Verify that passing a file path to the renderer uses the file content.

**Preconditions:**
- A temporary template file exists.

**Test Steps:**
1. Call `MarkdownRenderer.Render` with a model and the file path.
2. Verify the output matches the custom template logic.

**Expected Result:**
The custom template file is used.

---

### TC-05: Unknown Template Throws Exception

**Type:** Unit
**Class:** `MarkdownGeneration/MarkdownRendererTests.cs`

**Description:**
Verify that passing an unknown name (that is not a file) throws an exception listing available templates.

**Preconditions:**
- `MarkdownRenderer` is initialized.

**Test Steps:**
1. Call `MarkdownRenderer.Render` with "nonexistent-template".
2. Catch the exception.

**Expected Result:**
Exception message contains "Template 'nonexistent-template' not found" and lists "default" and "summary".

---

### TC-06: Summary Template Content

**Type:** Unit
**Class:** `MarkdownGeneration/MarkdownRendererTests.cs`

**Description:**
Verify the rendered output of the summary template contains all required elements.

**Preconditions:**
- A `ReportModel` with known version, timestamp, and changes.

**Test Steps:**
1. Render the model using the "summary" template.
2. Check for Terraform version string.
3. Check for Timestamp string.
4. Check for Summary table headers and content.
5. Ensure "Resource Changes" section is NOT present.

**Expected Result:**
Output contains header, version, timestamp, summary table, and NO detailed changes.

---

### TC-07: CLI Integration - Summary Template

**Type:** Integration
**Class:** `Docker/DockerIntegrationTests.cs` (or similar CLI test)

**Description:**
Verify that the CLI accepts `--template summary` and produces the expected output.

**Preconditions:**
- Docker image built (or local CLI executable).

**Test Steps:**
1. Run `tfplan2md plan.json --template summary`.
2. Capture stdout.

**Expected Result:**
Output matches the summary template format.

---

### TC-08: CLI Help Text

**Type:** Unit
**Class:** `CLI/HelpTextProviderTests.cs` (if exists, else `CliParserTests.cs`)

**Description:**
Verify that `--help` output lists the built-in templates.

**Preconditions:**
- None.

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()`.
2. Check output for "Built-in templates:" and "summary".

**Expected Result:**
Help text includes built-in template documentation.

## Test Data Requirements

- Update `TestData/azurerm-azuredevops-plan.json` (and others) to include a `timestamp` field (e.g., `"timestamp": "2025-12-20T10:00:00Z"`).

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| JSON missing `timestamp` | `Timestamp` property is null/empty, template handles it gracefully (e.g. prints empty string or "N/A") | TC-09 |
| Template name matches file name exactly | Built-in takes precedence (as per spec) | TC-02 |

## Open Questions

None.
