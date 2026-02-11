# Issue Analysis: Parent-Child Summary Count Mismatch

**Issue Number:** 069  
**Type:** Bug  
**Severity:** Minor (visual inconsistency)  
**Related Feature:** #068 Parent-Child Resource Grouping

## Problem Description

The summary line for parent-child resources shows incorrect member counts in Azure AD groups:

### Issue 1: Zero Counts When Should Be Non-Zero
```
0 👤 0 👥 0 💻 | ➕ 1 members | ❌ 1 members
```
Icon counts show `0` but should reflect actual member types.

### Issue 2: Count Mismatch Between Summary and Table
```
🔄 azuread_group mixed_engineering | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```
Table shows 3 member rows, but icon counts only show 2.

## Root Cause Analysis

### Timing Issue
The problem stems from when summaries are built:

1. **Summary Built Early**: `BuildGroupSummaryHtml()` is called during `BuildResourceChangeModel()` (line 24 in ReportModelBuilder.Build.cs)
2. **Before Merging**: This happens BEFORE `MergeParentChildRelationships()` runs
3. **Only Inline Members Counted**: The summary builder only sees members from the JSON `members` attribute
4. **Separate Members Missed**: Child `azuread_group_member` resources haven't been merged yet

### Code Flow

```
BuildResourceChangeModel()
  └─> BuildGroupSummaryHtml()  ← Counts only inline members
        └─> Loops over state["members"] array
        └─> Builds icon counts (👤 👥 💻 ❓)
        └─> Returns summary HTML

MergeParentChildRelationships()  ← Runs AFTER summaries built
  └─> Groups child resources under parent
  └─> UpdateParentSummaryWithChildCounts()
        └─> Appends action counts (➕ ❌) only
        └─> Does NOT update icon counts
```

### Affected Code

- `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs` (lines 24-103)
  - Only counts members from `JsonStateReader.GetStringArray(state, "members")`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs` (lines 395-416)
  - Appends child counts but doesn't rebuild icon counts

## Solution Approach

**Option 1: Rebuild Summary After Merging** (Recommended)

1. Add `MemberId` property to `ChildResourceRow` to store member IDs
2. Extract member IDs during parent-child merging
3. Create interface-based rebuilder pattern to avoid layer violations:
   - `IParentSummaryRebuilder` interface in MarkdownGeneration layer
   - Azure AD module implements the interface
   - Registry pattern for provider modules to register rebuilders
4. After `UpdateParentSummaryWithChildCounts()`, call rebuilders
5. Rebuilder counts all members from `ChildResourceGroups` and resolves types

**Benefits:**
- Accurate counts for inline + separate members
- Clean separation of concerns
- No architectural violations
- Works for all scenarios (inline-only, separate-only, mixed)

## Test Scenarios

1. **Inline Only**: Group with `members` attribute, no separate children
2. **Separate Only**: Group without `members`, with `azuread_group_member` children
3. **Mixed**: Group with both inline `members` and separate children
4. **Zero Members**: Group with no members
5. **Count Verification**: Icon counts match total member count
6. **Type Resolution**: Correct icons for known principal types

## Expected Fix

**Before:**
```
0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```

**After:**
```
0 👤 0 👥 0 💻 4 ❓ | ➕ 2 members
```
(Icon counts now include all 4 members: 2 from inline + 2 from separate)
