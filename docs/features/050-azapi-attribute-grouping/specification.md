# Feature: Improved AzAPI Attribute Grouping and Array Rendering

## Overview

Enhance the readability of azapi_resource JSON body attributes by implementing intelligent grouping and improved array rendering. This feature addresses two related problems:

1. **Prefix-based grouping:** Attributes with common prefixes create visual clutter and long property paths
2. **Array rendering:** Indexed attributes like `array[0].property` are repetitive and hard to scan

The solution will apply to all azapi resources that work with JSON body attributes (azapi_resource, azapi_update_resource, and potentially others).

## User Goals

- **Quickly identify configuration structure:** Users reviewing azapi resources in PRs should immediately understand the hierarchy and grouping of properties
- **Reduce visual clutter:** Long, repetitive property paths should be collapsed into logical groups
- **Easy array scanning:** Array items should be easy to compare and review without repetitive prefix noise
- **Maintain reviewability:** Changes to grouped attributes should remain clearly visible in update operations

## Scope

### In Scope

- Automatic detection of attributes with common prefixes (≥3 attributes required to trigger grouping)
- Grouping logic for array-indexed attributes (e.g., `security[0].foo`, `security[1].foo`)
- Grouping logic for nested object attributes (e.g., `cors.allowedOrigins[0]`, `cors.supportCredentials`)
- Improved rendering of array items to show structure clearly
- Multiple rendering options documented for maintainer selection
- Support for create, update, delete, and replace operations
- Applies to all azapi resources with JSON body attributes

### Out of Scope

