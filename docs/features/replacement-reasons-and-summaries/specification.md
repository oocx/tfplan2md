# Feature: Replacement Reasons and Resource Summaries

## Overview

Add summary lines to all resource types (not just role assignments and firewall rules) to help users quickly decide whether to expand the details section. Additionally, parse and display replacement reasons from Terraform plan JSON to explain why resources need to be recreated.

## User Goals

- **Quick scanning**: Understand what's changing without expanding every resource
- **Better decisions**: Quickly determine if a change needs detailed review
- **Understand replacements**: See which attribute change is forcing a resource replacement
- **Consistent experience**: Similar summary format across all resource types

## Scope

### In Scope

**Generic Resource Summaries (All Resources):**
- Add summary line above `<details>` section for all resource types
- Use hybrid approach tailored to action type:
  - **CREATE**: Show resource name and key identifying attributes
  - **UPDATE**: Show resource name and list of changed attributes (up to 3, then "+ N more")
  - **REPLACE**: Show resource name, replacement reason (when available), and change count
  - **DELETE**: Show resource name
- Intelligent key attribute selection based on provider and resource type
- Provider-specific fallback rules when no resource-specific mapping exists
- Generic fallback for unmapped providers

**Replacement Reasons:**
- Parse `replace_paths` from Terraform plan JSON (Terraform 1.2+)
- Display which attribute(s) forced replacement in REPLACE summaries
- Gracefully handle plans without `replace_paths` (older Terraform versions)

**Key Attribute Mappings:**

Priority order for CREATE summaries:
1. Resource-type-specific mappings (e.g., `azurerm_subnet` → show name, vnet, address_prefixes)
2. Provider-level fallback (e.g., azurerm default → show name, resource_group_name, location)
3. Generic fallback → show `name` or `display_name` (first that exists)

**Initial Resource Type Mappings:**

*azurerm (43 resources with unique attributes):*
- `azurerm_resource_group`: name, location
- `azurerm_storage_account`: name, resource_group_name, location, account_tier, account_replication_type
- `azurerm_virtual_network`: name, resource_group_name, location, address_space[0]
- `azurerm_subnet`: name, virtual_network_name, address_prefixes[0]
- `azurerm_public_ip`: name, resource_group_name, location, allocation_method
- `azurerm_key_vault`: name, resource_group_name, location, sku_name
- `azurerm_key_vault_secret`: name, key_vault_id
- `azurerm_key_vault_key`: name, key_vault_id, key_type
- `azurerm_linux_virtual_machine`: name, resource_group_name, location, size
- `azurerm_windows_virtual_machine`: name, resource_group_name, location, size
- `azurerm_linux_virtual_machine_scale_set`: name, resource_group_name, location, sku
- `azurerm_windows_virtual_machine_scale_set`: name, resource_group_name, location, sku
- `azurerm_kubernetes_cluster`: name, resource_group_name, location, kubernetes_version
- `azurerm_container_registry`: name, resource_group_name, location, sku
- `azurerm_container_app`: name, resource_group_name, location, container_app_environment_id
- `azurerm_app_service`: name, resource_group_name, location, app_service_plan_id
- `azurerm_app_service_plan`: name, resource_group_name, location, sku_name
- `azurerm_linux_web_app`: name, resource_group_name, location, service_plan_id
- `azurerm_windows_web_app`: name, resource_group_name, location, service_plan_id
- `azurerm_linux_function_app`: name, resource_group_name, location, service_plan_id
- `azurerm_windows_function_app`: name, resource_group_name, location, service_plan_id
- `azurerm_mssql_server`: name, resource_group_name, location, version
- `azurerm_mssql_database`: name, server_id
- `azurerm_mysql_flexible_server`: name, resource_group_name, location, sku_name
- `azurerm_postgresql_flexible_server`: name, resource_group_name, location, sku_name
- `azurerm_cosmosdb_account`: name, resource_group_name, location, offer_type
- `azurerm_redis_cache`: name, resource_group_name, location, sku_name
- `azurerm_log_analytics_workspace`: name, resource_group_name, location, sku
- `azurerm_application_insights`: name, resource_group_name, location, application_type
- `azurerm_monitor_action_group`: name, resource_group_name
- `azurerm_service_plan`: name, resource_group_name, location, os_type, sku_name
- `azurerm_dns_zone`: name, resource_group_name
- `azurerm_private_dns_zone`: name, resource_group_name
- `azurerm_lb`: name, resource_group_name, location, sku
- `azurerm_application_gateway`: name, resource_group_name, location, sku
- `azurerm_firewall`: name, resource_group_name, location, sku_name
- `azurerm_express_route_circuit`: name, resource_group_name, location, service_provider_name
- `azurerm_api_management`: name, resource_group_name, location, sku_name
- `azurerm_databricks_workspace`: name, resource_group_name, location, sku
- `azurerm_automation_account`: name, resource_group_name, location, sku_name
- `azurerm_recovery_services_vault`: name, resource_group_name, location, sku
- `azurerm_storage_blob`: name, storage_account_name, storage_container_name
- `azurerm_managed_disk`: name, resource_group_name, location, storage_account_type

