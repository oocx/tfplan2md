# Feature: Table Format for Role Assignment Display

## Overview

Improve consistency and readability of Azure role assignment resources by changing their display format from bullet lists to tables, matching the structure used by all other resource types in the generated markdown reports.

## User Goals

- **Visual consistency**: Role assignments should look similar to other resources in the report
- **Easier scanning**: Tables provide better structure for quick review than bullet lists
- **Quick overview**: Summary line provides at-a-glance understanding without expanding details
- **Familiar patterns**: Users already understand the table format from other resources

## Scope

### In Scope

**Display Format Changes:**
- Change role assignment rendering from bullet list to table format
- Use `<details>` wrapper with collapsible table (consistent with other CREATE/UPDATE resources)
- Add summary line above details for quick overview
- Maintain existing Azure-specific enhancements (readable role names, parsed scopes, principal mapping)

**Summary Line Format:**
- **For CREATE operations**: `Principal` → `Role` on `Scope`
  - Example: `Jane Doe` (User) → `Reader` on `rg-tfplan2md-demo`
- **For UPDATE operations**: Show only new/after values
  - Example: `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`
- **For REPLACE operations**: recreate as `Principal` → `Role` on `Scope`
  - Example: recreate as `Security Team` (Group) → `Contributor` on `rg-production`
- **For DELETE operations**: remove `Role` on `Scope` from Type `Principal`
  - Example: remove `Contributor` role on `rg-legacy` from User `John Doe`
- Use backticks for principal names, role names, and resource names in summary
- Include type labels in plain text (e.g., "(User)", "(Group)")
- No action symbols (➕, 🔄, ❌) in the summary line - they appear in the header only
- Long resource names are not truncated - display full names regardless of length
- **Description field**: If the `description` attribute is present and non-empty, display it on a new line below the summary in plain text (no special formatting)

**Field Handling:**
- `scope` and `principal_id` are required fields per azurerm provider, so always available
- For role: If `role_definition_id` is present, use existing role name mapping; if null, fall back to `role_definition_name` field

**Details Table Format:**
- Vertical key-value table with "Attribute" and "Value" columns (for CREATE/DELETE)
- Three-column table with "Attribute", "Before", and "After" (for UPDATE)
- Use plain text for all values (no bold, italic, or backticks within table cells)
- Use backticks for attribute names (e.g., `scope`, `role_definition_id`, `principal_id`)
- Include ALL attributes from the role assignment resource that have at least one non-null value (before or after)
- Common attributes include: `scope`, `role_definition_id`, `principal_id`, `name`, `principal_type`, `description`, `condition`, `condition_version`, `delegated_managed_identity_resource_id`, `skip_service_principal_aad_check`
- Maintain enhanced readability from existing role assignment feature (friendly names, parsed scopes)
- Attributes with null values in both before and after states should be omitted

**Styling Approach:**
- Summary line: Use backticks for key identifiers (principals, roles, resource names)
- Table content: Plain text only, no additional formatting
- Keep existing Azure enhancements (role name mapping, scope parsing, principal mapping)

### Out of Scope

- Changes to the underlying data transformation logic (AzureRoleDefinitionMapper, AzureScopeParser, PrincipalMapper)
- Changes to the CLI interface or principal mapping functionality
- Changes to other resource types or templates
- Modifications to firewall rules or other special resource renderings
- Changes to summary table at the top of reports
- **Displaying replacement reasons**: Terraform plan JSON includes `replace_paths` field showing which attributes forced replacement, but parsing and displaying this information will be implemented as a separate feature for all resource types

## User Experience

### Command-Line Interface

No changes to the CLI. The feature only affects the rendering output.

```bash
# Works the same as before
tfplan2md --principal-mapping principals.json plan.json > report.md
```

### Markdown Output

**Current format (bullet list):**
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader (create)

