# Test Plan: Comprehensive Demo

## Overview

This test plan validates the "Comprehensive Demo" feature, which consists of a static `plan.json` and related files designed to exercise all features of `tfplan2md`. The tests ensure that this demo plan is valid, contains the required scenarios, and renders correctly using the application.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Plan includes all action types | TC-01 | Integration |
| Plan includes module grouping | TC-01 | Integration |
| Plan includes sensitive values | TC-02 | Integration |
| Plan includes role assignments | TC-01 | Integration |
| Plan includes firewall rules | TC-01 | Integration |
| Plan renders with default template | TC-01 | Integration |
| Plan renders with summary template | TC-03 | Integration |
| Sensitive values revealed with flag | TC-02 | Integration |
| Principal mapping works | TC-01 | Integration |
| Demo files included in Docker image | TC-04 | E2E/Docker |

## Test Cases

### TC-01: Render_DefaultTemplate_ContainsAllFeatures

**Type:** Integration

**Description:**
Verifies that the comprehensive demo plan renders successfully with the default template and contains markers for all required features.

**Preconditions:**
- `examples/comprehensive-demo/plan.json` exists.
- `examples/comprehensive-demo/demo-principals.json` exists.

**Test Steps:**
1. Load the `plan.json` and `demo-principals.json` from the examples directory.
2. Execute `MarkdownRenderer` (or `Program.Main`) with these inputs.
3. Assert the output contains:
    - Module headers (e.g., `module.network`)
    - Action symbols (➕, 🔄, ♻️, ❌)
    - Specific resource types (e.g., `azurerm_firewall_network_rule_collection`)
    - Role assignment details (e.g., "Jane Doe")
    - Firewall rule diffs

**Expected Result:**
Rendering succeeds (exit code 0) and output contains all feature markers.

**Test Data:**
- `examples/comprehensive-demo/plan.json`
- `examples/comprehensive-demo/demo-principals.json`

---

### TC-02: Render_ShowSensitive_RevealsSecrets

**Type:** Integration

**Description:**
Verifies that the `--show-sensitive` flag correctly reveals masked values in the demo plan.

**Preconditions:**
- `examples/comprehensive-demo/plan.json` contains sensitive values.

**Test Steps:**
1. Execute `tfplan2md` with `--show-sensitive` against the demo plan.
2. Assert the output contains the plain text secret values (e.g., "super-secret-value").
3. Assert the output does NOT contain the masked string `(sensitive value)`.

**Expected Result:**
Secrets are visible in the output.

**Test Data:**
- `examples/comprehensive-demo/plan.json`

---

### TC-03: Render_SummaryTemplate_ShowsStats

**Type:** Integration

**Description:**
Verifies that the summary template renders the correct statistics for the demo plan.

**Preconditions:**
- `examples/comprehensive-demo/plan.json` exists.

**Test Steps:**
1. Execute `tfplan2md` with `--template summary` against the demo plan.
2. Assert the output contains the summary table.
3. Assert the counts match the expected values defined in the spec (e.g., "Create: 12").

**Expected Result:**
Summary table is rendered with correct counts.

**Test Data:**
- `examples/comprehensive-demo/plan.json`

---

### TC-04: Docker_Image_ContainsDemoFiles

**Type:** E2E/Docker

**Description:**
Verifies that the Docker image contains the comprehensive demo files at the expected location and they can be used to generate a report.

**Preconditions:**
- Docker image `tfplan2md:latest` (or test tag) is built.

**Test Steps:**
1. Run the Docker image with the command to list files in `/examples/comprehensive-demo/`.
2. Assert that `plan.json` and `demo-principals.json` are present.
3. Run the Docker image to generate a report using the internal files:
   `docker run --rm tfplan2md /examples/comprehensive-demo/plan.json --principals /examples/comprehensive-demo/demo-principals.json`
4. Assert that the command executes successfully (exit code 0) and outputs the report.

**Expected Result:**
Files exist and can be used from within the container.

**Test Data:**
- Built Docker image.

## Test Data Requirements

The feature *is* the test data. The files `examples/comprehensive-demo/plan.json` and `examples/comprehensive-demo/demo-principals.json` are required.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Missing principals file | Render without mapping (IDs only) | Covered by existing tests, but demo should ensure file exists |
| Invalid JSON in demo plan | Test should fail | TC-01 (implicit) |

## Non-Functional Tests

- **Performance**: The demo plan should render quickly (< 1s) to be useful as a "try it now" feature.

## Open Questions

- None.
