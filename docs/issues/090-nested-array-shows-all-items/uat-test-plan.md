# UAT Test Plan: Nested Array Rendering Fix (Issue #090)

## Objective
Validate that the bug fix correctly filters nested array items to show only changed items instead of all items in markdown reports.

## Bug Fix Summary
- **Issue:** Nested array changes showed ALL items instead of only changed items
- **Fix:** Modified `ExtractArrayItems` to filter out unchanged array items  
- **Impact:** Cleaner reports for resources with large nested structures

## Test Scenarios

### Scenario 1: Single Changed Array Item
**Data:** 6-item array with only item [4] changed
**Expected:** Only item [4] appears in the rendered markdown
**Validation:** Verify items [0], [1], [2], [3], [5] do NOT appear

### Scenario 2: Multiple Changed Array Items  
**Data:** 6-item array with items [1] and [4] changed
**Expected:** Only items [1] and [4] appear in the rendered markdown
**Validation:** Verify items [0], [2], [3], [5] do NOT appear

### Scenario 3: All Items Changed
**Data:** 6-item array with all 6 items changed
**Expected:** All 6 items appear in the rendered markdown
**Validation:** Verify all items [0-5] appear

## UAT Artifacts

**Feature-Specific Report:**
- Path: `docs/issues/090-nested-array-shows-all-items/uat-plan.md`
- Content: Markdown demonstrating the three test scenarios above

**Comprehensive Demo:**
- Path: `artifacts/comprehensive-demo-simple-diff.md` (GitHub)
- Path: `artifacts/comprehensive-demo.md` (Azure DevOps)
- Content: Full regression test to ensure no side effects

## Validation Instructions

For each test scenario in the UAT report:

1. **Check Array Item Filtering:**
   - Locate the `policyRule.if.allOf` array section
   - Count the number of items shown in the markdown table
   - Verify ONLY changed items appear (check Index column for `| [N] |` patterns)
   
2. **Verify Correct Items:**
   - Scenario 1: Should show `| [4] |` only
   - Scenario 2: Should show `| [1] |` and `| [4] |` only  
   - Scenario 3: Should show `| [0] |`, `| [1] |`, `| [2] |`, `| [3] |`, `| [4] |`, `| [5] |`

3. **Check Markdown Rendering:**
   - Tables render correctly with proper alignment
   - No broken formatting or escaped characters
   - Code blocks and property paths display correctly

## Success Criteria

- ✅ Scenario 1: Only 1 item rendered (not 6)
- ✅ Scenario 2: Only 2 items rendered (not 6)
- ✅ Scenario 3: All 6 items rendered
- ✅ Comprehensive demo renders without errors
- ✅ No regression in other markdown features
