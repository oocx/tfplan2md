# Feature: Enhanced Azure Role Assignment Display

## Overview

Transform Azure role assignment outputs in terraform plan reports from cryptic GUIDs and technical paths into human-readable information that developers can understand without consulting Azure documentation.

## User Goals

- Quickly understand which Azure role is being assigned (e.g., "Reader" instead of `acdd72a7-3385-48ef-bd42-f606fba81ae7`)
- Easily comprehend the scope of role assignments with clear hierarchical context
- Optionally identify principals by name instead of only by GUID
- Reduce time spent looking up Azure role definition IDs and parsing resource paths

## Scope

### In Scope

**Role Definition Enhancement:**
- Map Azure built-in role definition GUIDs to friendly names
- Display format: `"Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)"`
- Include comprehensive mapping of all known Azure built-in roles at time of implementation
- Custom role definitions remain unchanged (no mapping available)

**Scope Path Enhancement:**
- Parse Azure resource scope paths and display with human-readable context
- Support multiple scope types:
  - Management Group: `"my-mg-group (Management Group)"`
  - Subscription: `"subscription-name (subscription-id)"` or just ID if name unavailable
  - Resource Group: `"foo in subscription bar (subscription-id)"`
  - Specific Resources: `"Key Vault vault1 in resource group foo of subscription example-subscription (example-subscription-id)"`
- Handle common Azure resource types in resource-level scopes
- Differentiate resource names/IDs from connecting phrases with styling (bold, code formatting, or similar)

**Principal ID Enhancement:**
- Optional principal mapping via command-line argument
- CLI flag: `--principal-mapping <path>` or `--principals <path>`
- JSON format: `{"guid": "Display Name (Type)"}`
- Type included in display: `"John Doe (User)"`, `"DevOps Team (Group)"`, `"terraform-sp (Service Principal)"`
- If no mapping provided or ID not found, display raw GUID
- Documentation for generating mapping file using Azure CLI

**Resource Type:**
- Only affects `azurerm_role_assignment` resources
- Uses existing resource-specific template system

### Out of Scope

- Real-time Azure API lookups (requires authentication, adds latency)
- Automatic principal name resolution without user-provided mapping
- Mapping for custom role definitions (requires API access)
- Subscription name lookup from subscription ID (no data source available)
- Support for non-Azure providers
- Interactive prompts for missing principal mappings

## User Experience

### Command-Line Interface

**Without principal mapping (default):**
```bash
tfplan2md plan.json > report.md
```

**With principal mapping:**
```bash
tfplan2md --principal-mapping principals.json plan.json > report.md
```

**Docker without principal mapping:**
```bash
docker run --rm -v $(pwd):/workspace oocx/tfplan2md:latest /workspace/plan.json > report.md
```

**Docker with principal mapping:**
```bash
docker run --rm -v $(pwd):/workspace oocx/tfplan2md:latest \
  --principal-mapping /workspace/principals.json /workspace/plan.json > report.md
```

*Note: The principals.json file must be in the mounted directory and referenced with the container path*

### Markdown Output

**Implemented output format:**

Role assignments now use a table-based format with a human-readable summary line and collapsible details:

```markdown
#### ➕ azurerm_role_assignment.example

**Summary:** `John Doe` (User) → `Reader` on `my-rg`

<details>

| Attribute | Value |
|-----------|-------|
| `scope` | my-rg in subscription 12345678-1234-1234-1234-123456789012 |
| `role_definition_id` | Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7) |
| `principal_id` | John Doe (User) [abcdef01-2345-6789-abcd-ef0123456789] |
| `principal_type` | User |

</details>
```

**For update/replace actions**, the table shows Before/After columns:

```markdown
#### 🔄 azurerm_role_assignment.example

**Summary:** `Security Team` (Group) → `Storage Blob Data Contributor` on Storage Account `sttfplan2mddata`

Upgraded permissions for security auditing

<details>

| Attribute | Before | After |
|-----------|--------|-------|
| `scope` | Storage Account sttfplan2mdlogs in resource group rg-demo of subscription sub-one | Storage Account sttfplan2mddata in resource group rg-demo of subscription sub-one |
| `role_definition_id` | Storage Blob Data Reader (2a2b9908-6ea1-4ae2-8e65-a410df84e7d1) | Storage Blob Data Contributor (ba92f5b4-2d11-453d-a403-e96b0029c9fe) |
| `principal_id` | DevOps Team (Group) [22222222-2222-2222-2222-222222222222] | Security Team (Group) [33333333-3333-3333-3333-333333333333] |
| `principal_type` | Group | Group |
| `description` | Allow team to read storage data | Upgraded permissions for security auditing |

</details>
```