- **scope**: **rg-tfplan2md-demo** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: Jane Doe (User) [00000000-0000-0000-0000-000000000001]
```

**New format (table with summary):**

**Example 1: CREATE operation (without description)**
```markdown
#### ➕ module.security.azurerm_role_assignment.rg_reader

**Summary:** `Jane Doe` (User) → `Reader` on `rg-tfplan2md-demo`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-tfplan2md-demo in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | Jane Doe (User) [00000000-0000-0000-0000-000000000001] |

</details>
```

**Example 1b: CREATE operation (with description)**
```markdown
#### ➕ module.security.azurerm_role_assignment.storage_reader

**Summary:** `DevOps Team` (Group) → `Storage Blob Data Reader` on Storage Account `sttfplan2mdlogs`  
Allow DevOps team to read logs from the storage account

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] |
| `description` | Allow DevOps team to read logs from the storage account |

</details>
```

**Example 2: UPDATE operation (with description)**
```markdown
#### 🔄 module.security.azurerm_role_assignment.storage_reader

**Summary:** `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`  
Upgraded permissions for security auditing

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 | Storage Account sttfplan2mddata in resource group rg-tfplan2md-demo of subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [00000000-0000-0000-0000-000000000002] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |
| `description` | Allow team to read storage data | Upgraded permissions for security auditing |

</details>
```

**Example 3: REPLACE operation**
```markdown
#### ♻️ module.security.azurerm_role_assignment.app_role

**Summary:** recreate as `Security Team` (Group) → `Contributor` on `rg-production`  
Updated role assignment with new permissions

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | rg-production in subscription 12345678-1234-1234-1234-123456789012 | rg-production in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) | Contributor (b24988ac-6180-42a0-ab88-20f7382dd24c) |
| `principal_id` | DevOps Team (Group) [22222222-3333-4444-5555-666666666666] | Security Team (Group) [00000000-0000-0000-0000-000000000003] |
| `description` | Read-only access for DevOps | Updated role assignment with new permissions |

</details>
```

**Example 4: DELETE operation**
```markdown
#### ❌ module.security.azurerm_role_assignment.old_assignment

**Summary:** remove `Contributor` role on `rg-legacy` from User `John Doe`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | rg-legacy in subscription 87654321-4321-4321-4321-210987654321 |
| `role_definition_id` | Contributor (b24988ac-6180-42a0-ab88-20f7382dd24c) |
| `principal_id` | John Doe (User) [11111111-2222-3333-4444-555555555555] |

</details>
```

## Success Criteria

- [ ] Role assignments use table format with collapsible `<details>` wrapper
- [ ] Summary line appears above the details section for all actions
- [ ] CREATE operations show summary in format: Principal → Role on Scope
- [ ] UPDATE operations show only new/after values in summary
- [ ] REPLACE operations show summary in format: recreate as Principal → Role on Scope
- [ ] DELETE operations show summary in format: remove Role on Scope from Type Principal
- [ ] Summary uses backticks for principals, roles, and resource names
- [ ] Description field is displayed on a new line below summary if present and non-empty
- [ ] Details table uses plain text (no bold, italic, or backticks in values)
- [ ] Attribute names use backticks in table headers
- [ ] Details table includes all non-null attributes (scope, role_definition_id, principal_id, name, principal_type, description, condition, etc.)
- [ ] Attributes with null values in both before and after states are omitted from the table
- [ ] All existing Azure enhancements are preserved (role names, parsed scopes, principal mapping)
- [ ] Role assignments without principal mapping still render correctly
- [ ] Multiple role assignments in same report render consistently
- [ ] Format matches the visual style of other resources (storage accounts, virtual networks, etc.)
- [ ] Tests verify the new format for CREATE, UPDATE, REPLACE, and DELETE operations
- [ ] Tests verify summary line generation for all scenarios
- [ ] Comprehensive demo example is updated with new format
- [ ] Backward compatibility: reports still generate successfully (just with new format)
