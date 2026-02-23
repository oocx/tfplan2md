# Issue: Readable Display Name Incorrectly Applied to Resource's Own Identity Attributes

## Problem Description

The "readable display name" feature is incorrectly applied to a resource's own identity attributes (`id`, `name`) when rendering attribute tables. This causes redundant and confusing output where a resource's own identifier is decorated with semantic icons AND additional context that should only be used when referencing other resources.

**Current incorrect behavior:**
When a resource's own `id` or `name` attribute contains an Azure resource ID, it gets formatted with the full readable display name like:
```
| id | Storage Account `🆔 sttfplan2mdlogs` in resource group `📁 rg-tfplan2md-demo` of subscription `🔑 Production (...)` |
```

**Expected behavior:**
The attribute should only get the semantic icon, NOT the additional mapping:
```
| id | `🆔 /subscriptions/.../resourceGroups/rg-tfplan2md-demo/providers/Microsoft.Storage/storageAccounts/sttfplan2mdlogs` |
```
OR
```
| name | `🆔 sttfplan2mdlogs` |
```

**Why this matters:**
- The full readable display name is valuable when an attribute references ANOTHER resource (e.g., `scope`, `key_vault_id`, `managedEnvironmentId`)
- But when showing a resource's own identity, the expanded format is redundant because:
  - The user is already looking at that resource's details section
  - The resource group and subscription are typically shown in the summary line
  - The full path adds visual clutter without providing new information

## Steps to Reproduce

1. Create a Terraform plan with an `azurerm_storage_account` or similar resource
2. Ensure the resource has an `id` attribute that will be computed (shown as "known after apply" or with a value)
3. Generate the markdown report
4. Observe that the `id` attribute in the attribute table may show the full expanded format instead of just the icon + value

**Note:** The current test snapshots may not show this bug clearly because most computed `id` attributes are filtered out or shown differently. The bug would be most visible when:
- The `id` attribute has a known value (not "known after apply")
- Or when looking at update/delete operations where the `id` is in the "before" state

## Root Cause Analysis

### Affected Components

**Primary issue:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AzureResourceIdFormatter.cs`
  - Line 33-49: `TryFormat` method
  - This formatter is registered to format ANY Azure resource ID, without context about whether it's the resource's own ID or a reference

**Registration point:**
- File: `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRmValueFormatterRegistration.cs`
  - Line 30-32: Registers `AzureResourceIdFormatter` for all azurerm providers
  - Match pattern: `new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null)`
  - This pattern matches ALL attributes from azurerm provider

**Formatting call chain:**
1. Template: `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`
   - Line 27, 39, 50: Calls `format_attribute_value_table(attr.name, attr.after, change.provider_name)`
2. Helper registration: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Registry.cs`
   - Line 49: Maps template function to `FormatAttributeValueTableWithRegistry`
3. Semantic formatting: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs`
   - Line 88-110: `FormatAttributeValueTableWithRegistry` method
   - Line 100-107: Checks `valueFormatterRegistry.TryFormat(context)` BEFORE applying semantic formatting
4. Formatter: `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AzureResourceIdFormatter.cs`
   - Line 33-49: Returns enriched format for ANY Azure resource ID value

### What's Broken

The `AzureResourceIdFormatter` does not have any logic to distinguish between:
- **Own identity attributes:** `id`, `name` - should only get semantic icon (🆔)
- **Reference attributes:** `scope`, `key_vault_id`, `parent_id`, `managedEnvironmentId` - should get full readable display name

The formatter is registered with a match pattern that applies to ALL attributes, with no exceptions:
```csharp
registry.Register(
    new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null),
    new AzureResourceIdFormatter(scopeFormatter));
```

### Why It Happened

The feature was designed to provide helpful context for resource references, which is valuable. However, the implementation doesn't account for the special case where an attribute represents the resource's own identity rather than a reference to another resource.

The `MatchPattern` class accepts:
- `providerPattern` - matches the provider name
- `resourceTypePattern` - matches the resource type (null = all types)
- `attributeNamePattern` - matches the attribute name (null = all attributes)
- `valuePattern` - matches the attribute value pattern (null = all values)

The current registration uses `null` for both `resourceTypePattern` and `attributeNamePattern`, meaning it matches ALL attributes. There's no mechanism to exclude attributes like `id` and `name`.

## Suggested Fix Approach

### Option 1: Exclude identity attributes in the match pattern (Recommended)

Modify the registration in `AzureRmValueFormatterRegistration.cs` to exclude `id` and `name` attributes:

```csharp
// Match all attributes EXCEPT id and name (negative lookahead)
registry.Register(
    new MatchPattern(
        "(^azurerm$|.*/azurerm$)", 
        null, 
        "^(?!id$|name$).*",  // Regex negative lookahead to exclude 'id' and 'name'
        null),
    new AzureResourceIdFormatter(scopeFormatter));
```

**Pros:**
- Simple, localized change
- Uses existing pattern matching system
- Preserves all existing behavior for reference attributes

**Cons:**
- Relies on regex negative lookahead
- Need to ensure it doesn't break other edge cases

### Option 2: Add context parameter to indicate "own identity"

Extend the formatting context to include information about whether the attribute represents the resource's own identity:

1. Extend `ServiceResolutionContext` to include `IsOwnIdentity` flag
2. Modify template to pass this flag when rendering `id` or `name` attributes
3. Update `AzureResourceIdFormatter.TryFormat` to skip enrichment when `IsOwnIdentity` is true

**Pros:**
- More explicit and self-documenting
- Could be useful for other formatters

**Cons:**
- Larger change across multiple components
- Requires template modifications
- More complex

### Option 3: Add a separate formatter for identity attributes

Create a new `IdentityAttributeFormatter` that handles `id` and `name` attributes and register it with higher priority than `AzureResourceIdFormatter`:

```csharp
// Register identity formatter first (higher priority)
registry.Register(
    new MatchPattern("(^azurerm$|.*/azurerm$)", null, "^(id|name)$", null),
    new IdentityAttributeFormatter());

