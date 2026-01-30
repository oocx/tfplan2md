# UAT Report: Static Code Analysis Integration

**Status:** ❌ FAILED
**Date:** 2026-01-31
**UAT PR (GitHub):** #37 (Closed)
**UAT PR (AzDO):** #47 (Abandoned)

## Summary
The UAT session for Feature #056 failed due to rendering bugs and missing functionality required for practical use.

## Detailed Findings

### 1. Broken Markdown Tables (Unmatched Findings)
The "Unmatched Findings" table fails to render correctly as a markdown table. Instead, it displays as raw text blocks within the table header area. This is likely caused by multi-line messages (Trivy format) not being properly escaped or joined with `<br/>` tags.
**Evidence:** 
- Screenshot provided by Maintainer showing raw text blocks starting with `| ⚠️ High | Artifact: main.tf`.

### 2. Missing Resource-Level Findings
During the UAT run with `comprehensive-demo/plan.json`, all findings ended up in the "Unmatched Findings" section. 
**Cause:** The provided SARIF test data (Checkov/Trivy/TFLint) refers to AWS resources (`aws_s3_bucket.example`, `aws_security_group.public`), while the Plan JSON contains Azure resources (`azurerm_key_vault.main`, `azurerm_storage_account.logs`).
**Requirement:** Test artifacts must use aligned data to verify resource-level mapping works as intended.

### 3. Lack of Detail in Unmatched Findings
Findings in the "Unmatched Findings" section only show the file name. They lack line numbers, column information, or any logical context that would allow a user to locate the issue.
**Expectation:** Include line/column or resource/attribute hints in the "Finding" column.

### 4. Visibility of Resource Findings
Resources that have findings are wrapped in a collapsed `<details>` block by default.
**Expectation:** If a resource has security/quality findings, the `<details>` block must be automatically expanded (i.e., `<details open>`) to ensure visibility.

## Repro Steps
1. Generate the problematic artifact:
   ```bash
   dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- \
     examples/comprehensive-demo/plan.json \
     --code-analysis-results "examples/static-analysis/*.sarif" \
     --output artifacts/static-analysis-comprehensive-demo.md
   ```
2. View the resulting markdown in a renderer (GitHub/VS Code).
3. Observe broken tables in "Other Findings" and all items missing from resource blocks.

## Next Steps
- Handoff to **Developer** to fix the rendering and mapping issues.
- Update test data to include Azure SARIF findings that match the comprehensive demo.
- Implement "open" state for details blocks containing findings.
