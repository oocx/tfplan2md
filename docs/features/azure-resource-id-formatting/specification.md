# Feature: Universal Azure Resource ID Formatting

## Overview

Extend the human-readable Azure resource ID formatting (currently used only in role assignments) to all Azure resource IDs across azurerm resources. This improves readability by transforming long, cryptic resource paths into concise, structured displays.

## User Goals

- Quickly understand which Azure resources are being referenced without parsing long ID strings
- See resource IDs in the change table instead of hidden in large value sections
- Consistent formatting of resource IDs across all azurerm resource types
- Reduce cognitive load when reviewing infrastructure changes

## Scope

### In Scope

**Automatic Detection:**
- Pattern-based detection of Azure resource IDs in any attribute value
- Matches Azure resource ID structure:
  - Subscription: `/subscriptions/{guid}`
  - Resource Group: `/subscriptions/{guid}/resourceGroups/{name}`
  - Resource: `/subscriptions/{guid}/resourceGroups/{name}/providers/{provider}/{type}/{name}`
  - Management Group: `/providers/Microsoft.Management/managementGroups/{name}`
- No attribute name restrictions - works regardless of naming (`key_vault_id`, `workspace_id`, `parent_resource_id`, `scope`, etc.)

**Formatting:**
- Reuse existing `AzureScopeParser.Parse()` logic from role assignment feature
- Format examples:
  - Subscription: `subscription 12345678-1234-1234-1234-123456789012`
  - Resource Group: `rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012`
  - Resource: `Key Vault kv-tfplan2md in resource group rg-demo of subscription 12345678-1234-1234-1234-123456789012`
  - Storage Account: `Storage Account sttfplan2mdlogs in resource group rg-demo of subscription 12345678-1234-1234-1234-123456789012`
- Preserves all information from the original ID in a more readable format

**Display in Change Tables:**
- Formatted IDs should appear in the change table, not in the large values section
- If the formatted ID still exceeds the 100-character threshold for large values, create an explicit exception
- Exception rule: Any value matching the Azure resource ID pattern is always displayed inline, regardless of length

**Provider Scope:**
- Only applies to `azurerm` resources
- Does not affect other providers (azuread, azuredevops, aws, google, etc.)

### Out of Scope

- Custom formatting for non-Azure resource IDs
- Shortening subscription IDs or GUIDs
- Clickable links to Azure Portal (would break in multiple platforms)
- Subscription name resolution (no data source available in Terraform plan)
- Different formatting styles (uses existing AzureScopeParser logic only)

## User Experience

### Before

**In change table:**
```markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | `/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-tfplan2md-demo/providers/Microsoft.KeyVault/vaults/kv-tfplan2md` |
```

Or if the ID exceeded 100 characters, it would be in a separate "Large attributes" section below the table.

### After

**In change table:**
```markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | `Key Vault kv-tfplan2md in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012` |
```

### Example Resource Types Affected

Any azurerm resource with ID attributes:
- `azurerm_key_vault_secret` - `key_vault_id`
- `azurerm_monitor_diagnostic_setting` - `log_analytics_workspace_id`
- `azurerm_subnet` - `virtual_network_id`
- `azurerm_network_interface` - `subnet_id`
- `azurerm_virtual_machine` - `network_interface_ids[]`
- `azapi_resource` - `parent_id`
- Any other attribute containing Azure resource IDs

## Success Criteria

- [ ] Azure resource IDs in all azurerm resources are automatically detected by pattern
- [ ] Detected IDs are formatted using the existing AzureScopeParser logic
- [ ] Formatted IDs appear in change tables, not in large values sections
- [ ] No false positives (non-resource-ID values incorrectly formatted)
- [ ] All existing role assignment formatting tests continue to pass
- [ ] New tests verify formatting for common ID attributes (key_vault_id, workspace_id, subnet_id, etc.)
- [ ] Documentation updated to reflect universal Azure resource ID formatting

## Open Questions

None - the existing AzureScopeParser implementation provides all necessary logic.
