# Issue: False Positive "Already Imported" Warning for Resources with Read Actions

## Problem Description

The application incorrectly displays the "⚠️ Already imported" warning for resources that are actively being imported (not yet applied). Users see this warning even though the import operation has not been executed yet, causing confusion about whether the import block is necessary.

## Steps to Reproduce

1. Create a Terraform import block for an existing resource that will be read from the provider
2. Run `terraform plan` (Terraform reports `actions: ["read"]` for the import operation)
3. Convert the plan to markdown using tfplan2md
4. Observe the "⚠️ Already imported" warning in the Refactoring Summary table

## Expected Behavior

Resources with `actions: ["read"]` and an `importing.id` should show:
- **Status**: ✅ Ready
- **Meaning**: The import will be executed on the next `terraform apply`

## Actual Behavior

Resources with `actions: ["read"]` and an `importing.id` incorrectly show:
- **Status**: ⚠️ Already imported  
- **Meaning**: The import block is unnecessary and can be removed

This is a **false positive** - the warning appears when it shouldn't.

## Root Cause Analysis

### Affected Components

- **File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs#L30`
  - Method: `BuildResourceChangeModel(ResourceChange rc)`
  - Logic that determines `isRefactoringAlreadyApplied`

- **File**: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs#L142-165`
  - Method: `DetermineAction(IReadOnlyList<string> actions)`
  - Missing handling for `"read"` action

### What's Broken

The `DetermineAction` method does not recognize Terraform's `"read"` action:

```csharp
private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Contains(CreateAction) && actions.Contains(DeleteAction))
    {
        return ReplaceAction;
    }

    if (actions.Contains(CreateAction))
    {
        return CreateAction;
    }

    if (actions.Contains(DeleteAction))
    {
        return DeleteAction;
    }

    if (actions.Contains(UpdateAction))
    {
        return UpdateAction;
    }

    return NoOpAction;  // ❌ PROBLEM: Falls through for "read" and any unknown action
}
```

When Terraform reports `actions: ["read"]` for an import:
1. The method doesn't match any of the explicit checks
2. Falls through to `return NoOpAction` 
3. This causes line 30 to evaluate incorrectly:

```csharp
var isRefactoringAlreadyApplied = action == NoOpAction && (importId is not null || movedFromAddress is not null);
```

Since `action` is incorrectly identified as `NoOpAction` and `importId` exists, the condition evaluates to `true`, marking the import as already applied.

### Why It Happened

**Background**: Terraform's import functionality can report different actions depending on the scenario:

| Terraform Actions | Meaning | Should Show "Already Imported"? |
|-------------------|---------|----------------------------------|
| `["create"]` | Resource will be imported and created in state | ❌ No - show "✅ Ready" |
| `["read"]` | Resource will be imported by reading current state | ❌ No - show "✅ Ready" |
| `["no-op"]` | Resource already exists in state; import block unnecessary | ✅ Yes - show "⚠️ Already imported" |
| `["update"]` | Resource will be imported AND updated due to config drift | ❌ No - show "✅ Ready" |

The current implementation only explicitly handles `create`, `delete`, `update`, and `replace`. The `"read"` action, which is valid for imports, falls through to the default `NoOpAction` case, causing the false positive.

**Why "read" action exists**: According to Terraform documentation, when importing a resource using import blocks, Terraform may report a `"read"` action to indicate it will fetch and register the resource's current state from the provider without modifying it. This is distinct from `"no-op"` (which means the resource is already in state and no action is needed).

### Technical Context

