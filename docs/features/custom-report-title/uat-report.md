# UAT Report: Custom Report Title

**Date (UTC):** 2025-12-27T13:11:20Z

## Context

- **Feature:** Custom Report Title (`--report-title`)
- **Branch under test:** `uat/feature-custom-report-headline-uat-20251227130310`
- **Commit:** `3f585bda5a72aa62c3fbaa5415c52cd0e79a465b` (`fix: address report-title review feedback`)
- **Reference:** [test plan](test-plan.md), [UAT test plan](uat-test-plan.md)

## Goal

Validate that markdown rendering in **GitHub** and **Azure DevOps** PR UIs correctly displays:

1. A custom report title containing special characters (Scenario 1)
2. The default report title when the option is omitted (Scenario 2)

## Expected

From [test-plan.md](test-plan.md):

- **Scenario 1:** H1 heading renders literally as:
  - `Drift Detection # Results [Production]`
  - Specifically: `#` and `[` `]` should render as characters, not markdown syntax.

- **Scenario 2:** H1 heading renders as the default:
  - `Terraform Plan Summary`

## Actual

The UAT PR comments were populated using the default UAT artifacts:

- `artifacts/comprehensive-demo-standard-diff.md` (GitHub default)
- `artifacts/comprehensive-demo.md` (AzDO default)

Those artifacts contain the default headings (e.g., `# Terraform Plan Report`) and **do not include any output generated with `--report-title`**, so Scenario 1 (custom title rendering) could not be validated.

## Root Cause

The UAT workflow posts a **pre-generated markdown artifact** as PR comments.

Because `--report-title` is a *runtime input*, validating this feature requires an artifact that was generated with `--report-title` set to a title that includes special characters.

## Rework / Fix

A dedicated UAT artifact has been generated locally which includes the custom report title:

- Artifact: `artifacts/uat-custom-report-title.md`
- Generated with:
  - `--template summary`
  - `--report-title "Drift Detection # Results [Production]"`

The first line of the artifact is:

```markdown
# Drift Detection \# Results \[Production\]
```

This should render in GitHub/AzDO as:

```markdown
# Drift Detection # Results [Production]
```

## Recommended Next UAT Run

Run UAT using the dedicated artifact (so Scenario 1 is actually exercised):

```bash
scripts/uat-run.sh artifacts/uat-custom-report-title.md "Custom Report Title UAT: Verify the H1 heading renders as 'Drift Detection # Results [Production]' (literal # and [ ]) in GitHub and Azure DevOps PR comment rendering."
```

(Optional) Run a second UAT PR using a default artifact (or no `--report-title`) to validate Scenario 2.

## Outcome

- UAT **did not validate** the custom title rendering due to using artifacts without the custom title.
- Existing UAT PRs should be **closed/abandoned**, then UAT should be re-run using `artifacts/uat-custom-report-title.md`.
