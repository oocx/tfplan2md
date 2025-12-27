# Features

This document describes the features of `tfplan2md` from a user perspective.

## Overview

`tfplan2md` is a CLI tool that converts Terraform plan JSON files into human-readable markdown reports. It is designed primarily for use in CI/CD pipelines and is distributed as a Docker image.

## Markdown Quality Validation

**Status:** ✅ Implemented (v0.26.0+)

All generated markdown is validated to ensure correct rendering on GitHub and Azure DevOps:

- **Automated linting** - CI runs markdownlint-cli2 on generated reports
- **Property-based invariants** - Tests verify markdown properties that must always hold (no consecutive blank lines, proper table structure, balanced HTML tags)
- **Snapshot testing** - Detects unexpected changes in output
- **Template isolation testing** - Each template is tested independently
- **Fuzz testing** - Edge cases (special characters, Unicode, long values) are automatically tested
- **Docker-based tools** - No local tool dependencies required

See [docs/markdown-specification.md](markdown-specification.md) for the supported markdown subset and [docs/testing-strategy.md](testing-strategy.md) for testing details.

## Consistent Value Formatting

**Status:** ✅ Implemented

To improve readability and scannability, `tfplan2md` uses a consistent formatting strategy across all reports:

- **Data Values**: All actual data values (resource names, IP addresses, configuration settings) are formatted as `inline code` (backticks). This makes them visually distinct from labels and descriptions.
- **Labels & Keys**: Attribute names, table headers, and descriptive text are rendered as plain text.
- **Summaries**: Resource summaries use code formatting for all identifiers and configuration values (e.g., `name`, `resource_group`, `location`).
- **Diffs**: Large value diffs support both styled HTML (inline-diff) and standard text (standard-diff) formats, configurable via CLI.

This ensures that the most important information—the values changing in your infrastructure—always stands out.

## Universal Azure Resource ID Formatting

**Status:** ✅ Implemented

Long Azure resource IDs from the `azurerm` provider are rendered as readable scopes instead of raw GUID paths and are kept inside attribute tables (not relegated to the “Large attributes” section).

- **Automatic detection**: Pattern-based detection recognizes subscription, resource group, and resource IDs across all `azurerm` resources.
- **Readable output**: IDs are formatted via `AzureScopeParser`, with only the values wrapped as inline code (e.g., Key Vault `kv-demo` in resource group `rg-demo` of subscription `00000000-0000-0000-0000-000000000000`).
- **Stays in tables**: Azure IDs are exempt from large-value classification, so even very long IDs remain in the main attribute table.
- **Model-driven routing**: Large-value classification is computed in C#, and templates rely on `attr.is_large` without needing provider context.
- **Summaries included**: Resource summaries use the same formatting, preventing raw IDs from appearing in the header lines.
- **Non-Azure unaffected**: Other providers still render values as backticked inline code.

**Before:**
````markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | `/subscriptions/.../resourceGroups/.../providers/Microsoft.KeyVault/vaults/kv-demo` |
````

**After:**
````markdown
| Attribute | Value |
|-----------|-------|
| key_vault_id | Key Vault `kv-demo` in resource group `rg-demo` of subscription `00000000-0000-0000-0000-000000000000` |
````

## Custom Report Title

**Status:** ✅ Implemented

Users can provide a custom title for generated reports via the `--report-title` CLI option. This allows for better context when sharing reports or distinguishing between different pipeline runs.

- **Contextual Titles**: Add repository names, environment identifiers, or purpose (e.g., "Drift Detection") to the report title.
- **Automatic Escaping**: Special characters in titles are automatically escaped to ensure valid markdown headings.
- **Template Support**: The custom title is available to all templates (built-in and custom) via the `report_title` variable.
- **Fallback**: If no title is provided, templates fall back to their default titles (e.g., "Terraform Plan Report").
  Templates add the `#` heading marker; `report_title` only contains the escaped text.

Example:
```bash
tfplan2md plan.json --report-title "Drift Detection Results"
```

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
- The Total row shows only the sum of displayed actions (Add + Change + Replace + Destroy), excluding no-op resources

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

Resources with no changes (no-op) are **excluded from the Total count** and are **not displayed** in the detailed changes section. This design choice:
- Keeps the Total consistent with the visible action rows in the summary table
- Reduces output noise when reviewing plans with many unchanged resources
- Enables processing of large Terraform plans without hitting template iteration limits

The `summary.no_op` count is available to custom templates but not shown in the default template.

### Action Symbols

