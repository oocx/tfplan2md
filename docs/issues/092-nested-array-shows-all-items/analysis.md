# Issue: Nested Array Changes Show All Items Instead of Only Changed Items

## Problem Description

When an array element is added to a nested structure (e.g., adding `in[3]` with value `8` to `body.properties.policyRule.if.allOf[4].in`), the markdown report shows the ENTIRE nested array structure (all Items [0-5]) instead of just the modified array element (Item [4]).

This creates excessive output in the markdown report, showing unchanged array items alongside the single changed item, making it harder to identify what actually changed.

## Steps to Reproduce

1. Have an Azure Policy Definition resource (`azapi_resource.policy_definitions`) with a nested array structure like `body.properties.policyRule.if.allOf`
2. Modify ONLY one array element (e.g., `allOf[4]`) by adding a nested property (e.g., `in[3]` with value `8`)
3. Generate markdown report with tfplan2md
4. Observe that the report displays:
   - Change summary correctly shows "1🔧 body.properties.policyRule.if.allOf[4].in[3]"
   - But the detailed change section shows tables for Items [0] through [5]
   - Only Item [4] actually contains a change, the rest are unchanged

## Expected Behavior

The markdown report should show **ONLY** the changed array item (Item [4]) in the detailed change section, since:
- The change summary correctly identifies a single change at `allOf[4].in[3]`
- Items [0], [1], [2], [3], and [5] are unchanged and should not be displayed
- Only Item [4] should be shown with its before/after comparison

## Actual Behavior

The markdown report shows ALL array items (Items [0-5]) in the detailed change section, including:
- Item [0] - unchanged (equals/field for type)
- Item [1] - unchanged (equals/field for sku.tier)
- Item [2] - unchanged (equals/field for sku.family)
- Item [3] - unchanged (field/like for sku.name)
- Item [4] - **CHANGED** (field plus in[0-3] array with new in[3]=8 added)
- Item [5] - unchanged (equals/field for requestedBackupStorageRedundancy)

This clutters the report with irrelevant unchanged items.

## Root Cause Analysis

### Affected Components

**Primary File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Rendering.Update.cs`

**Key Function:** `SelectUpdateGroupsAndMainProps` (lines 155-195)

### What's Broken

The `SelectUpdateGroupsAndMainProps` function uses an **all-or-nothing approach** when deciding which array groups to render:

**Current Logic (lines 163-176):**
```csharp
foreach (var group in allGroups)
{
    var hasChange = group.MemberIndexes.Any(changedIndexes.Contains);
    if (!hasChange)
    {
        continue;  // Skip group if no changes
    }
    
    groupsToRender.Add(group);  // ← Include ENTIRE group if ANY property changed
    foreach (var index in group.MemberIndexes)
    {
        groupedIndexes.Add(index);  // Mark all members as rendered
    }
}
```

**The Problem:**
- Line 165 checks if ANY property in the group has a change
- If yes, line 171 adds the ENTIRE group to render
- Lines 172-175 mark ALL members as grouped (to be rendered)
- This means if `allOf[4].in[3]` changes, ALL of `allOf[0]` through `allOf[5]` are rendered

**Why It Happens:**
The grouping logic treats the entire `allOf` array as a single group. When extracting array items in `ExtractArrayItems` (in `AzApi.Rendering.Array.cs`, lines 57-120), it processes all items in the group, not just the changed ones.

Then, `RenderUpdateArrayPerItemTables` (in `AzApi.Rendering.Update.cs`, lines 407-425) renders every item in the extracted list without filtering for changes.

### Why It Happened

This behavior is **intentional based on the feature specification** (docs/features/034-azapi-attribute-grouping/specification.md, lines 170-184):

> **Question:** How should grouped attributes be displayed in update operations?
> 
> **Recommendation:** Start with option 1 (show full groups if any item changed) for MVP. This maintains context and is simpler to implement.

However, this design decision creates **poor user experience** in practice:
- For large arrays with many items, showing all unchanged items creates clutter
- The change summary correctly identifies the specific changed item, but the detailed view contradicts this by showing everything
- Reviewers must scan through many unchanged items to find the actual change

## Suggested Fix Approach

### Option 1: Filter Array Items to Show Only Changed Items (Recommended)

**Change in `SelectUpdateGroupsAndMainProps` or `ExtractArrayItems`:**

Instead of including ALL items when rendering an array group, filter to include only:
1. Items that have at least one changed property
2. Optionally: Include immediate neighbors (±1 index) for context

**Implementation:**
1. In `ExtractArrayItems`, add a parameter to filter by changed indexes
2. Pass the `changedIndexes` set to `ExtractArrayItems`
3. When building `byIndex` dictionary, only include items where at least one property index is in `changedIndexes`
4. This ensures only changed array items are rendered

**Pros:**
- Cleaner output - shows only what changed
- Aligns with user expectations based on change summary
- Reduces markdown file size for large arrays
- Easier to review PRs with complex nested structures

**Cons:**
- Loses some context (don't see full array structure)
- May need to handle edge cases (e.g., array index changes)

### Option 2: Add Configuration Flag for "Show Full Groups"

Add a configuration option to control whether full groups or only changed items are shown:
- Default: `show_only_changed_array_items = true`
- Allow users to opt into full-group display if desired

**Pros:**
- Flexible - users can choose based on their needs
- Backward compatible (can default to new behavior)

**Cons:**
- Adds complexity
- Configuration management overhead

### Option 3: Use Expandable Details Section for Unchanged Items

Show changed items inline, but put unchanged items in a `<details>` block:

```markdown
###### `allOf` Array

