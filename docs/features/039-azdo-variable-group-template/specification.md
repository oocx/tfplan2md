# Feature: Azure DevOps Variable Group Template

## Overview

Create a specialized template for Azure DevOps Variable Groups (`azuredevops_variable_group`) that displays all variables (both regular and secret) with their metadata in a clear table format. Currently, when variable group resources are included in Terraform plans, the default output shows "At least one attribute in this block is (or was) sensitive, so its contents will not be displayed" for all variables. This makes it impossible to understand what changes to variable group variables will be applied by a Terraform plan, forcing users to either apply blindly or manually inspect the Terraform state.

The new template will display all variables in a unified table, showing full metadata for both regular and secret variables while protecting actual secret values by displaying "(sensitive / hidden)" in place of the secret value.

## User Goals

- Users need to see which variables are being added, modified, or removed in a variable group
- Users want to understand what specific values are changing for regular variables during updates
- Users need to verify variable changes in CI/CD pipelines before applying Terraform plans
- Users want secret variable values to remain hidden for security while still seeing variable metadata (name, enabled status, etc.)
- Users need to see Key Vault integration details when a variable group is linked to Azure Key Vault
- Users need this information presented clearly in Azure DevOps pipeline comments and reports

## Scope

### In Scope

- Specialized template for `azuredevops_variable_group` resource type
- Display variable group metadata (name, description, project reference)
- Show all variables (from both `variable` and `secret_variable` arrays) in a unified table format with columns: Name, Value, Enabled, Content Type, Expires
- For secret variables: display all metadata (name, enabled, content_type, expires) but show "(sensitive / hidden)" in the Value column instead of the actual secret_value
- Categorize variables in update scenarios as: Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
- Match variables by `name` attribute for semantic diffing (across both variable arrays)
- For modified variables: show before/after values with `-` and `+` prefixes for changed attributes
- For modified variables: show single value without prefix for unchanged attributes
- Support create, update, and delete operations with appropriate display formats
- Handle large variable values using the existing large value display mechanism (values over 100 characters or multi-line)
- Display variable group-level attributes (allow_access) using standard attribute table
- Display Key Vault integration blocks (`key_vault`) in a separate table showing: name, service_endpoint_id, search_depth

### Out of Scope

- Displaying the actual values of secret variables (security requirement - show "(sensitive / hidden)" instead)
- Variable group permissions or access control details
- Standalone `azuredevops_variable` resources (if they exist)
- Custom sorting options beyond the natural order from Terraform
- Expandable/collapsible sections per variable (maintain single-table format)
- Detailed Key Vault secret enumeration (only show the key_vault block metadata)

## User Experience

### Current Behavior (Default Template)

When a variable group is modified, users see a completely opaque message:

```markdown
### 🔄 azuredevops_variable_group.example

| Attribute | Before | After |
|-----------|--------|-------|
| name | `example-variables` | `example-variables` |

- variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
- variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
+ variable {
    # At least one attribute in this block is (or was) sensitive,
    # so its contents will not be displayed.
  }
```

This makes it **impossible** to understand what is changing.

### New Behavior (Variable Group Template)

With the specialized template, the same changes are rendered with full transparency for all variable metadata:

```markdown
<details open>
<summary>🔄 azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code> | 3 🔧 variables</summary>
<br>

### 🔄 azuredevops_variable_group.example

**Variable Group:** `example-variables`

**Description:** `Variable group for example pipeline`

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
| ➕ | `ENV` | `Production` | - | - | - |
| 🔄 | `APP_VERSION` | - `1.0.0`<br>+ `1.0.0` | - `false`<br>+ `false` | - | - |
| 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | - `true`<br>+ `false` | - | - |
| ❌ | `ENVIRONMENT` | `development` | `false` | - | - |

</details>
```

Note how the `SECRET_KEY` variable shows its metadata (name, enabled status) but displays `(sensitive / hidden)` in the Value column instead of the actual secret value.

### Handling Large Variable Values

If a variable value exceeds 100 characters or contains line breaks, it will be moved to the large values section:

