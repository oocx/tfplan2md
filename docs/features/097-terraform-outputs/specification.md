# Feature: Terraform Outputs

## Overview

Add support for rendering Terraform outputs in tfplan2md reports. Outputs are shown as tables with output name, description, sensitivity status, and value. The feature intelligently positions module outputs with their containing modules and global outputs at the end of the report. It ensures sensitive output values are masked by default to prevent accidental credential exposure, with an optional CLI flag to reveal them when needed.

This feature enhances plan reviews by showing what data will be exposed from the Terraform configuration, helping teams verify outputs before applying changes.

## User Goals

- **See planned outputs**: Understand what values will be exported from the Terraform configuration after apply
- **Verify output values**: Review output values (especially computed ones) before they are finalized
- **Prevent credential leaks**: Ensure sensitive outputs are masked by default to avoid exposing secrets in PR reviews or logs
- **Leverage existing display enhancements**: Benefit from existing display name mappings (e.g., Azure resource IDs, principal mappings) in output values automatically
- **Understand module boundaries**: See which outputs belong to which modules for better context
- **Control sensitivity display**: Optionally reveal sensitive values when appropriate (e.g., in secure environments)

## Scope

### In Scope

#### 1. Output Data Model

Terraform plan JSON includes outputs in two locations:

1. **`output_changes`** (top-level): Contains output value changes with actions (`create`, `update`, `delete`, `no-op`)
   - Structure per output:
     - `actions`: Array of actions (e.g., `["create"]`, `["update"]`, `["no-op"]`)
     - `before`: Value before the change (for updates/deletes)
     - `after`: Value after the change (for creates/updates)
     - `after_unknown`: Boolean indicating if value is computed/unknown
     - `before_sensitive`: Boolean indicating if before value is sensitive
     - `after_sensitive`: Boolean indicating if after value is sensitive

2. **`configuration.root_module.outputs`** (and nested modules): Contains output metadata
   - Structure per output:
     - `description`: Optional human-readable description
     - `sensitive`: Optional boolean (defaults to false if not present in configuration)
     - `expression.references`: Array of resource/variable references the output depends on

**Module Outputs:**
- Found in `configuration.root_module.modules[].outputs` (nested structure)
- Each module has its own `outputs` object with the same structure as root outputs

#### 2. Output Positioning

- **Module outputs**: Rendered immediately after the module's resource changes (within the module's section)
- **Global/root outputs**: Rendered in a dedicated section after all resource changes (after the last module)
- **No outputs**: If there are no outputs, the section is omitted entirely (no empty "Outputs" section)

#### 3. Output Table Format

Each output is rendered as a single-row table with 4 columns:

| Column | Content | Formatting |
|--------|---------|------------|
| **Name** | Output name | Code-formatted (backticks) |
| **Description** | Output description from configuration | Plain text (or `-` if no description) |
| **Sensitive** | Whether output is marked as sensitive | `Yes` (plain text) or `-` |
| **Value** | The output value (before/after depending on action) | Code-formatted, with display enhancements applied |

**Header Format:**
```markdown
| Name | Description | Sensitive | Value |
|------|-------------|-----------|-------|
```

**Example Row:**
```markdown
| `repository_id` | The ID of the created Git repository | - | `80128bc2-17ff-45f8-ad59-d7609a605c75` |
```

#### 4. Sensitive Value Masking

**Default Behavior (without `--show-sensitive`):**
- If `after_sensitive` is `true`: Display `(sensitive value)` in plain text instead of the actual value
- If `before_sensitive` is `true` and showing before value: Display `(sensitive value)`
- The "Sensitive" column still shows `Yes` to indicate it's a sensitive output

**With `--show-sensitive` flag:**
- Display actual sensitive values in code-formatted text
- The "Sensitive" column still shows `Yes` for transparency

**Rationale:**
- Prevents accidental credential exposure in PR reviews, logs, CI/CD output
- Follows Terraform's CLI pattern of hiding sensitive outputs by default
- Users can opt-in when working in secure environments

#### 5. Display Name Mappings

Output values should leverage ALL existing display name mapping and formatting features automatically:

