# Issue: Parent-Child Resource Summary Shows Incorrect Member Counts

## Problem Description

The summary line for Azure AD group parent-child resources shows incorrect member counts in two scenarios:

### Issue 1: Zero Count When Should Be Non-Zero
```
0 👤 0 👥 0 💻 | ➕ 1 members | ❌ 1 members
```
Icon counts show `0` for all member types, but should show `1` for the appropriate type.

### Issue 2: Count Mismatch Between Summary and Table
```
🔄 azuread_group mixed_engineering — 👥 Engineering Mixed | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```
The table shows 3 member rows, but the icon summary only shows 2 unknown members (2 ❓).

## Steps to Reproduce

1. Create a Terraform plan with Azure AD groups that have both:
   - Inline `members` attribute (array of member IDs)
   - Separate `azuread_group_member` child resources
2. Convert the plan to markdown using tfplan2md
3. Observe the summary line for the group resource

**Example scenario:**
```terraform
resource "azuread_group" "engineering" {
  display_name = "Engineering Mixed"
  members = ["user-id-1", "user-id-2"]  # 2 inline members
}

resource "azuread_group_member" "extra" {
  group_object_id = azuread_group.engineering.id
  member_object_id = "user-id-3"  # 1 separate member
}
```

**Expected:** Summary shows 3 members with correct icon counts  
**Actual:** Summary shows 0 users / 0 groups / 0 SPs / 2 unknown, mismatched with 3 table rows

## Expected Behavior

Summary counts should:
1. Match the actual number of members shown in the table
2. Include both inline members (from parent `members` attribute) and separate members (from `azuread_group_member` child resources)
3. Show correct icon counts based on resolved principal types (👤 users, 👥 groups, 💻 service principals, ❓ unknown)

## Actual Behavior

- Icon counts (👤 👥 💻 ❓) only reflect inline members from the `members` attribute
- Separate `azuread_group_member` child resources are not counted in icon totals
- Action counts (➕ ❌) in the suffix correctly include separate members
- This causes a mismatch: icon counts don't match the number of rows in the member table

## Root Cause Analysis

### Affected Components

- File: `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs#L24-103`
  - Component: `BuildGroupSummaryHtml()` method
  - Problem: Only counts members from inline `members` attribute

- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs#L21-32`
  - Component: `Build()` method
  - Problem: Builds summaries before parent-child merging

- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L21-100`
  - Component: `MergeParentChildRelationships()` method
  - Problem: Runs after summaries are already built

- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs#L395-416`
  - Component: `UpdateParentSummaryWithChildCounts()` method
  - Problem: Only appends action counts, doesn't rebuild icon counts

### What's Broken

**Timing Issue:** Summaries are built before parent-child merging completes.

The code flow is:

```
1. Build() method
   ├─> BuildResourceChangeModel() for each resource (line 27)
   │   └─> Creates ResourceChangeModel with SummaryHtml
   │       └─> AzureAdSummaryBuilder.BuildGroupSummaryHtml() called here
   │           └─> Counts only inline members from state["members"]
   │           └─> Builds icon counts: "0 👤 0 👥 0 💻 2 ❓"
   │
   └─> MergeParentChildRelationships() (line 32) ← HAPPENS AFTER SUMMARIES BUILT
       ├─> BuildInlineRows() - extracts inline members
       ├─> BuildSeparateRows() - matches azuread_group_member children
       ├─> Groups both types into ChildResourceGroups
       └─> UpdateParentSummaryWithChildCounts()
           └─> Appends "| ➕ 2 members" to existing summary
           └─> Does NOT rebuild icon counts!
```

**Result:** 
- Icon counts (👤 👥 💻 ❓) reflect only inline members seen at build time
- Separate `azuread_group_member` resources are merged later
- Action suffix (➕ ❌) is correct because it's added after merging
- Icon counts remain stale and don't match the final member table

### Why It Happened

Feature #068 (Parent-Child Resource Grouping) was merged without updating the Azure AD summary builder to account for the new parent-child merging workflow. The summary builder was designed when all members were inline; it didn't anticipate separate child resources being merged later.

## Previous Fix Attempt: PR #453

PR #453 attempted to fix this issue but was **closed without merging**. Here's what it tried:

### PR #453 Approach

1. **Architecture:** Interface-based rebuilder pattern
   - Created `IParentSummaryRebuilder` interface in MarkdownGeneration layer
   - Created `ParentSummaryRebuilderRegistry` registry class
   - Implemented `AzureAdGroupSummaryRebuilder` in Azure AD provider
   - Extended `IProviderModule` with `RegisterParentSummaryRebuilders()` method

2. **Data Flow:**
   - Extended `ChildResourceRow` with `MemberId` property
   - Stored member IDs during parent-child merging
   - After `UpdateParentSummaryWithChildCounts()`, called rebuilders
   - Rebuilders counted members from `ChildResourceGroups` and resolved types

