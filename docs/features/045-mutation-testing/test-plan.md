# Test Plan: Mutation Testing

## Overview

This test plan defines the strategy for verifying the implementation of mutation testing in the tfplan2md repository. The goal is to ensure that Stryker.NET is correctly integrated with TUnit and that the reporting and CI/CD workflows function as expected.

Reference Specification: [specification.md](specification.md)
Reference Architecture: [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Stryker.NET-TUnit Compatibility | TC-01 | Integration |
| Scoped Mutation Runs (Parsing/Summaries) | TC-02 | Integration |
| Baseline Mutation Score (≥75%) | TC-03 | Quality |
| Local Script Execution (`scripts/mutation-test.sh`) | TC-04 | Integration |
| CI Workflow Integration (Periodic/Manual) | TC-05 | Workflow |
| Issue-Based Reporting (New issue per run) | TC-06 | Workflow |
| Survived Mutation Resolution | TC-07 | Process |

## User Acceptance Scenarios

> **Purpose**: Verify that mutation testing provides actionable insights and integrates cleanly with the developer workflow and CI/CD reporting.

### Scenario 1: Local Mutation Run

**User Goal**: A developer wants to check the effectiveness of tests for changes in the `Parsing/` logic.

**Test PR Context**:
- **GitHub**: N/A (Local execution verification)
- **Azure DevOps**: N/A

**Expected Output**:
- The script `scripts/mutation-test.sh --target Parsing` executes successfully.
- An HTML report is generated locally in `StrykerOutput/`.
- The report correctly identifies killed and survived mutations in the `Parsing/` directory.

**Success Criteria**:
- [ ] Script returns exit code 0 (or correct code based on thresholds if implemented).
- [ ] Report is accessible and interpretable.

---

### Scenario 2: CI Periodic Reporting

**User Goal**: Maintainers want to see a weekly snapshot of test effectiveness.

**Test PR Context**:
- **GitHub**: Verify that a new issue is created in the repository after the manual workflow dispatch or scheduled run.

**Expected Output**:
- A new GitHub Issue is created with a title like `Mutation Testing Report - [Date]`.
- The issue body contains:
  - Mutation score.
  - Summary counts (Killed, Survived, Timeout).
  - A list of top survived mutants.
  - Instructions on how to download the full HTML report artifact.

**Success Criteria**:
- [ ] Issue is created automatically.
- [ ] Artifact is available for download in the workflow run.
- [ ] Information matches the JSON output produced by Stryker.

## Test Cases

### TC-01: Stryker.NET-TUnit Compatibility

**Type:** Integration

**Description:**
Verify that Stryker.NET can successfully drive TUnit tests via `dotnet test` within the `src/tests/Oocx.TfPlan2Md.TUnit/` project.

**Preconditions:**
- Stryker.NET installed as a local tool.
- Project builds successfully.

**Test Steps:**
1. Run `dotnet stryker` with a minimal scope (e.g., a single file in `Parsing/`).
2. Observe if Stryker correctly identifies and runs the TUnit tests.
3. Verify that mutants are successfully tested (killed or survived).

**Expected Result:**
Stryker completes the run without unexpected execution errors related to the test framework.

---

### TC-02: Scoped Mutation Runs

**Type:** Integration

**Description:**
Verify that the mutation testing can be limited to the critical paths `Parsing/` and `MarkdownGeneration/Summaries/`.

**Test Steps:**
1. Run `scripts/mutation-test.sh --target Parsing`.
2. Run `scripts/mutation-test.sh --target Summaries`.
3. Check the reports to ensure only mutations in the target directories were applied.

**Expected Result:**
Mutation coverage is isolated to the requested directories.

---

### TC-03: Baseline Mutation Score

**Type:** Quality

**Description:**
Verify that the combined mutation score for the critical paths meets the initial target of ≥75%.

**Test Steps:**
1. Run a full mutation test on both critical paths.
2. Calculate the average/combined mutation score from the results.

**Expected Result:**
Mutation score is ≥75%.

---

### TC-04: Local Script Execution

**Type:** Integration

**Description:**
Verify the usability of the `scripts/mutation-test.sh` wrapper.

**Test Steps:**
1. Run `scripts/mutation-test.sh --help` (if implemented) or check argument handling.
2. Run with various valid and invalid `--target` values.

**Expected Result:**
The script handles arguments correctly and provides helpful feedback on failure.

---

### TC-05: CI Workflow Integration

**Type:** Workflow

**Description:**
Verify the manual dispatch and artifact persistence in the GitHub Actions workflow.

**Test Steps:**
1. Manually trigger the `Mutation Testing` workflow from the Actions tab.
2. Wait for completion (must be ≤ 35 minutes).
3. Verify that the `StrykerOutput` artifact is attached to the run.

**Expected Result:**
Workflow completes successfully within the time budget and artifacts are stored.

---

### TC-06: Issue-Based Reporting (New Issue per Run)

**Type:** Workflow

**Description:**
Verify that the CI workflow creates a new GitHub Issue for every run.

**Test Steps:**
1. Trigger the mutation testing workflow twice.
2. Check the repository issues.

**Expected Result:**
Two distinct issues are created, each containing the results of its respective run.

---

### TC-07: Survived Mutation Resolution

**Type:** Process

**Description:**
Verify the end-to-end "Kill a Mutant" workflow.

**Test Steps:**
1. Identify a survived mutation from a previous run.
2. Add a new TUnit test case or improve an existing one to cover that code path.
3. Run the mutation test again for that specific file.
4. Verify that the mutation is now "Killed".

**Expected Result:**
The mutation score improves and the specific mutant is marked as killed.

## Test Data Requirements

- Existing `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` files remain relevant.
- Stryker requires the production code and test projects to be in a buildable state.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Mutation causes infinite loop | Stryker kills it as a "Timeout" mutant using its internal timeout mechanism | TC-01 |
| No tests touch a file in scope | 0% mutation score for that file; all mutants survive | TC-02 |
| Build failure during mutation | Stryker reports a compilation error and exits with non-zero code | TC-01 |
| GitHub API rate limit during issue creation | Workflow should handle or log the error; retry logic if possible | TC-06 |

## Non-Functional Tests

- **Performance**: Ensure a full run on critical paths takes ≤ 30 minutes.
- **Maintainability**: Verify that `stryker-config.json` is clean and documented.

## Open Questions

- None at this stage.