- **Azure Resource IDs**: Apply universal Azure resource ID formatting (Feature 042)
- **Principal Mappings**: Resolve principal IDs (users, groups, service principals) to display names (Feature 042)
- **Subscription Display Names**: Show subscription display names (Feature 042)
- **Management Group Display Names**: Show management group display names (Feature 042)
- **Role Display Names**: Show role names instead of GUIDs (Feature 042)
- **Resource-specific enhancements**: Any special formatting for specific resource types
- **Icons and visual formatting**: Apply existing icon/formatting rules where applicable

**How it works:**
- Output values reference resource attributes (e.g., `azuredevops_git_repository.example.id`)
- When the output value is a resource ID or contains mappable identifiers, apply the same formatting used for resource attributes
- This happens automatically through the existing value rendering pipeline

**Example:**
```terraform
output "new_repository_id" {
  value = azuredevops_git_repository.new_repo.id
}
```

If the display name mapping feature is enabled for Azure DevOps repositories, the output value will show:
```markdown
| `new_repository_id` | The ID of the new repository | - | `new-repo-name (80128bc2-17ff-45f8-ad59-d7609a605c75)` |
```

#### 6. Output Actions and Value Display

Outputs can have different actions, similar to resources:

- **`create`**: New output being added
  - Show `after` value
  - If `after_unknown` is `true`: Show `(known after apply)` in plain text

- **`update`**: Existing output value changing
  - Show `after` value
  - If `after_unknown` is `true`: Show `(known after apply)` in plain text
  - Future enhancement: Could show before → after diff in a 2-row format

- **`delete`**: Output being removed
  - Show `before` value
  - **Note**: Output deletes are rare (only when output is removed from configuration)

