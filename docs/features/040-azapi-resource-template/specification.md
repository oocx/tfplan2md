# Feature: Custom Template for azapi_resource

## Overview

The azapi provider provides a thin layer around the Azure Resource Manager REST API, enabling management of Azure resources that don't yet have full azurerm provider support or that need access to preview features. The `azapi_resource` resource type is particularly challenging to review in Terraform plans because most configuration resides in the `body` attribute as JSON, making it difficult to understand what's being created or changed.

This feature will create a resource-specific template for `azapi_resource` that transforms the JSON body content into a human-readable format, making it easy for developers to review azapi resources with confidence.

## User Goals

- **Understand azapi changes quickly**: Developers need to see what properties are being configured without parsing dense JSON
- **Review body content easily**: The JSON `body` attribute should be rendered in a format that highlights the actual resource configuration
- **Identify resource types**: Clear indication of which Azure resource type is being managed (e.g., `Microsoft.Automation/automationAccounts@2021-06-22`)
- **Access documentation**: Quick links to the relevant Azure REST API documentation for the resource being managed
- **Compare changes effectively**: For updates, developers need to see which properties in the body changed, not just that "body changed"

## Scope

### In Scope

1. **Custom template for azapi_resource**: A resource-specific Scriban template in `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`

2. **Body content representation**: Transform the JSON body into readable output using one or more representation strategies:
   - Flattened table with dot-notation property paths (e.g., `properties.sku.name`)
   - Nested hierarchical view showing structure
   - Combination approach (summary + details)
   - Other representations as determined during requirements gathering

3. **Standard attributes display**: Show key azapi_resource attributes in a clear format:
   - `type` (Azure resource type with API version)
   - `name` (resource name)
   - `parent_id` (resource group or parent resource)
   - `location` (Azure region)
   - `tags` (if present)
   - `response_export_values` (if present)

4. **Change detection**: For update operations, highlight which properties in the body changed

5. **Documentation links**: Auto-generate links to Azure REST API documentation on Microsoft Learn based on the resource type

6. **Resource summary**: One-line summary showing resource name, type, and key identifying information

### Out of Scope

- Support for `azapi_update_resource` (different resource type, may be addressed in future feature)
- Support for `azapi_data_resource` (data source, not a resource change)
- Dynamic JSON schema validation (beyond what Terraform provides)
- Syntax highlighting or validation of JSON content
- Conversion of azapi resources to azurerm equivalents
- Custom handling for specific Azure resource types beyond the body rendering strategy

## User Experience

### Summary Line

When an `azapi_resource` appears in a plan report, it should display a clear summary:

**Create:**
```
➕ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources | westeurope
```

**Update:**
```
🔄 azapi_resource myAccount — Microsoft.Automation/automationAccounts | 2 properties changed
```

**Delete:**
```
❌ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources
```

### Detailed View Structure

The expanded resource view should include:

1. **Resource Header**: Resource type with API version, formatted as inline code
   - Example: `Type: Microsoft.Automation/automationAccounts@2021-06-22`

2. **Documentation Link**: Auto-generated link to Azure REST API docs
   - Format: `[View API Documentation](https://learn.microsoft.com/rest/api/...)`
   - Link structure needs research to determine pattern

3. **Standard Attributes Table**: Show non-body attributes (name, parent_id, location, etc.)

4. **Body Content**: The JSON body rendered in a readable format
   - **Research needed**: Determine the best representation strategy
   - Options to explore:
     - **Flattened table**: One row per property path with dot notation
     - **Hierarchical tree**: Nested structure showing parent-child relationships
     - **Hybrid approach**: High-level summary + collapsible details
     - **Simple JSON**: Formatted JSON in a code block (fallback)

5. **Change Indicators** (for updates): Clear marking of which properties changed
   - Similar to existing firewall rule and NSG templates
   - Show before/after values for changed properties

### Example Output (Conceptual)

```markdown
<details>
<summary>➕ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources | westeurope</summary>

**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/automation-account)

| Attribute | Value |
|-----------|-------|
| name | `myAccount` |
| parent_id | Resource Group `example-resources` |
| location | `🌍 westeurope` |

#### Body

| Property | Value |
|----------|-------|
| properties.disableLocalAuth | `✅ true` |
| properties.publicNetworkAccess | `❌ false` |
| properties.sku.name | `Basic` |

</details>
```

## Success Criteria

