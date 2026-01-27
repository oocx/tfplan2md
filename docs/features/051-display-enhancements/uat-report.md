# UAT Report: Display Enhancements

**Status:** ❌ Failed
**Date:** 2026-01-27
**Build:** b7d5c62

## Summary
UAT was performed on GitHub and Azure DevOps using the `artifacts/apim-display-enhancements-demo.md` artifact. The test failed due to regressions and inconsistent icon application.

## Test Environment
- **GitHub PR:** [#31](https://github.com/oocx/tfplan2md-uat/pull/31)
- **Azure DevOps PR:** [#42](https://dev.azure.com/oocx/test/_git/test/pullrequest/42)

## Results

### 1. Regressions (FAILED)
The Maintainer reported that the plan output is missing features from the latest main branch. Specifically, summary attributes for `azurerm_subscription` are missing in the generated report.

### 2. Subscription Icons (FAILED)
The subscription icon (🔑) is not consistently applied. For example, it was missing in the Key Vault section.

### 3. Syntax Highlighting (PASSED)
Syntax highlighting for XML and JSON blocks worked as expected in the PRs.

### 4. APIM Summaries (PASSED)
APIM summaries correctly included service names and operation titles where applicable.

## Feedback from Maintainer (Azure DevOps)
> - this does not seem to be based on the latest code. We added summary attributes to azurerm_subscription, they are now gone. 
> - subscription does not always have the subscription icon: Key Vault k

## Next Steps
- Handoff to Developer to rebase on main and fix the regression.
- Investigate why subscription icons are not showing for all resource types.