- **Related Feature**: [docs/features/057-terraform-import-moved-blocks/specification.md](../../features/057-terraform-import-moved-blocks/specification.md)
- **Related Architecture**: [docs/features/057-terraform-import-moved-blocks/architecture.md](../../features/057-terraform-import-moved-blocks/architecture.md#3-unnecessary-block-detection)

From the architecture document:
> Classification rule (per spec):
> - If a resource has import/move metadata and `actions = ["no-op"]`, then mark it as **Already imported/moved**.

The rule is correctly specified, but the implementation fails to distinguish between true `"no-op"` actions and other actions (like `"read"`) that should not trigger the warning.

## Suggested Fix Approach

### Solution 1: Add Explicit Handling for "read" Action (Recommended)

**Change**: Add a new action constant and explicit handling in `DetermineAction`:

```csharp
private const string ReadAction = "read";

private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Contains(CreateAction) && actions.Contains(DeleteAction))
    {
        return ReplaceAction;
    }

    if (actions.Contains(CreateAction))
    {
        return CreateAction;
    }

    if (actions.Contains(DeleteAction))
    {
        return DeleteAction;
    }

    if (actions.Contains(UpdateAction))
    {
        return UpdateAction;
    }

    if (actions.Contains(ReadAction))
    {
        return ReadAction;  // ✅ Explicitly handle read
    }

    return NoOpAction;
}
```

**Rationale**: 
- Treats `"read"` as a distinct action type, preventing it from falling through to `NoOpAction`
- The existing logic at line 30 remains unchanged but now works correctly:
  - When `action == ReadAction`, `isRefactoringAlreadyApplied` will be `false` ✅
  - When `action == NoOpAction`, `isRefactoringAlreadyApplied` will be `true` ✅

### Solution 2: Strict "no-op" Check (Alternative)

**Change**: Check for explicit `"no-op"` action instead of using it as a fallback:

```csharp
private const string NoOpActionLiteral = "no-op";

private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Contains(CreateAction) && actions.Contains(DeleteAction))
    {
        return ReplaceAction;
    }

    if (actions.Contains(CreateAction))
    {
        return CreateAction;
    }

    if (actions.Contains(DeleteAction))
    {
        return DeleteAction;
    }

    if (actions.Contains(UpdateAction))
    {
        return UpdateAction;
    }

    if (actions.Contains(NoOpActionLiteral))
    {
        return NoOpAction;
    }

    return "unknown";  // Or throw for debugging
}
```

**Rationale**:
- More defensive; only treats explicit `"no-op"` from Terraform as no-op
- Unknown actions don't incorrectly trigger import warnings
- Cons: Requires handling of "unknown" action type throughout the codebase

### Recommended Approach

**Use Solution 1** because:
1. Aligns with Terraform's documented action types
2. Minimal code changes required
3. No risk of breaking existing functionality for known action types
4. Template rendering already uses action symbols/icons that can accommodate read as a distinct type if needed
5. Follows the pattern established for other action types

## Related Tests

Tests that should pass after the fix:

### Unit Tests (to be added)
- [ ] `DetermineAction` returns `"read"` when actions contain `["read"]`
- [ ] `IsRefactoringAlreadyApplied` is `false` when action is `"read"` with `importId`
- [ ] `IsRefactoringAlreadyApplied` is `true` when action is `"no-op"` with `importId`
- [ ] `IsRefactoringAlreadyApplied` is `false` when action is `"create"` with `importId`
- [ ] `IsRefactoringAlreadyApplied` is `false` when action is `"update"` with `importId`

### Integration Tests (existing snapshots may need updates)
- [ ] `tests/Oocx.TfPlan2Md.TUnit/TestData/refactoring-comprehensive.json` - if it contains read actions
- [ ] Resource summary HTML builder tests for refactoring scenarios
- [ ] Refactoring summary table rendering tests

### New Test Data Needed
- A Terraform plan JSON with `actions: ["read"]` and `importing.id` to verify correct "✅ Ready" status

## Additional Context

### Terraform Action Types Reference

Based on Terraform documentation and the web search results:

| Action | Typical Use Case | In tfplan2md |
|--------|------------------|--------------|
| `"create"` | New resource | Mapped to CreateAction |
| `"delete"` | Resource removal | Mapped to DeleteAction |
| `"update"` | In-place modification | Mapped to UpdateAction |
| `"create"` + `"delete"` | Replacement | Mapped to ReplaceAction |
| `"read"` | Data source or import read | ❌ Currently unmapped → falls to NoOpAction |
| `"no-op"` | No changes needed | Mapped to NoOpAction |

### Related Code Locations

- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
  - Line 17-21: Action constants
  - Line 30: `isRefactoringAlreadyApplied` logic
  - Line 142-165: `DetermineAction` method

- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`
  - Line 137-146: Building RefactoringOperationModel (uses `IsRefactoringAlreadyApplied`)

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
  - Line 44: Template that renders the warning
  
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/summary.sbn`
  - Line 23: Template that renders the warning

### Questions for Maintainer (if needed)

1. Should the `"read"` action be represented with a distinct icon/symbol in templates, or should it use the same symbol as `"create"` for imports?
2. Are there any other Terraform action types we should handle explicitly?
3. Should we log a warning when encountering unknown action types for debugging purposes?

## Definition of Done

The fix is complete when:

- [ ] `"read"` action is explicitly recognized in `DetermineAction` method
- [ ] `IsRefactoringAlreadyApplied` is `false` for import operations with `"read"` action
- [ ] Unit tests added to verify correct behavior for all action types with import metadata
- [ ] Integration tests pass (snapshot tests updated if needed)
- [ ] Test data added with `actions: ["read"]` scenario
- [ ] "⚠️ Already imported" warning only appears for true `"no-op"` imports
- [ ] Documentation updated if action symbol mapping changes

## Files to Modify

1. **Primary Fix**:
   - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
     - Add `ReadAction` constant
     - Add `"read"` handling in `DetermineAction` method

2. **Tests**:
   - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderTests.cs` (or create new test file)
     - Add tests for `DetermineAction` with various action combinations
     - Add tests for `IsRefactoringAlreadyApplied` logic

3. **Test Data**:
   - Create new test plan JSON with `actions: ["read"]` scenario
   - Or add to existing `refactoring-comprehensive.json`

4. **Documentation** (optional):
   - Update architecture doc if action handling is documented there
