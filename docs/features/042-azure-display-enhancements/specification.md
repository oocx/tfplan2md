# Feature: Azure Display Enhancements

## Overview

Enhance the display of Azure resources and identities across all Azure providers (azurerm, azapi, azuread, azdevops) by automatically formatting Azure resource IDs, showing human-readable names for subscriptions, management groups, and role definitions, and improving summaries for specific Azure resource types.

This feature builds on the existing "Universal Azure Resource ID Formatting" capability by extending it to detect Azure resource IDs anywhere in attribute values (not just known attributes), and by adding display name resolution for Azure infrastructure entities (subscriptions, management groups, tenants, roles).

## User Goals

- **Reduce cognitive load**: See meaningful names instead of GUIDs and long resource IDs throughout the report
- **Faster comprehension**: Quickly understand which subscriptions, roles, and management groups are involved in changes
- **Consistent formatting**: Apply the same human-readable formatting rules everywhere Azure IDs appear
- **Better resource context**: Understand DNS records and PIM assignments at a glance through descriptive summaries

## Scope

### In Scope

**Universal Azure Resource ID Detection:**
- Detect and format any attribute value that matches the Azure resource ID pattern
- Apply to all Azure providers: azurerm, azapi, azuread, azdevops
- Existing readable resource name formatting applies automatically

**Subscription Display Names:**
- Format subscription IDs as `display_name (subscription_id)` everywhere they appear
- Apply within readable resource names (e.g., "Key Vault `vm-kv-ds-gwc` in resource group `vmkv-rg-ds-gwc` of subscription `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)`")
- Apply to standalone subscription ID attributes

**Management Group Display Names:**
- Show display names for all management groups
- Format root management group as "Tenant `<tenant_name>` root"
- Apply within resource IDs that reference management groups (e.g., "policy assignment `my-policy` in management group `Production`")

**Role Definition Names:**
- Automatically recognize built-in Azure role GUIDs and display their standard names (e.g., "Owner", "Contributor", "Reader")
- Support custom role definitions via mapping file
- Allow mapping file to override built-in role names
- Apply to `role_definition_id` attributes and related role attributes across all Azure resources

**Resource-Specific Summaries:**
- `azurerm_private_dns_a_record`: Display as `name.zone_name` (FQDN format, e.g., `record1.contoso.local`)
- `azurerm_pim_eligible_role_assignment`: Display as "Assign `<role_name>` to `<principal_name>`" with resolved names for both
- `azurerm_role_management_policy`: Display as "`<role_name>` in `<scope_display_name>`" (e.g., "`Contributor` in resource group `foo` of subscription `bar`")

**Mapping File Extension:**
- Extend the existing principal mapping JSON file to include new sections:
  - `subscriptions`: Array of `{ "id": "guid", "displayName": "..." }`
  - `managementGroups`: Array of `{ "id": "string", "displayName": "..." }`
  - `tenants`: Array of `{ "id": "guid", "displayName": "..." }`
  - `roles`: Array of `{ "id": "guid", "displayName": "..." }`
- Keep backward compatibility with existing principal mapping files

**Documentation:**
- Document the new mapping file sections in project documentation (README.md, relevant docs)
- Provide Azure CLI commands/scripts to populate subscriptions, management groups, tenants, and roles
- Do NOT update website documentation (separate PR)

**Fallback Behavior:**
- Display raw ID when no mapping is available
- When `--debug` is enabled: Show failed mapping attempts in debug output with context (which resource referenced the unmapped ID) and reason for failure

### Out of Scope

- Automatically fetching Azure metadata (subscriptions, management groups, roles) at runtime - users must provide mappings
- Support for non-Azure providers
- Website documentation updates (handled separately)
- Changes to the core resource ID parsing logic (AzureScopeParser) unless required for new features
- Automatic tenant ID detection or resolution beyond what's provided in the mapping file

## User Experience

### Before and After Examples

**Subscription IDs:**

Before:
```markdown
| subscription_id | `d1828a48-fced-4ea2-b2ec-4b9623f327fd` |
```

After (with mapping):
```markdown
| subscription_id | `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)` |
```

**Subscription in Readable Resource Names:**

Before:
```markdown
Key Vault `vm-kv-ds-gwc` in resource group `vmkv-rg-ds-gwc` of subscription `d1828a48-fced-4ea2-b2ec-4b9623f327fd`
```

After (with mapping):
```markdown
Key Vault `vm-kv-ds-gwc` in resource group `vmkv-rg-ds-gwc` of subscription `Production (d1828a48-fced-4ea2-b2ec-4b9623f327fd)`
```