- [ ] `azapi_resource` has a custom template in the appropriate directory
- [ ] The template renders all standard attributes (name, type, parent_id, location, tags) in a clear table
- [ ] The JSON body content is transformed into a readable format (format determined during implementation)
- [ ] For update operations, changed properties in the body are clearly highlighted
- [ ] Documentation links are auto-generated and point to valid Azure REST API documentation
- [ ] The resource summary line includes resource name, type, and key context
- [ ] The template follows the project's report style guide (icons, formatting, escape handling)
- [ ] All rendered markdown is valid and passes markdownlint validation
- [ ] Template works for create, update, delete, and replace operations
- [ ] Complex nested JSON structures are handled gracefully (no template errors)
- [ ] Long property paths or values are handled (wrapping, truncation, or large value sections)

## Open Questions

### 1. Body Representation Strategy

**Question:** What is the best way to represent the JSON body content for reviewability?

**Options to explore:**
- **A) Flattened table with dot notation**
  - Pros: Flat structure, easy to scan, works well with diff highlighting
  - Cons: Long property paths, loses hierarchy visibility
  
- **B) Nested/hierarchical view**
  - Pros: Shows structure, natural JSON representation
  - Cons: Complex to render in tables, harder to diff
  
- **C) Hybrid approach**
  - Pros: Best of both worlds - summary + details
  - Cons: More complex template logic
  
- **D) Formatted JSON code block**
  - Pros: Simple, preserves exact structure
  - Cons: Less readable, harder to scan for changes

**Recommendation:** Research and prototype 2-3 approaches with realistic examples, then select the most readable option. Consider that:
- Users need to quickly identify what's configured
- Changes must be easy to spot in diffs
- Both simple and deeply-nested JSON should work well

### 2. Documentation Link Pattern

**Question:** What is the pattern for Azure REST API documentation links?

**Research needed:**
- Can we reliably construct the URL from the resource type?
- Example type: `Microsoft.Automation/automationAccounts@2021-06-22`
- Expected link pattern: `https://learn.microsoft.com/rest/api/automation/automation-account/...`
- Are there consistent patterns across all Azure services?
- What happens for preview APIs or non-standard resource types?

**Approach:**
- Research Microsoft Learn REST API documentation structure
- Identify URL patterns for common Azure resource providers
- Determine if API version affects the URL
- Consider fallback if link can't be generated (show resource type without link)

### 3. Change Detection for Body

**Question:** How do we diff JSON body content for update operations?

**Options:**
- Use Terraform's attribute_changes if available
- Parse before/after JSON and compute property-level diff
- Use existing tfplan2md helpers (e.g., `diff_array` for nested collections)
- Show entire before/after body sections with formatting

**Decision needed:** Determine if Terraform plan JSON provides granular body changes or if we need custom diff logic.

### 4. Handling Sensitive Values

**Question:** Are there sensitive values in azapi_resource body that need masking?

**Considerations:**
- Terraform marks entire resources as sensitive sometimes
- Azure resource properties may contain secrets (connection strings, keys)
- Should follow existing `--show-sensitive` CLI flag behavior

**Approach:** Test with realistic examples and ensure template respects existing sensitive value handling.

### 5. Large Body Content

**Question:** How do we handle very large or deeply nested JSON bodies?

**Considerations:**
- Some Azure resources have extensive configuration
- Deeply nested JSON (5+ levels)
- Large arrays (e.g., 50+ items)

**Options:**
- Use existing "Large attributes" collapsible sections
- Truncate with "show more" pattern
- Automatic flattening for deep nesting
- No special handling (keep in main view)

**Decision needed:** Determine thresholds and handling strategy aligned with existing large value handling.

### 6. Resource Type Parsing

**Question:** How do we extract meaningful names from the resource type string?

**Example:** `Microsoft.Automation/automationAccounts@2021-06-22`
- Provider: `Microsoft.Automation`
- Type: `automationAccounts`
- API Version: `2021-06-22`

**Needed:** Logic to parse and display these components clearly, possibly with better formatting or capitalization (e.g., "Automation Accounts" vs "automationAccounts").

## Next Steps

1. **Architect** to review this specification and design:
   - Template structure and view model approach
   - Body representation strategy (with prototypes if needed)
   - Documentation link generation logic
   - JSON diff approach for updates

2. **Developer** to implement:
   - Template file creation
   - View model or helper functions for body transformation
   - Documentation link generator
   - Integration with existing template resolution

3. **Quality Engineer** to define test cases:
   - Various azapi_resource configurations (simple, nested, large)
   - All operation types (create, update, delete, replace)
   - Edge cases (missing body, sensitive values, empty body)

4. **Technical Writer** to document:
   - Add azapi_resource template to features documentation
   - Update README examples
   - Document representation choices made

5. **UAT Tester** to validate:
   - Real-world azapi_resource plan files
   - Rendering on GitHub and Azure DevOps
   - Readability and usefulness for reviewers