```markdown
<details open>
<summary>🔄 azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code> | 1 🔧 variables</summary>
<br>

### 🔄 azuredevops_variable_group.example

**Variable Group:** `example-variables`

#### Variables

| Change | Name | Enabled | Content Type | Expires |
| ------ | ---- | ------- | ------------ | ------- |
| 🔄 | `CONNECTION_STRING` | `true` | - | - |

<details>
<summary>Large values: CONNECTION_STRING (2 lines, 1 changed)</summary>

### `CONNECTION_STRING`

**Before:**
```
Server=tcp:db-old.database.windows.net,1433;Database=mydb;
```

**After:**
```
Server=tcp:db-new.database.windows.net,1433;Database=mydb;
```

</details>

</details>
```

### Create Operation

For create operations, show a simpler table without the Change column:

```markdown
<details open>
<summary>➕ azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code></summary>
<br>

### ➕ azuredevops_variable_group.example

**Variable Group:** `example-variables`

**Description:** `Variable group for example pipeline`

#### Variables

| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
| `APP_VERSION` | `1.0.0` | `false` | - | - |
| `ENV` | `Production` | - | - | - |
| `API_KEY` | `(sensitive / hidden)` | `true` | - | - |

</details>
```

### Delete Operation

For delete operations, show the variables being deleted:

```markdown
<details open>
<summary>❌ azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code></summary>
<br>

### ❌ azuredevops_variable_group.example

**Variable Group:** `example-variables`

#### Variables (being deleted)

| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
| `APP_VERSION` | `1.0.0` | `false` | - | - |
| `ENVIRONMENT` | `development` | `false` | - | - |
| `DB_PASSWORD` | `(sensitive / hidden)` | `true` | - | - |

</details>
```

### Key Vault Integration

When a variable group is linked to Azure Key Vault, display the key_vault block(s) in a separate table:

```markdown
<details open>
<summary>➕ azuredevops_variable_group <b><code>keyvault_example</code></b> — <code>keyvault-variables</code></summary>
<br>

### ➕ azuredevops_variable_group.keyvault_example

**Variable Group:** `keyvault-variables`

**Description:** `Variable group linked to Azure Key Vault`

#### Key Vault Integration

| Name | Service Endpoint ID | Search Depth |
| ---- | ------------------- | ------------ |
| `my-keyvault` | `a1b2c3d4-e5f6-7890-abcd-ef1234567890` | `1` |

#### Variables

| Name | Value | Enabled | Content Type | Expires |
| ---- | ----- | ------- | ------------ | ------- |
| `LOCAL_VAR` | `local-value` | - | - | - |

</details>
```

Note: Variables retrieved from Key Vault are not listed in the `variable` or `secret_variable` arrays - only the Key Vault connection metadata is shown.

## Technical Details

### Data Structure

Based on the `examples/azuredevops/terraform_plan.json`, the variable group structure is:

```json
{
  "type": "azuredevops_variable_group",
  "change": {
    "before": {
      "name": "example-variables",
      "description": "Variable group for example pipeline",
      "project_id": "0f0b93a6-f450-49b2-ad52-fe3303c2f9aa",
      "allow_access": true,
      "variable": [
        {
          "name": "APP_VERSION",
          "value": "1.0.0",
          "enabled": false,
          "content_type": "",
          "expires": ""
        }
      ],
      "secret_variable": [
        {
          "name": "SECRET_KEY",
          "value": "supersecret"
        }
      ]
    },
    "after": { /* similar structure */ },
    "before_sensitive": {
      "secret_variable": true,
      "variable": [{}, {}]
    },
    "after_sensitive": {
      "secret_variable": true,
      "variable": [{}, {}]
    }
  }
}
```

### Key Observations

1. **Regular variables** are in the `variable` array with attributes: `name`, `value`, `enabled`, `content_type`, `expires`
2. **Secret variables** are in the `secret_variable` array with attributes: `name`, `secret_value`, `enabled`, `content_type`, `expires`
3. The `secret_variable` array is marked as `true` in `before_sensitive` and `after_sensitive`
4. Individual entries in the `variable` array are **not marked sensitive** (they are empty objects `{}` in the sensitivity markers)
5. Not all attributes are always present (e.g., `enabled` may be null/missing for new variables)
6. Key Vault integration is represented by `key_vault` blocks with attributes: `name`, `service_endpoint_id`, `search_depth`