// Register resource ID formatter for everything else
registry.Register(
    new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null),
    new AzureResourceIdFormatter(scopeFormatter));
```

**Pros:**
- Clean separation of concerns
- Easy to test
- Preserves existing behavior

**Cons:**
- Need to understand formatter registration priority order
- Adds another formatter class

## Related Tests

Tests that should be reviewed and potentially updated:

### Semantic Formatting Tests
- File: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`
- Should verify that `id` and `name` attributes only get semantic icons

### Azure Scope Formatting Tests
- File: `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/AzureScopeParserTests.cs`
- Lines 77, 137, 147, 157, 202: Show expected full readable display name format
- These tests are correct for REFERENCE attributes

### Role Assignment Tests
- File: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/RoleAssignmentTemplateTests.cs`
- Line 44, 58: Show `scope` attribute with full readable display name
- These are correct - `scope` is a reference to another resource

### Snapshot Tests
- Directory: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`
- Files to examine:
  - `comprehensive-demo-full.md`: Shows correct behavior for `name` attribute (line showing `| name | 🆔 sttfplan2mdlogs |`)
  - `azapi-update-resource-delete.md`: Shows `resource_id` with full format (correct - this is a reference)
  - `azapi-update-resource-update.md`: Shows `resource_id` with full format (correct - this is a reference)

### Tests that might need updates after fix:
1. Create a test case specifically for a resource's own `id` attribute showing a known Azure resource ID
2. Verify that `id` only shows semantic icon without the expanded format
3. Verify that reference attributes like `scope`, `parent_id`, `key_vault_id` still get the full format

## Additional Context

### Related Features
- Feature: `docs/features/019-azure-resource-id-formatting/specification.md` - Original feature for readable display names
- Feature: `docs/features/024-visual-report-enhancements/specification.md` - Semantic icons
- Feature: `docs/features/029-report-presentation-enhancements/specification.md` - Attribute formatting
- Feature: `docs/features/061-extensible-provider-registry/specification.md` - Value formatter registry

### Files to Review for Fix Implementation

**Core logic:**
1. `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AzureResourceIdFormatter.cs` - The formatter itself
2. `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRmValueFormatterRegistration.cs` - Registration with match pattern
3. `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/MatchPattern.cs` - Understanding how pattern matching works

**Formatting pipeline:**
1. `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs` - Where formatters are called
2. `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs` - Semantic icon formatting
3. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn` - Template that calls formatting

**Testing:**
1. `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`
2. Snapshot test files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`

### Example of Correct Behavior

**For a resource's own attributes (what we want):**
```markdown
| Attribute | Value |
| --------- | ----- |
| id | `🆔 /subscriptions/12345.../providers/Microsoft.Storage/storageAccounts/mystorageacct` |
| name | `🆔 mystorageacct` |
| location | `🌍 eastus` |
| resource_group_name | `📁 my-rg` |
```

**For reference attributes (already correct):**
```markdown
| Attribute | Value |
| --------- | ----- |
| scope | Storage Account `🆔 sttfplan2mdlogs` in resource group `📁 rg-tfplan2md-demo` of subscription `🔑 Production (...)` |
| key_vault_id | Key Vault `🆔 kv-prod-001` in resource group `📁 rg-security` of subscription `🔑 Production (...)` |
| parent_id | Resource Group `📁 rg-parent` in subscription `🔑 Production (...)` |
```

## Recommended Implementation Path

1. **Investigation Phase** (Already Complete)
   - ✅ Understand the formatting pipeline
   - ✅ Identify the root cause
   - ✅ Locate all affected components
   - ✅ Review existing tests

2. **Design Decision**
   - Choose between Option 1 (match pattern), Option 2 (context flag), or Option 3 (separate formatter)
   - Recommendation: **Option 1** for simplicity, fallback to **Option 3** if regex proves problematic

3. **Implementation**
   - Modify `AzureRmValueFormatterRegistration.cs` to exclude `id` and `name` attributes
   - Test with a simple plan that has known `id` values

4. **Testing**
   - Add explicit test case for resource's own `id` attribute with Azure resource ID value
   - Run existing snapshot tests to ensure no regression
   - Update snapshots if needed (though current snapshots may already be correct)

5. **Documentation**
   - Update feature documentation if needed
   - Add comments explaining why `id` and `name` are excluded from the formatter

## Open Questions

1. Are there other identity attributes besides `id` and `name` that should be excluded?
   - Possibly `self_link` in some providers?
   - Possibly `arn` in AWS provider?

2. Should the fix apply to other providers (AWS, GCP) or only Azure?
   - The issue describes Azure-specific behavior, but the pattern might exist elsewhere

3. Are there any edge cases where a resource's `id` attribute legitimately references another resource?
   - Unlikely, but worth considering

4. How should `name` attributes be handled when they contain full Azure resource IDs?
   - Current tests show `name` only contains the simple name (e.g., `mystorageacct`), not full IDs
   - If a `name` contains a full ID, should it be parsed or shown as-is?
