# Feature: Azure DevOps Variable Group Template

## Overview

Create a specialized template for Azure DevOps Variable Groups (`azuredevops_variable_group`) that displays all non-secret variables with their names and values in a clear table format. Currently, when variable group resources are included in Terraform plans, the default output shows "At least one attribute in this block is (or was) sensitive, so its contents will not be displayed" for all variables, even those that are not secrets. This makes it impossible to understand what changes to variable group variables will be applied by a Terraform plan, forcing users to either apply blindly or manually inspect the Terraform state.

The new template will distinguish between secret and non-secret variables, displaying all non-secret variables clearly while protecting actual secrets.

## User Goals

- Users need to see which non-secret variables are being added, modified, or removed in a variable group
- Users want to understand what specific values are changing for non-secret variables during updates
- Users need to verify variable changes in CI/CD pipelines before applying Terraform plans
- Users want secret variables (those in `secret_variable` blocks) to remain hidden for security
- Users need this information presented clearly in Azure DevOps pipeline comments and reports

## Scope

### In Scope

- Specialized template for `azuredevops_variable_group` resource type
- Display variable group metadata (name, description, project reference)
- Show non-secret variables in a table format with columns: Name, Value, Enabled, Content Type, Expires
- Categorize variables in update scenarios as: Added (➕), Modified (🔄), Removed (❌), or Unchanged (⏺️)
- Match variables by `name` attribute for semantic diffing
- For modified variables: show before/after values with `-` and `+` prefixes for changed attributes
- For modified variables: show single value without prefix for unchanged attributes
- Hide `secret_variable` entries completely (they are marked as sensitive in Terraform)
- Support create, update, and delete operations with appropriate display formats
- Handle large variable values using the existing large value display mechanism (values over 100 characters or multi-line)
- Display variable group-level attributes (allow_access, key_vault references) using standard attribute table

### Out of Scope

- Displaying the actual values of `secret_variable` entries (security requirement)
- Key Vault-linked variable groups (will use default template or future enhancement)
- Variable group permissions or access control details
- Standalone `azuredevops_variable` resources (if they exist)
- Custom sorting options beyond the natural order from Terraform
- Expandable/collapsible sections per variable (maintain single-table format)

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

With the specialized template, the same changes are rendered with full transparency for non-secret variables:

```markdown
<details open>
<summary>🔄 azuredevops_variable_group <b><code>example</code></b> — <code>example-variables</code> | 2 🔧 variables</summary>
<br>

### 🔄 azuredevops_variable_group.example

**Variable Group:** `example-variables`

**Description:** `Variable group for example pipeline`

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
| ➕ | `ENV` | `Production` | - | - | - |
| 🔄 | `APP_VERSION` | - `1.0.0`<br>+ `1.0.0` | - `false`<br>+ `false` | - | - |
| ❌ | `ENVIRONMENT` | `development` | `false` | - | - |

**Note:** Secret variables are not displayed for security reasons.

</details>
```

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

**Note:** Secret variables are not displayed for security reasons.

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

**Note:** Secret variables are not displayed for security reasons.

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

**Note:** Secret variables are not displayed for security reasons.

</details>
```

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

1. **Non-secret variables** are in the `variable` array
2. **Secret variables** are in the `secret_variable` array
3. The `secret_variable` array is marked as `true` in `before_sensitive` and `after_sensitive`
4. Individual entries in the `variable` array are **not marked sensitive** (they are empty objects `{}` in the sensitivity markers)
5. Variable attributes include: `name`, `value`, `enabled`, `content_type`, `expires`
6. Not all attributes are always present (e.g., `enabled` may be null/missing for new variables)

### Security Consideration

The template must **never display** any entry from the `secret_variable` array. Only the `variable` array should be processed and displayed.

### Empty Value Handling

- `enabled`: If null or missing, display as `-` (dash)
- `content_type`: If empty string or null, display as `-`
- `expires`: If empty string or null, display as `-`
- `value`: Should always be present for non-secret variables; if missing, display as `(null)`

### Semantic Diffing

Variables should be matched by their `name` attribute (similar to NSG security rules). This ensures that:
- Adding a new variable is clearly marked as ➕
- Removing a variable is clearly marked as ❌
- Modifying a variable shows before/after values as 🔄
- Reordering variables doesn't create false change indicators

## Success Criteria

- [ ] Template created for `azuredevops_variable_group` resource type at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azuredevops/variable_group.sbn`
- [ ] Template directory created for provider-specific templates: `Templates/azuredevops/`
- [ ] Non-secret variables displayed in table format with all relevant columns
- [ ] Variables categorized correctly as Added, Modified, Removed, or Unchanged using semantic matching by name
- [ ] Secret variables (`secret_variable` array) are completely hidden with a clear note explaining why
- [ ] Large variable values (>100 chars or multi-line) handled using existing large value display mechanism
- [ ] Modified variables show before/after values with `-` and `+` prefixes for changed attributes
- [ ] Unchanged attributes in modified variables show single value without prefix
- [ ] Empty/null attribute values displayed as `-` (dash)
- [ ] Create, update, and delete operations each have appropriate table layouts
- [ ] Variable group metadata (name, description) displayed prominently
- [ ] Summary line includes variable group name and change count for updates
- [ ] Template follows Report Style Guide formatting standards (code formatting for values, plain text for labels)
- [ ] All existing tests pass
- [ ] New tests verify variable group rendering for:
  - Create operation with non-secret variables
  - Update operation with added/modified/removed variables
  - Delete operation with non-secret variables
  - Presence of secret variables (verify they are not displayed)
  - Large variable values
  - Empty/null attribute values
- [ ] Documentation updated in `docs/features.md`
- [ ] Example output included in feature documentation

## Open Questions

**Q: Should we show a count of secret variables that exist but are hidden?**

**A:** Yes, for transparency. Add a note like:
```markdown
**Note:** This variable group contains 2 secret variables that are not displayed for security reasons.
```

This helps users understand that secret variables exist without exposing their values.

**Q: How should we handle variables with null/unknown values (shown as "known after apply")?**

**A:** Display as `(known after apply)` in the Value column, matching Terraform's standard terminology.

**Q: Should the Enabled column show checkmarks instead of true/false?**

**A:** No. Use standard boolean representation or dash for consistency with the rest of tfplan2md. The Report Style Guide specifies that data values should be rendered as inline code (backticks).

**Q: What if a variable group has ONLY secret variables and no regular variables?**

**A:** Show the variable group metadata and a note:
```markdown
**Note:** This variable group contains only secret variables, which are not displayed for security reasons.
```

Do not show an empty table.

**Q: Should we display key_vault-linked variable groups differently?**

**A:** Out of scope for this feature. If a variable group uses Key Vault integration, the template can either:
1. Fall back to the default template (showing key_vault configuration), or
2. Show a message indicating Key Vault integration and defer detailed variable display to future enhancement

For the initial implementation, fallback to default template is acceptable.
