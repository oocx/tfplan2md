# Issue: Readable Display Names Applied to Resource Identity Attributes

## Problem Description

The "readable display name" feature is incorrectly applied to a resource's own identity attributes (`id`, `name`) when rendering attribute tables. This causes redundant and confusing output where a resource's own identifier is decorated with semantic icons and additional context that should only be used when **referencing other resources**.

### Example

For `azurerm_monitor_metric_alert`, the `id` attribute currently renders as:
```
MetricAlerts 🆔 `name` in resource group 📁 `rg name` of subscription 🔑 `subscription name (subscription id)`
```

Instead, it should render as simply the raw value of the `id` attribute (the Azure resource ID), since this is the resource's own identity, not a reference to another resource.

## Steps to Reproduce

1. Create a Terraform plan with an `azurerm_monitor_metric_alert` resource (or any Azure resource with an `id` attribute)
2. Run tfplan2md to generate the markdown report
3. Observe the `id` attribute in the resource's attribute table
4. The `id` value shows a "readable display name" with icons and contextual information

## Expected Behavior

- When an attribute IS the resource's own identity (like the `id` or `name` attribute of the resource itself), it should render as the raw value without semantic icons or contextual expansion
- The "readable display name" feature should ONLY be applied when an attribute value **references another resource**
- For example, if a resource has a `virtual_network_id` attribute pointing to a different resource, THEN the readable display name should be used

## Actual Behavior

The semantic formatting system applies "readable display name" formatting to **all** attributes named `id` or `name`, regardless of whether they represent:
1. The resource's own identity (should NOT use readable display names)
2. A reference to another resource (SHOULD use readable display names)

## Root Cause Analysis

### Affected Components

**Primary Issue Location:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs#L237-253`
  - Method: `TryFormatNameAttribute` - Always formats `name` attributes with 🆔 icon
  - This is called for ALL attributes, including the resource's own `name` attribute

**Template Rendering:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn#L27,39,50-51`
  - Lines 27, 39, 50-51: Call `format_attribute_value_table(attr.name, attr.after/before, change.provider_name)`
  - This applies semantic formatting to every attribute in the details table

**Summary Building:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs#L51-53`
  - Lines 51-53: Calls `FormatAttributeValueSummary("name", nameValue!, null)` for resource summary
  - This is actually correct usage - building a contextual summary display
  - However, this shows the pattern is applied globally

**Semantic Formatting Chain:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs#L246-295`
  - Method: `FormatAttributeValueCore` - Orchestrates all semantic formatting
  - Line 272: Calls `TryFormatSemanticValue` which eventually calls `TryFormatNameAttribute`

**Azure Scope Formatting:**
- File: `src/Oocx.TfPlan2Md/Platforms/Azure/ScribanHelpers.Azure.cs#L149-185`
  - Method: `FormatAzureScopeForTable` - Builds readable scope strings
  - Lines 166, 171: Uses `FormatAttributeValueTable("name", ...)` to format resource names in scope descriptions
  - This is a legitimate use case where readable formatting IS desired

### What's Broken

The semantic formatting system doesn't distinguish between:

1. **Self-referential attributes**: Attributes that describe the resource itself
   - Examples: `id`, `name` (when it's the resource's own name)
   - Should render as: Raw value without decoration
   
2. **Reference attributes**: Attributes that point to other resources
   - Examples: `virtual_network_id`, `subnet_id`, `role_definition_id`
   - Should render as: Readable display name with context

Currently, the `TryFormatNameAttribute` method applies formatting based solely on the attribute **name** (`name`, `resource_group_name`), not on the **context** of where that attribute appears.

### Why It Happened

The readable display name feature was designed to make Azure resource references more readable. The implementation correctly identifies attribute names and values that should be formatted (subscription IDs, resource group names, etc.). However, it lacks context awareness to know:
- "Is this the resource's OWN identity attribute?"
- "Or is this a REFERENCE to another resource?"

The feature spec (docs/features/042-azure-display-enhancements/specification.md) focuses on formatting IDs and names throughout the report but doesn't explicitly address the distinction between self-identity and references.

## Suggested Fix Approach

### Option 1: Context-Aware Formatting (Recommended)

Add a parameter to the formatting functions to indicate whether we're formatting a **self-identity attribute** vs a **reference attribute**.

**Changes needed:**

1. **Update `format_attribute_value_table` function signature:**
   - Add optional parameter: `is_self_identity: bool = false`
   - When `is_self_identity` is true, skip semantic formatting for `id` and `name` attributes
   - Return raw value with appropriate code wrapping

2. **Update template usage in `_resource.sbn`:**
   - Detect when `attr.name == "id"` and pass `is_self_identity: true`
   - For most attributes, use default behavior (reference formatting)
   
3. **Create a list of "identity attributes":**
   - Define which attribute names represent self-identity: `["id"]`
   - The `name` attribute is trickier - it's sometimes self-identity, sometimes a reference
   - May need resource-type-specific logic

4. **Update semantic formatting helpers:**
   - Modify `TryFormatNameAttribute` and similar methods to accept context parameter
   - Skip formatting when context indicates self-identity

### Option 2: Attribute Name Pattern Matching (Simpler)

