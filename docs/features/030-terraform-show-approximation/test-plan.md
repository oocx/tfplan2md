# Test Plan: Terraform Show Output Approximation Tool

## Overview

This test plan covers the `TerraformShowRenderer` tool, which generates output approximating `terraform show` from Terraform plan JSON files. The goal is to ensure high fidelity with real Terraform output, including ANSI colors and proper formatting for all resource operations.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| CLI accepts `--input`, `--output`, `--no-color`, `--help`, `--version` | TC-01, TC-02, TC-03 | Unit (CLI) |
| Tool reads Terraform JSON plan files (format version 1.2+) | TC-04, TC-05 | Unit (App) |
| Output approximates `terraform show` format with high fidelity | TC-06, TC-07, TC-08, TC-09 | Integration |
| ANSI color codes match Terraform's conventions | TC-06, TC-07 | Integration |
| All plan operations supported: create, update, delete, replace, read, no-op | TC-08 | Integration |
| Output includes legend, summary, detailed breakdown, and footer | TC-06, TC-07, TC-08, TC-09 | Integration |
| Works with comprehensive-demo plan.json file | TC-09 | Integration |
| Clear error messages for invalid input or file errors | TC-10, TC-11, TC-12 | Unit (App) |
| Help text documents all CLI options | TC-02 | Unit (CLI) |
| Version output shows tool version | TC-03 | Unit (CLI) |
| `--no-color` flag strips ANSI codes | TC-13 | Integration |
| Output to stdout works when `--output` is omitted | TC-14 | Unit (App) |

## User Acceptance Scenarios

### Scenario 1: Authentic Terminal Output Generation

**User Goal**: Generate a text file that looks exactly like `terraform show` output to be used in a website comparison.

**Test PR Context**:
- **GitHub**: Verify rendering of the generated text in a code block.
- **Azure DevOps**: Verify rendering of the generated text in a code block.

**Expected Output**:
- The output should contain the standard Terraform legend.
- Resource changes should be prefixed with `+`, `~`, `-`, etc.
- ANSI color codes should be present (visible as escape sequences in raw text, or rendered if the viewer supports it).
- The summary line `Plan: X to add, Y to change, Z to destroy.` should be present.

**Success Criteria**:
- [ ] Output structure matches `terraform show` baseline.
- [ ] Colors are applied to the correct elements (green for `+`, yellow for `~`, etc.).
- [ ] Indentation is consistent with Terraform's style.

---

### Scenario 2: Plain Text Output for Documentation

**User Goal**: Generate a plain text version of the plan for documentation where ANSI codes are not supported.

**Test PR Context**:
- **GitHub**: Verify the `--no-color` output in a PR comment.

**Expected Output**:
- Same content as Scenario 1 but without any `\u001b[` escape sequences.

**Success Criteria**:
- [ ] No ANSI escape codes in the output.
- [ ] All text content and structure remains intact.

## Test Cases

### TC-01: CLI_ValidArguments_ParsesCorrectly

**Type:** Unit

**Description:**
Verifies that the CLI parser correctly handles valid combinations of `--input` and `--output`.

**Expected Result:**
`CliOptions` object contains the correct paths.

---

### TC-02: CLI_HelpFlag_DisplaysHelp

**Type:** Unit

**Description:**
Verifies that `--help` or `-h` displays the help text and exits with code 0.

---

### TC-03: CLI_VersionFlag_DisplaysVersion

**Type:** Unit

**Description:**
Verifies that `--version` or `-v` displays the tool version and exits with code 0.

---

### TC-04: App_UnsupportedFormatVersion_ReturnsExitCode4

**Type:** Unit

**Description:**
Verifies that a plan JSON with `format_version` < 1.2 results in exit code 4 and a clear error message.

---

### TC-05: App_InvalidJson_ReturnsExitCode3

**Type:** Unit

**Description:**
Verifies that an invalid JSON file results in exit code 3 and a clear error message.

---

### TC-06: Renderer_Regression_Plan1_MatchesRealTerraform

**Type:** Integration

**Description:**
Verifies that the tool generates exactly the same output as real `terraform show` for `plan1.json`.

**Test Data:**
- Input: `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.json`
- Expected: `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.txt`

---

### TC-07: Renderer_Regression_Plan2_MatchesRealTerraform

**Type:** Integration

**Description:**
Verifies that the tool generates exactly the same output as real `terraform show` for `plan2.json`.

**Test Data:**
- Input: `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json`
- Expected: `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.txt`

---

### TC-08: Renderer_AllOperations_MatchesBaseline

**Type:** Integration

**Description:**
Renders a plan containing `create`, `update`, `delete`, `replace`, `read`, and `no-op` operations.

**Test Data:**
A custom JSON plan containing all these operations.

---

### TC-09: Renderer_ComprehensiveDemo_MatchesBaseline

**Type:** Integration

**Description:**
Renders the `comprehensive-demo` plan and verifies the output structure.

**Test Data:**
`examples/comprehensive-demo/plan.json`

---

### TC-10: App_InputFileNotFound_ReturnsExitCode2

**Type:** Unit

**Description:**
Verifies that a non-existent input file results in exit code 2.

---

### TC-11: App_OutputWriteFailure_ReturnsExitCode2

**Type:** Unit

**Description:**
Verifies that failure to write to the output path (e.g., permission denied) results in exit code 2.

---

### TC-12: CLI_MissingRequiredInput_ReturnsExitCode1

**Type:** Unit

**Description:**
Verifies that omitting the `--input` argument results in exit code 1.

---

### TC-13: Renderer_NoColorFlag_StripsAnsiCodes

**Type:** Integration

**Description:**
Verifies that the `--no-color` flag produces output without any ANSI escape sequences.

---

### TC-14: App_NoOutputFlag_WritesToStdout

**Type:** Unit

**Description:**
Verifies that when `--output` is omitted, the renderer writes to the standard output stream.

## Test Data Requirements

- `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.json` - Terraform plan JSON
- `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan1.txt` - Expected `terraform show` output for plan1
- `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.json` - Terraform plan JSON with replacement
- `tests/Oocx.TfPlan2Md.Tests/TestData/TerraformShow/plan2.txt` - Expected `terraform show` output for plan2
- `examples/comprehensive-demo/plan.json` (Existing)
- `tests/Oocx.TfPlan2Md.Tests/TestData/unsupported-version-plan.json` (New) - Plan with `format_version: "1.1"`

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty `resource_changes` | Shows legend and "Plan: 0 to add, 0 to change, 0 to destroy." | TC-08 |
| Sensitive values | Shows `(sensitive value)` | TC-09 |
| Unknown values | Shows `(known after apply)` | TC-06 |
| Action reason present | Shows the reason in parentheses below the resource header | TC-08 |

## Non-Functional Tests

- **Performance**: The tool should process large plans (e.g., 100+ resources) in under 2 seconds.
- **Error Handling**: All exceptions should be caught and reported as "Error: <message>" to stderr.