### Security Consideration

The template must **never display** the `secret_value` attribute from `secret_variable` entries. Instead, display "(sensitive / hidden)" in the Value column while showing all other metadata (name, enabled, content_type, expires).

### Empty Value Handling

- `enabled`: If null or missing, display as `-` (dash)
- `content_type`: If empty string or null, display as `-`
- `expires`: If empty string or null, display as `-`
- `value`: Should always be present for non-secret variables; if missing, display as `(null)`

### Semantic Diffing

Variables should be matched by their `name` attribute across both the `variable` and `secret_variable` arrays (similar to NSG security rules). This ensures that:
- Adding a new variable (regular or secret) is clearly marked as ➕
- Removing a variable (regular or secret) is clearly marked as ❌
- Modifying a variable shows before/after values as 🔄
- Reordering variables doesn't create false change indicators
- Variables from both arrays are displayed in a unified table

## Success Criteria

- [ ] Template created for `azuredevops_variable_group` resource type at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/variable_group.sbn`
- [ ] Template directory created for provider-specific templates: `Templates/azuredevops/`
- [ ] All variables (from both `variable` and `secret_variable` arrays) displayed in unified table format with all relevant columns
- [ ] Secret variables show all metadata (name, enabled, content_type, expires) but display "(sensitive / hidden)" in Value column
- [ ] Variables categorized correctly as Added, Modified, Removed, or Unchanged using semantic matching by name across both arrays
- [ ] Large variable values (>100 chars or multi-line) handled using existing large value display mechanism (regular variables only)
- [ ] Modified variables show before/after values with `-` and `+` prefixes for changed attributes
- [ ] Unchanged attributes in modified variables show single value without prefix
- [ ] Empty/null attribute values displayed as `-` (dash)
- [ ] Create, update, and delete operations each have appropriate table layouts
- [ ] Variable group metadata (name, description) displayed prominently
- [ ] Summary line includes variable group name and change count for updates
- [ ] Key Vault blocks displayed in a separate table with columns: name, service_endpoint_id, search_depth
- [ ] Template follows Report Style Guide formatting standards (code formatting for values, plain text for labels)
- [ ] All existing tests pass
- [ ] New tests verify variable group rendering for:
  - Create operation with both regular and secret variables
  - Update operation with added/modified/removed variables (both types)
  - Delete operation with both regular and secret variables
  - Secret variables display metadata but show "(sensitive / hidden)" for values
  - Large variable values (regular variables only)
  - Empty/null attribute values
  - Key Vault integration blocks
- [ ] Documentation updated in `docs/features.md`
- [ ] Example output included in feature documentation

## Open Questions

**Q: How should we display secret variables?**

**A (RESOLVED):** Display secret variables in the same table as regular variables, showing all metadata (name, enabled, content_type, expires) but displaying "(sensitive / hidden)" in the Value column instead of the actual `secret_value`. No separate note is needed.

**Q: How should we handle variables with null/unknown values (shown as "known after apply")?**

**A:** Display as `(known after apply)` in the Value column, matching Terraform's standard terminology.

**Q: Should the Enabled column show checkmarks instead of true/false?**

**A:** No. Use standard boolean representation or dash for consistency with the rest of tfplan2md. The Report Style Guide specifies that data values should be rendered as inline code (backticks).

**Q: What if a variable group has ONLY secret variables and no regular variables?**

**A:** Show the variable group metadata and the variables table with all secret variables displaying "(sensitive / hidden)" in the Value column. Display the table normally - the secret variables are not hidden, only their values are.

**Q: How should we display Key Vault-linked variable groups?**

**A (RESOLVED):** Show a separate table for `key_vault` blocks with columns:
- **name**: The name of the Key Vault
- **service_endpoint_id**: The Azure service endpoint ID
- **search_depth**: The search depth for secrets

The Key Vault table should appear after the Variables section. Variables from Key Vault are not enumerated in the variable arrays, so only the key_vault block metadata is shown.
