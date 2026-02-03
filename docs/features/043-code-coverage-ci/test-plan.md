# Test Plan: Code Coverage Reporting and Enforcement in CI

## Overview

This test plan defines the strategy for verifying the automated code coverage collection, reporting, and enforcement feature in the CI pipeline. It ensures that coverage is tracked correctly, thresholds are enforced, and maintainer overrides work as expected.

Reference: [docs/features/043-code-coverage-ci/specification.md](docs/features/043-code-coverage-ci/specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Coverage metrics collected on every PR | TC-01 | Integration (CI) |
| Line and branch coverage thresholds enforced as required PR checks | TC-02, TC-03 | Unit & Integration |
| Coverage summary visible in PR interface | TC-04, TC-05 | Unit & UAT |
| Detailed coverage reports accessible via one-click link from PR | TC-06 | Integration (CI) |
| Failed coverage checks block PR merge (but can be overridden) | TC-07, TC-08 | Integration (CI) |
| Coverage badge displayed in README showing current main branch coverage | TC-09, TC-10 | Unit & UAT |
| Historical coverage data preserved and accessible for trend analysis | TC-11 | Unit |
| Initial thresholds set based on measured current coverage | TC-12 | Manual/Process |
| Coverage applies uniformly to all production code in `src/` directory | TC-13 | Static Analysis |
| Coverage collection adds less than 2 minutes to CI pipeline duration | TC-14 | Performance |

## User Acceptance Scenarios

### Scenario 1: Coverage Regressions are Caught

**User Goal**: Ensure that a PR reducing code coverage below the threshold is flagged and blocked.

**Test PR Context**:
- **GitHub**: Create a PR with new code that has no test coverage.
- **Azure DevOps**: N/A (Mainly GitHub Actions focus, but verify summary if posted to AzDo).

**Expected Output**:
- The "Code Coverage" check fails in GitHub Actions.
- A PR comment or job summary clearly shows the drop in coverage and the violated threshold.

**Success Criteria**:
- [ ] PR check fails.
- [ ] Detailed report shows missing coverage in the new code.

### Scenario 2: Coverage Failure Override

**User Goal**: Allow a maintainer to bypass a coverage failure for legitimate reasons.

**Test PR Context**:
- Start with a PR that fails the coverage check (from Scenario 1).
- Apply the `coverage-override` label.

**Expected Output**:
- The "Code Coverage" check passes (or shows success status) despite failing thresholds.
- The summary indicates that an override is active.

**Success Criteria**:
- [ ] PR becomes mergeable (from coverage perspective).
- [ ] Override status is documented in the check details.

### Scenario 3: Coverage Badge and History Update

**User Goal**: Verify that the README badge and history data are updated upon merge.

**Test PR Context**:
- Merge a PR that changes coverage.

**Expected Output**:
- `assets/coverage-badge.svg` updated with new percentage.
- `docs/coverage/history.json` contains a new entry for the merged commit.

**Success Criteria**:
- [ ] Badge reflects correct coverage.
- [ ] History entry is accurate.

---

## Test Cases

### TC-01: Coverage Collection Triggering

**Type:** Integration (CI)

**Description:** Verifies that coverage is collected on every PR and main branch build.

**Preconditions:** PR is opened or commit pushed to `main`.

**Test Steps:**
1. Open a PR.
2. Verify that the `pr-validation.yml` workflow triggers and runs the coverage step.

**Expected Result:** Coverage analysis starts automatically.

---

### TC-02: Threshold Violation (Failure)

**Type:** Unit & Integration

**Description:** Verifies that coverage below the threshold causes a failure.

**Preconditions:** Thresholds set to 80% line, 80% branch. Plan produces 75% coverage.

**Test Steps:**
1. Run the threshold check logic with Cobertura report showing 75% coverage.
2. Verify the output status and exit code.

**Expected Result:** Logic returns 'Fail' and non-zero exit code.

---

### TC-03: Threshold Met (Success)

**Type:** Unit & Integration

**Description:** Verifies that coverage meeting or exceeding the threshold passes.

**Preconditions:** Thresholds set to 80% line. Plan produces 85% coverage.

**Test Steps:**
1. Run the threshold check logic with Cobertura report showing 85% coverage.

**Expected Result:** Logic returns 'Pass' and zero exit code.

---

### TC-04: PR Comment Generation

**Type:** Unit

**Description:** Verifies the formatting of the markdown coverage summary.

**Preconditions:** Coverage data: 85.5% line, 70.2% branch.

**Test Steps:**
1. Generate the markdown summary using the report generator.
2. Verify the content contains both line and branch metrics.

**Expected Result:** Markdown is correctly formatted and accurate.

---

### TC-05: Job Summary Visibility

**Type:** Integration (CI)

**Description:** Verifies that coverage summary appears in `GITHUB_STEP_SUMMARY`.

**Test Steps:**
1. Run the CI workflow.
2. Check the "Summary" tab of the GitHub Action run.

**Expected Result:** Coverage table is visible.

---

### TC-06: Artifact Upload

**Type:** Integration (CI)

**Description:** Verifies that the full HTML coverage report is uploaded as a workflow artifact.

**Test Steps:**
1. Run the CI workflow.
2. Check the "Artifacts" section of the run.

**Expected Result:** A zip/folder containing the HTML report is present.

---

### TC-07: PR Merge Block

**Type:** Integration (CI)

**Description:** Verifies that failed coverage prevents merging when designated as a required check.

**Test Steps:**
1. Create a PR with failing coverage.
2. Verify that the "Merge" button is disabled (assuming branch protection is configured).

**Expected Result:** Merge is blocked.

---

### TC-08: Label Override

**Type:** Integration (CI)

**Description:** Verifies that the `coverage-override` label bypasses enforcement.

**Test Steps:**
1. Add `coverage-override` label to a PR with failing coverage.
2. Re-run or wait for check to update.

**Expected Result:** Coverage check passes.

---

### TC-09: Badge Generation

**Type:** Unit

**Description:** Verifies that the SVG badge is generated correctly from metrics.

**Test Steps:**
1. Provide 95% coverage to the badge generator.
2. Verify the SVG content contains "95%" and appropriate color (e.g., green).

**Expected Result:** Valid SVG produced.

---

### TC-10: Badge Update in PR

**Type:** Integration (CI)

**Description:** Verifies that the CI commits the updated badge back to the PR branch for internal (non-fork) PRs.

**Preconditions:** Submit an internal PR (not from a fork) that changes coverage.

**Test Steps:**
1. Submit an internal PR.
2. Verify a new commit is added by the CI bot updating the badge.
3. Submit a fork PR and verify NO commit is added (only summary/status check).

**Expected Result:** `assets/coverage-badge.svg` is updated in internal PRs; fork PRs only get reporting without commits.

---

### TC-11: History Update

**Type:** Unit

**Description:** Verifies that `docs/coverage/history.json` is updated with new metrics.

**Test Steps:**
1. Run the history management logic with new metrics.
2. Verify the JSON file contains the new entry.

**Expected Result:** Valid JSON with appended data.

---

### TC-12: Initial Threshold Determination

**Type:** Process

**Description:** Verify that initial thresholds are set based on current main coverage.

**Test Steps:**
1. Measure coverage on `main`.
2. Ensure `pr-validation.yml` (or config file) uses these values as baseline.

**Expected Result:** Thresholds match actual baseline.

---

### TC-13: Production Code Scope

**Type:** Audit

**Description:** Verify all production code in `src/` is included and no `[ExcludeFromCodeCoverage]` remains.

**Test Steps:**
1. Search codebase for `[ExcludeFromCodeCoverage]`.
2. Verify the Cobertura report includes all production assemblies.

**Expected Result:** No exclusions found; all prod projects present.

---

### TC-14: CI Performance Impact

**Type:** Performance

**Description:** Verify coverage collection adds < 2 minutes to pipeline duration.

**Test Steps:**
1. Compare run duration with and without coverage.

**Expected Result:** Difference is less than 120 seconds.

## Test Data Requirements

- `Cobertura-Sample.xml`: A representative Cobertura report for unit testing the parser.
- `History-Baseline.json`: A sample history file for testing updates.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty Cobertura report | Graceful failure or 0% coverage reported | TC-02 |
| Malformed XML | Error message in logs, check fails | TC-02 |
| No branch coverage data | Handle null/missing branch data gracefully | TC-04 |
| Extremely long history file | Ensure performance remains acceptable | TC-11 |

## Open Questions

None. Decision made to skip automatic badge/history commits for fork PRs due to write access limitations, focusing on reporting via status checks and comments for those.
