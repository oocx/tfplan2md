# UAT Report: Azure DevOps Dark Theme Support

## UAT Result

**Status:** Passed

**GitHub PR:** #21 (Closed)
**Azure DevOps PR:** #32 (Approved)

## Validation Summary

**Test Description:**
Verify that Terraform plan reports in Azure DevOps render with theme-appropriate borders in both Light and Dark themes. Check random resources like azurerm_key_vault (uses default template) and azurerm_firewall_network_rule_collection (uses resource-specific template). Expected outcome for Azure DevOps Dark Theme: borders around resource containers should be subtle and blend well with the dark background (NOT bright white). Azure DevOps Light Theme: borders should remain visible using ADO native palette variable. GitHub: borders absent as expected.

**Results:**
- **GitHub**: Verified that borders are absent as expected (as GitHub strips inline styles on `<details>`).
- **Azure DevOps**: Verified that borders use the native ADO palette variable `--palette-neutral-10`, which correctly adjusts for Light and Dark themes.
- **Resources checked**: `azurerm_key_vault` (default template) and `azurerm_firewall_network_rule_collection` (resource-specific template) both show consistent styling.