**Role Assignments:**

Before:
```markdown
| role_definition_id | `/subscriptions/.../providers/Microsoft.Authorization/roleDefinitions/8e3af657-a8ff-443c-a75c-2fe8c4bcb635` |
```

After (with built-in role recognition):
```markdown
| role_definition_id | `Owner` |
```

**Private DNS A Record Summary:**

Before:
```markdown
### ➕ azurerm_private_dns_a_record `example`
```

After:
```markdown
### ➕ azurerm_private_dns_a_record `record1.contoso.local`
```

**PIM Eligible Role Assignment Summary:**

Before:
```markdown
### ➕ azurerm_pim_eligible_role_assignment `example`
```

After (with mappings):
```markdown
### ➕ azurerm_pim_eligible_role_assignment `example`: Assign `Contributor` to `Jane Doe`
```

**Role Management Policy Summary:**

Before:
```markdown
### 🔁 azurerm_role_management_policy `this`
```

After (with mappings):
```markdown
### 🔁 azurerm_role_management_policy `this`: `Contributor` in resource group `foo` of subscription `Production (abc-123...)`
```

**Management Group:**

Before:
```markdown
| management_group_id | `mg-production` |
```

After (with mapping):
```markdown
| management_group_id | `Production Workloads` |
```

Root management group:
```markdown
| management_group_id | Tenant `Contoso Corp` root |
```

### CLI Usage

No changes to CLI flags. Users provide the extended mapping file via the existing `--principal-mapping` flag:

```bash
tfplan2md plan.json --principal-mapping azure-mappings.json --output report.md
```

Example mapping file structure:
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
    { "id": "d1828a48-fced-4ea2-b2ec-4b9623f327fd", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-production", "displayName": "Production Workloads" }
  ],
  "tenants": [
    { "id": "tenant-guid", "displayName": "Contoso Corp" }
  ],
  "roles": [
    { "id": "custom-role-guid", "displayName": "Custom Deployment Role" }
  ]
}
```

### Debug Output

When `--debug` is enabled and mappings fail:

```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'azure-mappings.json'
- Found 45 principals
- Found 3 subscriptions
- Found 2 management groups
- Found 1 tenant
- Found 5 custom roles

Failed to resolve 3 IDs:
- Subscription `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_resource_group.example`) - not found in mapping file
- Management group `mg-unknown` (referenced in `azurerm_management_group_policy_assignment.test`) - not found in mapping file
- Role definition `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`) - not found in mapping file or built-in roles
```

## Success Criteria

- [ ] Any attribute value matching Azure resource ID pattern gets readable formatting (all Azure providers)
- [ ] Subscription IDs display as `display_name (subscription_id)` when mapping available
- [ ] Subscription display names appear within readable resource name outputs
- [ ] Management group display names are resolved from mapping
- [ ] Root management group displays as "Tenant `<tenant_name>` root"
- [ ] Built-in Azure roles are automatically recognized by GUID
- [ ] Custom roles are resolved from mapping file
- [ ] Mapping file can override built-in role names
- [ ] `azurerm_private_dns_a_record` summary shows `name.zone_name` format
- [ ] `azurerm_pim_eligible_role_assignment` summary shows "Assign `<role_name>` to `<principal_name>`"
- [ ] `azurerm_role_management_policy` summary shows "`<role_name>` in `<scope_display_name>`"
- [ ] `role_definition_id` attributes show readable names across all Azure resources
- [ ] Unmapped IDs fall back to raw ID display
- [ ] Debug output shows failed mappings with context when `--debug` is enabled
- [ ] Mapping file JSON schema extended with subscriptions, managementGroups, tenants, roles sections
- [ ] Documentation includes Azure CLI commands/scripts to populate new mapping sections
- [ ] Existing principal mapping files continue to work (backward compatibility)
- [ ] All tests pass with new formatting rules
- [ ] No changes to website documentation (deferred to separate PR)

## Open Questions

### For Architect

**Role ID Detection Strategy:**

Two possible approaches for detecting role definition IDs:

1. **Attribute name matching**: Look for attributes specifically named `role_definition_id`, `role_id`, etc.
   - Pros: Explicit, predictable, less chance of false positives
   - Cons: Must maintain list of known role attribute names

2. **GUID pattern matching**: Any GUID value, try to resolve it (could be role, principal, subscription, etc.)
   - Pros: Automatically handles any role attribute naming, more flexible
   - Cons: More lookups, potential performance impact

Which approach should be used, or should it be a hybrid?

### For Maintainer

None at this time - all requirements have been clarified.
