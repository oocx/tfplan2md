# Resource-Specific Templates Specification

## Overview

This feature enables specialized Markdown rendering for complex Terraform resources where the default flattened attribute diff is hard to understand. Resource-specific templates receive raw JSON data and use helper functions to compute and render semantic diffs.

## Problem Statement

When Terraform modifies collection-based resources (e.g., firewall rules), inserting an item mid-collection causes index shifts for all subsequent items. The flattened diff shows changes like:

```
rule[2].name: "allow-https" → "allow-dns"
rule[3].name: "allow-http" → "allow-https"
...
```

This obscures the actual change: one rule was added. Resource-specific templates solve this by comparing items by key (e.g., `rule.name`) and rendering semantic diffs.

## Template Resolution

### Organization

Templates are organized by provider namespace:

```
Templates/
├── default.sbn                                      # Global default
├── azurerm/
│   └── firewall_network_rule_collection.sbn        # Resource-specific
└── aws/
    └── security_group.sbn                          # Future example
```

### Resolution Order

For a resource of type `azurerm_firewall_network_rule_collection`:

1. **Resource-specific**: `Templates/azurerm/firewall_network_rule_collection.sbn`
2. **Provider-default**: `Templates/azurerm/default.sbn` (future consideration)
3. **Global default**: `Templates/default.sbn`

The first matching template is used. If no resource-specific template exists, the global default renders attribute tables. Note that the default template now renders different table formats depending on the `action`:

- **create**: 2-column `Attribute | Value` (shows the planned/after values)
- **delete**: 2-column `Attribute | Value` (shows the prior/before values)
- **update** and **replace**: 3-column `Attribute | Before | After`

This avoids empty or meaningless columns for create/delete scenarios and makes the output more concise and readable.

### Module Grouping & Template Data

The default template renders resource changes grouped by module via a top-level `module_changes` collection. Use `module_changes` when implementing custom templates that should follow the grouped output.

Each `module` item contains:

- `module_address` - The full module path (string). Empty string indicates the root module and should be rendered as `root`.
- `changes` - Array of resource change objects (same structure as described in the main template data section).

Example:

```scriban
{{ for module in module_changes }}
### Module: {{ if module.module_address && module.module_address != "" }}`{{ module.module_address }}`{{ else }}root{{ end }}

{{ for change in module.changes }}
#### {{ change.action_symbol }} {{ change.address }}
  ...
{{ end }}

---
{{ end }}
```

Note: Modules without any resource changes are omitted from `module_changes` to keep reports concise.

### Custom Templates

When a user provides a custom template via `--template`, the same resolution applies within their template directory. Users can override bundled templates or add new ones. If no matching template is found in the custom directory, the bundled templates are used as fallback.

## Data Model

### Extended ResourceChangeModel

Resource-specific templates receive additional data beyond the flattened `attribute_changes`:

| Property | Type | Description |
|----------|------|-------------|
| `address` | string | Full resource address |
| `type` | string | Resource type (e.g., `azurerm_firewall_network_rule_collection`) |
| `name` | string | Resource name |
| `provider_name` | string | Provider (e.g., `registry.terraform.io/hashicorp/azurerm`) |
| `action` | string | Action: `create`, `update`, `delete`, `replace`, `no-op` |
| `action_symbol` | string | Icon: ➕, 🔄, ❌, ♻️ |
| `attribute_changes` | array | Flattened attribute diffs (existing) |
| `before_json` | object | Raw JSON of current state (new) |
| `after_json` | object | Raw JSON of planned state (new) |

### Accessing Raw Data

Templates can access nested structures directly:

```scriban
{{ for rule in after_json.rule }}
  {{ rule.name }}: {{ rule.protocols | array.join ", " }}
{{ end }}
```

## Helper Functions

### `diff_array`

Computes semantic differences between two arrays using a key property.

**Signature:**
```scriban
{{ diff_array <before_array> <after_array> "<key_property>" }}
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `before_array` | array | Array from current state |
| `after_array` | array | Array from planned state |
| `key_property` | string | Property name to identify items (e.g., `"name"`) |

**Returns:** Object with collections:
| Property | Type | Description |
|----------|------|-------------|
| `added` | array | Items in `after` but not in `before` |
| `removed` | array | Items in `before` but not in `after` |
| `modified` | array | Items in both with different values (includes `before` and `after`) |
| `unchanged` | array | Items in both with identical values |

**Example:**
```scriban
{{ diff = diff_array before_json.rule after_json.rule "name" }}

