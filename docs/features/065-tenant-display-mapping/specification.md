# Feature: Tenant Display Name Mapping

## Overview

Add display name mapping for Entra ID tenants with visual icons, extending the existing Azure display enhancements. This feature affects all Azure-related providers (azurerm, azapi, azuread, azdevops) and ensures tenant IDs are presented with human-readable names and distinctive icons throughout reports.

## User Goals

- **Quick tenant identification**: See tenant names instead of GUIDs when reviewing multi-tenant resources
- **Visual distinction**: Easily distinguish tenants from other Azure entities using the 🏢 icon
- **Consistent formatting**: Apply the same readable formatting to tenants as other Azure entities (subscriptions, management groups)
- **Selective mapping**: Generate mappings for only the specific tenants relevant to their infrastructure, even when authenticated to multiple tenants

## Scope

### In Scope

**Tenant Display Names:**
- Format tenant IDs as `display_name (tenant_id)` everywhere they appear (consistent with subscription formatting)
- Apply 🏢 icon to all tenant values in attributes and summaries
- Icon format: `🏢 <display_name (tenant_id)>` (icon outside backticks, followed by display name and ID in backticks)
- Apply to all Azure provider resources (azurerm, azapi, azuread, azdevops) that reference tenant IDs
- Support tenant display in attribute tables and resource summaries

**Management Group Icons:**
- Apply 🗂️ icon to all management group values in attributes and summaries
- Icon format: `🗂️ <display_name>` (icon outside backticks, followed by display name in backticks)
- Enhance existing management group display functionality from feature 063

**Mapping File Extension:**
- Extend the existing mapping JSON file to include a `tenants` section:
  - Format: Array of `{ "id": "guid", "displayName": "..." }`
- Keep backward compatibility with existing mapping files (tenants section is optional)

**Documentation:**
- Document the new `tenants` mapping file section
- Provide Azure CLI commands to populate tenant mappings for specific tenants only
- Show how to retrieve principals (users, groups, service principals) from specific tenants only
- Show how to retrieve subscriptions from specific tenants only
- Show how to retrieve management groups from specific tenants only
- Show how to retrieve role definitions from specific tenants only
- Provide practical examples for multi-tenant scenarios where users are authenticated to many tenants but only need mappings for a subset

**Examples and Test Data:**
- Update all examples in `examples/` directory to include tenant mappings where tenants are referenced
- Update all test snapshots to include mapped tenants for all tenants used
- Ensure comprehensive coverage of tenant display scenarios across all Azure providers

**Fallback Behavior:**
- Display raw tenant ID when no mapping is available
- When `--debug` is enabled: Show unmapped tenant IDs in debug output with context

### Out of Scope

- Automatically fetching tenant metadata at runtime - users must provide mappings via mapping file
- Changes to subscription, management group, or role mapping logic (already covered in feature 063)
- Website documentation updates (handled separately)
- Multi-tenant authentication or tenant switching logic
- Automatic tenant discovery from Azure CLI configuration

## User Experience

### Before and After Examples

**Tenant ID in Attributes:**

Before:
```markdown
| tenant_id | `12345678-1234-1234-1234-123456789012` |
```

After (with mapping):
```markdown
| tenant_id | 🏢 `Contoso Corp (12345678-1234-1234-1234-123456789012)` |
```

**Tenant in AzureAD Resources:**

Before:
```markdown
### ➕ azuread_user `john.doe`
| tenant_id | `12345678-1234-1234-1234-123456789012` |
```

After (with mapping):
```markdown
### ➕ azuread_user `john.doe`
| tenant_id | 🏢 `Contoso Corp (12345678-1234-1234-1234-123456789012)` |
```

**Management Group Display (Enhanced):**

Before:
```markdown
| management_group_id | `mg-production` |
```

After (with mapping and icon from feature 063):
```markdown
| management_group_id | 🗂️ `Production Workloads` |
```

Root management group:
```markdown
| management_group_id | 🗂️ Tenant `Contoso Corp` root |
```

### Mapping File Structure

Extended mapping file with new `tenants` section:

```json
{
  "users": [
    { "id": "user-guid", "displayName": "Jane Doe" }
  ],
  "groups": [
    { "id": "group-guid", "displayName": "DevOps Team" }
  ],
  "servicePrincipals": [
    { "id": "sp-guid", "displayName": "CI/CD Pipeline" }
  ],
  "subscriptions": [
    { "id": "sub-guid", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-production", "displayName": "Production Workloads" }
  ],
  "tenants": [
    { "id": "12345678-1234-1234-1234-123456789012", "displayName": "Contoso Corp" },
    { "id": "87654321-4321-4321-4321-210987654321", "displayName": "Fabrikam Inc" }
  ],
  "roles": [
    { "id": "custom-role-guid", "displayName": "Custom Deployment Role" }
  ]
}
```

### CLI Usage

No changes to CLI flags. Users provide the extended mapping file via the existing `--principal-mapping` flag:

```bash
tfplan2md plan.json --principal-mapping azure-mappings.json --output report.md
```

### Documentation Examples

**Retrieve Tenant Information for Specific Tenants:**

```bash
# Get display names for specific tenants only
az account tenant list --query "[?tenantId=='12345678-1234-1234-1234-123456789012' || tenantId=='87654321-4321-4321-4321-210987654321'].{id:tenantId, displayName:displayName}" -o json
```

**Retrieve Users from Specific Tenants:**

```bash
# For tenant A
az ad user list --tenant 12345678-1234-1234-1234-123456789012 --query "[].{id:id, displayName:displayName}" -o json

# For tenant B
az ad user list --tenant 87654321-4321-4321-4321-210987654321 --query "[].{id:id, displayName:displayName}" -o json

# Combine results manually or with jq
```

**Retrieve Service Principals from Specific Tenants:**

```bash
# For specific tenants only
az ad sp list --tenant 12345678-1234-1234-1234-123456789012 --all --query "[].{id:id, displayName:displayName}" -o json
```

**Retrieve Subscriptions from Specific Tenants:**

```bash
# Filter subscriptions by tenant
az account list --query "[?tenantId=='12345678-1234-1234-1234-123456789012' || tenantId=='87654321-4321-4321-4321-210987654321'].{id:id, displayName:name}" -o json
```

## Success Criteria

- [ ] Tenant IDs display as `display_name (tenant_id)` when mapping available
- [ ] Tenants display with 🏢 icon in all contexts (attributes and summaries)
- [ ] Tenant display formatting applies to all Azure providers (azurerm, azapi, azuread, azdevops)
- [ ] Management groups display with 🗂️ icon (enhancement to feature 063)
- [ ] Mapping file JSON schema extended with optional `tenants` section
- [ ] Documentation includes Azure CLI commands to retrieve tenant mappings for specific tenants
- [ ] Documentation shows how to retrieve principals from specific tenants only
- [ ] Documentation shows how to retrieve subscriptions from specific tenants only
- [ ] Documentation shows how to retrieve management groups from specific tenants only
- [ ] Documentation shows how to retrieve roles from specific tenants only
- [ ] All examples in `examples/` directory include tenant mappings where tenants are referenced
- [ ] All test snapshots include mapped tenants for all tenants used
- [ ] Unmapped tenant IDs fall back to raw ID display
- [ ] Debug output shows unmapped tenant IDs with context when `--debug` is enabled
- [ ] Existing mapping files without `tenants` section continue to work (backward compatibility)
- [ ] All tests pass with new formatting rules

