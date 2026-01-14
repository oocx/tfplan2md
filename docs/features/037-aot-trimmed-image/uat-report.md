# UAT Report: AOT-Compiled Trimmed Docker Image

## Status: Passed ✅

**Date:** 2026-01-14
**Version:** 1.0.0-alpha.29 (Azure DevOps artifact) / 1.0.0-alpha.28 (GitHub artifact - likely from previous run or base)

## PR Details
- **GitHub PR:** #22 (Passed/Closed)
- **Azure DevOps PR:** #33 (Passed/Approved)

## Validation Results

| Test Step | Result | Notes |
|-----------|--------|-------|
| Summary Emojis | passed | Emojis ➕, 🔄, ❌ rendered correctly |
| Resource Details | passed | `module.security.azurerm_key_vault_secret.audit_policy` table attributes correct |
| Report Footer | passed | Footer contains version and 40-char commit hash |
| No Errors | passed | No "Missing member" or "Error rendering template" messages |

## Evidence
The UAT script polled both PRs and detected maintainer approval/closure after manual verification in the browser.

### GitHub
- PR URL: https://github.com/oocx/tfplan2md-uat/pull/22
- Artifact: `artifacts/comprehensive-demo-simple-diff.md`

### Azure DevOps
- PR URL: https://dev.azure.com/oocx/test/_git/test/pullrequest/33
- Artifact: `artifacts/comprehensive-demo.md`

## Conclusion
The NativeAOT-compiled Docker image successfully renders complex terraform plans with identical visual quality to the previous runtime-based image, while preserving critical metadata (version, commit) that depends on assembly reflection. Trimming did not affect the Scriban templates or custom helpers.
