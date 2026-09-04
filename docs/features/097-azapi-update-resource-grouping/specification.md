# Feature: Apply Attribute Grouping to azapi_update_resource

## Overview

Extend the intelligent attribute grouping and array rendering implemented in Feature 034 to the `azapi_update_resource` Terraform resource type. Currently, `azapi_resource` benefits from prefix-based grouping and improved array rendering, but `azapi_update_resource` falls back to the generic `_resource.sbn` template, which renders attributes as a flat, ungrouped list. This creates an inconsistent user experience when reviewing partial Azure resource updates.

## User Goals

- **Consistent experience across azapi resources:** Users reviewing both `azapi_resource` and `azapi_update_resource` should see the same intelligent grouping and clean array rendering
- **Improved readability for partial updates:** Users making targeted updates to Azure resources should see grouped attributes just like they do for full resource creates
- **Reduced visual clutter in updates:** Long, repetitive property paths in update operations should be collapsed into logical groups
- **Easy comparison of update changes:** Users should easily identify what's being updated in a structured, grouped format

## Scope

### In Scope

- Create dedicated template `azapi/update_resource.sbn` for the `azapi_update_resource` resource type
- Apply the same grouping logic from Feature 034 to `azapi_update_resource` body attributes:
  - Automatic detection of attributes with common prefixes (≥3 attributes required)
  - Array-indexed attribute grouping (e.g., `security[0].foo`, `security[1].foo`)
  - Nested object attribute grouping (e.g., `cors.allowedOrigins[0]`, `cors.supportCredentials`)
  - Hybrid rendering strategy (matrix tables for ≤8 properties/item, per-item tables for >8)
- Adapt the template to handle the different data structure of `azapi_update_resource`:
  - `type` - Azure resource type (e.g., `Microsoft.Web/sites@2022-03-01`)
  - `resource_id` - The full Azure resource ID being updated
  - `body` - The JSON body with partial update fields
  - No `name`, `parent_id`, `location`, or `tags` at top level
- Reuse existing Scriban helpers from `AzApi.Rendering.cs` (specifically `RenderAzapiBody()`)
- Support for update and delete operations on `azapi_update_resource`

### Out of Scope

