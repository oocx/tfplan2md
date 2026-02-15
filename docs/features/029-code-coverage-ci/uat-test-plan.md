# UAT Test Plan: Code Coverage Reporting and Enforcement in CI

## Goal
Verify that code coverage metrics are correctly collected, reported in PRs, and enforced as quality gates in GitHub Actions.

## Artifacts
**Artifact to use:** `artifacts/uat-coverage-demo.md` (A placeholder or simulated report if needed, but primarily verified via CI runs).

**Creation Instructions (if new artifact needed):**
- Simulated Cobertura XML with varying coverage levels.
- PRs in the repository with and without test coverage for new code.

## Test Steps
1. Create a PR in `oocx/tfplan2md` with a new class and no associated tests.
2. Observe the GitHub Actions PR validation run.
3. Verify the coverage summary in the PR comment and job summary.
4. Verify that the coverage check fails.
5. Add tests for the new class and verify the coverage check now passes.
6. Add the `coverage-override` label and verify the check passes even if coverage is low.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- **PR Comment:** Look for a "Code Coverage Summary" table.
- **GitHub Check Details:** Verify the "Code Coverage" check status and details.
- **README Badge:** Verify the badge in the repo root after merge.

**Exact Attributes:**
- **Line Coverage %**: Should match the percentage of lines covered in the Cobertura report.
- **Branch Coverage %**: Should match the branch coverage reported.
- **Thresholds**: Verify they match the values defined in the CI configuration.

**Expected Outcome:**
- A clear, readable summary is posted to the PR.
- Low coverage blocks the PR unless overridden.
- Badge and history files are updated automatically.

**Before/After Context:**
- Previously, coverage was not tracked or enforced in CI. This feature provides a safety net against regressions and makes the testing health visible to all contributors.
