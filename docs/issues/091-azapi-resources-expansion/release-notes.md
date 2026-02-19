# Release Notes: Issue 091 - AzAPI Resources Expansion

## Type
🐛 Bug Fix

## Summary
Fixed resources being always expanded by default in rendered markdown output. Resources (AzApi, azurerm_network_security_group, azurerm_firewall_application_rule_collection, azurerm_firewall_network_rule_collection, azuredevops_variable_group) now only expand by default when they have code analysis warnings, matching the behavior of the default resource template.

## Problem
Several resource types were configured to always expand (`<details open>`) in the rendered markdown output, regardless of whether they had code analysis warnings. This made it difficult to read plans with many resources, especially AzAPI resources with large nested body content.

### Affected Resource Types
- `azapi_resource` (AzAPI provider)
- `azurerm_network_security_group` (AzureRM provider)
- `azurerm_firewall_application_rule_collection` (AzureRM provider)
- `azurerm_firewall_network_rule_collection` (AzureRM provider)
- `azuredevops_variable_group` (Azure DevOps provider)

### Before
All instances of these resource types were rendered with `<details open>`, causing them to be expanded by default regardless of content or warnings:

```html
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
```

This resulted in:
- ❌ Long, cluttered markdown reports with all resource details visible
- ❌ Difficult code review experience, especially for plans with many resources
- ❌ Inconsistent behavior compared to the default resource template
- ❌ Excessive scrolling required to navigate through the report

### After
These resources now follow the same pattern as the default resource template—expanding only when code analysis warnings exist:

```html
<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }} style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
```

This provides:
- ✅ Cleaner, more compact markdown reports
- ✅ Better code review experience—focus on resources with warnings
- ✅ Consistent behavior across all resource types
- ✅ Ability to expand individual resources on demand

## Changes Made

### Source Code
- **`src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`**
  - Changed `<details open>` to `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`
  
- **`src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/network_security_group.sbn`**
  - Changed `<details open>` to `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`
  
- **`src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_application_rule_collection.sbn`**
  - Changed `<details open>` to `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`
  
- **`src/Oocx.TfPlan2Md/Providers/AzureRM/Templates/azurerm/firewall_network_rule_collection.sbn`**
  - Changed `<details open>` to `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`
  
- **`src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn`**
  - Changed `<details open>` to `<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }}`

### Test Updates
- Updated 28 snapshot test baselines to reflect resources now being collapsed by default
- All affected snapshots regenerated with `SNAPSHOT_UPDATE_OK` marker

## Impact

### Affected Scenarios
All terraform plans containing the following resource types will now render with collapsed details sections by default:
- **AzAPI resources**: All `azapi_resource` instances
- **Network security**: `azurerm_network_security_group` resources
- **Firewall rules**: `azurerm_firewall_application_rule_collection` and `azurerm_firewall_network_rule_collection` resources
- **Azure DevOps**: `azuredevops_variable_group` resources

### User-Facing Changes
- ✅ **Improved readability**: Resources are collapsed by default, reducing visual clutter
- ✅ **Better code review workflow**: Reviewers can scan summaries and expand only resources of interest
- ✅ **Automatic expansion for warnings**: Resources with code analysis findings still expand automatically to draw attention
- ✅ **Consistent behavior**: All resource types now follow the same expansion logic
- ✅ **No information loss**: All resource details remain available via manual expansion

### Compatibility
- **Non-breaking change**: Existing reports will render more compactly but contain the same information
- **Behavioral change**: Resources will be collapsed by default instead of expanded
- **Code analysis warnings preserved**: Resources with warnings still expand automatically
- **Snapshot tests updated**: 28 snapshot baselines regenerated to reflect new default state

## Technical Details

The fix aligns these five resource templates with the standard pattern already implemented in other resource templates (e.g., `azuread/invitation.sbn`, `azuread/group_without_members.sbn`). The `<details>` element now conditionally includes the `open` attribute based on whether `change.code_analysis_findings.size > 0`.

This ensures that:
1. **Default state**: Resources are collapsed, showing only the summary line
2. **Warning state**: Resources with code analysis warnings expand automatically to highlight issues
3. **Manual expansion**: Users can still expand any resource by clicking the summary line

The change is purely presentational—no data is added or removed from the markdown output.

## Related Features
- Feature 040: AzAPI Resource Template (original implementation of azapi_resource template)
- Feature 034: AzAPI Attribute Grouping (AzAPI body rendering)
- Default resource template pattern (established the conditional expansion pattern)

## Verification

### Test Coverage
- ✅ All unit tests pass
- ✅ All snapshot tests pass with regenerated baselines (28 snapshots updated)
- ✅ Manual verification confirms resources collapse by default and expand when warnings present

### Examples

**AzAPI Resource (No Warnings):**
```html
<details style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>➕ azapi_resource.example</summary>
...
</details>
```

**Network Security Group (With Warning):**
```html
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>🔄 azurerm_network_security_group.app_nsg</summary>
...
<!-- Code analysis warning displayed -->
</details>
```

## Documentation Updates
- This release notes document created at `docs/issues/091-azapi-resources-expansion/release-notes.md`
- No changes required to README.md (behavior is internal to rendering)
- No changes required to CHANGELOG.md (auto-generated by Versionize)
