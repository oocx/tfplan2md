# Features

This document describes the features of `tfplan2md` from a user perspective.

## Overview

`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is designed primarily for use in CI/CD pipelines and is distributed as a Docker image.

## Input

- **Stdin (default)**: Read Terraform plan JSON from standard input
  ```bash
  terraform show -json plan.tfplan | tfplan2md
  ```
- **File path**: Read from a file specified as an argument
  ```bash
  tfplan2md plan.json
  ```

## Output

- **Stdout (default)**: Write markdown to standard output
- **File**: Write to a file using the `--output` flag
  ```bash
  tfplan2md plan.json --output plan.md
  ```

## Report Content

The default report includes:

- **Summary**: Overview of changes with count and resource type breakdown for each action (e.g., "3 to add: 1 azurerm_resource_group, 2 azurerm_storage_account")
- **Detailed changes**: List of affected resources with their actions and attribute changes

### Summary Resource Type Breakdown

The summary table includes a "Resource Types" column that shows which resource types are affected by each action. This helps users quickly understand what types of resources are being created, modified, replaced, or destroyed without reading through the detailed changes section.

**Features:**
- Shows count and full resource type name for each type (e.g., "3 azurerm_storage_account")
- Resource types are sorted alphabetically within each action
- Each resource type appears on its own line using HTML `<br/>` tags
- Empty when an action has 0 resources
- The Total row does not show a breakdown (only the total count)

**Example:**
```markdown
| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | 6 | 1 azurerm_resource_group<br/>3 azurerm_storage_account<br/>2 azurerm_virtual_network |
| 🔄 Change | 3 | 2 azurerm_app_service<br/>1 azurerm_sql_database |
| ♻️ Replace | 1 | 1 azurerm_kubernetes_cluster |
| ❌ Destroy | 0 | |
| **Total** | **10** | |
```

### No-Op Resources

Resources with no changes (no-op) are counted in the summary but are **not displayed** in the detailed changes section. This design choice:
- Reduces output noise when reviewing plans with many unchanged resources
- Enables processing of large Terraform plans without hitting template iteration limits

### Action Symbols

Resources are displayed with emoji symbols indicating the action:
- `➕` - create (add new resource)
- `🔄` - update (modify existing resource)
- `❌` - delete (remove resource)
- `♻️` - replace (delete and recreate resource)

### Sensitive Values

- Sensitive values are **masked by default** for security
- Use `--show-sensitive` flag to reveal sensitive values in the output

### Attribute Tables

Attribute tables in the default template now vary by the resource change action to make output more concise and meaningful:

- **create**: 2-column table (`Attribute | Value`) showing the *after* values
- **delete**: 2-column table (`Attribute | Value`) showing the *before* values
- **update** and **replace**: 3-column table (`Attribute | Before | After`)

Null or unknown attributes are omitted from the tables to avoid meaningless rows, and sensitive attributes are masked unless `--show-sensitive` is used.

### Module Grouping (NEW)

Resource changes in the report are now grouped by Terraform module. Each module with at least one change is rendered as its own section (module sections for modules without changes are omitted to keep reports concise).

- **Module header**: Each module is shown as an H3 heading ("### Module: <module_address>"), where `module_address` is the full module path from the Terraform plan (e.g., `module.network.module.subnet`). The root module is shown as `root`.
- **Resource headings**: Resources within a module are shown as H4 headings ("#### <action_symbol> <address>") to preserve a proper document hierarchy.
- **Ordering**: Modules are listed so that the root module appears first, followed by other modules in lexicographic order. Nested modules are presented in a flat list but the sort order ensures child modules follow their parent modules.
- **Template variable**: Templates have access to a top-level `module_changes` collection (in addition to the existing `changes` collection). Each item has:
  - `module_address` (string, empty for root)
  - `changes` (array of resource change objects, same structure as items in `changes`)

Example usage in a Scriban template:

```scriban
{{ for module in module_changes }}
### Module: {{ if module.module_address && module.module_address != "" }}`{{ module.module_address }}`{{ else }}root{{ end }}

{{ for change in module.changes }}
#### {{ change.action_symbol }} {{ change.address }}

