# Architecture: Custom Template for azapi_resource

## Status

Proposed

## Overview

This document describes the technical design for implementing a custom Scriban template for `azapi_resource` resources in the `azapi` Terraform provider. The goal is to transform the JSON `body` attribute into a human-readable format that makes it easy to review Azure API resource configurations in pull requests.

## Analysis

The `azapi_resource` is unique because most of its configuration resides in a JSON `body` attribute rather than discrete Terraform attributes. This makes it challenging to review changes without parsing dense JSON. The solution requires:

1. **Body representation strategy** - Transforming JSON into readable tables
2. **Documentation links** - Linking to Azure REST API documentation
3. **Change detection** - Highlighting what changed in the body for updates
4. **Sensitive value handling** - Respecting existing `--show-sensitive` behavior
5. **Large value handling** - Managing deeply nested or large JSON structures

## Key Design Decisions

### Decision 1: Body Representation Strategy

**Options Considered:**

#### Option A: Flattened Table with Dot Notation
Display body properties in a flat table using dot notation for nested paths:

| Property | Value |
|----------|-------|
| `properties.sku.name` | `Basic` |
| `properties.disableLocalAuth` | `true` |
| `properties.publicNetworkAccess` | `false` |

**Pros:**
- Simple, scannable format
- Works well with markdown tables
- Easy to implement with recursive traversal
- Natural fit for change highlighting (before/after columns)
- Consistent with how Terraform displays nested attributes

**Cons:**
- Long property paths can be verbose
- Loses visual hierarchy (harder to see object structure)
- May be overwhelming for deeply nested JSON (5+ levels)

#### Option B: Nested Hierarchical View
Display body properties in a hierarchical structure:

```
properties:
  sku:
    name: Basic
  disableLocalAuth: true
  publicNetworkAccess: false
```

**Pros:**
- Shows structure naturally
- Familiar to users who read JSON/YAML
- Clearer parent-child relationships

**Cons:**
- Doesn't fit well in markdown tables
- Harder to implement change detection (before/after comparison)
- Less scannable for quick reviews
- Requires custom rendering logic

#### Option C: Hybrid Approach
Show a summary table for top-level or changed properties, with collapsible details for complex nested structures:

**Pros:**
- Best of both worlds for different use cases
- Can optimize for common cases

**Cons:**
- More complex template logic
- Inconsistent presentation
- Harder to maintain

#### Option D: Formatted JSON Code Block
Display the body as formatted JSON in a code block:

**Pros:**
- Simplest implementation
- Preserves exact structure
- Familiar to developers

**Cons:**
- Not as readable as tables
- Harder to scan for specific values
- Change detection requires full JSON diff
- Doesn't leverage markdown table capabilities

**Recommendation: Option A - Flattened Table with Dot Notation**

**Rationale:**
- Most consistent with existing tfplan2md patterns for nested attributes
- Easy to scan during PR reviews
- Natural fit for before/after comparison in update operations
- Leverages existing helpers for semantic formatting (booleans, values)
- Simple to implement with recursive JSON traversal
- Works well with existing large value handling patterns

**Implementation Note:** For deeply nested structures (5+ levels), we can apply the existing large value handling pattern (collapsible section) to keep the main view clean.

---

### Decision 2: Documentation Link Generation

**Challenge:** Azure REST API documentation URLs don't follow a fully predictable pattern from resource type strings.

**Examples of inconsistency:**
- `Microsoft.Automation/automationAccounts` → `/rest/api/automation/automation-account/`
- `Microsoft.Storage/storageAccounts` → `/rest/api/storagerp/storage-accounts/` (note: "storagerp" not "storage")
- `Microsoft.Network/virtualNetworks` → `/rest/api/virtualnetwork/virtual-networks/`

**Options Considered:**

#### Option A: Heuristic URL Construction
Attempt to construct the URL using common patterns:
1. Extract service name from `Microsoft.{Service}`
2. Convert to lowercase
3. Convert resource type to kebab-case
4. Construct: `https://learn.microsoft.com/rest/api/{service}/{resource}/`

