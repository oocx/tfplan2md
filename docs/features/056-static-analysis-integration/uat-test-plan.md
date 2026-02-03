# UAT Test Plan: Static Code Analysis Integration

## Goal
Verify that static code analysis findings from security and quality tools (Checkov, Trivy, TFLint) render correctly in GitHub and Azure DevOps PR comments, with proper severity highlighting and resource mapping.

## Artifacts
**Artifact to use:** `artifacts/static-analysis-comprehensive-demo.md`

**Creation Instructions (if new artifact needed):**
- **Source Plan:** `examples/comprehensive-demo/plan.json`
- **SARIF Files:** 
  - `examples/static-analysis/checkov.sarif`
  - `examples/static-analysis/trivy.sarif`
  - `examples/static-analysis/tflint.sarif`
- **Command:** 
  ```bash
  tfplan2md examples/comprehensive-demo/plan.json \
    --code-analysis-results "examples/static-analysis/*.sarif" \
    --out artifacts/static-analysis-comprehensive-demo.md
  ```
- **Rationale:** This combines a standard Terraform plan with findings from multiple popular tools to test summary aggregation, resource mapping, and visual prominence of overlapping findings.

## Test Steps
1. Run UAT using the `UAT Tester` agent for both GitHub and Azure DevOps.
2. Verify the generated PR comments.

## Validation Instructions (Test Description)

### 1. Code Analysis Summary
**Specific Resources/Sections:** `Code Analysis Summary` section at the top.
**Expected Outcome:** 
- Displays a table with counts for Critical, High, Medium, Low, and Informational.
- Shows "Tools Used: Checkov, Trivy, TFLint" (with versions if available).
- Displays a status icon (e.g., 🚨 or ⚠️) reflecting the highest severity found.

### 2. Resource-Level Findings (Variant C)
**Specific Resources/Sections:** `azurerm_key_vault.vault` and `azurerm_storage_account.storage`.
**Expected Outcome:**
- Findings are displayed in a "Security & Quality Findings" table under each resource.
- Check for `azurerm_key_vault.vault`: Should show "Public access allowed" (Critical/Checkov).
- Check for `azurerm_storage_account.storage`: Should show "Encryption not enabled" (High/Trivy).
- Severity level icons (🚨, ⚠️, ℹ️) are visible and aligned.
- "Details" or "Fix Guide" links are clickable and lead to the tool's documentation.

### 3. Other Findings Section
**Specific Resources/Sections:** `Other Findings` section at the end of the report.
**Expected Outcome:**
- `Module: network`: Shows findings specifically mapped to the network module.
- `Unmatched Findings`: Shows findings that couldn't be mapped to any resource address in the plan (e.g., global terraform version warnings).

### 4. Visual Prominence
**Expected Outcome:** 
- Critical findings should have more prominent icons (🚨) compared to Low (ℹ️) findings.
- The summary status line should be bold if critical items exist.

### 5. Platform Consistency
**Expected Outcome:**
- Tables render correctly in both GitHub and Azure DevOps.
- Emojis/Icons render properly on both platforms.
- No broken markdown syntax or empty tables.