- **`no-op`**: Output value unchanged
  - Show current value (from `before` or `after`, they're identical)
  - Include no-op outputs in the table (unlike resources, users want to see all outputs)

#### 7. CLI Flag

Add new command-line flag:

```
--show-sensitive
```

**Behavior:**
- When present: Display actual values for sensitive outputs
- When absent: Mask sensitive output values as `(sensitive value)`
- Applies to both module outputs and global outputs
- Does NOT affect resource attribute sensitivity masking (separate concern)

**Location in help text:**
- Group with other display/formatting flags (near `--debug`, `--show-unchanged-values`, etc.)

#### 8. Section Headers

**For module outputs:**
```markdown
#### Outputs
```
(4th-level header, nested within the module's 3rd-level section)

**For global outputs:**
```markdown
## Outputs
```
(2nd-level header, same level as "Summary", "Resource Changes")

#### 9. Ordering

**Within each outputs section:**
- Alphabetical by output name
- Consistent, predictable ordering for review

**Module output sections:**
- Appear after the module's resource changes, before the next module

**Global output section:**
- Appears after all resource changes and module sections
- Before debug information (if present)

### Out of Scope

- **Before/After diff view for updated outputs**: Show only the `after` value for updates (future enhancement could show before → after)
- **Output dependency visualization**: Don't show which resources an output depends on (that info is in `expression.references` but not displayed)
- **Output grouping by type**: Don't group outputs by type or category (use alphabetical ordering)
- **Computed value prediction**: If `after_unknown` is `true`, show `(known after apply)` - don't attempt to predict the value
- **Module output summary counts**: Don't show counts like "3 outputs" in module headers
- **Changes to resource attribute sensitivity masking**: This feature only affects OUTPUT values, not resource attributes
- **Filtering outputs by sensitivity**: No flag to hide/show only sensitive or non-sensitive outputs
- **Output value history**: Don't show historical output values across plan versions

## User Experience

### Module Outputs Example

For a module `module.database`:

```markdown
### Module: `module.database`

<details>
<summary>➕ azurerm_postgresql_server <b><code>main</code></b> — <code>db-prod-001</code> <code>🌍 eastus</code></summary>
<br>

| Attribute | Value |
|-----------|-------|
| name | `db-prod-001` |
| location | `eastus` |
| sku_name | `GP_Gen5_2` |

</details>

#### Outputs

| Name | Description | Sensitive | Value |
|------|-------------|-----------|-------|
| `connection_string` | PostgreSQL connection string | Yes | (sensitive value) |
| `database_id` | The ID of the PostgreSQL server | - | `/subscriptions/abc-123.../resourceGroups/rg-db/providers/Microsoft.DBforPostgreSQL/servers/db-prod-001` |
| `fqdn` | Fully qualified domain name | - | `db-prod-001.postgres.database.azure.com` |

---
```

### Global Outputs Example

At the end of the report (after all resource changes):

```markdown
## Outputs

| Name | Description | Sensitive | Value |
|------|-------------|-----------|-------|
| `application_url` | The URL of the deployed application | - | `https://app-prod.azurewebsites.net` |
| `pipeline_id` | The ID of the created build pipeline | - | (known after apply) |
| `repository_id` | The ID of the created Git repository | - | `example-repo (80128bc2-17ff-45f8-ad59-d7609a605c75)` |
| `storage_account_key` | Primary access key for storage account | Yes | (sensitive value) |
```

### With `--show-sensitive` Flag

```markdown
## Outputs

| Name | Description | Sensitive | Value |
|------|-------------|-----------|-------|
| `application_url` | The URL of the deployed application | - | `https://app-prod.azurewebsites.net` |
| `storage_account_key` | Primary access key for storage account | Yes | `SomeActualSecretKeyValue123==` |
```

### No Outputs Scenario

If the plan has no outputs:
- The "Outputs" section is omitted entirely
- No placeholder or "No outputs" message

### Display Name Mapping Applied

For an output that references an Azure resource ID:

```terraform
output "key_vault_id" {
  value = azurerm_key_vault.main.id
}
```

**Without mapping:**
```markdown
| `key_vault_id` | The ID of the key vault | - | `/subscriptions/d1828a48-.../resourceGroups/rg-kv/providers/Microsoft.KeyVault/vaults/kv-prod-001` |
```

**With Azure display name mappings:**
```markdown
| `key_vault_id` | The ID of the key vault | - | Key Vault `kv-prod-001` in resource group `rg-kv` of subscription `Production (d1828a48-...)` |
```

## Success Criteria

- [ ] Outputs are parsed from Terraform plan JSON (`output_changes` and `configuration` sections)
- [ ] Module outputs appear immediately after their module's resource changes
- [ ] Global/root outputs appear in a dedicated section after all resource changes
- [ ] Output tables have 4 columns: Name, Description, Sensitive, Value
- [ ] Output names are code-formatted (backticks)
- [ ] Output descriptions are plain text (or `-` if absent)
- [ ] "Sensitive" column shows `Yes` for sensitive outputs, `-` otherwise
- [ ] Output values are code-formatted (backticks) when displayed
- [ ] Sensitive output values show `(sensitive value)` by default (plain text, not code-formatted)
- [ ] Computed values show `(known after apply)` when `after_unknown` is `true` (plain text, not code-formatted)
- [ ] `--show-sensitive` flag reveals actual sensitive values (code-formatted)
- [ ] Display name mappings (Azure IDs, principals, subscriptions, etc.) apply to output values automatically
- [ ] Outputs are ordered alphabetically by name within each section
- [ ] `create` action outputs show `after` value
- [ ] `update` action outputs show `after` value
- [ ] `delete` action outputs show `before` value
- [ ] `no-op` action outputs show current value
- [ ] Plans with no outputs omit the Outputs section entirely
- [ ] Module outputs use 4th-level header (`#### Outputs`)
- [ ] Global outputs use 2nd-level header (`## Outputs`)
- [ ] Report style guide is updated to document output table format
- [ ] CLI help text includes `--show-sensitive` flag with clear description
- [ ] Existing tests pass (no regression in other features)
- [ ] New tests cover output rendering with various scenarios (sensitive, computed, module vs global, display name mappings)

## Open Questions

### For Architect

1. **Data Model Extension**: Should we extend the `TerraformPlan` record to include optional `OutputChanges` property, or parse outputs on-demand in the rendering pipeline?

2. **Output Metadata Correlation**: `output_changes` has the values but not descriptions. `configuration.root_module.outputs` has descriptions but not values. How should we efficiently correlate these two data sources for each output?

3. **Module Output Parsing**: Module outputs are nested in `configuration.root_module.modules[].outputs`. Should we build a module-to-outputs mapping during parsing, or query on-demand during rendering?

4. **Value Rendering Pipeline**: Should output values go through the same rendering pipeline as resource attribute values to automatically get display name mappings, or do they need special handling?

5. **Sensitivity Detection**: Some outputs might have `sensitive = true` in configuration but `after_sensitive = false` in output_changes (or vice versa). Which should take precedence for masking?

6. **Update Actions**: For `update` action outputs, should we show before → after in the current phase, or defer that to a future enhancement and only show `after` value for now?

### For Maintainer

None at this time - all user-facing requirements have been clarified.
