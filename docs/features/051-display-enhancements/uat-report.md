# UAT Report: Display Enhancements

**Status:** ❌ Failed
**Date:** 2026-01-27
**Build:** b7d5c62

## Summary
UAT was performed on GitHub and Azure DevOps using the `artifacts/apim-display-enhancements-demo.md` artifact. The Maintainer rejected the PR due to significant regressions and incomplete implementation of the feature.

## Test Environment
- **GitHub PR:** [#31](https://github.com/oocx/tfplan2md-uat/pull/31)
- **Azure DevOps PR:** [#42](https://dev.azure.com/oocx/test/_git/test/pullrequest/42)

## Detailed Findings

### 1. Versioning & Regressions (CRITICAL)
- **Outdated Codebase:** The generated report appears to be based on an outdated version of the code.
- **Missing Features:** Summary attributes for `azurerm_subscription` (which were previously implemented and merged to `main`) are missing from this report.
- **APIM Operation Summaries:** The formatting for APIM operation summaries is reported as outdated. 
  - *Example found:* `azurerm_api_management_api_operation get_profile Get Profile — get-profile users apim-demo in 📁 rg-tfplan2md-demo`

### 2. Icon Consistency (FAILED)
The subscription icon (🔑) is missing in several contexts where it was expected:
- Key Vault resources: `Key Vault kv-tfplan2md in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012`
- Resource Group summary: `📁 rg-old in subscription 12345678-1234-1234-1234-123456789012`
- Resource Group summary: `📁 rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012`

### 3. Missing Emoji (Unrelated/Cleanup)
- **Firewall Rules:** Rule name attributes in firewall network rule collections lack the `🆔` emoji.

### 4. Syntax Highlighting (PASSED)
- Syntax highlighting for XML/JSON blocks worked correctly.

## Maintainer Feedback Summary
> - this does not seem to be based on the latest code. We added summary attributes to azurerm_subscription, they are now gone. 
> - subscription does not always have the subscription icon
> - operation summary is oudated
> - in general, this looks like this was generated with an outdated version
> - rule name attributes in firewall network rule collection lack 🆔 emoji
> - Suspicion that code review was pushed to a temporary UAT branch and might have polluted the context.

## Next Steps
- **Handoff to Developer** to:
  - Rebase the feature branch on the latest `main` to restore `azurerm_subscription` summary attributes.
  - Fix the APIM operation summary formatting to match the latest spec.
  - Ensure the subscription icon (🔑) is applied consistently across all resource types and parent-child summary strings.
  - Add the `🆔` emoji to firewall network rule names.
  - Regenerate the UAT artifact (`artifacts/apim-display-enhancements-demo.md`) after these fixes.