... render attribute tables ...

{{ end }}

---
{{ end }}
```

This grouping is enabled by default and cannot be disabled (it keeps reports concise and improves readability for multi-module plans).

## CI/CD Integration

### Cumulative Release Notes

When creating GitHub releases and Docker deployments, the release workflow automatically generates cumulative release notes that include all changes since the last release. This is particularly useful when not every version is deployed to Docker Hub.

**How it works:**
- The release workflow queries the GitHub API to find the last published release
- It extracts all changelog sections between that version and the current version being released
- GitHub release notes include accumulated changes from all intermediate versions
- If no previous release exists (first release), only the current version's changes are included

**Example:**
- Last Docker release: v0.8.0
- Versions created since then: v0.9.0, v0.10.0, v0.11.0
- When releasing v0.12.0, the release notes will include changes from v0.9.0, v0.10.0, v0.11.0, and v0.12.0

This ensures Docker Hub users can see the complete set of changes included in each deployment, even when intermediate versions were skipped.

## Templates

Reports are generated using customizable templates powered by [Scriban](https://github.com/scriban/scriban).

- **Default template**: A built-in template is included in the Docker image
- **Custom templates**: Provide a custom template file using the `--template` flag
  ```bash
  tfplan2md plan.json --template /path/to/custom-template.md
  ```

### Template Data

Templates have access to all data required to render detailed change information, including:
- Resource addresses and types
- Change actions (create, update, delete, replace)
- Attribute changes (before/after values)
- Metadata from the Terraform plan

## CLI Interface

Simple single-command interface with flags:

| Flag | Description |
|------|-------------|
| `--output <file>` | Write output to a file instead of stdout |
| `--template <file>` | Use a custom Scriban template file |
| `--principal-mapping <file>` | Map Azure principal IDs to names using a JSON file |
| `--show-sensitive` | Show sensitive values unmasked |
| `--help` | Display help information |
| `--version` | Display version information |

## Error Handling

- **Exit codes**: `0` on success, non-zero on failure
- **Error messages**: Clear and actionable, written to stderr
- **No partial output**: The tool fails cleanly without producing incomplete reports
- **Lenient parsing**: Processes the plan as long as required fields are valid (no strict format validation)

## Terraform Compatibility

- Supports Terraform **1.14 and later**
- Lenient parsing approach—does not validate the full plan format, only the fields needed for report generation

## Enhanced Azure Role Assignment Display

The `azurerm_role_assignment` resource now displays human-readable information instead of cryptic GUIDs and technical paths.

**Features:**
- **Comprehensive built-in role mapping**: All 473 Azure built-in role definition GUIDs are automatically mapped to friendly names (e.g., "Reader", "Contributor", "Storage Blob Data Reader")
- **Hierarchical scope display**: Azure resource scopes are parsed and displayed with clear context:
  - Management Groups: `**my-mg** (Management Group)`
  - Subscriptions: `subscription **sub-id**`
  - Resource Groups: `**my-rg** in subscription **sub-id**`
  - Resources: Recognizes 20+ common Azure resource types with friendly names:
    - Compute: Virtual Machine, Virtual Machine Scale Set, AKS Cluster, Managed Disk
    - Storage: Storage Account, Key Vault, Container Registry, Cosmos DB Account
    - Networking: Virtual Network, Subnet, Load Balancer, Application Gateway, Azure Firewall, Private Endpoint
    - Web/Apps: App Service, App Service Plan, SQL Server, SQL Database
    - Monitoring: Log Analytics Workspace, Application Insights
    - Data: Azure Cache for Redis, Event Hubs Namespace, Service Bus Namespace
    - Graceful fallback to capitalized type names for unmapped resources
- **Optional principal mapping**: Map principal IDs to names using a JSON file with the `--principal-mapping` flag

**Example output:**
```markdown
#### ➕ azurerm_role_assignment.example (create)

- **scope**: **my-rg** in subscription **12345678-1234-1234-1234-123456789012**
- **role_definition_id**: Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)
- **principal_id**: John Doe (User) [abcdef01-2345-6789-abcd-ef0123456789]
```

**Usage:**
```bash
# Without principal mapping (role and scope are still enhanced)
tfplan2md plan.json

