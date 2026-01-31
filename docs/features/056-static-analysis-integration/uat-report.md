# UAT Report: Static Code Analysis Integration

**Status:** FAILED ❌

**Date:** 2026-01-31 01:37:36 UTC
**Artifact Used:** [artifacts/static-analysis-comprehensive-demo.md](artifacts/static-analysis-comprehensive-demo.md)

## PRs Reviewed

- **GitHub:** [#39](https://github.com/oocx/tfplan2md-uat/pull/39) (Failed)
- **Azure DevOps:** [#49](https://dev.azure.com/oocx/test/_git/test/pullrequest/49) (Failed)

## Issues Found

### 1. Missing 'Resource Types' column in Code Analysis Summary
- **Description:** The "Code Analysis Summary" table only contains "Severity" and "Count" columns. It should consistently include a "Resource Types" column, just like the main Terraform plan summary table at the top of the report.
- **Expected:**
  `| Severity | Count | Resource Types |`
- **Actual:**
  `| Severity | Count |`
- **Evidence:** See [artifacts/static-analysis-comprehensive-demo.md](artifacts/static-analysis-comprehensive-demo.md#L20-L28)

## Validation results

- [x] **Summary counts:** Correct counts rendered.
- [x] **Tool List:** Checkov, tflint, and Trivy versions correctly displayed.
- [x] **Resource Mapping:** Findings for resources correctly shown in separate tables.
- [x] **Attribute Highlighting:** Attributes marked with severity icons in change tables.
- [x] **Remediation Links:** Verified clickable links lead to correct documentation.
- [x] **Visual Consistency:** Rendering is clear across both platforms.