Use attribute naming patterns to detect references vs self-identity:
- Attributes ending in `_id` (e.g., `virtual_network_id`) → Reference, apply formatting
- Attribute named exactly `id` → Self-identity, raw value only
- Attribute named exactly `name` → Depends on resource type or default to raw

This is simpler but less accurate - some attributes like `name` are ambiguous.

### Option 3: Allowlist/Blocklist Approach

Create lists of attributes that should NEVER be formatted:
- Blocklist: `["id"]` - never apply readable display names to these
- Allowlist: All other attributes can be formatted

This is the simplest but least flexible approach.

## Recommended Implementation

**Use Option 1 (Context-Aware Formatting) with these steps:**

1. **Add `is_self_identity` parameter to formatting chain:**
   - `FormatAttributeValueTable` and related methods
   - Thread this parameter through `FormatAttributeValueCore` → `TryFormatSemanticValue` → `TryFormatNameAttribute`

2. **Update `_resource.sbn` template:**
   ```scriban
   {{~ if attr.name == "id" ~}}
   {{ value = format_attribute_value_table(attr.name, attr.after, change.provider_name, true) ~}}
   {{~ else ~}}
   {{ value = format_attribute_value_table(attr.name, attr.after, change.provider_name) ~}}
   {{~ end ~}}
   ```

3. **Update `TryFormatNameAttribute`:**
   ```csharp
   private static bool TryFormatNameAttribute(
       string attributeName, 
       string value, 
       ValueFormatContext context,
       bool isSelfIdentity,  // NEW PARAMETER
       out string formatted)
   {
       // Skip formatting for self-identity attributes
       if (isSelfIdentity && attributeName.Equals("name", StringComparison.OrdinalIgnoreCase))
       {
           formatted = string.Empty;
           return false;
       }
       
       // Existing logic...
   }
   ```

4. **Define identity attribute detection:**
   ```csharp
   private static bool IsSelfIdentityAttribute(string attributeName)
   {
       return attributeName.Equals("id", StringComparison.OrdinalIgnoreCase);
       // Note: "name" is NOT included here because it's sometimes used in references
   }
   ```

5. **Update ResourceSummaryHtmlBuilder carefully:**
   - The summary builder SHOULD use readable formatting for building contextual summaries
   - But attribute tables should NOT format self-identity attributes
   - This is a legitimate difference in usage context

## Related Tests

### Existing Test Patterns

**File: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`**
- Contains tests for semantic formatting of various attribute types
- Tests use pattern: `FormatAttributeValueTable(attributeName, value, providerName)`
- No existing tests specifically for `id` or `name` attribute formatting

### New Tests Needed

1. **Test: Format id attribute as self-identity:**
   ```csharp
   [Test]
   public void FormatAttributeValueTable_IdAttribute_AsSelfIdentity_ReturnsRawValue()
   {
       var result = FormatAttributeValueTable("id", "/subscriptions/.../resourceGroups/rg/providers/...", null, isSelfIdentity: true);
       
       result.Should().Be("`/subscriptions/.../resourceGroups/rg/providers/...`");
       // Should NOT contain readable display name
   }
   ```

2. **Test: Format name attribute as self-identity:**
   ```csharp
   [Test]
   public void FormatAttributeValueTable_NameAttribute_AsSelfIdentity_ReturnsRawValue()
   {
       var result = FormatAttributeValueTable("name", "my-resource-name", null, isSelfIdentity: true);
       
       result.Should().Be("`my-resource-name`");
       // Should NOT contain 🆔 icon
   }
   ```

3. **Test: Format reference attributes normally:**
   ```csharp
   [Test]
   public void FormatAttributeValueTable_VirtualNetworkId_AsReference_AppliesReadableFormat()
   {
       var result = FormatAttributeValueTable("virtual_network_id", "/subscriptions/.../virtualNetworks/vnet", null);
       
       // Should apply readable display name formatting
       result.Should().Contain("🆔"); // Or appropriate formatting
   }
   ```

4. **Integration test: Verify attribute table rendering:**
   - Create test with resource that has `id` attribute
   - Verify `id` renders as raw value in attribute table
   - Verify other `*_id` attributes use readable formatting

## Additional Context

### Feature History

- **docs/features/042-azure-display-enhancements/**: Introduced readable display names for Azure resources
- **docs/features/029-report-presentation-enhancements/**: Added semantic icons for attributes
- The feature was designed to make references to other resources more readable
- The spec doesn't explicitly address self-identity vs references

### Related Code Patterns

**Legitimate uses of readable formatting:**
1. `FormatAzureScopeForTable` - Building scope descriptions (good)
2. Resource summaries - Building one-line resource descriptions (good)
3. Reference attributes in tables - Making cross-resource references clear (good)

**Problematic uses:**
1. Attribute tables - Self-identity attributes get decorated (bad)

### Design Consideration

The distinction between "self-identity" and "reference" is conceptually clear but programmatically subtle:
- How do we know if `name` is the resource's own name or a reference?
- For most attributes, the answer is: attribute names ending in `_id` or `_name` are references
- The bare `id` attribute is ALWAYS self-identity
- The bare `name` attribute is context-dependent

**Recommendation:** Start with the simplest case (`id` attribute only) and expand to `name` if needed based on real-world examples.
