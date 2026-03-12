# Work Protocol: Missing NSG Rule In Generated Report

**Work Item:** `docs/issues/112-missing-nsg-rule-report/`
**Branch:** `fix/112-missing-nsg-rule-report`
**Workflow Type:** Bug Fix
**Created:** 2026-03-12

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst

- **Date:** 2026-03-12
- **Summary:** Reproduced the missing NSG rule with the external Terraform plan, traced the loss to the specialized AzureRM NSG renderer, and identified the regression as post-`v1.29.0` with the current renderer file introduced in commit `47980b8c`.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/analysis.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** Terminal sessions were intermittently closing for longer combined commands, so reproduction was completed with smaller commands and file-based output checks.

### Architect

- **Date:** 2026-03-12
- **Summary:** Reviewed the issue analysis, core architecture, and the AzureRM NSG rendering path. Concluded that no new architecture is required because the canonical parent-child merge model already exists; the fix should make the specialized NSG renderer consume merged `ChildResourceGroups` instead of rebuilding rule rows solely from parent state.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/architecture.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The expected `specification.md` artifact was not present in the work item folder, so the architecture review used `analysis.md` as the source artifact.

### Quality Engineer

- **Date:** 2026-03-12
- **Summary:** Created an issue-scoped automated test plan and UAT plan focused on preventing invisible NSG child-rule regressions. Mapped the derived requirements from `analysis.md` and `architecture.md` to integration, unit, regression, and UAT coverage for separate child `create`, `update`, `delete`, and mixed inline plus separate scenarios.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/test-plan.md`, `docs/issues/112-missing-nsg-rule-report/uat-test-plan.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The work item still has no `specification.md`, so the test plan explicitly treats `analysis.md` and `architecture.md` as the authoritative requirement sources. Repository testing guidance now reflects TUnit rather than the older xUnit wording in the agent mode prompt, so the plan follows current repo practice.

### Task Planner

- **Date:** 2026-03-12
- **Summary:** Reviewed the issue analysis, architecture review, automated test plan, and UAT plan, then translated them into four implementation-ready user stories covering regression tests, the canonical NSG renderer fix, merge and summary consistency, and focused UAT artifact preparation. Added an explicit coverage matrix so every documented scenario and test case maps to at least one story.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/tasks.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The work item has no `specification.md`, so the plan uses `analysis.md` as the requirements source and cross-checks it against `architecture.md`, `test-plan.md`, and `uat-test-plan.md`.

### Developer

- **Date:** 2026-03-12
- **Summary:** Added issue-scoped regression coverage for no-op NSG parents with separate child-rule create, update, delete, and mixed inline-plus-separate scenarios, then fixed the AzureRM NSG renderer to consume merged `Security Rules` child groups whenever separate NSG rule resources are attached to the parent. This keeps summaries, filtering, and rendered table rows aligned while preserving the existing inline-only renderer fallback.
- **Artifacts Produced:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs`, `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/NsgMergedSecurityRulesRenderer.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/NsgMergedSecurityRuleRenderingTests.cs`, `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/nsg-with-separate-rule-updates.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The initial inline implementation tripped the repository CA1506 coupling threshold on `NsgRenderer`, so the merged-table rendering logic was extracted into a dedicated helper. Snapshot verification also required regenerating the affected baseline because the fixed renderer now emits the standard markdown separator row for merged child tables.

### Code Reviewer

- **Date:** 2026-03-12
- **Summary:** Reviewed the Issue 112 renderer fix against the issue analysis, architecture, tasks, test plan, and UAT plan. The implementation is aligned with the documented root cause and passes the automated test and coverage checks, but the review requests changes because required workflow artifacts and verification steps are still incomplete.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/code-review.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** Docker was unavailable in the review environment, which blocked the mandatory Docker build check and prevented completion of the comprehensive-demo regeneration plus markdownlint verification.

### Technical Writer

- **Date:** 2026-03-12
- **Summary:** Reviewed the issue analysis, architecture, test plan, UAT plan, and implemented NSG renderer behavior, then added the missing focused UAT artifacts for separate child-rule create, update, delete, and mixed inline-plus-separate scenarios. The new artifact is aligned with the merged `Security Rules` rendering contract and gives reviewers a compact report that makes missing-row regressions obvious without relying on the larger demo set.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/uat-plan.json`, `docs/issues/112-missing-nsg-rule-report/uat-plan.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** I could not regenerate the markdown artifact from the CLI in this environment, so the focused `uat-plan.md` was authored to match the implemented renderer behavior and the existing issue requirements, but it remains unverified against a live tool run.

### Developer (Rework)

- **Date:** 2026-03-12
- **Summary:** Addressed the remaining code-review feedback by extracting repeated test literals into documented private constants, then verified the regression suite still passes. I also re-ran the previously blocked verification steps in the current environment: the focused UAT markdown now comes from a live `tfplan2md` run, markdownlint passes for `artifacts/comprehensive-demo.md`, and the Docker image builds successfully when using the repository's actual Docker configuration (`docker build -t tfplan2md:local -f src/Dockerfile .`).
- **Artifacts Produced:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/NsgMergedSecurityRuleRenderingTests.cs`, `docs/issues/112-missing-nsg-rule-report/uat-plan.md`, `docs/issues/112-missing-nsg-rule-report/uat-test-plan.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The original review assumptions for Docker were stale in this workspace. Docker itself is available, but the repository Dockerfile lives at `src/Dockerfile`, so root-level `docker build -t tfplan2md:local .` fails even though the correct build command succeeds.

### Code Reviewer (Re-review)

- **Date:** 2026-03-12
- **Summary:** Re-reviewed Issue 112 after the developer rework. Verified the focused UAT artifacts exist and match the UAT plan, the full solution test suite passes when using the repository wrapper with a longer timeout, coverage exceeds thresholds, the comprehensive demo regenerates and passes markdownlint, and the Docker image builds successfully with the repository Dockerfile. The implementation is aligned with the issue analysis and architecture, but the review still requests one final process fix because the branch changes a snapshot file without a commit message containing the required `SNAPSHOT_UPDATE_OK` token.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/code-review.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** The default 120-second wrapper timeout was too short for the full test suite in this environment, so verification required re-running `scripts/test-with-timeout.sh` with `--timeout-seconds 300`.

### Code Reviewer (Approval)

- **Date:** 2026-03-12
- **Summary:** Verified that the remaining snapshot-policy requirement is being satisfied by the follow-up branch commit containing `SNAPSHOT_UPDATE_OK`. With that final process item addressed, the review is approved and the change is ready for UAT handoff because it affects generated markdown output.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/code-review.md`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
- **Problems Encountered:** None.

### Release Manager

- **Date:** 2026-03-12
- **Summary:** Reviewed the issue analysis, architecture, test plan, UAT plan, approved code review, and release prerequisites. The Maintainer explicitly waived UAT for this release, so release is proceeding as a documented process exception for a user-visible markdown rendering fix. Added release notes and a focused screenshot showing the restored separate NSG rule row.
- **Artifacts Produced:** `docs/issues/112-missing-nsg-rule-report/release-notes.md`, `docs/issues/112-missing-nsg-rule-report/nsg-separate-rule-fix.png`, `docs/issues/112-missing-nsg-rule-report/work-protocol.md`
