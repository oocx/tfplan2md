# Issue: JSON Parsing Error When Rendering Azure Storage and Role Assignment Resources

## Problem Description

tfplan2md v1.16.0 and v1.16.1 fail to render Terraform plans containing Azure resources (specifically `azurerm_storage_container` and `azurerm_role_assignment`) with the error:

```
Unexpected error: JsonElementHasWrongType, Object, Array
```

The same plan renders successfully in v1.15.1.

## Steps to Reproduce

1. Create a Terraform plan with:
   - `azurerm_storage_container` resource (container for existing storage account)
   - `azurerm_role_assignment` resource
2. Run `tfplan2md` v1.16.0 or v1.16.1 on the plan
3. Observe the error: `Unexpected error: JsonElementHasWrongType, Object, Array`

## Expected Behavior

The plan should render successfully with output showing:
- Role assignment with scope, principal_id, and role_definition_name
- Storage container with container_access_type, encryption_scope_override_enabled, name, and storage_account_id

## Actual Behavior

The application crashes with:
```
Unexpected error: JsonElementHasWrongType, Object, Array
```

This error is caught by the generic exception handler in `ProgramEntry.cs` line 76, indicating an unhandled `System.Text.Json` exception.

## Root Cause Analysis

### Affected Components

**Primary Suspect:**
- File: `src/Oocx.TfPlan2Md/Parsing/ConfigurationReferenceResolver.cs#L126-L132`
- Component: Configuration Reference Matching (new in v1.16.0)

**Related Components:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L666-L672`
- Component: Inline Child Entry Extraction (new in v1.16.0)

### What's Broken

The error "JsonElementHasWrongType, Object, Array" is thrown by `System.Text.Json` when code attempts to call `.EnumerateArray()` on a `JsonElement` with `ValueKind == JsonValueKind.Object`.

**Primary Root Cause:**

In `ConfigurationReferenceResolver.cs`, the `AddResourceReferences` method processes the Terraform configuration block to build a reference index. At lines 126-132:

```csharp
if (!expressionProperty.Value.TryGetProperty("references", out var referencesElement) || referencesElement.ValueKind != JsonValueKind.Array)
{
    continue;
}

var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var referenceElement in referencesElement.EnumerateArray())
```

The code checks if `references` is an Array and continues (skips) if it's not. However, in some Terraform configurations, the `references` field in the `expressions` block may be structured as:
- An **Array** of reference strings (expected by current code)
- An **Object** with reference data (unexpected structure causing the crash)

**Terraform Configuration Structure Variation:**

Terraform's JSON plan format may represent `references` differently depending on:
- Terraform version
- Provider version  
- Resource type
- Whether the reference is simple (string) or complex (with metadata)

When `references` is an Object instead of an Array, the check on line 126 should detect this and `continue`, but the error suggests that either:
1. The check is not being executed (unlikely - code flow is straightforward)
2. The `ValueKind` check is passing when it shouldn't (more likely a JSON structure issue)
3. There's another code path calling `EnumerateArray()` on the configuration data

**Secondary Suspect:**

The `ExtractInlineEntries` method in `ReportModelBuilder.ParentChildMerging.cs` (line 666) also checks for Array types:

```csharp
if (!element.TryGetProperty(attributeName, out var property) || property.ValueKind != JsonValueKind.Array)
{
    return [];
}
```

This could fail if an inline attribute (like `members` for Azure AD groups) is an Object instead of an Array in the state JSON.

### Why It Happened

The parent-child resource grouping feature (feature 068) introduced in v1.16.0 added two new JSON processing paths:

1. **Configuration Reference Matching** (commit 566971c) - Parses the `configuration` block from Terraform plans to match child resources to parents by analyzing expression references
2. **Inline Child Entry Extraction** - Parses inline attributes like `members` arrays to render them as child tables

These new features make assumptions about JSON structure that may not hold for all Terraform providers, versions, or resource configurations. The code includes defensive checks but may not handle all edge cases where the Terraform JSON schema varies from expectations.

**Version Context:**
- v1.15.1: No configuration parsing or parent-child merging
- v1.16.0: Added configuration reference resolver and parent-child merging
- v1.16.1: Bug fixes for parent-child member counts (but not the JSON parsing issue)

The regression occurred because v1.16.0 introduced new code paths that process the Terraform configuration JSON, and these paths were not tested against plans where `references` or inline attributes have unexpected JSON types.

## Suggested Fix Approach

### Option 1: Enhanced Defensive Checks (Recommended)

Add explicit type validation before calling `.EnumerateArray()`:

**In `ConfigurationReferenceResolver.cs` (line 126-132):**
```csharp
if (!expressionProperty.Value.TryGetProperty("references", out var referencesElement))
{
    continue;
}

