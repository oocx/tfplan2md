# Issue: Totals Count Mismatch in Summary Table

**Status:** ✅ Resolved

## Problem Description

The "Total" row in the summary table showed an incorrect count that didn't match the sum of the individual action rows above it.

### Example from comprehensive-demo.md

```markdown
| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 12 | ... |
| 🔄 Change | 5 | ... |
| ♻️ Replace | 2 | ... |
| ❌ Destroy | 3 | ... |
| **Total** | **42** | |
```

Expected total: 12 + 5 + 2 + 3 = **22**  
Actual total: **42** ❌

## Steps to Reproduce

1. Generate a report from `examples/comprehensive-demo/plan.json`
2. View the summary table in the generated markdown
3. Compare the Total count (42) to the sum of individual rows (22)

## Expected Behavior

The Total should equal the sum of Add + Change + Replace + Destroy actions: **22**

## Actual Behavior

The Total shows **42**, which is 20 more than expected.

## Root Cause Analysis

### Affected Components

- File: [src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs](../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs#L149)
- Method: `ReportModelBuilder.Build`

### What's Broken

On line 149, the Total is calculated as:

```csharp
Total = allChanges.Count
```

This includes **all** resource changes, including "no-op" changes (resources with only null actions in Terraform).

Investigation shows the comprehensive demo plan has:
- 12 create actions
- 5 update actions
- 5 delete actions
- 20 null actions (mapped to "no-op")
- **Total: 42** ✓ (matches the bug)

### Why It Happened

The summary table template ([default.sbn](../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn)) only displays 4 action rows:
- ➕ Add (create)
- 🔄 Change (update)
- ♻️ Replace (create+delete)
- ❌ Destroy (delete)

It doesn't display a row for "no-op" changes, so the Total should exclude those changes.

## Implemented Fix

Updated [ReportModel.cs](../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs#L120-L137) to calculate Total from displayed action counts:

```csharp
var toAdd = BuildActionSummary(allChanges.Where(c => c.Action == "create"));
var toChange = BuildActionSummary(allChanges.Where(c => c.Action == "update"));
var toDestroy = BuildActionSummary(allChanges.Where(c => c.Action == "delete"));
var toReplace = BuildActionSummary(allChanges.Where(c => c.Action == "replace"));
var noOp = BuildActionSummary(allChanges.Where(c => c.Action == "no-op"));

var summary = new SummaryModel
{
    ToAdd = toAdd,
    ToChange = toChange,
    ToDestroy = toDestroy,
    ToReplace = toReplace,
    NoOp = noOp,
    Total = toAdd.Count + toChange.Count + toDestroy.Count + toReplace.Count
};
```

This ensures the Total matches what's visible in the summary table.

## Test Coverage

- ✅ New test: `Build_SummaryTotal_ExcludesNoOpActions` - Verifies Total excludes no-op changes ([ReportModelBuilderNoOpTests.cs](../../tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ReportModelBuilderNoOpTests.cs))
- ✅ Updated existing test expectations for no-op plans
- ✅ Snapshot tests updated - comprehensive-demo, summary-template, firewall-rules
- ✅ All 261 tests passing

## Additional Context

- The `summary.NoOp` property exists but is not displayed in the default template
- No-op changes are also excluded from the "Resource Changes" section by default (they're in `allChanges` but not `displayChanges`)
- This is consistent with the principle that no-op changes should not be counted as "changes" in the summary
