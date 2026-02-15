# UAT Report: Terraform Import and Moved Blocks

**Date:** 2026-02-01
**Tester:** Copilot (UAT Tester Agent)
**Result:** ✅ PASS

## Summary
The Terraform `import` and `moved` block rendering was validated on both GitHub and Azure DevOps. The Refactoring Summary table and resource-level annotations displayed correctly as per the test plan.

## Platform Results

| Platform | Status | PR Link |
| -------- | ------ | ------- |
| GitHub | ✅ PASS | https://github.com/oocx/tfplan2md-uat/pull/47 |
| Azure DevOps | ✅ PASS | https://dev.azure.com/oocx/test/_git/test/pullrequest/56 |

## Validation Details

### 1. Refactoring Summary Table
- **GitHub:** Table rendered at the end of the report. Imports sorted before Moves. Ready status icons (✅) and "Already moved" (⚠️) icons were correctly displayed.
- **AzDO:** Table rendered correctly with Markdown formatting preserved.

### 2. Resource-Level Annotations
- **GitHub:** Summary tags contained `📥 Imported` and `🔀 Moved from` annotations with correct icons and code formatting for IDs/addresses.
- **AzDO:** Layout remained stable and readable within the summary tag.

## Evidence
Artifact used: `artifacts/refactoring-demo.md`
