# Issue: Readable Display Name Incorrectly Applied to Resource Identity Attributes

## Problem Description

The "readable display name" feature, which adds semantic icons (🆔, 📁, etc.) and enhanced formatting to attribute values, is being incorrectly applied to a resource's own identity attributes (`id`, `name`) when rendering attribute tables. This causes redundant and confusing output where a resource's own identifier is decorated with icons and context that should only be used when **referencing other resources**.

### Example of Incorrect Behavior

When rendering an `azuread_user` resource, the attribute table currently shows:

| Attribute | Value |
| --------- | ----- |
| id | `🆔 00000000-0000-0000-0000-000000000000` |
| name | `🆔 john.doe` |
| user_principal_name | `🆔 john.doe@example.com` |

The `id` and `name` attributes should display their **raw values** without semantic icons because they are the resource's **own identity**, not references to other resources.

### Expected Correct Behavior

| Attribute | Value |
| --------- | ----- |
| id | `00000000-0000-0000-0000-000000000000` |
| name | `john.doe` |
| user_principal_name | `🆔 john.doe@example.com` |

**Key Distinction:**
- **Identity attributes** (`id`, `name`) of the resource being created/updated should show raw values
- **Cross-reference attributes** (e.g., `principal_id` in a role assignment pointing to a user's ID) should continue to get the readable display name treatment with semantic icons

## Steps to Reproduce

1. Create a Terraform plan that includes a resource with `id` and `name` attributes (e.g., `azuread_user`, `azurerm_resource_group`, etc.)
2. Run tfplan2md to generate the markdown report
3. Observe the attribute table in the rendered output
4. Notice that the resource's own `id` and `name` attributes have semantic icons (🆔) applied

## Expected Behavior

**Identity attributes of the resource itself should display raw values:**
- `id` attribute → Display as inline code: `` `00000000-0000-0000-0000-000000000000` ``
- `name` attribute → Display as inline code: `` `john.doe` ``

**Cross-reference attributes should continue to get semantic formatting:**
- `principal_id` in `azurerm_role_assignment` → `🆔 <value>` (references another resource's identity)
- `user_principal_name` → `🆔 <value>` (is a user identity attribute, distinct from generic `name`)
- `subscription_id` → `🔑 <value>` (references a subscription)
- `repository_id` → `🗃️ <value>` (references a repository)

## Actual Behavior

The semantic formatting is applied to **all** `id` and `name` attributes regardless of context, including a resource's own identity attributes.

## Root Cause Analysis

### Affected Components

**Primary files:**
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs`
  - **Lines:** 237-253 (`TryFormatNameAttribute` method)
  - **Lines:** 263-279 (`TryFormatNameAttributePlain` method)
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs`
  - **Lines:** 174-254 (`TryFormatSemanticValue` method - dispatches to identity formatting)
- **File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs`
  - **Lines:** 88-111 (`FormatAttributeValueTableWithRegistry` - entry point)
  - **Lines:** 123-153 (`FormatAttributeValueTableWithRegistryResource` - resource-aware entry point)
  - **Lines:** 246-295 (`FormatAttributeValueCore` - core formatting logic)

**Template files using the formatting:**
- All `.sbn` template files (e.g., `src/Oocx.TfPlan2Md/Providers/AzureAD/Templates/azuread/user.sbn`)
- Templates call `format_attribute_value_table_resource(attr.name, attr.after, change.provider_name, change.type)`

### What's Broken

The `TryFormatNameAttribute` method in `SemanticFormatting.Identity.cs` (lines 237-253) applies semantic formatting to **any** attribute named `name`:

```csharp
private static bool TryFormatNameAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
{
    if (attributeName.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase))
    {
        formatted = FormatIconValue($"📁 {value}", context, false);
        return true;
    }

    if (attributeName.Equals("name", StringComparison.OrdinalIgnoreCase))
    {
        formatted = FormatIconValue($"🆔 {value}", context, false);  // ← PROBLEM: Applied to ALL "name" attributes
        return true;
    }

    formatted = string.Empty;
    return false;
}
```

This method is called for **every attribute value** rendered in tables, including:
1. A resource's own `name` attribute (identity - should be plain)
2. Cross-reference attributes like `principal_name` (should have icon)

The code has no way to distinguish between these two cases.

### Why It Happened

The semantic formatting feature was designed to enhance readability by adding icons to attribute values that reference other resources (e.g., `principal_id`, `subscription_id`, `repository_id`). The feature correctly handles these cross-reference attributes.

However, when the feature added support for the `name` attribute, it didn't account for the fact that:
1. A resource has its own `name` attribute (identity - should be plain)
2. Other resources might reference this resource by name (cross-reference - should have icon)

The current implementation treats all `name` attributes the same way, applying semantic formatting unconditionally.

### Context Flow

The formatting pipeline works as follows:

1. **Template** (e.g., `user.sbn:27`):
   ```
   {{ value = format_attribute_value_table_resource(attr.name, attr.after, change.provider_name, change.type) }}
   ```

2. **Registry.cs:50** - Template helper registration:
   ```csharp
   scriptObject.Import("format_attribute_value_table_resource", new Func<...>(...) => 
       FormatAttributeValueTableWithRegistryResource(...)));
   ```

3. **SemanticFormatting.Registry.cs:123-153** - `FormatAttributeValueTableWithRegistryResource`:
   - Tries value formatter registry first (for principal mapping, etc.)
   - Falls back to semantic formatting

4. **SemanticFormatting.Registry.cs:246-295** - `FormatAttributeValueCore`:
   - Checks registry icons
   - Calls `TryFormatSemanticValue`

5. **SemanticFormatting.cs:174-254** - `TryFormatSemanticValue`:
   - Calls various `TryFormat*` methods including `TryFormatNameAttribute`

6. **SemanticFormatting.Identity.cs:237-253** - `TryFormatNameAttribute`:
   - **BUG HERE:** Returns formatted `🆔 {value}` for ANY attribute named `name`

## Suggested Fix Approach

### Option 1: Context-Aware Attribute Detection (Recommended)

Modify the semantic formatting logic to distinguish between:
- **Identity attributes:** The resource's own `id` and `name` 
- **Cross-reference attributes:** Attributes referencing other resources

**Implementation approach:**
1. Add a parameter or context flag to indicate if we're formatting a resource's own attributes vs cross-references
2. Modify `TryFormatNameAttribute` to check this context
3. Only apply semantic formatting when the attribute is a cross-reference, not an identity

**Code change location:**
- `SemanticFormatting.Identity.cs:237-253` - Add context check
- Templates may need to differentiate identity vs cross-reference attributes (or this could be auto-detected)

### Option 2: Attribute Name Exclusion List

Create an exclusion list of attribute names that should never receive semantic formatting when they represent a resource's own identity:

**Excluded attributes:**
- `id` (resource's own ID)
- `name` (resource's own name)
- Other identity attributes specific to the resource itself

**Code change location:**
- Add a helper method: `IsResourceIdentityAttribute(attributeName, resourceType)` 
- Call this in `TryFormatSemanticValue` before dispatching to specific formatters
- Return `false` immediately for identity attributes

### Option 3: Smart Detection Based on Attribute Patterns

Use attribute naming patterns to detect identity vs cross-reference:
- Identity: `id`, `name` (no suffix)
- Cross-reference: `principal_id`, `user_id`, `subscription_id`, `*_name` (with prefix/suffix)

This approach is more fragile but requires less explicit configuration.

### Recommended Solution

**Use Option 1 or Option 2** (or a combination):
1. **Option 2** is simpler to implement initially - add exclusions for `id` and `name` in `TryFormatNameAttribute`
2. **Option 1** provides better long-term design - templates can indicate the semantic context of attributes

**Initial fix (Option 2):**
```csharp
private static bool TryFormatNameAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
{
    // Exclude resource identity attributes from semantic formatting
    if (attributeName.Equals("id", StringComparison.OrdinalIgnoreCase) ||
        attributeName.Equals("name", StringComparison.OrdinalIgnoreCase))
    {
        formatted = string.Empty;
        return false;  // Let default formatting handle it
    }

    if (attributeName.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase))
    {
        formatted = FormatIconValue($"📁 {value}", context, false);
        return true;
    }

    formatted = string.Empty;
    return false;
}
```

**Note:** A similar change would be needed in `TryFormatNameAttributePlain` (lines 263-279).

## Related Tests

Tests that should pass after the fix:

- [ ] Attribute table tests verifying `id` displays as plain inline code
- [ ] Attribute table tests verifying `name` displays as plain inline code
- [ ] Tests verifying cross-reference attributes (e.g., `principal_id`) still get semantic icons
- [ ] Tests verifying `resource_group_name` still gets 📁 icon (it's a cross-reference)
- [ ] Tests verifying `user_principal_name` still gets 🆔 icon (it's an identity attribute, not generic `name`)
- [ ] Snapshot tests for `azuread_user`, `azurerm_resource_group`, and other resources with `id`/`name` attributes

## Additional Context

### Related Features

The semantic formatting feature was introduced in:
- **docs/features/024-visual-report-enhancements/** (if it exists, or similar numbered feature)
- **docs/features/053-azuread-resources-enhancements/specification.md** - Azure AD identity attributes

### Design Intent

From the feature specifications and code comments:
- **Purpose:** Enhance readability by adding semantic icons to attribute values
- **Target:** Cross-reference attributes that point to other resources (principal IDs, subscription IDs, etc.)
- **Intent:** Help users quickly identify what type of resource is being referenced

The feature was **not** intended to decorate a resource's own identity attributes with redundant icons.

### Impact Assessment

**User Impact:**
- **Confusion:** Users see redundant icons on identity attributes (e.g., `id: 🆔 <value>`)
- **Visual noise:** Attribute tables become cluttered with unnecessary icons
- **Inconsistent semantics:** The 🆔 icon loses meaning when applied to both identity and references

**Fix Priority:** **High** - This affects readability and user experience across all resource types

### Verification Steps

After implementing the fix:

1. **Generate test reports** with resources containing `id` and `name` attributes
2. **Verify identity attributes** display as plain inline code: `` `value` ``
3. **Verify cross-references** still display with icons: `` `🆔 value` ``
4. **Run snapshot tests** to ensure no unintended regressions
5. **Visual inspection** of rendered markdown in GitHub/Azure DevOps

## Definition of Done

The issue is resolved when:
- [ ] Identity attributes (`id`, `name`) display as plain inline code without semantic icons
- [ ] Cross-reference attributes continue to receive semantic formatting
- [ ] All existing tests pass
- [ ] New tests added to verify the fix
- [ ] Snapshot tests updated if needed
- [ ] Documentation updated if the behavior change affects user-facing features
- [ ] Code reviewed and approved
- [ ] UAT passed (if needed)
- [ ] Release notes prepared