**Pros:**
- Works for many common resources
- No external dependencies
- Simple to implement

**Cons:**
- Will be wrong for some services (Storage → storagerp)
- No way to validate if the link is correct
- Preview APIs may have different patterns
- Custom resource providers won't work

#### Option B: Static Mapping Table
Maintain a lookup table for known resource types:

```csharp
private static readonly Dictionary<string, string> DocumentationUrls = new()
{
    ["Microsoft.Automation/automationAccounts"] = "https://learn.microsoft.com/rest/api/automation/automation-account/",
    ["Microsoft.Storage/storageAccounts"] = "https://learn.microsoft.com/rest/api/storagerp/storage-accounts/",
    // ...
};
```

**Pros:**
- Accurate URLs for mapped resources
- Can be extended over time
- Fallback for unmapped types

**Cons:**
- Requires manual maintenance
- Will be incomplete for new/obscure resources
- Increases code maintenance burden

#### Option C: No Documentation Links
Don't attempt to generate documentation links; just display the resource type clearly.

**Pros:**
- No maintenance burden
- Always correct (by omission)
- Simple implementation

**Cons:**
- Misses opportunity to provide helpful context
- Users must manually search for documentation

#### Option D: Best-Effort Heuristic with Fallback
Use heuristic URL construction (Option A) but:
- Display the link as "View API Documentation (best effort)"
- Make it clear the link may not be accurate
- Consider it a helpful hint, not guaranteed navigation

**Pros:**
- Provides value for most common cases
- No ongoing maintenance
- Clear about uncertainty
- Better than nothing

**Cons:**
- Some broken links will frustrate users
- May give false confidence

**Recommendation: Option D - Best-Effort Heuristic**

**Rationale:**
- Provides value for the majority of common Azure services (Automation, Compute, Network)
- No ongoing maintenance burden (important for sustainability)
- Clear communication that it's a best-effort link
- Users can still search if the link is wrong
- Aligns with "helpful but not perfect" UX philosophy
- Can be improved later with a mapping table if community feedback indicates it's needed

**Implementation Approach:**
1. Parse resource type: `Microsoft.{Service}/{ResourceType}@{ApiVersion}`
2. Extract service name, convert to lowercase: `automation`
3. Convert resource type to kebab-case with spaces: `automation-accounts`
4. Construct URL: `https://learn.microsoft.com/rest/api/{service}/{resource}/`
5. Display with disclaimer: `📚 [View API Documentation (best-effort)](url)`
6. For non-Microsoft resource types, omit the link entirely