**Item [4]** (Changed)
| Property | Before | After |
...

<details>
<summary>Unchanged items (5)</summary>

**Item [0]**
...

</details>
```

**Pros:**
- Preserves full context while keeping focus on changes
- Clean default view with optional expansion

**Cons:**
- More complex rendering logic
- Not all markdown viewers support `<details>` well in PR comments

## Recommendation

**Implement Option 1** (filter to show only changed array items) as it:
1. Directly addresses the user complaint
2. Aligns with the change summary behavior
3. Improves readability for the most common case
4. Is relatively simple to implement

**Follow-up consideration:** After implementing Option 1, gather user feedback. If users request seeing full array context, consider implementing Option 3 as an enhancement.

## Related Tests

After implementing the fix, verify:

- [ ] Test case: Array with 6 items, only item [4] changed → renders only item [4]
- [ ] Test case: Array with multiple changed items → renders all changed items
- [ ] Test case: Nested arrays (e.g., `allOf[4].in[3]`) → renders correct nesting
- [ ] Test case: Empty array → handles gracefully
- [ ] Test case: All items changed → renders all items
- [ ] Existing AzApi update rendering tests still pass
- [ ] Integration test with real Azure Policy Definition resource

**Specific test files to review/update:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/ScribanHelpersAzApiUpdateRenderingTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/ScribanHelpersAzApiGroupingTests.cs`

## Additional Context

### Related Feature Specification

- Feature: docs/features/034-azapi-attribute-grouping/specification.md
- Section: "Update Operation Handling" (lines 170-184)
- This issue represents feedback on the MVP approach taken in that feature

### Related Code Files

1. **AzApi.Rendering.Update.cs** - Main update rendering logic
   - `SelectUpdateGroupsAndMainProps` (lines 155-195) - Group selection logic
   - `RenderUpdateArrayPerItemTables` (lines 407-425) - Array item rendering

2. **AzApi.Rendering.Array.cs** - Array extraction logic
   - `ExtractArrayItems` (lines 57-120) - Extracts array items from grouped properties
   - `TryParseArrayItemPath` (lines 130-177) - Parses array paths

3. **AzApi.Grouping.cs** - Grouping detection logic
   - `IdentifyGroupedPrefixes` (lines 104-121) - Identifies array groups
   - `BuildArrayGroupCandidates` (lines 186-212) - Builds array grouping candidates

### Example Resource Path

From the bug report:
- Resource type: `azapi_resource.policy_definitions`
- Resource ID: `Validate-SQL-DB`
- Changed path: `body.properties.policyRule.if.allOf[4].in[3]`
- Change: Added value `8` to the `in` array
- Issue: All `allOf[0-5]` items shown instead of just `allOf[4]`

### Potential Edge Cases to Consider

1. **Array index reordering** - If array items are reordered, showing only "changed" items might be confusing
2. **Nested array changes** - Ensure nested array filtering works correctly
3. **Array with all items changed** - Should show all items (current behavior is correct)
4. **Array with all items deleted** - Should handle gracefully
5. **Empty arrays** - Should not crash or show empty sections
6. **Single-item arrays** - Should work correctly (no context to lose)

## Questions for Developer

1. Should we show any context items (e.g., ±1 array index) or only strictly changed items?
2. Should the filtering apply at the array level (e.g., `allOf[4]`) or at the property level (e.g., `allOf[4].in[3]`)?
   - Current analysis suggests array level (show all of `allOf[4]` if any property within it changed)
3. How should we handle the case where an array item is deleted vs. modified vs. added?
   - All three should probably be shown as "changes"

## Definition of Done

- [ ] Code changes implemented to filter array items to only changed items
- [ ] Unit tests added for new filtering behavior
- [ ] Existing tests updated if needed
- [ ] Integration test with sample Azure Policy Definition resource
- [ ] Manual verification with the original bug report scenario
- [ ] Documentation updated if needed (feature specification notes the change)