3. **Changes Made:**
   - 13 files changed
   - 628 additions, 4 deletions
   - Created issue analysis in `docs/issues/069-parent-child-summary-count-mismatch/`
   - Significant architectural additions

### Why PR #453 Was Likely Closed

Based on the PR description and the work protocol document in the diff, PR #453 appears to have been closed because:

1. **Over-engineered Solution:** Added significant architectural complexity (new interfaces, registry pattern, module extensions) for what might be a simpler problem

2. **Snapshot Updates Required:** The PR noted "Snapshots will be updated in follow-up (requires `SNAPSHOT_UPDATE_OK` token)" - tests may have been failing

3. **Architectural Concerns:** Introduced new dependencies and patterns that may have been unnecessary:
   - New interface layer between MarkdownGeneration and Providers
   - Registry pattern for what's currently only one use case (Azure AD groups)
   - Extended the provider module interface

4. **Alternative Approach Possible:** The problem might be solvable with a simpler approach that doesn't require architectural changes

## Suggested Fix Approach

### Option 1: Simple Post-Merge Update (Recommended)

**Simpler approach** that doesn't require architectural changes:

1. **When:** After `MergeParentChildRelationships()` completes
2. **Where:** In `ReportModelBuilder.Build()` method
3. **How:**
   - After line 32 (`MergeParentChildRelationships(allChanges)`), add a second pass
   - For Azure AD group resources with `ChildResourceGroups`:
     - Count all members from `ChildResourceGroups[0].Rows` (the members group)
     - Resolve member types using `PrincipalMapper` (already available)
     - Rebuild the icon count portion of the summary
     - Replace the old icon counts in `SummaryHtml` with new counts

4. **Benefits:**
   - No new interfaces or architectural patterns
   - Uses existing `PrincipalMapper` service
   - Localized change in one method
   - Clear separation: build initial summary, merge children, update summary
   - Minimal code changes

5. **Implementation:**
```csharp
// After MergeParentChildRelationships(allChanges);
UpdateAzureAdGroupSummaries(allChanges);

private void UpdateAzureAdGroupSummaries(List<ResourceChangeModel> allChanges)
{
    foreach (var change in allChanges)
    {
        if (!string.Equals(change.Type, "azuread_group", StringComparison.OrdinalIgnoreCase))
            continue;
            
        if (change.ChildResourceGroups.Count == 0)
            continue;
            
        var membersGroup = change.ChildResourceGroups
            .FirstOrDefault(g => g.Label.Equals("members", StringComparison.OrdinalIgnoreCase));
            
        if (membersGroup == null)
            continue;
            
        // Extract member IDs from rows, count by type using PrincipalMapper
        // Rebuild icon count string
        // Replace old counts in SummaryHtml
    }
}
```

### Option 2: Summary Rebuilder Pattern (PR #453 Approach)

If we want the more extensible solution from PR #453:

**Pros:**
- More extensible for future parent-child scenarios
- Clean separation via interfaces
- Provider modules control their own summary rebuilding

**Cons:**
- Much more complex (13 files, 628 lines)
- Adds architectural patterns for single use case
- May be premature abstraction
- Requires more testing and review

**Recommendation:** Start with Option 1 (simple approach). If we add more parent-child scenarios that need summary rebuilding, refactor to Option 2.

## Related Tests

Tests that should pass after the fix:

- [ ] `AzureAdGroupTests` - Group with inline members only
- [ ] `ParentChildMergingTests` - Group with separate members only
- [ ] `ParentChildMergingTests` - Group with both inline and separate members (mixed)
- [ ] Snapshot tests for Azure AD group summaries
- [ ] Integration test with `examples/azuread-resources-demo.json`

Verify:
1. Icon counts match member table row counts
2. Member types are correctly resolved and counted
3. Action counts (➕ ❌) remain correct
4. Summary format matches existing pattern

## Additional Context

### Related Issues and PRs

- **Feature #068:** Parent-Child Resource Grouping (introduced the merging workflow)
- **Issue #447:** This bug report
- **PR #453:** First fix attempt (closed) - branch `copilot/fix-parent-child-summary-counts`
- **PR #456:** Current fix attempt (in progress) - branch `copilot/fix-summary-member-counts`
- **GitHub UAT PR #67:** UAT for feature #068
- **Azure DevOps UAT PR #72:** UAT for feature #068

### Links to Code

- Summary builder: `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs`
- Parent-child merging: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
- Report building: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`
- Principal mapper: `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`
- Example test data: `examples/azuread-resources-demo.json`

### Severity Assessment

**Severity:** Minor
- Does not block feature #068 from merging
- Visual inconsistency only
- Data is still present (in the member table)
- Action counts are correct
- No functionality broken

**Impact:** 
- User experience: Confusing to see icon counts that don't match table rows
- Data accuracy: All data is present, just summary counts are wrong
- Workaround: Users can look at the member table for accurate counts
