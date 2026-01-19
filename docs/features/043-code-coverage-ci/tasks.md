# Tasks: Code Coverage Reporting and Enforcement in CI

## Overview

Implement automated code coverage collection, reporting, and enforcement in the CI pipeline (GitHub Actions). Coverage results will be visible in PRs, enforced against thresholds, and tracked over time.

Reference: [docs/features/043-code-coverage-ci/specification.md](docs/features/043-code-coverage-ci/specification.md)

## Tasks

### Task 1: Audit and Remove Code Coverage Exclusions

**Priority:** High

**Description:**
Ensure coverage reflects all production code as requested in the specification. Remove any `[ExcludeFromCodeCoverage]` attributes from production code.

**Acceptance Criteria:**
- [x] No `[ExcludeFromCodeCoverage]` attributes exist in `src/Oocx.TfPlan2Md` (excluding test projects).
- [x] Documentation updated to reflect "no exclusions" policy if necessary.

**Dependencies:** None

---

### Task 2: Implement Coverage Collection in CI

**Priority:** High

**Description:**
Update `pr-validation.yml` to collect coverage using the Microsoft Testing Platform / TUnit coverage collector and produce a Cobertura report.

**Acceptance Criteria:**
- [ ] `dotnet test` (or equivalent TUnit runner) in `pr-validation.yml` produces a Cobertura XML report.
- [ ] `dotnet test` also produces an HTML report.
- [ ] HTML coverage report is uploaded as a GitHub Actions artifact.
- [ ] Cobertura XML report is uploaded as a (short-term) GitHub Actions artifact for debugging.
- [ ] Performance impact is measured and confirmed to be within limits (< 2 minutes).

**Dependencies:** Task 1

---

### Task 3: Implement Metrics Parser and Threshold Enforcement

**Priority:** High

**Description:**
Create a script or tool to parse the Cobertura XML report, extract line/branch coverage percentages, compare them against thresholds, and fail the CI job if they are below.

**Acceptance Criteria:**
- [ ] Tool/script accurately parses Cobertura XML.
- [ ] Initial thresholds are set based on current measured coverage on `main`.
- [ ] CI job fails if line coverage or branch coverage falls below defined thresholds.
- [ ] Exit codes are used correctly to signal success/failure to GitHub Actions.

**Dependencies:** Task 2

---

### Task 4: Implement PR Visibility (Job Summary and Comments)

**Priority:** Medium

**Description:**
Format the coverage metrics into a markdown table and publish it to the GitHub Job Summary and as a PR comment.

**Acceptance Criteria:**
- [ ] GitHub Job Summary (`GITHUB_STEP_SUMMARY`) contains a "Code Coverage Summary" table with line and branch metrics.
- [ ] A PR comment is created/updated with the same summary.
- [ ] Summary includes links to the detailed HTML report artifact.
- [ ] Summary clearly indicates which thresholds passed or failed.

**Dependencies:** Task 3

---

### Task 5: Implement Maintainer Override Mechanism

**Priority:** Medium

**Description:**
Update the CI workflow to respect the `coverage-override` label. If present, the coverage check should succeed even if thresholds are not met.

**Acceptance Criteria:**
- [ ] Coverage check passes if the `coverage-override` label is present on the PR.
- [ ] The PR summary/comment explicitly states that an override is active.
- [ ] The label check works correctly for both internal and fork PRs (respecting token limitations).

**Dependencies:** Task 4

---

### Task 6: Implement Badge and Trend Tracking

**Priority:** Medium

**Description:**
Implement automatic updates for the coverage badge and history data. This should only run for internal PRs to avoid token permission issues with forks.

**Acceptance Criteria:**
- [ ] `assets/coverage-badge.svg` is updated based on the latest coverage from the PR validation run.
- [ ] `docs/coverage/history.json` (or similar) is updated with new metrics.
- [ ] For internal PRs, these changes are committed back to the branch by the CI.
- [ ] README.md links to the badge correctly.

**Dependencies:** Task 4

---

### Task 7: UAT and Final Validation

**Priority:** High

**Description:**
Perform User Acceptance Testing to ensure all scenarios from the test plan are covered.

**Acceptance Criteria:**
- [ ] Scenario 1: Coverage regressions are caught (verified via UAT PR).
- [ ] Scenario 2: Coverage failure override works (verified via UAT PR).
- [ ] Scenario 3: Coverage badge and history update correctly (verified upon merge).
- [ ] All test cases (TC-01 through TC-14) from `test-plan.md` are executed and pass.

**Dependencies:** All previous tasks

## Implementation Order

1. **Task 1 & Task 2 (Partial)**: Audit exclusions and get initial coverage numbers from CI to establish the baseline.
2. **Task 3**: Build the enforcement logic and integrate into CI.
3. **Task 4**: Add visibility to PRs.
4. **Task 5**: Add the override mechanism.
5. **Task 6**: Add badge and trends.
6. **Task 7**: Final UAT and documentation updates.

## Open Questions

- Should we use a dedicated tool like `ReportGenerator` for parsing Cobertura and generating the summary, or a custom script? (Recommendation: `ReportGenerator` or a lightweight shell/python script to minimize dependencies).