- Changes to the grouping algorithm itself (that's already implemented in Feature 034)
- Changes to other azapi resource types beyond `azapi_update_resource`
- Custom grouping rules specific to `azapi_update_resource`
- Changes to top-level attribute rendering (type, resource_id) - only body attributes get grouping
- Backwards incompatible changes to the existing `azapi_resource` template

## User Experience

### Current Experience (Problem)

When reviewing an `azapi_update_resource` with arrays or nested structures, users see a flat list:

```markdown
###### Body

| Property | Before | After |
|----------|--------|-------|
| siteConfig.netFrameworkVersion | `v4.8` | `v6.0` |
| siteConfig.alwaysOn | `❌ false` | `✅ true` |
| siteConfig.connectionStrings[0].name | - | `Database` |
| siteConfig.connectionStrings[0].connectionString | - | `Server=tcp:...` |
| siteConfig.connectionStrings[0].type | - | `SQLAzure` |
| siteConfig.connectionStrings[1].name | - | `Redis` |
| siteConfig.connectionStrings[1].connectionString | - | `myredis...` |
| siteConfig.connectionStrings[1].type | - | `RedisCache` |
```

**Problems:**
- Long, repetitive property paths (`siteConfig.connectionStrings[0]...`)
- Hard to see grouping and structure
- Inconsistent with how `azapi_resource` renders the same data

### Proposed Experience (Solution)

After implementing this feature, `azapi_update_resource` will render with the same grouping as `azapi_resource`:

```markdown
###### Body - `siteConfig`

| Property | Before | After |
|----------|--------|-------|
| netFrameworkVersion | `v4.8` | `v6.0` |
| alwaysOn | `❌ false` | `✅ true` |

###### `connectionStrings` Array (new)

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
```

**Benefits:**
- Consistent with `azapi_resource` rendering
- Clean property names without repetitive prefixes
- Clear visual separation between different sections
- Easier to review and understand partial updates

## How Users Will Interact

Users don't interact with this feature directly - it automatically improves the rendering of `azapi_update_resource` in markdown reports.

**Workflow:**
1. User runs `tfplan2md` on a Terraform plan containing `azapi_update_resource` changes
2. Tool detects the resource type as `azapi_update_resource`
3. Tool resolves to the new `azapi/update_resource.sbn` template (instead of falling back to `_resource.sbn`)
4. Template applies the same grouping logic from Feature 034 via `RenderAzapiBody()` helper
5. User reviews the cleaner, grouped markdown output in their PR

**No configuration required** - grouping is automatic based on attribute structure.

## Success Criteria

- [ ] `azapi_update_resource` resolves to dedicated template `azapi/update_resource.sbn`
- [ ] Template correctly extracts and displays `type` and `resource_id` attributes
- [ ] Body attributes with ≥3 common prefix components are automatically grouped
- [ ] Array-indexed attributes are rendered with improved structure (matching Feature 034 behavior)
- [ ] Nested object attributes are grouped appropriately
- [ ] Update operations show before/after values within grouped sections
- [ ] Delete operations for `azapi_update_resource` render correctly with grouping
- [ ] The rendering follows the project's report style guide (data as code, labels as text)
- [ ] No information is lost - all attributes are displayed
- [ ] Grouping behavior is consistent with `azapi_resource` template
- [ ] Edge cases are handled gracefully (empty body, single items, deeply nested)
- [ ] Documentation link generation works (if applicable for update resources)
- [ ] All existing tests pass
- [ ] New tests validate `azapi_update_resource` grouping behavior

## Technical Approach

### Template Implementation

Create `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn` with structure:

1. **Header section:** Display resource type and resource_id
2. **Documentation link:** Generate Azure API documentation link based on type (if applicable)
3. **Body rendering:** Call `render_azapi_body` helper with appropriate parameters for update/delete operations
4. **Code analysis findings:** Include security/quality findings section

### Key Differences from `azapi_resource` Template

| Aspect | `azapi_resource` | `azapi_update_resource` |
|--------|------------------|-------------------------|
| Metadata extraction | `extract_azapi_metadata` returns name, parent_id, location, tags | Only type and resource_id are relevant |
| Top-level attributes | Show name, parent_id, location in table | Show type and resource_id only |
| Tags rendering | Display tags badges | No tags (updates don't include tags) |
| Body context | Full resource body | Partial update body |
| Create action | Supported | Not applicable (update resource doesn't create) |
| Replace action | Supported | Not applicable |

### Reusing Existing Helpers

The template will leverage existing Scriban helpers:
- `render_azapi_body` - Main rendering function with grouping logic
- `azure_api_doc_link` - Generate documentation links
- `format_attribute_value_table` - Format values consistently
- `get_attribute_finding_indicator` - Security/quality indicators

## Dependencies

- **Feature 034** - Improved AzAPI Attribute Grouping and Array Rendering (REQUIRED)
  - Provides the `render_azapi_body` Scriban helper with grouping logic
  - Defines the hybrid rendering strategy
  - Implements prefix detection and array grouping algorithms
- Existing `azapi/resource.sbn` template (as reference implementation)
- Scriban helper functions in `AzApi.Rendering.cs`, `AzApi.Rendering.Update.cs`
- Template resolution system in `TemplateResolver.cs`
- Report style guide (`docs/report-style-guide.md`)

## Open Questions

### 1. Documentation Link Behavior

**Question:** Should `azapi_update_resource` include Azure API documentation links like `azapi_resource` does?

**Consideration:** Update resources have a `type` field that could be used to generate the link, but they're updating existing resources rather than creating new ones.

**Recommendation:** Yes, include the documentation link. It's helpful for users to reference the API documentation when reviewing what attributes are being updated.

### 2. Resource ID Display Format

**Question:** Should the `resource_id` be displayed with any special formatting or truncation?

**Consideration:** Azure resource IDs can be very long (e.g., `/subscriptions/{id}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}`).

**Options:**
- Display full ID as code (current approach for similar fields)
- Truncate middle sections with ellipsis
- Display hierarchically (subscription → resource group → resource)

**Recommendation:** Display full ID as code for MVP. Users need the complete ID for verification. Consider truncation/formatting in future if feedback indicates it's too verbose.

### 3. Empty Body Handling

**Question:** How should the template handle `azapi_update_resource` with an empty body?

**Consideration:** Partial updates might have an empty body in edge cases or if the update only changes computed fields.

**Recommendation:** Display `*Body: (empty)*` message, consistent with `azapi_resource` template behavior.

## Constraints

- Must maintain consistency with Feature 034's grouping behavior
- Must follow report style guide (data as code, labels as text, icons, etc.)
- Template must handle both update and delete actions
- Must work correctly with sensitive value masking (`--show-sensitive` flag)
- All rendered markdown must pass markdownlint validation
- Must not break existing `azapi_resource` rendering

## Assumptions

- `azapi_update_resource` body attributes have similar structure to `azapi_resource` body attributes
- Users want consistent grouping behavior across all azapi resource types
- The minimum threshold of 3 attributes for grouping is appropriate for update resources
- Most `azapi_update_resource` usage includes body updates (not just empty bodies)
- The `RenderAzapiBody()` helper is sufficiently generic to handle both resource types

## Related Features

- **Feature 034** - Improved AzAPI Attribute Grouping and Array Rendering (direct dependency)
- **Feature 040** - Custom Template for azapi_resource (reference implementation)
- Report Style Guide - Defines formatting standards this feature must follow
