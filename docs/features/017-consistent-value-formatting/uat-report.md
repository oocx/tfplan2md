# UAT Report: Consistent Value Formatting

**Date:** 2025-12-25

## UAT Result

**Status:** Failed (requires developer changes)

This UAT validated markdown rendering in real PR comments on both GitHub and Azure DevOps. Maintainer feedback indicates there are still rendering/formatting issues that must be addressed in the generator (not by UAT iteration).

## Platforms

### GitHub

- PR: https://github.com/oocx/tfplan2md/pull/81
- Result: Feedback requiring rework (not approved)

**Maintainer feedback (verbatim):**

> The diff for large values in module.security.azurerm_key_vault_secret.audit_policy is not working as expected. I was expecting to see a standard diff with +/-, but only see this:
>
> ```
> line1: allow
> line2: log old activity
> line2: log critical activity
> line3: end
> ```

**Developer-facing interpretation:**

- GitHub UAT expects **standard diff** rendering for *large values* to be visually unambiguous in a PR comment, with explicit `+`/`-` markers (typically via a fenced code block using `diff`).
- The observed output indicates GitHub is seeing a non-standard representation for the *large value diff* in at least this resource, likely due to the wrong diff mode being posted for GitHub, or due to the content being rendered without the `diff` fence/markers.

**Action items for developer:**

- Ensure the GitHub UAT artifact uses **standard-diff** for large values (e.g., `--large-value-format standard-diff`).
- Ensure that the produced markdown for large values contains an explicit `diff` fenced block with `+`/`-` markers for changes (and that this survives GitHub rendering in PR comments).
- Confirm that GitHub UAT posting cannot accidentally review an inline-diff artifact when standard-diff is required.

### Azure DevOps

- PR: https://dev.azure.com/oocx/test/_git/test/pullrequest/9
- Result: Feedback requiring rework (not approved)

**Maintainer feedback (verbatim):**

> In the summary "Network Security Group: nsg-app", the name of the nsg is an attribute value and therefore should be rendered in a code block

**Developer-facing interpretation:**

- In Azure DevOps, the summary line for NSG currently renders the NSG name as plain text in at least one place.
- This violates the “data values are code-formatted” rule for consistent value formatting.

**Action items for developer:**

- Ensure NSG names shown in **summary lines** are rendered as code (backticks) consistently.
- Verify this specifically for the summary entry text of the form `Network Security Group: <name>`.

## Artifacts Used During UAT

- Initial UAT mistakenly posted the minimal simulation artifact (not suitable for real UAT): `artifacts/uat-minimal.md`
- Comprehensive demo report was posted afterward.

During iteration, two variants were generated to support platform expectations:

- Inline diff (Azure DevOps default): `--large-value-format inline-diff`
- Standard diff (GitHub expectation): `--large-value-format standard-diff`

## Cleanup

- GitHub PR #81 was closed.
- Azure DevOps PR #9 was abandoned.
- UAT branch `uat/simulation-2025-12-25` was deleted.

## Notes

- This document intentionally focuses on **user-visible rendering issues** observed in real PR comment environments.
- Once the developer fixes the issues above, UAT should be re-run on both platforms using the comprehensive demo artifact.