{{ for rule in diff.added }}
  ➕ {{ rule.name }}
{{ end }}

{{ for item in diff.modified }}
  🔄 {{ item.after.name }}
{{ end }}
```

## User Extension Mechanisms

### Template-Defined Functions

Users can define helper functions directly in their templates:

```scriban
{{ func format_ports(ports) }}
  {{ ports | array.join ", " }}
{{ end }}

{{ format_ports rule.destination_ports }}
```

### Include Directive

Templates can include shared helpers from the same directory:

```scriban
{{ include 'helpers.sbn' }}
{{ my_custom_helper value }}
```

The `ITemplateLoader` restricts includes to the custom template directory for security.

## Initial Resource Template: azurerm_firewall_network_rule_collection

### Output Format

```markdown
### 🔄 azurerm_firewall_network_rule_collection.main

**Collection:** `web-tier-rules` | **Priority:** 100 | **Action:** Allow

#### Rule Changes

| | Rule Name | Protocols | Source | Destination | Ports |
|-|-----------|-----------|--------|-------------|-------|
| ➕ | allow-dns | UDP | 10.0.0.0/16 | 168.63.129.16 | 53 |
| 🔄 | allow-https | TCP | 10.0.0.0/16 | * | 443 |
| ❌ | legacy-ftp | TCP | 10.0.1.0/24 | 10.0.2.5 | 21 |
| — | allow-http | TCP | 10.0.0.0/16 | * | 80 |

<details>
<summary>Modified rule details</summary>

**allow-https**
- `source_addresses`: `10.0.1.0/24` → `10.0.0.0/16`

</details>
```

### Display Requirements

- **Collection metadata**: Name, priority, and action shown prominently
- **Status icons**: ➕ (added), 🔄 (modified), ❌ (removed), — (unchanged)
- **Sort order**: Added → Modified → Removed → Unchanged
- **Unchanged rules**: Shown with muted indicator for complete picture
- **Multi-value fields**: Joined with commas (protocols, ports, addresses)
- **Modified details**: Expandable section showing specific attribute changes
- **Diff key**: `rule.name` uniquely identifies rules within a collection

## CLI Extensions

### `--list-templates`

Lists bundled resource-specific templates:

```
$ tfplan2md --list-templates

Bundled Resource Templates:
  azurerm/firewall_network_rule_collection  - Firewall rules with semantic diff

Use --template <path> to provide custom templates.
```

## Security Considerations

- Templates cannot access .NET objects unless explicitly imported
- `include` directive restricted to template directory
- Scriban limits: `LoopLimit` (1000), `RecursiveLimit` (100), `RegexTimeOut` (10s)

## Implementation Status

### Implemented ✅

| Feature | Status | Notes |
|---------|--------|-------|
| Extended ResourceChangeModel | ✅ | `before_json` and `after_json` properties added |
| Template resolution by resource type | ✅ | `Templates/{provider}/{resource}.sbn` → `Templates/default.sbn` |
| `diff_array` helper function | ✅ | Returns `added`, `removed`, `modified`, `unchanged` collections |
| Error handling per resource | ✅ | Renders error message for resource, continues with others |
| azurerm_firewall_network_rule_collection template | ✅ | Full semantic diff with collapsible details |
| Custom template directory support | ✅ | Via `MarkdownRenderer(customTemplateDirectory)` constructor |

### Not Yet Implemented 📋

| Feature | Status | Notes |
|---------|--------|-------|
| `--list-templates` CLI option | 📋 | Deferred to future PR |
| Provider-default templates | 📋 | Future consideration |
| `include` directive security | 📋 | Not yet restricted to template directory |
| Additional resource templates | 📋 | Based on user feedback |

## Future Considerations

- Provider-default templates (e.g., `Templates/azurerm/default.sbn`)
- Additional resource templates based on user feedback
- Additional helper functions as needed for new templates
- `--list-templates` CLI option
