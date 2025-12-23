# Architecture: Network Security Group Security Rules Template

## Status

No architectural changes required.

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

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn` (New file)