Resources are displayed with emoji symbols indicating the action:
- `➕` - create (add new resource)
- `🔄` - update (modify existing resource)
- `❌` - delete (remove resource)
- `♻️` - replace (delete and recreate resource)

### Resource Summaries

**Status:** ✅ Implemented

Each resource change displays a concise one-line summary above the `<details>` section to help users quickly scan and understand changes without expanding every resource.

**Summary formats by action:**

- **CREATE**: Shows resource name and key identifying attributes based on resource type
  - Example: `` `sttfplan2mdlogs` in `rg-tfplan2md-demo` (eastus) | Standard LRS ``
  - Uses resource-specific attribute mappings for 43 azurerm resources, plus fallbacks for azuredevops, azuread, azapi, and msgraph providers

### Large Attribute Value Display

**Status:** ✅ Implemented

Large attribute values (multi-line text or strings > 100 characters) are automatically moved from the main attribute table to a separate collapsible `<details>` section to improve readability.

**Features:**
- **Automatic Detection**: Values with newlines or exceeding 100 characters are treated as large.
- **Collapsible Section**: Large values are grouped in a `<details>` section below the resource table.
- **Summary Line**: The summary shows the number of lines and changed lines for each large attribute.
- **Formatting Options**:
  - `inline-diff` (default): Azure DevOps-optimized HTML with character-level diff highlighting.
  - `standard-diff`: GitHub-compatible markdown diff blocks.
- **Complete Replacement**: If a value is completely replaced (no common lines), it shows separate "Before" and "After" blocks.

**Usage:**
```bash
# Default (inline-diff)
tfplan2md plan.json

# Standard diff (better for GitHub)
tfplan2md plan.json --large-value-format standard-diff
```

- **UPDATE**: Shows resource name and list of changed attributes (up to 3, then "+ N more")
  - Example: `` `sttfplan2mddata` | Changed: account_replication_type, tags.cost_center ``

- **REPLACE**: Shows resource name and replacement reason when available (Terraform 1.2+)
  - With reason: `` recreate `snet-db` (address_prefixes changed: force replacement) ``
  - Without reason: `` recreating `nsg-app` (1 changed) ``
  - Shows up to 3 attributes that forced replacement, then "+ N more"

- **DELETE**: Shows resource name
  - Example: `` `sttfplan2mdlegacy` ``

**Provider-specific mappings:**

The summary builder includes intelligent attribute selection for multiple providers:
- **azurerm**: 43 resource types with specific mappings (storage accounts, VMs, networks, databases, etc.)
- **azuredevops**: Projects and pipelines
- **azuread**: Groups, users, service principals
- **azapi**: Generic resources and actions
- **msgraph**: Microsoft Graph API resources

When no specific mapping exists, the tool falls back to provider-level defaults (e.g., azurerm → name, resource_group_name, location) or generic fallbacks (name, display_name).

**Replacement reasons:**

For resources with action "replace", the summary includes which attribute(s) caused the replacement. This information is parsed from the `replace_paths` field in Terraform plan JSON (available in Terraform 1.2+). When this field is not available, the summary shows the count of changed attributes instead.

All summary values are automatically escaped to ensure valid markdown output.

### Sensitive Values

- Sensitive values are **masked by default** for security
- Use `--show-sensitive` flag to reveal sensitive values in the output

### Attribute Tables

Attribute tables in the default template now vary by the resource change action to make output more concise and meaningful:

- **create**: 2-column table (`Attribute | Value`) showing the *after* values
- **delete**: 2-column table (`Attribute | Value`) showing the *before* values
- **update** and **replace**: 3-column table (`Attribute | Before | After`)

**Unchanged attribute filtering (default behavior):**
- Attributes where the before and after values are identical are **hidden by default** to reduce noise and help users quickly identify actual changes
- Use `--show-unchanged-values` flag to display all attributes including unchanged ones
- This filtering applies to all attribute change tables regardless of which template is used

Null or unknown attributes are omitted from the tables to avoid meaningless rows, and sensitive attributes are masked unless `--show-sensitive` is used.

## Large Attribute Value Display

**Status:** ✅ Implemented

To improve readability, large attribute values (multi-line or >100 characters) are automatically moved from the main attribute table to a collapsible `<details>` section below the resource summary.