- Grouping attributes with fewer than 3 shared-prefix items (threshold is fixed at 3)
- Custom/configurable grouping rules per resource type
- Grouping for non-azapi resources (this is azapi-specific)
- Changes to non-body attributes (location, name, type, etc.)
- Interactive/dynamic grouping (collapsing/expanding beyond what's already in the rendering)

## User Experience

### Current Experience (Problem)

When reviewing an azapi_resource with arrays or nested structures:

```markdown
###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |
| connectionStrings[0].name | `Database` |
| connectionStrings[0].connectionString | `Server=tcp:...` |
| connectionStrings[0].type | `SQLAzure` |
| connectionStrings[1].name | `Redis` |
| connectionStrings[1].connectionString | `myredis...` |
| connectionStrings[1].type | `RedisCache` |
| appSettings[0].name | `ASPNETCORE_ENVIRONMENT` |
| appSettings[0].value | `Production` |
| appSettings[1].name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| appSettings[1].value | `InstrumentationKey=...` |
```

**Problems:**
- Long, repetitive property paths
- Hard to see where one array item ends and another begins
- Difficult to quickly understand the structure
- Visual clutter obscures important values

### Proposed Experience (Solution)

After implementing grouping (exact format TBD based on rendering option selection):

```markdown
###### Body - `siteConfig`

| Property | Value |
|----------|-------|
| netFrameworkVersion | `v6.0` |
| alwaysOn | `✅ true` |

###### `connectionStrings` Array

**Item [0]**

| Property | Value |
|----------|-------|
| name | `Database` |
| connectionString | `Server=tcp:...` |
| type | `SQLAzure` |

**Item [1]**

| Property | Value |
|----------|-------|
| name | `Redis` |
| connectionString | `myredis...` |
| type | `RedisCache` |

###### `appSettings` Array

**Item [0]**

| Property | Value |
|----------|-------|
| name | `ASPNETCORE_ENVIRONMENT` |
| value | `Production` |

**Item [1]**

| Property | Value |
|----------|-------|
| name | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| value | `InstrumentationKey=...` |
```

**Benefits:**
- Clear visual separation between arrays
- Clean property names without repetitive prefixes
- Easy to see structure at a glance
- Array items are explicitly labeled

## How Users Will Interact

Users don't interact with this feature directly - it automatically improves the rendering of azapi resources in markdown reports.

**Workflow:**
1. User runs `tfplan2md` on a Terraform plan containing azapi resources
2. Tool detects attributes with ≥3 shared prefixes
3. Tool automatically groups and renders them using the selected rendering strategy
4. User reviews the cleaner, more structured markdown output in their PR

**No configuration required** - grouping is automatic based on attribute structure.

## Success Criteria

- [ ] Attributes with ≥3 common prefix components are automatically grouped
- [ ] Array-indexed attributes (e.g., `array[0].property`) are rendered with improved structure
- [ ] Nested object attributes (e.g., `object.property`) are handled appropriately
- [ ] Grouping preserves all information - no data is lost or hidden
- [ ] For update operations, changed attributes within groups are clearly highlighted
- [ ] The rendering follows the project's report style guide (data as code, labels as text)
- [ ] All azapi resource types with JSON bodies benefit from this improvement
- [ ] Edge cases are handled gracefully (empty arrays, single items, deeply nested, mixed types)
- [ ] Rendering options are documented with examples for maintainer selection
- [ ] Performance remains acceptable (no significant rendering slowdown)
- [ ] The solution works consistently across create, update, delete, and replace operations

## Open Questions

### 1. Rendering Strategy Selection ✅ **RESOLVED**

**Decision:** Implement **Option 1D** (Hybrid - Separate Tables for Array Items) combined with **Option 2C/2A** array rendering strategy.

**Specifics:**
- **Prefix grouping (1D):** Arrays with ≥3 attributes get dedicated sections (`###### \`arrayName\` Array`). Non-array grouped properties remain in the main table with full paths.
- **Array rendering (2C/2A):** 
  - Arrays with ≤8 properties per item → Traditional table format (items as rows, properties as columns)
  - Arrays with >8 properties per item → Index-grouped subsections (separate table per item)

**Rationale:**
- Clear separation between arrays and other properties
- Arrays are visually prominent with dedicated subheadings
- Compact table format (2C) for most common cases
- Graceful fallback to per-item tables (2A) for complex arrays
- Non-array grouped properties stay in context without creating excessive sections

**Reference:** See `rendering-options.md` for detailed examples and comparison.

### 2. Update Operation Handling

**Question:** How should grouped attributes be displayed in update operations?

**Considerations:**
- Should unchanged array items be hidden or shown?
- Should the entire array group show if only one item changed?
- How to highlight changed properties within grouped items?

**Options:**
- Always show full groups if any item changed (simpler, maintains context)
- Only show changed items within arrays (more concise, may lose context)
- Use diff markers within grouped tables (hybrid approach)

**Recommendation:** Start with option 1 (show full groups if any item changed) for MVP. This maintains context and is simpler to implement. Can refine based on user feedback.

### 3. Nested Array Handling

**Question:** How deep should grouping logic recurse?

**Example:** `security[0].rules[0].ports[0]` - should this create nested groups or flatten?

**Recommendation:** Start with single-level array grouping, extend if needed based on real-world examples.

### 4. Mixed Attribute Types

**Question:** How to handle arrays with inconsistent property sets?

**Example:**
```
items[0].type = "A"
items[0].config = "x"
items[1].type = "B"
items[1].settings = "y"  // Different property name
```

**Options:**
- Show all properties across all items (with null/- for missing)
- Keep items separate if schemas differ significantly
- Use fallback rendering for heterogeneous arrays

### 5. Threshold Configurability

**Question:** Should the grouping threshold (≥3) ever be configurable?

**Current decision:** No, threshold is fixed at 3.

**Future consideration:** If users request different thresholds, add CLI option `--azapi-grouping-threshold` (default: 3).

## Dependencies

- Existing azapi_resource template (`src/Oocx.TfPlan2Md/Templates/Providers/azapi/azapi_resource.scriban`)
- Scriban helper functions for JSON flattening (`src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/`)
- Report style guide (`docs/report-style-guide.md`)
- Test snapshots for azapi resources

## Constraints

- Must maintain backward compatibility with existing azapi resource rendering structure
- Must follow report style guide (data as code, labels as text, icons, etc.)
- Performance: Grouping logic should not significantly impact rendering time
- All rendered markdown must pass markdownlint validation
- Must work correctly with sensitive value masking (`--show-sensitive` flag)
- Must handle edge cases gracefully without template errors

## Assumptions

- All azapi resources that work with JSON bodies will benefit from this feature
- Users prefer cleaner, grouped rendering over flat, repetitive property paths
- The minimum threshold of 3 attributes is appropriate for most use cases
- Array items within the same array have similar structure (homogeneous arrays are common)

## Related Features

- Feature 040: Custom Template for azapi_resource (original azapi template implementation)
- Report Style Guide: Defines formatting standards this feature must follow