*azurerm provider fallback:* name, resource_group_name, location

*azuredevops (1 resource with unique attributes):*
- `azuredevops_project`: name, visibility

*azuredevops provider fallback:* name, project_id

*azuread (3 resources with unique attributes):*
- `azuread_group`: display_name, security_enabled (show "(Security)" if true, omit if false)
- `azuread_user`: display_name, user_principal_name
- `azuread_service_principal`: display_name, application_id

*azuread provider fallback:* display_name

*azapi (2 resources with unique attributes):*
- `azapi_resource`: name, type, parent_id
- `azapi_resource_action`: action, resource_id

*azapi provider fallback:* name, type

*msgraph (4 generic resources):*
- `msgraph_resource`: url, body.displayName (if exists)
- `msgraph_update_resource`: url, body.displayName (if exists)
- `msgraph_resource_action`: url, action
- `msgraph_resource_collection`: url, parent_id

*msgraph provider fallback:* url (or body.displayName if exists)

**Note:** The microsoft/msgraph provider uses generic resource types with URL-based routing rather than specific resource types. The `url` attribute indicates what MS Graph API resource is being managed (e.g., "applications", "users", "groups").

*Generic fallback (any unmapped provider):* name or display_name (first that exists)

### Out of Scope

- Custom templates for individual resource types (already handled by existing resource-specific template system)
- Changes to role assignment or firewall rule templates (they already have summaries)
- Summarization of complex nested structures (just show attribute names, not deep analysis)
- Configurable summary formats (use fixed format for consistency)

## User Experience

### CREATE Example

#### With Resource-Specific Mapping

**azurerm_storage_account:**

```
#### ➕ azurerm_storage_account.logs

**Summary:** `sttfplan2mdlogs` in `rg-tfplan2md-demo` (eastus) | Standard LRS

<details>
...
</details>
```

#### With Provider Fallback

**azurerm_monitor_action_group (no specific mapping):**

```
#### ➕ azurerm_monitor_action_group.alerts

**Summary:** `ag-critical-alerts` in `rg-tfplan2md-demo` (eastus)

<details>
...
</details>
```

#### With Generic Fallback

**random_string (unmapped provider):**

```
#### ➕ random_string.suffix

**Summary:** 8 characters

<details>
...
</details>
```

### UPDATE Example

```
#### 🔄 azurerm_storage_account.data

**Summary:** `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center

<details>
...
</details>
```

**With many changes:**

```
#### 🔄 azurerm_key_vault.main

**Summary:** `kv-tfplan2md` | Changed: soft_delete_enabled, purge_protection_enabled, network_acls, +3 more

<details>
...
</details>
```

### REPLACE Example

**With replacement reason (Terraform 1.2+):**

```
#### ♻️ azurerm_subnet.db

**Summary:** recreate `snet-db` (address_prefixes changed: forces replacement)

<details>
...
</details>
```

**Without replacement reason (older Terraform or missing data):**

```
#### ♻️ azurerm_network_security_group.app

**Summary:** recreating `nsg-app` (1 changed)

<details>
...
</details>
```

**Multiple attributes causing replacement:**

```
#### ♻️ azurerm_linux_virtual_machine.web

**Summary:** recreate `vm-web-01` (size, admin_username changed: force replacement)

<details>
...
</details>
```

### DELETE Example

```
#### ❌ azurerm_storage_account.legacy

**Summary:** `sttfplan2mdlegacy`

<details>
...
</details>
```

## Technical Details

### Data Model Changes

**Update `Change` record in TerraformPlan.cs:**

```csharp
public record Change(
    [property: JsonPropertyName("actions")] IReadOnlyList<string> Actions,
    [property: JsonPropertyName("before")] object? Before,
    [property: JsonPropertyName("after")] object? After,
    [property: JsonPropertyName("after_unknown")] object? AfterUnknown,
    [property: JsonPropertyName("before_sensitive")] object? BeforeSensitive,
    [property: JsonPropertyName("after_sensitive")] object? AfterSensitive,
    [property: JsonPropertyName("replace_paths")] IReadOnlyList<IReadOnlyList<object>>? ReplacePaths = null
);
```

**Add to `ResourceChangeModel`:**

```csharp
public class ResourceChangeModel
{
    // ... existing properties ...
    
