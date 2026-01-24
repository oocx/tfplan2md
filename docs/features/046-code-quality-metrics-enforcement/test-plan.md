# Test Plan: Code Quality Metrics Enforcement

## Overview

This test plan covers the enforcement of code quality metrics (cyclomatic complexity, maintainability index, line length, and file length) as specified in [specification.md](specification.md) and [architecture.md](architecture.md).

The primary goal is to ensure that code quality guidelines are automatically enforced during build and CI, while existing violations are managed via baselines and incrementally fixed through refactoring.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `ScribanHelpers.AzApi.cs` split into ≤4 partial files, each under 300 lines | TC-01 | Static Analysis |
| `ReportModel.cs` split into separate class files, each under 300 lines | TC-02 | Static Analysis |
| `VariableGroupViewModelFactory.cs` refactored to under 400 lines | TC-03 | Static Analysis |
| `ResourceSummaryBuilder.cs` refactored to under 400 lines | TC-04 | Static Analysis |
| `AzureRoleDefinitionMapper.Roles.cs` refactored to under 400 lines | TC-05 | Static Analysis |
| CA1502 configured with threshold=15, severity=error | TC-06 | Build |
| .editorconfig configured with max_line_length = 160, IDE0055=error | TC-07 | Build |
| CA1505/CA1506 configured with threshold=20, severity=error | TC-08 | Build |
| Build fails when thresholds are exceeded (unbaselined) | TC-09 | Build |
| All existing tests pass after refactoring | TC-10 | Unit/Integration |
| CI build enforces all quality thresholds | TC-11 | CI |
| Test files are exempt from complexity/maintainability rules | TC-12 | Build |

## User Acceptance Scenarios

> **Purpose**: These scenarios help verify that common developer and maintainer workflows around code quality are supported and clear.

### Scenario 1: Violating Quality Metrics

**User Goal**: Understand why a build failed due to quality regressions.

**Test PR Context**:
- **GitHub**: Verify PR build fails when a new method with high complexity (>15) is introduced.
- **Azure DevOps**: Verify PR build fails when a new file exceeds line length limits (160 chars).

**Expected Output**:
- Build failure with clear error codes (CA1502, IDE0055, etc.).
- Line and file location of the violation accurately reported.

**Success Criteria**:
- [ ] Build fails as expected.
- [ ] Error message is actionable for the developer.

---

### Scenario 2: Refactoring Large Files

**User Goal**: Verify that large files have been refactored according to guidelines without breaking functionality.

**Test PR Context**:
- **GitHub/Azure DevOps**: Review PRs for the 5 targeted files.

**Expected Output**:
- File line counts are within the specified limits.
- Functional tests pass.
- Code remains readable and logically organized.

**Success Criteria**:
- [ ] Line counts verified via `wc -l` or IDE.
- [ ] All previous unit and integration tests are green.

## Test Cases

### TC-01 to TC-05: File Length Verification

**Type:** Static Analysis

**Description:**
Verify that the files targeted for refactoring meet the line count requirements.

**Preconditions:**
- Refactoring task for the specific file is completed.

**Test Steps:**
1. Run `wc -l <file_path>` for each refactored file.
2. For `ScribanHelpers.AzApi.cs`, verify it's split into partial files and total count per file is < 300.
3. For others, verify count is within target (300-400 lines).

**Expected Result:**
Each file (or partial) is below the maximum allowed line count.

---

### TC-06 to TC-08: Configuration Verification

**Type:** Build

**Description:**
Verify that `.editorconfig` and `CodeMetricsConfig.txt` are correctly configured.

**Preconditions:**
- Configuration changes are applied.

**Test Steps:**
1. Inspect `.editorconfig` for `max_line_length`, `CA1502`, `CA1505`, `CA1506` severities.
2. Inspect `CodeMetricsConfig.txt` for threshold values.
3. Verify `src/Directory.Build.props` includes `CodeMetricsConfig.txt` as `AdditionalFiles`.

**Expected Result:**
Settings match the architecture decision.

---

### TC-09: Enforcement Verification (Fail Case)

**Type:** Build

**Description:**
Verify that new violations cause the build to fail.

**Preconditions:**
- Code base is currently clean or baseline is in place.

**Test Steps:**
1. Create a new dummy C# file with a method that has cyclomatic complexity > 15.
2. Create a line longer than 160 characters.
3. Run `dotnet build`.

**Expected Result:**
Build fails with errors for each violation.

---

### TC-10: Regression Testing

**Type:** Unit/Integration

**Description:**
Verify that refactoring didn't break existing functionality.

**Preconditions:**
- All refactoring completed.

**Test Steps:**
1. Run `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`.

**Expected Result:**
All tests pass.

---

### TC-11: CI Enforcement

**Type:** CI

**Description:**
Verify that CI blocks PRs with violations.

**Preconditions:**
- PR created with a quality violation.

**Test Steps:**
1. Push a branch with an unbaselined violation.
2. Observe CI status on GitHub.

**Expected Result:**
CI build fails and blocks merge.

---

### TC-12: Test File Exemption

**Type:** Build

**Description:**
Verify that test files are not subjected to the same complexity/maintainability rules.

**Steps:**
1. Introduce a complex method in a test file (e.g. `src/tests/.../SomeTest.cs`) with complexity > 15.
2. Run `dotnet build`.

**Expected Result:**
Build succeeds (or at least doesn't fail due to CA1502 in that file).

## Test Data Requirements

No specific JSON test data files are needed for these infrastructure/refactoring tests, as they primarily use the source code itself.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| String literals exceeding line length | Flagged by IDE0055, requires manual suppression/approval if justified | TC-09 |
| Very complex state machines | Flagged by CA1502, requires suppression with justification if justified | TC-09 |
| Partial classes split across files | Each file's line count is evaluated separately | TC-01 |

## Non-Functional Tests

- **Build Performance**: Monitor if adding these analyzers significantly increases build time.
- **Developer Feedback**: Ensure error messages provide enough context to fix the issue.

## Open Questions

1. **Baseline maintenance**: How often should we re-generate or "clean up" the baseline? (Recommended: after each major refactoring PR).
