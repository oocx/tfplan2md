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

- **Summary**: Overview of changes (e.g., "3 to add, 1 to change, 2 to destroy")
- **Detailed changes**: List of affected resources with their actions and attribute changes

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

### Supported Resources

| Provider | Resource Type | Template |
|----------|--------------|----------|
| azurerm | `azurerm_firewall_network_rule_collection` | Semantic rule diffing with `diff_array` |

### Helper Functions

Templates have access to the `diff_array` function for semantic collection diffing:

```scriban
{{ diff = diff_array before_json.rule after_json.rule "name" }}
{{ for rule in diff.added }}
  ➕ {{ rule.name }}
{{ end }}
```

See [resource-specific-templates.md](features/resource-specific-templates.md) for full specification.

## Future Considerations

The following features may be added in future versions:

- Additional resource-specific templates based on user feedback
- Provider-default templates (e.g., `Templates/azurerm/default.sbn`)
- `--list-templates` CLI option to list bundled templates
