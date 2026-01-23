# UAT Test Plan: Mutation Testing

## Goal
Verify that mutation testing is correctly integrated, produces actionable reports locally, and creates clear, informative GitHub issues for periodic tracking.

## Artifacts
**Artifact to use:** `StrykerOutput/` (Local) and GitHub Issues (CI)

**Creation Instructions (if new artifact needed):**
- **Command:** `scripts/mutation-test.sh --target Parsing`
- **Rationale:** Verifies the local workflow and report generation for a critical code path.

## Test Steps
1. Run the `scripts/mutation-test.sh` script locally for the `Parsing` target.
2. Inspect the generated HTML report.
3. Manually trigger the "Mutation Testing" workflow in GitHub Actions.
4. Verify the creation of a new GitHub Issue with the run results.

## Validation Instructions (Test Description)

### 1. Local HTML Report
**Specific Elements:**
- Open the HTML report in `StrykerOutput/reports/mutation-report.html`.
- Drill down into `src/tools/Oocx.TfPlan2Md/Parsing/TerraformPlanParser.cs`.

**Expected Outcome:**
- You should see a list of mutants.
- Clicking on a mutant should show the code change (e.g., `==` changed to `!=`).
- Status should be clearly marked (Killed, Survived, No Coverage).

### 2. GitHub Issue Reporting
**Specific Elements:**
- Check the newly created issue title: `Mutation Testing Report - [YYYY-MM-DD]`.
- Check the issue body for the **Mutation Score** and **Sumary Table**.

**Expected Outcome:**
- The issue should summarize the run results clearly.
- It should mention the names of top survived mutants or files with low scores.
- It must contain a link or instruction to download the `.zip` artifact from the **Actions** run.

**Before/After Context:**
- Previously, we only had code coverage (lines hit). Now, we have a measure of "assertion strength" – if we break the logic, do the tests actually fail?

## Success Criteria
- [ ] Local script runs successfully and opens/generates a report.
- [ ] GitHub workflow creates a **New Issue** for every run (as specifically requested by Maintainer).
- [ ] Mutation reports are informative enough to guide a developer to fix a weak test.
