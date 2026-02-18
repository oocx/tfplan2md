# Issue: No-op parent resources with child changes cause children to disappear from Resource Changes section

## Problem Description

When a Terraform plan contains:
1. A parent resource with action `no-op` (no changes)
2. Child resources with actual changes (update/create/delete)

The child resources are correctly counted in the Summary table, but they do not appear in the Resource Changes section. The Resource Changes section is completely omitted from the report.

## Steps to Reproduce

1. Create a Terraform plan JSON with:
   - An `azurerm_network_security_group` resource with action `["no-op"]`
   - Multiple `azurerm_network_security_rule` resources with action `["update"]` that reference the parent NSG
2. Run tfplan2md on the plan: `tfplan2md plan.json`
3. Observe the output:
   - Summary table shows: "🔄 Change | 2 | 2 azurerm_network_security_rule"
   - Resource Changes section is missing entirely

See test file: `TestData/nsg-with-separate-rule-updates.json`

## Expected Behavior

The Resource Changes section should display:
- The parent `azurerm_network_security_group` resource with its child rules shown in a table
- The child rules should show their attribute changes (description, source_address_prefixes, etc.)

## Actual Behavior

The Resource Changes section is completely omitted from the report, even though the Summary table correctly counts 2 changes.

## Root Cause Analysis

### Affected Components

- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`
  - Lines 48-50: Filter that removes no-op resources from displayChanges
  - Lines 79-94: Module grouping logic

- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
  - Lines 106-109: Logic that removes child resources from allChanges after merging

### What's Broken

The parent-child merging logic and the no-op filtering logic create an unintended interaction:

1. **Summary Calculation** (line 36-40 in Build.cs):
   - Happens BEFORE parent-child merging
   - Correctly counts all resources including the 2 child updates
   - Result: Summary shows "2 to change"

2. **Parent-Child Merging** (line 43 in Build.cs):
   - `MergeParentChildRelationships(allChanges)` is called
   - The parent NSG (no-op) is found in the plan
   - The 2 child NSG rules are matched via `network_security_group_name` == `name`
   - Child rules are added to `removedChildren` set (line 392 in ParentChildMerging.cs)
   - Child rules are removed from `allChanges` list (lines 106-109 in ParentChildMerging.cs)
   - Child rules are added to parent's `ChildResourceGroups` property
   - Result: `allChanges` now contains only the parent NSG (no-op)

3. **Display Filtering** (lines 48-50 in Build.cs):
   ```csharp
   var displayChanges = allChanges
       .Where(c => c.Action != NoOpAction || c.CodeAnalysisFindings.Count > 0 || c.ImportId is not null || c.MovedFromAddress is not null)
       .ToList();
   ```
   - The parent NSG has `Action == NoOpAction`
   - The parent NSG has no code analysis findings, import ID, or moved-from address
   - The filter excludes the parent NSG
   - Result: `displayChanges` is empty

4. **Module Grouping** (lines 79-94 in Build.cs):
   - Groups `displayChanges` by module
   - Since `displayChanges` is empty, `moduleGroups` is empty
   - Result: Template receives empty `module_changes`

5. **Template Rendering** (`default.sbn` line 20):
   ```sbn
   {{ if module_changes.size > 0 }}
   ## Resource Changes
   ...
   {{ end }}
   ```
   - Since `module_changes.size == 0`, the entire Resource Changes section is skipped
   - Result: No Resource Changes section in output

### Why It Happened

The no-op filter was designed to exclude resources with no meaningful changes to prevent clutter and avoid exceeding Scriban's iteration limit. However, it doesn't account for no-op parent resources that have children with actual changes. The children are "hidden" inside the parent's `ChildResourceGroups`, but the parent itself is then filtered out.

## Suggested Fix Approach

Modify the no-op filtering logic to preserve parent resources that have children with changes:

1. **Update the display filter** in `ReportModelBuilder.Build.cs` (lines 48-50):
   - Change the condition to also check if `c.ChildResourceGroups.Count > 0`
   - This ensures no-op parents with child tables are not filtered out

2. **Proposed code change**:
   ```csharp
   var displayChanges = allChanges
       .Where(c => c.Action != NoOpAction 
                   || c.CodeAnalysisFindings.Count > 0 
                   || c.ImportId is not null 
                   || c.MovedFromAddress is not null
                   || c.ChildResourceGroups.Count > 0)  // NEW: Preserve parents with children
       .ToList();
   ```

3. **Verify the fix**:
   - Test with `TestData/nsg-with-separate-rule-updates.json`
   - Verify the Resource Changes section appears
   - Verify the parent NSG shows child rules in a table
   - Verify the child rules show their attribute changes

## Related Tests

Tests that should pass after the fix:

- [ ] Existing parent-child tests in `ReportModelBuilderParentChildTests.cs`
- [ ] Existing no-op tests in `ReportModelBuilderNoOpTests.cs`
- [ ] New test case: No-op parent with children having changes (add to test suite)

## Additional Context

- Related feature: docs/features/068-parent-child-resource-grouping/specification.md
- Parent-child relationship for NSG: `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs` lines 157-179
- Similar patterns affect other parent-child relationships:
  - `azurerm_virtual_network` → `azurerm_subnet`
  - `azurerm_route_table` → `azurerm_route`
  - `azurerm_dns_zone` → `azurerm_dns_*_record`
  - `azuread_group` → `azuread_group_member`
  - `azuredevops_team` → `azuredevops_team_members`
