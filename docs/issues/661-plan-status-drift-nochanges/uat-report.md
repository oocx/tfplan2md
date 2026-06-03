# UAT Report — Issue 661 (Plan Status / Drift No-Changes)

- **Date:** 2026-06-03
- **Run Type:** Real UAT (`scripts/uat-run.sh --create-only`)
- **Status:** Awaiting Maintainer Approval

## Scope Executed

1. Validate no false non-applyable warning on effective no-change plans.
2. Validate warning still appears on actionable non-applyable plans.
3. Validate drift-no-op plans do not show drift noise.

## Feature-Specific Artifacts Used

1. `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/status-not-applyable.md`
2. `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/status-not-applyable-actionable.md`
3. `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/drift-no-op-entries.md`

## UAT PRs Created

- **GitHub PR:** https://github.com/oocx/tfplan2md-uat/pull/125
- **Azure DevOps PR:** https://dev.azure.com/oocx/test/_git/test/pullrequest/112

## Notes

- Comprehensive demo regression artifacts were appended automatically by `scripts/uat-run.sh`.
- Final pass/fail decision requires maintainer review:
  - GitHub: apply `uat-approved` / `uat-rejected`
  - Azure DevOps: Approve / Reject vote