    /// <summary>
    /// Paths to attributes that triggered replacement (from replace_paths in plan JSON).
    /// Each path is an array representing the attribute path (e.g., ["address_prefixes", 0]).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; init; }
    
    /// <summary>
    /// Human-readable summary of the resource change for quick scanning.
    /// Format varies by action type.
    /// </summary>
    public string? Summary { get; init; }
}
```

### Implementation Approach

**1. Summary Generation (ReportModelBuilder or new helper class):**

- Create `ResourceSummaryBuilder` class with:
  - `BuildSummary(ResourceChangeModel change)` → returns summary string
  - `GetKeyAttributes(string provider, string resourceType, object state)` → returns key attribute values
  - Provider/resource type mapping registry

**2. Key Attribute Selection Logic:**

```
1. Parse provider from resource type (e.g., "azurerm" from "azurerm_storage_account")
2. Look up resource-specific mapping (e.g., azurerm_storage_account → [name, resource_group_name, location, account_tier, account_replication_type])
3. If no specific mapping, use provider fallback (e.g., azurerm → [name, resource_group_name, location])
4. If no provider mapping, use generic fallback ([name] or [display_name])
5. Extract values from state object (after for CREATE, before for DELETE, after for UPDATE/REPLACE)
6. Format as summary string
```

**3. Replacement Reason Parsing:**

```
1. Check if ReplacePaths exists and is not empty
2. Convert paths to human-readable attribute names (e.g., ["address_prefixes", 0] → "address_prefixes[0]")
3. Format as: "attribute1, attribute2 changed: force replacement"
4. Limit to 3 attributes, then "+ N more"
```

**4. Template Changes:**

Update `default.sbn` to include `{{ change.summary }}` above the `<details>` tag:

```scriban
{{ if change.summary }}
**Summary:** {{ change.summary }}

{{ end }}
<details>
```

### Formatting Rules

**CREATE:**
- Show key attributes in format: `name` in `resource_group` (location) | tier/type
- Use backticks for identifiers
- Use parentheses for supplementary info
- Use pipe (|) to separate logical groups

**UPDATE:**
- Format: `name` | Changed: attr1, attr2, attr3, +N more
- Show up to 3 changed attributes by name
- Add "+ N more" if more than 3 changes

**REPLACE:**
- With reason: recreate `name` (attr1, attr2 changed: force replacement)
- Without reason: recreating `name` (N changed)
- Show up to 3 replacement paths

**DELETE:**
- Format: `name` (simple, just identification)

## Success Criteria

- [ ] `replace_paths` field is parsed from Terraform plan JSON
- [ ] `ReplacePaths` property is added to `ResourceChangeModel`
- [ ] `Summary` property is added to `ResourceChangeModel`
- [ ] Summary is generated for all CREATE operations using key attribute mappings
- [ ] Summary is generated for all UPDATE operations showing changed attributes
- [ ] Summary is generated for all REPLACE operations with replacement reason when available
- [ ] Summary is generated for all DELETE operations showing resource name
- [ ] 20 azurerm resource types have specific key attribute mappings
- [ ] 3+ azuredevops resource types have specific key attribute mappings
- [ ] 3+ azuread resource types have specific key attribute mappings
- [ ] 3+ azapi resource types have specific key attribute mappings
- [ ] 3+ msgraph resource types have specific key attribute mappings
- [ ] Provider-level fallback works for unmapped resource types within mapped providers
- [ ] Generic fallback works for completely unmapped providers
- [ ] Summary line appears above `<details>` in default template
- [ ] Summary formatting matches specification (backticks, pipes, parentheses)
- [ ] Tests verify summary generation for all action types
- [ ] Tests verify key attribute selection for specific mappings, provider fallback, and generic fallback
- [ ] Tests verify replacement reason parsing and display
- [ ] Comprehensive demo is updated with new summaries
- [ ] Documentation updated with new feature

## Open Questions

- Should we show attribute values in UPDATE summaries, or just names? (Recommendation: names only to keep summaries concise)
- For resources without a `name` attribute, should we show the Terraform address as fallback? (Recommendation: yes, use resource address if no name/display_name exists)
- Should replacement reason handling be case-sensitive for attribute names? (Recommendation: preserve case from Terraform)
- Should we limit summary line length and truncate if needed? (Recommendation: no truncation, summaries should be naturally short)