# With principal mapping
tfplan2md --principal-mapping principals.json plan.json

# Docker with principal mapping
docker run -v $(pwd):/data oocx/tfplan2md \
  --principal-mapping /data/principals.json /data/plan.json
```

**Creating a principal mapping file:**
```bash
# Generate mapping for all Azure AD principals
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

The principal mapping JSON format:
```json
{
  "principal-guid": "Display Name (Type)"
}
```

## Distribution

### Docker Image

- **Base image**: .NET Chiseled (distroless) for minimal attack surface
- **Registry**: Docker Hub (`oocx/tfplan2md`)
- **Tagging**: Semantic versioning with multiple tags per release:
  - Full version: `1.2.3`
  - Minor version: `1.2` (updated with each patch)
  - Major version: `1` (updated with each minor/patch)
  - `latest` (always points to the most recent release)

### Usage Example

```bash
# From stdin (using latest)
terraform show -json plan.tfplan | docker run -i oocx/tfplan2md

# Pin to specific version
docker run -i oocx/tfplan2md:1.2.3

# Pin to minor version (get patch updates automatically)
docker run -i oocx/tfplan2md:1.2

# From file (mounted volume)
docker run -v $(pwd):/data oocx/tfplan2md /data/plan.json --output /data/plan.md
```

### Releases

Docker images are automatically built and pushed when a new version tag is created. See [spec.md](spec.md) for details on the CI/CD process and versioning strategy.

## Resource-Specific Templates

Complex Terraform resources like firewall rule collections can have misleading diffs when using simple attribute-based comparison. When items shift indices in arrays, the default diff shows changes to every attribute after the insertion point.

Resource-specific templates solve this by:
- Comparing collection items semantically by key (e.g., rule name)
- Showing which items were added, removed, modified, or unchanged
- Providing clear, table-based output for complex nested structures

When rendering the full report, the default renderer applies resource-specific templates automatically for any resources that have a matching template; if none exists, the global default template is used.

### Supported Resources

| Provider | Resource Type | Template |
|----------|--------------|----------|
| azurerm | `azurerm_firewall_network_rule_collection` | Semantic rule diffing with `diff_array` |

#### Firewall Rule Collections

For `azurerm_firewall_network_rule_collection`, rules are rendered in a single table to provide a concise overview. Each rule is displayed as a row with standard attributes: Name, Protocols, Source, Destination, Destination Ports, and Description.

- **Added rules**: Shown with ➕ icon and the new values.
- **Removed rules**: Shown with ❌ icon and the old values.
- **Modified rules**: Shown with 🔄 icon. Changed attributes display both before and after values in the same cell, prefixed with `-` and `+` respectively, separated by `<br>` for visual clarity. Unchanged attributes show the single value without any prefix.
- **Unchanged rules**: Shown with ⏺️ icon for completeness.

Example of a modified rule with changed source addresses and description:
```markdown
| 🔄 | allow-http | TCP | - 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24 | * | 80 | - Allow HTTP traffic<br>+ Allow HTTP traffic from web and API tiers |
```

This layout makes it easy to inspect per-rule changes without index-shift noise from array diffs, and the diff-style formatting clearly shows what changed.

### Helper Functions

Templates have access to custom Scriban helper functions:

**`diff_array`** - Semantic collection diffing:
```scriban
{{ diff = diff_array before_json.rule after_json.rule "name" }}
{{ for rule in diff.added }}
  ➕ {{ rule.name }}
{{ end }}
```

**`format_diff`** - Before/after diff formatting:
```scriban
{{ format_diff (item.before.protocols | array.join ", ") (item.after.protocols | array.join ", ") }}
```
Returns the single value if unchanged, or `"- before<br>+ after"` if different.

See [resource-specific-templates.md](features/resource-specific-templates.md) for full specification.

## Future Considerations

The following features may be added in future versions:

- Additional resource-specific templates based on user feedback
- Provider-default templates (e.g., `Templates/azurerm/default.sbn`)
- `--list-templates` CLI option to list bundled templates
