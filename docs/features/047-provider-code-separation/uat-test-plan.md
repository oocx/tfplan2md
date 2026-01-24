# UAT Test Plan: Provider Code Separation

## Goal
Verify that the restructuring of provider-specific code and templates doesn't break the rendering of reports in GitHub and Azure DevOps.

## Artifacts
**Artifacts to use:** 
1. `artifacts/comprehensive-demo-github.md`
2. `artifacts/comprehensive-demo-azdo.md`

**Creation Instructions:**
- **Source Plan:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-azuredevops-plan.json`
- **Commands:**
  - `tfplan2md --plan <source> --out artifacts/comprehensive-demo-github.md --render-target github`
  - `tfplan2md --plan <source> --out artifacts/comprehensive-demo-azdo.md --render-target azuredevops`
- **Rationale:** This plan contains resources from multiple providers (`azurerm`, `azuredevops`) and uses custom templates that were moved. Using the new `--render-target` flag verifies both the flag itself and the underlying platform-specific formatting.

## Test Steps
1. Run the UAT simulation using the `UAT Tester` agent.
2. Verify `artifacts/comprehensive-demo-github.md` on GitHub.
3. Verify `artifacts/comprehensive-demo-azdo.md` on Azure DevOps.

## Validation Instructions (Test Description)
**Specific Resources/Sections:**
- `azurerm_network_security_group` (Custom template: `azurerm/network_security_group`)
- `azurerm_role_assignment` (Custom template: `azurerm/role_assignment`)
- `azuredevops_variable_group` (Custom template: `azuredevops/variable_group`)
- Large attribute changes (e.g., descriptions or JSON bodies)

**Exact Attributes:**
- Verify that `azurerm_role_assignment` correctly displays the principal name and role name (this verifies that the shared Azure platform services moved to `Platforms/Azure` are still working).
- Verify that `azurerm_network_security_group` displays rules in the enhanced table format.
- **Diff Formatting Check:**
  - In `comprehensive-demo-github.md`, verify large attribute changes use the "Simple Diff" format (plain markdown with `+`/`-`).
  - In `comprehensive-demo-azdo.md`, verify large attribute changes use the "Inline Diff" format (HTML with colored backgrounds).

**Expected Outcome:**
- The report should look EXACTLY as it did before the restructuring (with the exception of using the new flag).
- All icons, semantic formatting, and provider-specific layouts must be preserved.
- The correct diff formatter must be applied based on the `--render-target` flag.
- No "Template not found" or "MarkdownRenderException" should occur.

**Before/After Context:**
- This is a structural refactoring. The output MUST be identical to the previous version. We are verifying that the new template loading mechanism correctly finds the moved templates in their new locations.
