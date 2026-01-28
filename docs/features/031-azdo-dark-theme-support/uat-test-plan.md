# UAT Test Plan: Azure DevOps Dark Theme Support

## Goal
Verify that Terraform plan reports in Azure DevOps render with theme-appropriate borders in both Light and Dark themes.

## Artifacts
**Artifact to use:** `artifacts/comprehensive-demo.md`

**Creation Instructions:**
1. Use the `scripts/generate-demo-artifacts.sh` script (or run manually) to generate the comprehensive demo if not already up-to-date.
2. Ensure the generated Markdown contains the new border color string: `rgb(var(--palette-neutral-10, 153, 153, 153))`

**Rationale:**
The `comprehensive-demo.md` includes various resource types and triggers multiple templates (both default and resource-specific), providing a complete overview of the styling changes.

## Test Steps
1. Run UAT using the `UAT Tester` agent for **Azure DevOps**.
2. Run UAT using the `UAT Tester` agent for **GitHub** (to ensure no regressions).
3. Verify the generated PR comments.

## Validation Instructions (Test Description)

**Specific Resources/Sections:**
- Check random resources like `azurerm_key_vault` (uses default template).
- Check `azurerm_firewall_network_rule_collection` (uses resource-specific template).

**Exact Attributes:**
- The `<details>` element's `style` attribute.

**Expected Outcome:**
- **Azure DevOps (Dark Theme)**: The borders around resource containers should be subtle and blend well with the dark background. They should NOT be bright white or high-contrast gray.
- **Azure DevOps (Light Theme)**: The borders should remain visible and professional, similar to previous versions but using the ADO native palette variable.
- **GitHub**: Borders will be absent (as GitHub strips inline styles on `<details>`), which is the expected current behavior.

**Before/After Context:**
- **Before**: Reports in ADO dark mode had `#f0f0f0` (near-white) borders which were very bright and distracting against the dark background.
- **After**: Reports use `--palette-neutral-10`, which ADO automatically switches to a dark neutral color in dark mode.