**Edge Cases:**
- **Preview APIs:** Use the same URL pattern (API version typically not in URL)
- **Custom resource providers:** No link (can't construct meaningful URL)
- **Third-party providers:** No link

---

### Decision 3: Change Detection for JSON Body

**Challenge:** For update operations, we need to show what changed in the `body` attribute, not just "body changed".

**Terraform Plan Data Available:**
- `before` and `after` state as JSON objects
- `before_sensitive` and `after_sensitive` for masking
- `replace_paths` for attributes that trigger replacement

**Options Considered:**

#### Option A: Flatten Both Before/After, Show All Properties with Before/After Columns
Show all body properties in a table with before/after columns, similar to existing attribute changes:

| Property | Before | After |
|----------|--------|-------|
| `properties.sku.name` | `Basic` | `Standard` |
| `properties.disableLocalAuth` | `false` | `true` |
| `properties.publicNetworkAccess` | `true` | `true` |

**Pros:**
- Shows full context (unchanged and changed properties)
- Consistent with existing attribute change tables
- Clear visual comparison

**Cons:**
- Can be verbose for large bodies
- Many unchanged rows may clutter the view

#### Option B: Show Only Changed Properties
Only display properties that have different before/after values:

| Property | Before | After |
|----------|--------|-------|
| `properties.sku.name` | `Basic` | `Standard` |
| `properties.disableLocalAuth` | `false` | `true` |

**Pros:**
- Focused on what matters (the change)
- Less visual clutter
- Faster to scan

**Cons:**
- Missing context of unchanged values
- Harder to understand full resource state

#### Option C: Hybrid - Changed Properties + Collapsible Full View
Show changed properties by default, with a collapsible section for all properties:

**Changed:**
| Property | Before | After |
|----------|--------|-------|
| `properties.sku.name` | `Basic` | `Standard` |

<details><summary>All properties</summary>
| Property | Before | After |
|----------|--------|-------|
| ... |
</details>

**Pros:**
- Best of both worlds
- Focused review with opt-in context

**Cons:**
- More complex template logic
- Duplicate data in two views

**Recommendation: Option B - Show Only Changed Properties (with existing `--show-unchanged` flag support)**

**Rationale:**
- Aligns with the project's philosophy of focused, scannable diffs
- Existing `--show-unchanged-values` flag can control this behavior
- Less visual clutter in PR comments
- Consistent with how tfplan2md handles other attribute changes
- Users who need full context can use `--show-unchanged-values`

**Implementation:**
1. Recursively flatten both `before` and `after` JSON to dot-notation paths
2. Compare each path's value
3. Only include properties where before ≠ after (or exclusive to one side)
4. Respect `ShowUnchangedValues` flag from `ReportModel` to optionally show all
5. Apply existing sensitive value masking
6. Use existing semantic formatters for booleans, nulls, etc.

**Change Indicators:**
- Added properties: `(none)` → value
- Removed properties: value → `(none)`
- Modified properties: old value → new value

---

### Decision 4: Sensitive Value Handling

**Requirement:** The template must respect the existing `--show-sensitive` CLI flag behavior.

**Existing Pattern:**
- `AttributeChangeModel` includes `IsSensitive` flag
- Values are masked as `(sensitive)` unless `--show-sensitive` is used
- Masking happens in `ReportModelBuilder` during attribute extraction

**For azapi_resource Body:**

**Options Considered:**

#### Option A: Terraform's Sensitivity Applies to Entire Body
If the `body` attribute is marked sensitive in Terraform's `before_sensitive` or `after_sensitive`, mask the entire body.

**Pros:**
- Respects Terraform's sensitivity marker
- Simple to implement
- Conservative approach (safe)

**Cons:**
- May be overly broad (some properties in body may not be sensitive)
- Loses granularity

#### Option B: Per-Property Sensitivity from Terraform Plan
Parse `before_sensitive.body` and `after_sensitive.body` to extract per-property sensitivity:

```json
{
  "after_sensitive": {
    "body": {
      "properties": {
        "administratorLoginPassword": true
      }
    }
  }
}
```

**Pros:**
- Granular masking (only sensitive properties masked)
- Respects Terraform's sensitivity tracking
- Users get more visibility

**Cons:**
- More complex parsing
- Requires deep navigation of `before_sensitive`/`after_sensitive` structures
- Edge case: what if sensitivity structure doesn't match body structure?

#### Option C: Heuristic Sensitive Property Detection
Use heuristics to identify likely sensitive properties (e.g., `password`, `secret`, `key`, `token`):

**Pros:**
- Works even when Terraform doesn't mark properties sensitive
- Provides defensive masking

**Cons:**
- Can mask non-sensitive properties (false positives)
- Can miss sensitive properties (false negatives)
- Not aligned with Terraform's explicit sensitivity tracking

**Recommendation: Option B - Per-Property Sensitivity from Terraform Plan**

**Rationale:**
- Aligns with Terraform's explicit sensitivity tracking (authoritative source)
- Provides granular control (only mask what Terraform marks as sensitive)
- Consistent with how tfplan2md handles sensitivity for all other attributes
- Respects user's `--show-sensitive` flag consistently

**Implementation:**
1. Parse `before_sensitive` and `after_sensitive` for the resource
2. Navigate into `body` sub-structure (if present)
3. During JSON flattening, check each path against the sensitivity structure
4. Mark individual properties as sensitive if matched
5. Apply masking unless `--show-sensitive` is enabled

**Fallback:** If `body` itself is marked sensitive (not sub-properties), mask the entire body with a message: `(sensitive - use --show-sensitive to view)`

---

### Decision 5: Large/Complex JSON Handling

**Challenge:** Some Azure resources have deeply nested or large JSON bodies that would be overwhelming in a flat table.

**Existing Pattern:**
- tfplan2md has `LargeValueFormat` enum with modes: `InlineDiff`, `SimpleDiff`
- Large values (over threshold) are moved to collapsible sections
- `IsLarge` flag on `AttributeChangeModel`

**Options Considered:**

#### Option A: Apply Existing Large Value Threshold Per-Property
Treat each flattened property independently:
- If value length > threshold (e.g., 200 chars), mark as large
- Move large properties to collapsible section
- Keep small properties in main table

**Pros:**
- Consistent with existing large value handling
- Granular control
- Works well for mixed small/large properties

**Cons:**
- May fragment the body view if some properties are large

#### Option B: Treat Entire Body as Large if Total Size Exceeds Threshold
Calculate total flattened body size:
- If total > threshold (e.g., 2000 chars), treat entire body as large
- Render entire body in collapsible section

**Pros:**
- Keeps body properties together
- Simpler logic

**Cons:**
- All-or-nothing approach
- May hide small changes in large bodies

#### Option C: Depth-Based Threshold
If nesting depth > threshold (e.g., 5 levels), treat as large:

**Pros:**
- Targets deeply nested structures specifically

**Cons:**
- Depth doesn't always correlate with readability
- Misses wide but shallow structures

**Recommendation: Option A - Apply Existing Large Value Threshold Per-Property**

**Rationale:**
- Consistent with existing tfplan2md behavior
- Provides best user experience (small changes visible immediately, large details on demand)
- Leverages existing `IsLarge` flag and rendering logic
- Handles mixed scenarios well (e.g., small `name` property and large `configuration` property)

**Implementation:**
1. Flatten body JSON to dot-notation paths
2. For each property, check value length against large value threshold (200 chars)
3. Set `IsLarge = true` for properties exceeding threshold
4. Use existing template pattern: small properties in main table, large properties in collapsible section
5. Apply existing `LargeValueFormat` enum (user can control inline vs simple diff)

**Threshold:** Reuse existing large value threshold (200 characters) for consistency.

**Array Handling:** For array properties, consider total serialized length. If an array is large, collapse it but show count (e.g., `[12 items]` as summary).

---

### Decision 6: Template Structure and Location

**Template File Location:**
```
src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn
```

**Naming Convention:** `{provider}/{resource}.sbn`

**Existing Patterns:**
- `azurerm/firewall_network_rule_collection.sbn`
- `azurerm/network_security_group.sbn`
- `azurerm/role_assignment.sbn`

**Integration:**
- Template resolver already supports `{provider}/{resource}.sbn` pattern
- No changes needed to template resolution logic
- Template receives `ResourceChangeModel` with `BeforeJson` and `AfterJson` populated

**View Model Factory:**

**Question:** Do we need a custom view model factory like `FirewallNetworkRuleCollectionViewModelFactory`?

**Analysis:**
- Firewall and NSG templates use custom view models for semantic diffing (rule-by-rule comparison)
- azapi_resource body is generic JSON, not a specific structure like firewall rules
- All logic can be in template or Scriban helpers

**Decision:** **No custom view model factory needed. All logic in template and Scriban helpers.**

**Rationale:**
- Generic JSON flattening can be done with Scriban helper functions
- No domain-specific semantics to model (unlike firewall rules with names, priorities, etc.)
- Keeps implementation simpler and maintainable
- Template can directly access `BeforeJson` and `AfterJson` via Scriban helpers

**Required Scriban Helpers:**

We need new helper functions to support the template:

1. **`flatten_json(jsonObject, prefix="")`**
   - Recursively flatten JSON to dot-notation key-value pairs
   - Returns: `List<{ path: string, value: object }>`
   
2. **`extract_azapi_metadata(change)`**
   - Extract key azapi_resource attributes (type, name, parent_id, location, tags)
   - Returns: structured object with formatted values

3. **`azure_api_doc_link(resourceType)`**
   - Construct best-effort Azure REST API documentation link
   - Returns: URL string or null if not applicable

4. **`parse_resource_type(resourceType)`**
   - Parse `Microsoft.Service/resourceType@apiVersion` into components
   - Returns: `{ provider, service, resourceType, apiVersion }`

5. **`compare_json_properties(beforeJson, afterJson, showUnchanged)`**
   - Flatten both, compare, return list of changed properties
   - Returns: `List<{ path: string, before: string, after: string, isLarge: bool, isSensitive: bool }>`

---

## Implementation Guidance

### Overview

The implementation consists of:
1. **Scriban helper functions** for JSON processing and formatting
2. **Scriban template** for rendering azapi_resource
3. **Integration** with existing template resolution (already in place)

### Component Breakdown

#### 1. Scriban Helpers (New Functions)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs`

**Functions to Implement:**

```csharp
namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Scriban helper functions for azapi_resource template.
/// Related feature: docs/features/040-azapi-resource-template/specification.md
/// </summary>
internal static partial class ScribanHelpers
{
    /// <summary>
    /// Flattens a JSON object into dot-notation key-value pairs.
    /// </summary>
    /// <param name="jsonObject">The JSON object (from BeforeJson or AfterJson).</param>
    /// <param name="prefix">Prefix for nested property paths (for recursion).</param>
    /// <returns>List of { path, value, is_large } objects.</returns>
    public static List<object> FlattenJson(object? jsonObject, string prefix = "");

    /// <summary>
    /// Compares two JSON objects and returns changed properties.
    /// </summary>
    /// <param name="beforeJson">State before change.</param>
    /// <param name="afterJson">State after change.</param>
    /// <param name="beforeSensitive">Sensitive marker structure for before state.</param>
    /// <param name="afterSensitive">Sensitive marker structure for after state.</param>
    /// <param name="showUnchanged">Whether to include unchanged properties.</param>
    /// <param name="showSensitive">Whether to unmask sensitive values.</param>
    /// <returns>List of { path, before, after, is_large, is_sensitive, is_changed } objects.</returns>
    public static List<object> CompareJsonProperties(
        object? beforeJson,
        object? afterJson,
        object? beforeSensitive,
        object? afterSensitive,
        bool showUnchanged,
        bool showSensitive);

    /// <summary>
    /// Constructs a best-effort Azure REST API documentation link.
    /// </summary>
    /// <param name="resourceType">Resource type string (e.g., "Microsoft.Automation/automationAccounts@2021-06-22").</param>
    /// <returns>URL string or null if not a Microsoft resource.</returns>
    public static string? AzureApiDocLink(string resourceType);

    /// <summary>
    /// Parses an azapi resource type string into components.
    /// </summary>
    /// <param name="resourceType">Resource type string.</param>
    /// <returns>Object with provider, service, resource_type, api_version properties.</returns>
    public static object ParseAzureResourceType(string resourceType);

    /// <summary>
    /// Extracts and formats key azapi_resource attributes for display.
    /// </summary>
    /// <param name="change">The resource change model.</param>
    /// <returns>Object with name, type, parent_id, location, tags properties.</returns>
    public static object ExtractAzapiMetadata(object change);
}
```

**Implementation Notes:**
- Use `System.Text.Json` for JSON parsing (consistent with existing codebase)
- Reuse existing formatting helpers (`FormatAttributeValueTable`, `WrapInlineCode`, etc.)
- Respect large value thresholds (200 characters)
- Handle null/missing properties gracefully
- Ensure AOT compatibility (no reflection)

#### 2. Scriban Template

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`

**Template Structure:**

```scriban
{{~## Template for azapi_resource
     Displays Azure REST API resources with body content in flattened table format.
     Related feature: docs/features/040-azapi-resource-template/specification.md
~}}
<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>{{ change.summary_html }}</summary>
<br>

{{~ # Extract resource type components ~}}
{{~ type_info = parse_azure_resource_type(change.type) ~}}

**Type:** `{{ change.type | escape_markdown }}`

{{~ # Show documentation link (best-effort) ~}}
{{~ doc_link = azure_api_doc_link(change.type) ~}}
{{~ if doc_link ~}}
📚 [View API Documentation (best-effort)]({{ doc_link }})
{{~ end ~}}

{{~ # Standard attributes table ~}}
{{~ metadata = extract_azapi_metadata(change) ~}}
{{~ if metadata.name || metadata.parent_id || metadata.location ~}}

| Attribute | Value |
|-----------|-------|
{{~ if metadata.name ~}}
| name | {{ metadata.name }} |
{{~ end ~}}
{{~ if metadata.parent_id ~}}
| parent_id | {{ metadata.parent_id }} |
{{~ end ~}}
{{~ if metadata.location ~}}
| location | {{ metadata.location }} |
{{~ end ~}}
{{~ end ~}}

{{~ # Tags badges (for create/delete) ~}}
{{~ if change.tags_badges ~}}
{{ change.tags_badges }}
{{~ end ~}}

{{~ # Body content rendering ~}}
{{~ if change.action == "create" ~}}
    {{~ # Create: show after body ~}}
    {{~ body_props = flatten_json(change.after_json.body) ~}}
    {{~ small_props = body_props | array.filter "!is_large" ~}}
    {{~ large_props = body_props | array.filter "is_large" ~}}
    
    {{~ if small_props.size > 0 ~}}
    
#### Body Configuration

| Property | Value |
|----------|-------|
    {{~ for prop in small_props ~}}
| {{ prop.path | escape_markdown }} | {{ format_attribute_value_table(prop.path, prop.value, change.provider_name) }} |
    {{~ end ~}}
    {{~ end ~}}
    
    {{~ if large_props.size > 0 ~}}
<details>
<summary>Large body properties</summary>

    {{~ for prop in large_props ~}}
##### **{{ prop.path | escape_markdown }}:**

{{ format_large_value(null, prop.value, large_value_format) }}

    {{~ end ~}}
</details>
    {{~ end ~}}

{{~ else if change.action == "delete" ~}}
    {{~ # Delete: show before body ~}}
    {{~ body_props = flatten_json(change.before_json.body) ~}}
    {{~ small_props = body_props | array.filter "!is_large" ~}}
    {{~ large_props = body_props | array.filter "is_large" ~}}
    
    {{~ if small_props.size > 0 ~}}
    
#### Body Configuration

| Property | Value |
|----------|-------|
    {{~ for prop in small_props ~}}
| {{ prop.path | escape_markdown }} | {{ format_attribute_value_table(prop.path, prop.value, change.provider_name) }} |
    {{~ end ~}}
    {{~ end ~}}
    
    {{~ if large_props.size > 0 ~}}
<details>
<summary>Large body properties</summary>

    {{~ for prop in large_props ~}}
##### **{{ prop.path | escape_markdown }}:**

{{ format_large_value(prop.value, null, large_value_format) }}

    {{~ end ~}}
</details>
    {{~ end ~}}

{{~ else if change.action == "update" ~}}
    {{~ # Update: compare before/after body ~}}
    {{~ body_changes = compare_json_properties(change.before_json.body, change.after_json.body, change.before_json.body_sensitive, change.after_json.body_sensitive, show_unchanged_values, show_sensitive) ~}}
    {{~ small_changes = body_changes | array.filter "!is_large" ~}}
    {{~ large_changes = body_changes | array.filter "is_large" ~}}
    
    {{~ if small_changes.size > 0 ~}}
    
#### Body Changes

| Property | Before | After |
|----------|--------|-------|
    {{~ for change_item in small_changes ~}}
| {{ change_item.path | escape_markdown }} | {{ format_attribute_value_table(change_item.path, change_item.before, change.provider_name) }} | {{ format_attribute_value_table(change_item.path, change_item.after, change.provider_name) }} |
    {{~ end ~}}
    {{~ end ~}}
    
    {{~ if large_changes.size > 0 ~}}
<details>
<summary>Large body property changes</summary>

    {{~ for change_item in large_changes ~}}
##### **{{ change_item.path | escape_markdown }}:**

{{ format_large_value(change_item.before, change_item.after, large_value_format) }}

    {{~ end ~}}
</details>
    {{~ end ~}}

{{~ end ~}}

</details>
```

**Template Notes:**
- Uses existing `change.summary_html` for collapsible summary line
- Leverages existing helpers: `escape_markdown`, `format_attribute_value_table`, `format_large_value`
- Respects `show_unchanged_values` and `large_value_format` from `ReportModel`
- Consistent styling with existing templates
- Handles all actions: create, update, delete, replace (replace is like create)

#### 3. Helper Registration

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.Registry.cs`

Add new helper registrations:

```csharp
scriptObject.Import("flatten_json", new Func<object?, string, List<object>>(FlattenJson));
scriptObject.Import("compare_json_properties", new Func<object?, object?, object?, object?, bool, bool, List<object>>(CompareJsonProperties));
scriptObject.Import("azure_api_doc_link", new Func<string, string?>(AzureApiDocLink));
scriptObject.Import("parse_azure_resource_type", new Func<string, object>(ParseAzureResourceType));
scriptObject.Import("extract_azapi_metadata", new Func<object, object>(ExtractAzapiMetadata));
```

#### 4. Testing Strategy

**Unit Tests:**
- Test each Scriban helper function in isolation
- Test with various JSON structures (simple, nested, arrays, nulls)
- Test sensitivity masking
- Test large value detection

**Template Tests:**
- Snapshot tests with realistic azapi_resource plans
- Test all actions: create, update, delete, replace
- Test with/without documentation links
- Test with sensitive values (masked and unmasked)
- Test with large body properties

**Integration Tests:**
- End-to-end test with complete Terraform plan containing azapi_resource
- Validate markdown output with markdownlint

### Edge Cases to Handle

1. **Empty Body:** If `body` is `null` or `{}`, show "(empty)" message
2. **Body is String:** If `body` is a string (not parsed JSON), show as code block
3. **Non-Microsoft Resource Types:** Don't show documentation link for custom providers
4. **Entire Body Marked Sensitive:** Mask entire body with message
5. **Missing Attributes:** Handle when `name`, `location`, etc. are missing
6. **Large Arrays:** Summarize as `[N items]` with collapsible details
7. **Deep Nesting:** Paths like `properties.config.settings.advanced.option1` should work

### Performance Considerations

- **JSON Flattening:** Linear time complexity O(n) where n is number of properties
- **Comparison:** O(n) for comparing flattened properties
- **Large Bodies:** Memory usage scales with body size; acceptable for typical resources (<100KB)
- **Template Rendering:** Scriban is fast; no performance concerns for typical use cases

---

## Integration Points

### Existing Components Used

1. **Template Resolution:** `TemplateResolver.ResolveResourceTemplate()` already supports `{provider}/{resource}.sbn`
2. **Scriban Helpers:** Reuse existing formatting helpers for consistency
3. **Large Value Handling:** Use existing `LargeValueFormat` and `IsLarge` patterns
4. **Sensitive Value Masking:** Follow existing `--show-sensitive` behavior
5. **Report Model:** Access via `change.before_json`, `change.after_json`, `change.provider_name`

### No Changes Required To

- `ReportModelBuilder` - already populates `BeforeJson` and `AfterJson`
- `TemplateResolver` - already supports provider/resource templates
- `MarkdownRenderer` - already invokes resource-specific templates
- Template resolution logic - works as-is

---

## Risk Assessment

### Low Risk

✅ **Template rendering** - Well-understood pattern, many existing examples  
✅ **JSON flattening** - Straightforward recursive traversal  
✅ **Existing helper reuse** - Proven formatters for consistency  
✅ **Large value handling** - Established pattern  

### Medium Risk

⚠️ **Azure documentation links** - Best-effort heuristic may produce broken links  
*Mitigation:* Clear disclaimer in template; users can still search manually

⚠️ **Sensitivity handling for body** - Complex JSON structure to navigate  
*Mitigation:* Fallback to masking entire body if per-property fails

⚠️ **Very large bodies** - May produce long markdown  
*Mitigation:* Existing large value collapsing mechanism

### High Risk

❌ **None identified**

---

## Success Criteria

- [ ] `azapi/resource.sbn` template created in Templates directory
- [ ] New Scriban helpers implemented and registered
- [ ] JSON body flattened to dot-notation table format
- [ ] Standard attributes (type, name, parent_id, location, tags) displayed
- [ ] Azure REST API documentation links generated (best-effort)
- [ ] Update operations show changed properties with before/after values
- [ ] Sensitive values masked unless `--show-sensitive` flag used
- [ ] Large body properties moved to collapsible sections
- [ ] Template works for create, update, delete, replace actions
- [ ] All tests pass (unit, template, integration)
- [ ] Markdown output passes markdownlint validation
- [ ] UAT validation with real azapi_resource plans

---

## Future Enhancements

**Not in Current Scope (Potential Future Features):**

1. **Resource-Specific Body Formatting:** Custom rendering for well-known Azure resource types (e.g., different format for Automation Accounts vs. Storage Accounts)
   - *Reason deferred:* Generic approach handles 90% of cases; specific formatting can be added based on user feedback

2. **Schema-Aware Validation:** Validate body against Azure resource schemas
   - *Reason deferred:* Terraform already validates; adds complexity without clear benefit

3. **Documentation Link Mapping Table:** Maintain accurate URL mappings for common resources
   - *Reason deferred:* Best-effort heuristic is sufficient for v1; can add if users report frequent broken links

4. **Semantic Diffing for Body Arrays:** Smart comparison of array items (like firewall rules)
   - *Reason deferred:* Generic approach works; semantic diffing requires domain knowledge per resource type

5. **Body Property Descriptions:** Add tooltips or descriptions for common properties
   - *Reason deferred:* Out of scope; users can reference Azure docs

---

## Appendix: Example Output

### Create Operation

```markdown
<details>
<summary>➕ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources | westeurope</summary>
<br>

**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation (best-effort)](https://learn.microsoft.com/rest/api/automation/automation-accounts/)

| Attribute | Value |
|-----------|-------|
| name | `myAccount` |
| parent_id | Resource Group `example-resources` |
| location | 🌍 `westeurope` |

#### Body Configuration

| Property | Value |
|----------|-------|
| properties.disableLocalAuth | ✅ `true` |
| properties.publicNetworkAccess | ❌ `false` |
| properties.sku.name | `Basic` |

</details>
```

### Update Operation

```markdown
<details>
<summary>🔄 azapi_resource myAccount — Microsoft.Automation/automationAccounts | 2 properties changed</summary>
<br>

**Type:** `Microsoft.Automation/automationAccounts@2021-06-22`

📚 [View API Documentation (best-effort)](https://learn.microsoft.com/rest/api/automation/automation-accounts/)

| Attribute | Value |
|-----------|-------|
| name | `myAccount` |
| parent_id | Resource Group `example-resources` |
| location | 🌍 `westeurope` |

#### Body Changes

| Property | Before | After |
|----------|--------|-------|
| properties.sku.name | `Basic` | `Standard` |
| properties.disableLocalAuth | ❌ `false` | ✅ `true` |

</details>
```

---

## References

- **Feature Specification:** `docs/features/040-azapi-resource-template/specification.md`
- **Existing Templates:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/*`
- **Scriban Helpers:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.*.cs`
- **Template Resolution:** `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateResolver.cs`
- **Azure REST API Docs:** https://learn.microsoft.com/rest/api/