**Features:**
- **Automatic detection**: Attributes with newlines or long values are automatically identified.
- **Collapsible section**: All large attributes are grouped in a single "Large attributes" section.
- **Summary line**: Shows the count of large attributes and total lines (e.g., "Large attributes (2 attributes, 147 lines)").
- **Display formats**:
  - **`inline-diff`** (default): Azure DevOps-optimized HTML with line-by-line and character-level diff highlighting. Note that GitHub strips these HTML styles (content remains readable), so this format is best for Azure DevOps.
  - **`standard-diff`**: Cross-platform markdown code blocks with `diff` syntax highlighting. This format is fully portable and works on both GitHub and Azure DevOps.

**Usage:**
Control the display format using the `--large-value-format` CLI option:
```bash
tfplan2md plan.json --large-value-format standard-diff
```

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

## Markdown Quality and Validation

tfplan2md ensures generated markdown is valid and renders correctly on GitHub and Azure DevOps:

- **Automatic escaping**: All external input (resource names, attribute values, module addresses) is automatically escaped to prevent broken tables and headings
- **Selective escaping**: Only structural characters that break markdown (pipes `|`, backticks `` ` ``, angle brackets `<>`, ampersands `&`, backslashes `\`) are escaped; readable characters like parentheses, brackets, asterisks, and underscores are preserved
- **Newline normalization**: Newlines in attribute values are converted to `<br/>` tags for table compatibility
- **Heading spacing**: Blank lines are automatically added before and after headings to ensure proper rendering
- **Table formatting**: Tables use padded separator rows that satisfy markdownlint requirements
- **CI validation**: The comprehensive demo output is validated with markdownlint-cli2 on every PR and commit to main

The escaping logic is centralized in the `escape_markdown` Scriban helper, which all templates must use for external input.

For details on the markdown subset and formatting rules, see [docs/markdown-specification.md](markdown-specification.md).

## Templates

Reports are generated using customizable templates powered by [Scriban](https://github.com/scriban/scriban).

### Built-in Templates

tfplan2md includes multiple built-in templates that can be selected by name:

- **`default`** (used when no template is specified): Full report with summary and detailed resource changes
- **`summary`**: Compact summary showing only Terraform version, plan timestamp, and action counts with resource type breakdown

Select a built-in template using the `--template` option:
```bash
tfplan2md plan.json --template summary
```

### Custom Templates

Provide a custom template file using the `--template` flag:
```bash
tfplan2md plan.json --template /path/to/custom-template.sbn
```

**Template resolution order:**
1. Check if the provided value matches a built-in template name
2. If not, attempt to load it as a file path
3. If neither exists, display an error listing available built-in templates

**Important:** Custom templates must use the `escape_markdown` helper on all external input to ensure valid markdown output. See built-in templates for examples.

### Template Variables

Templates have access to the following variables:

- **`terraform_version`** - Terraform version string (e.g., "1.14.0")
- **`format_version`** - Plan format version (e.g., "1.2")
- **`timestamp`** - Plan generation timestamp in RFC3339 format (e.g., "2025-12-20T10:00:00Z"), if available in the plan JSON
- **`show_unchanged_values`** - Boolean indicating whether unchanged attribute values are included in the output
- **`summary`** - Summary object with action details:
  - `to_add`, `to_change`, `to_destroy`, `to_replace`, `no_op` - Each is an `ActionSummary` object containing:
    - `count` - Number of resources for this action
    - `breakdown` - Array of `ResourceTypeBreakdown` objects, each with `type` (resource type name) and `count` (number of that type)
  - `total` - Total number of resources with changes (excludes no-op resources)
- **`changes`** - List of resource changes (no-op resources excluded), each with:
  - `address` - Full resource address
  - `type` - Resource type
  - `action` - Action string ("create", "update", "delete", "replace")
  - `action_symbol` - Emoji symbol for the action
  - `summary` - One-line summary of the resource change (auto-generated)
  - `replace_paths` - Array of attribute paths that triggered replacement (Terraform 1.2+, may be null)
  - `attribute_changes` - List of attribute changes with `name`, `before`, `after`, and `is_sensitive`
  - `before_json`, `after_json` - Raw JSON state (for resource-specific templates)
- **`module_changes`** - Resource changes grouped by module, each with:
  - `module_address` - Module address (empty string for root)
  - `changes` - Array of resource changes for this module

**Note:** The `timestamp` field is optional and may be `null` if not present in the Terraform plan JSON.

## CLI Interface

Simple single-command interface with flags:

| Flag | Description |
|------|-------------|
| `--output <file>` | Write output to a file instead of stdout |
| `--template <name\|file>` | Use a built-in template by name (default, summary) or a custom Scriban template file |
| `--report-title <text>` | Override the report's level-1 heading |
| `--large-value-format <format>` | Format for large/multi-line attributes: `inline-diff` (default, HTML with inline diff) or `standard-diff` (markdown diff fence) |
| `--principal-mapping <file>` | Map Azure principal IDs to names using a JSON file |
| `--show-unchanged-values` | Include unchanged attribute values in tables (hidden by default) |
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

The `azurerm_role_assignment` resource uses a table-based format with human-readable summaries instead of cryptic GUIDs and technical paths.

**Features:**
- **Comprehensive built-in role mapping**: All 473 Azure built-in role definition GUIDs are automatically mapped to friendly names (e.g., "Reader", "Contributor", "Storage Blob Data Reader")
- **Hierarchical scope display**: Azure resource scopes are parsed and displayed with clear context:
  - Management Groups: `my-mg (Management Group)`
  - Subscriptions: `subscription sub-id`
  - Resource Groups: `my-rg in subscription sub-id`
  - Resources: Recognizes 20+ common Azure resource types with friendly names:
    - Compute: Virtual Machine, Virtual Machine Scale Set, AKS Cluster, Managed Disk
    - Storage: Storage Account, Key Vault, Container Registry, Cosmos DB Account
    - Networking: Virtual Network, Subnet, Load Balancer, Application Gateway, Azure Firewall, Private Endpoint
    - Web/Apps: App Service, App Service Plan, SQL Server, SQL Database
    - Monitoring: Log Analytics Workspace, Application Insights
    - Data: Azure Cache for Redis, Event Hubs Namespace, Service Bus Namespace
    - Graceful fallback to capitalized type names for unmapped resources
- **Optional principal mapping**: Map principal IDs to names using a JSON file with the `--principal-mapping` (or `--principals`, `-p`) flag
- **Table-based output**: Clean collapsible `<details>` sections with attribute tables
- **Smart summaries**: One-line summary showing principal → role → scope relationship

**Example output:**
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
    awk '{printf "  \"%s\": \"%s\",\n", $1, $2}'
  az ad group list --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s\",\n", $1, $2}'
  az ad sp list --all --query "[].{id:id,name:displayName}" -o tsv | \
    awk '{printf "  \"%s\": \"%s\",\n", $1, $2}' | \
    sed '$ s/,$//'
  echo "}"
} > principals.json
```

The principal mapping JSON format:
```json
{
  "principal-guid": "Display Name"
}
```

The type (User, Group, ServicePrincipal) is automatically read from the Terraform plan's `principal_type` attribute and displayed as: `Display Name (Type) [guid]`.

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
| azurerm | `azurerm_network_security_group` | Security rule diffing with `diff_array` |

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

#### Network Security Groups

For `azurerm_network_security_group`, security rules are rendered in a single table with columns for Name, Priority, Direction, Access, Protocol, Source Addresses, Source Ports, Destination Addresses, Destination Ports, and Description. Rules are matched by name, categorized (➕, 🔄, ❌, ⏺️), and sorted by ascending priority.

- **Added rules**: ➕ with the new values.
- **Removed rules**: ❌ with the old values.
- **Modified rules**: 🔄 with before/after values in the same cell using `-` and `+` prefixes separated by `<br>`.
- **Unchanged rules**: ⏺️ for completeness.

Example of a modified NSG rule with a port change and description update:

```markdown
| 🔄 | allow-http | 110 | Inbound | Allow | Tcp | - *<br>+ 10.0.1.0/24, 10.0.2.0/24 | * | * | - 80<br>+ 8080 | - Allow HTTP<br>+ Allow alternate HTTP |
```

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
Returns the single escaped value if unchanged, or `"- escapedBefore<br>+ escapedAfter"` if different. Values are escaped for markdown safety while the `<br>` tag is preserved to render as a line break in tables.

See [resource-specific-templates.md](features/resource-specific-templates.md) for full specification.

## Markdown Rendering Quality

tfplan2md ensures high-quality markdown output that renders correctly in all markdown viewers:

- **Heading spacing**: All headings are automatically preceded by a blank line, preventing content from running into headings
- **Table formatting**: Blank lines before table rows are collapsed to prevent table breakage
- **Consistent structure**: Proper document hierarchy with module sections (H3) and resource entries (H4)

These improvements are applied automatically during rendering and require no configuration.

## Future Considerations

The following features may be added in future versions:

- Additional resource-specific templates based on user feedback
- Provider-default templates (e.g., `Templates/azurerm/default.sbn`)
- `--list-templates` CLI option to list bundled templates