// Explicitly check and skip if not an Array
if (referencesElement.ValueKind != JsonValueKind.Array)
{
    continue;
}

var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var referenceElement in referencesElement.EnumerateArray())
{
    // existing logic
}
```

**In `ReportModelBuilder.ParentChildMerging.cs` (line 666-672):**
```csharp
if (!element.TryGetProperty(attributeName, out var property))
{
    return [];
}

// Explicitly check and return empty if not an Array
if (property.ValueKind != JsonValueKind.Array)
{
    return [];
}

var results = new List<InlineChildEntry>();
foreach (var item in property.EnumerateArray())
{
    // existing logic
}
```

### Option 2: Try-Catch with Logging

Wrap `EnumerateArray()` calls in try-catch blocks and log the unexpected structure for debugging:

```csharp
try
{
    foreach (var referenceElement in referencesElement.EnumerateArray())
    {
        // existing logic
    }
}
catch (InvalidOperationException)
{
    // Log: Unexpected JSON structure - references is not an Array
    // Continue processing without this reference data
    continue;
}
```

### Option 3: Test Case First

Create a minimal test plan JSON with `references` as an Object to reproduce the issue, then implement the fix and verify it resolves the problem.

### Recommended Approach

1. **Immediate Fix**: Add explicit `ValueKind` checks with early returns before ALL `.EnumerateArray()` calls (Option 1)
2. **Root Cause Investigation**: Request the failing Terraform plan JSON from the user to identify the exact structure causing the issue
3. **Test Coverage**: Add unit tests covering:
   - `references` as Object instead of Array
   - `references` as String instead of Array
   - `references` as null
   - Inline attributes as Object instead of Array
4. **Validation**: Test the fix against the user's original failing plan

## Related Tests

Tests that should pass after the fix:

- [ ] `ConfigurationReferenceResolverTests` - Add test case for `references` as Object
- [ ] `ConfigurationReferenceResolverTests` - Add test case for missing `references` property
- [ ] `ReportModelBuilderParentChildTests` - Add test case for inline attribute as Object
- [ ] `ReportModelBuilderParentChildTests` - Add test case for inline attribute as null
- [ ] Integration test with the user's actual failing plan (requires user-provided test data)

## Additional Context

**Related Commits:**
- [566971c](https://github.com/oocx/tfplan2md/commit/566971c2223fbd7adfd409b6ba20b441ba4a8b55) - add configuration reference matching
- [d32e82c](https://github.com/oocx/tfplan2md/commit/d32e82c2a70eb7fba26faebf0535be4f4bec6aea) - add child resource rendering pipeline
- [15bae7f](https://github.com/oocx/tfplan2md/commit/15bae7f00ce314cfe3478666dc367e810339df96) - add parent-child relationship registry

**Related Features:**
- [docs/features/068-parent-child-resource-grouping/](../../features/068-parent-child-resource-grouping/) - Parent-child resource grouping

**Versions Affected:**
- v1.16.0 ✗ (broken)
- v1.16.1 ✗ (broken)
- v1.15.1 ✓ (works)

**Terraform Resources Involved:**
- `azurerm_storage_container`
- `azurerm_role_assignment`
- Potentially any resource with complex `references` in configuration expressions

**User Impact:**
- **Severity**: HIGH - Complete failure to render certain Terraform plans
- **Workaround**: Downgrade to v1.15.1
- **Affected Users**: Anyone using v1.16.x with Azure resources that have Object-typed `references` in their configuration expressions
