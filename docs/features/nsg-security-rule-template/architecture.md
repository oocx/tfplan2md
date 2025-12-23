# Architecture: Network Security Group Security Rules Template

## Status

✅ **Implemented** - No architectural changes were required.

The NSG template was successfully implemented following the existing resource-specific template pattern.

## Analysis

The requirement is to provide a specialized Markdown template for `azurerm_network_security_group` resources to display security rules in a semantic, readable table. This fits perfectly within the existing **Resource-Specific Templates** architecture.

We can leverage the existing Scriban template engine and helper functions (`diff_array`, `format_diff`) to implement this feature without modifying the core C# codebase.

## Implementation Guidance

### Template Location
Create a new template file: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn`

### Template Logic

1.  **Data Source**: The template will use `before_json.security_rule` and `after_json.security_rule` arrays.
2.  **Diffing**: Use `diff_array` with `"name"` as the key property to identify added, modified, removed, and unchanged rules.
    ```scriban
    {{ diff = diff_array before_json.security_rule after_json.security_rule "name" }}
    ```
3.  **Sorting**: The rules should be sorted by `priority` (ascending).
    *   For `added`, `removed`, `unchanged` arrays: `| array.sort "priority"`
    *   For `modified` array: `| array.sort "after.priority"` (since modified items are objects with `before`/`after` properties)
4.  **Address/Port Handling**: NSG rules have both singular (e.g., `source_address_prefix`) and plural (e.g., `source_address_prefixes`) properties. The template should handle this precedence:
    *   If plural property exists and is not empty -> join with commas.
    *   Else if singular property exists -> use it.
    *   Else -> display `*`.
    *   *Recommendation*: Define a reusable macro or function within the template to handle this logic for Source/Dest Addresses and Ports to avoid code duplication.

### Column Layout
The table should include the following columns:
1.  **Change** (Icon)
2.  **Name**
3.  **Priority**
4.  **Direction**
5.  **Access**
6.  **Protocol**
7.  **Source Addresses** (Combined prefix/prefixes)
8.  **Source Ports** (Combined range/ranges)
9.  **Destination Addresses** (Combined prefix/prefixes)
10. **Destination Ports** (Combined range/ranges)
11. **Description**

### Standalone Rules Note
This template applies to the `azurerm_network_security_group` resource. Rules defined as standalone `azurerm_network_security_rule` resources appear as separate changes in the Terraform plan and will **not** be aggregated into this table. They will continue to be rendered by the default template (or their own template if one were created, but that is outside the scope of aggregating them here).

## Components Affected

### New Files
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn` - NSG rule template
- `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererNsgTemplateTests.cs` - Unit tests
- `tests/Oocx.TfPlan2Md.Tests/TestData/nsg-rule-changes.json` - Test data

### Modified Files
- `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/TemplateIsolationTests.cs` - Added NSG template validation
- `docs/features.md` - Updated with NSG template documentation
- `docs/features/resource-specific-templates.md` - Added NSG template example
- `tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/comprehensive-demo.md` - Updated snapshot
- `artifacts/comprehensive-demo.md` - Regenerated with NSG template rendering

## Implementation Details

### Template Structure

The template follows the established pattern:

1. **Header Section**: Displays the NSG name and action
2. **Helper Functions**: Four custom functions handle singular/plural field precedence:
   ```scriban
   {{ func source_addresses(rule) }}
     {{- if rule.source_address_prefixes && rule.source_address_prefixes.size > 0 -}}{{ ret rule.source_address_prefixes | array.join ", " }}{{- end -}}
     {{- if rule.source_address_prefix && rule.source_address_prefix != "" -}}{{ ret rule.source_address_prefix }}{{- end -}}
     {{ ret "*" }}
   {{ end }}
   ```
3. **Conditional Rendering**: Three paths based on action type (update/create/delete)
4. **Fallback**: Default attribute table if no security_rule array exists

### Key Design Decisions

1. **Function Returns**: Used Scriban `ret` statements in helper functions to return clean strings without whitespace
2. **Whitespace Control**: Used standard `{{ }}` delimiters for table rows (not `{{~ ~}}`) to preserve newlines
3. **Sorting**: Applied `| array.sort "priority"` and `| array.sort "after.priority"` for proper ordering
4. **Priority Handling**: In modified rules, used `format_diff` on priority values directly (without string conversion)

### Testing Strategy

Implemented comprehensive test coverage:
- **Create/Delete scenarios**: Verify table structure and content for add/remove operations
- **Update scenarios**: Verify semantic diffing with all four categories (Added, Modified, Removed, Unchanged)
- **Priority sorting**: Verify rules appear in ascending priority order
- **Field handling**: Verify singular/plural precedence for addresses and ports
- **Integration**: Template isolation tests ensure valid Scriban syntax and proper markdown structure