### Principal Mapping File Format

**JSON structure:**

The mapping file is a simple JSON object mapping principal IDs (GUIDs) to display names. The principal type (User, Group, ServicePrincipal) is automatically detected from the Terraform plan's `principal_type` attribute.

```json
{
  "abcdef01-2345-6789-abcd-ef0123456789": "John Doe",
  "11111111-2222-3333-4444-555555555555": "DevOps Team",
  "99999999-8888-7777-6666-555555555555": "terraform-sp"
}
```

The display format includes both the name and type: `"John Doe (User) [abcdef01-2345-6789-abcd-ef0123456789]"`. The type comes from the Terraform plan itself, not from the mapping file.

### Generating Principal Mapping

**Documentation will include Azure CLI commands:**

```bash
# Generate mapping for users, groups, and service principals
{
  echo "{"
  az ad user list --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (User)\",\n", $1, $2}'
  az ad group list --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (Group)\",\n", $1, $2}'
  az ad sp list --all --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s (Service Principal)\",\n", $1, $2}' | \
    sed '$ s/,$//'
  echo "}"
} > principals.json
```

**Example for limited scope (faster):**
```bash
# Only specific service principals
az ad sp list --filter "startswith(displayName,'terraform')" \
  --query "[].{id:id,name:displayName}" -o json | \
  jq 'map({(.id): (.displayName + " (Service Principal)")}) | add'
```

## Success Criteria

- [x] Built-in Azure role definition GUIDs are mapped to friendly names (473 Azure built-in roles)
- [x] Role display format includes both name and GUID: `Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)`
- [x] Scope paths are parsed and displayed with hierarchical context
- [x] Management group, subscription, resource group, and resource-level scopes are handled
- [x] Resource names in scopes use backtick code formatting for visual distinction
- [x] Optional `--principal-mapping` (alias: `--principals`, `-p`) CLI argument loads JSON mapping file
- [x] Principal IDs are enhanced when mapping is provided: `John Doe (User) [guid]`
- [x] Principal IDs display raw GUID when no mapping exists
- [x] Unmapped principals gracefully fall back to GUID display
- [x] Documentation includes Azure CLI commands for generating mapping file
- [x] Changes only affect `azurerm_role_assignment` resource type (resource-specific template)
- [x] Tests cover all scope types and mapping scenarios
- [x] Tests verify behavior with and without principal mapping
- [x] Backward compatibility maintained (existing reports still work)

## Implementation Notes

**Output Format:**
- Table-based layout with collapsible `<details>` sections
- Summary line shows principal → role → scope in a single line
- Description field (if present) appears between summary and details table
- `role_definition_name` attribute is not shown in the table (redundant with the enhanced `role_definition_id` display)

**Technical Details:**
- Scope parsing handles 20+ Azure resource types with friendly names
- Role mapping embedded in code for all Azure built-in roles
- Principal mapping loaded from JSON file via `PrincipalMapper` class
- Type-aware principal names use the `principal_type` attribute from Terraform plan
- Template helper functions: `azure_scope_info`, `azure_role_info`, `azure_principal_info`, `collect_attributes`

## Resolved Design Decisions

1. **Styling approach**: Resource names in scopes use backtick code formatting (e.g., `` `my-rg` ``). Principal and role names in the summary use backticks. The table content does not use additional formatting to keep it clean.

2. **Subscription name handling**: Not implemented. Subscription IDs are shown directly since there's no reliable way to map subscription IDs to names without API calls or additional configuration files.

3. **Error handling**: Malformed principal mapping files are handled gracefully—the mapper returns an empty dictionary and falls back to showing raw GUIDs. No error is thrown to the user.

4. **Azure role mapping source**: Built-in role mapping is embedded in code (`AzureRoleDefinitionMapper.Roles.cs`) for simplicity and performance. This avoids external file dependencies and ensures the mapping is always available.

5. **Resource type coverage**: Implemented 20+ common Azure resource types including:
   - Compute: Virtual Machine, AKS Cluster, Virtual Machine Scale Set, Managed Disk
   - Storage: Storage Account, Key Vault, Container Registry, Cosmos DB Account
   - Networking: Virtual Network, Subnet, Load Balancer, Application Gateway, Azure Firewall
   - Web/Data: App Service, SQL Server, SQL Database, Event Hubs, Service Bus
   - Monitoring: Log Analytics Workspace, Application Insights
   - Graceful fallback to capitalized type names for unmapped resources
