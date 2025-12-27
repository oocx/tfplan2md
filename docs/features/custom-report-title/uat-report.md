# UAT Report: Custom Report Title

**Date (UTC):** 2025-12-27T13:25:58Z

## Context

- **Feature:** Custom Report Title (`--report-title`)
- **Branch under test:** `uat/feature-custom-report-headline-uat-20251227130310`
- **Commit:** `3aa5b5242c29fca0a7e95fa1fcdb63e7ef5d40e3` (`test(uat): add custom report title UAT artifact`)
- **Reference:** [test plan](test-plan.md), [UAT test plan](uat-test-plan.md)

## Goal

Validate that markdown rendering in **GitHub** and **Azure DevOps** PR UIs correctly displays:

1. A custom report title containing special characters (Scenario 1)

## Expected

From [uat-test-plan.md](uat-test-plan.md):

- The H1 heading should display exactly as `Drift Detection # Results [Production]`.
- The `#` should render literally (not interpreted as markdown syntax).
- The `[` and `]` should render literally (not interpreted as a markdown link).

## UAT Runs

### Attempt 1 (Blocked)

The UAT PR comments were populated using the default UAT artifacts:

- `artifacts/comprehensive-demo-standard-diff.md` (GitHub default)
- `artifacts/comprehensive-demo.md` (AzDO default)

Those artifacts **did not include any output generated with `--report-title`**, so Scenario 1 (custom title rendering) could not be validated.

### Attempt 2 (Passed)

A dedicated UAT artifact was generated and used:

- Artifact: `artifacts/uat-custom-report-title.md`
- First line in artifact:

```markdown
# Drift Detection \# Results \[Production\]
```

UAT was executed using:

```bash
scripts/uat-run.sh artifacts/uat-custom-report-title.md "Custom Report Title UAT: Verify the H1 heading renders as 'Drift Detection # Results [Production]' (literal # and [ ]) in GitHub and Azure DevOps PR comment rendering."
```

PRs created:

- **GitHub:** https://github.com/oocx/tfplan2md-uat/pull/9
- **Azure DevOps:** https://dev.azure.com/oocx/test/_git/test/pullrequest/21

Approval detected:

- **GitHub:** PR was closed (treated as UAT passed by the polling script)
- **Azure DevOps:** reviewer approval detected (treated as UAT passed by the polling script)

## Outcome

- **Status:** Passed
- **Validated:** Scenario 1 (custom title renders correctly in GitHub + Azure DevOps PR comments)
- **Not validated in this UAT run:** Scenario 2 (default title when option is omitted)
