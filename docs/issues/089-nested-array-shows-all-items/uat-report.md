# UAT Report: Nested Array Filtering Fix (Issue #089)

## Status: 🕐 AWAITING MAINTAINER REVIEW

## Test Execution Date
2026-02-18

## UAT Tester
GitHub Copilot - UAT Tester Agent

## Summary
UAT PRs successfully created on both GitHub and Azure DevOps. Both feature-specific and regression test artifacts have been posted. The rendering is ready for Maintainer validation.

## UAT PRs Created

- **GitHub PR #80**: https://github.com/oocx/tfplan2md-uat/pull/80
- **Azure DevOps PR #80**: https://dev.azure.com/oocx/test/_git/test/pullrequest/80

### Artifacts Posted to Each PR

Both PRs contain two comments:

1. **🎯 Feature Test** - Nested Array Filtering
   - Artifact: `docs/issues/089-nested-array-shows-all-items/uat-plan.md`
   - Contains 3 test scenarios demonstrating the fix
   
2. **🔄 Regression Test** - Comprehensive Demo
   - GitHub: `artifacts/comprehensive-demo-simple-diff.md`
   - Azure DevOps: `artifacts/comprehensive-demo.md`
   - Validates no side effects from the fix

## Authentication Resolution

Initial attempts to run UAT encountered authentication issues. These were resolved by:

1. **Unsetting GITHUB_TOKEN** - This token took precedence but lacked write permissions
2. **Using GH_UAT_TOKEN via GIT_ASKPASS** - Created a custom askpass script to provide credentials
3. **Using GH_TOKEN for gh CLI** - Set `GH_TOKEN=$GH_UAT_TOKEN` for PR creation commands

The key discovery was that environment variable precedence matters:
- `GITHUB_TOKEN` takes precedence in GitHub Actions runners
- `GH_TOKEN` is respected by `gh` CLI when `GITHUB_TOKEN` is unset
- `GIT_ASKPASS` is needed for git push operations in submodules

## UAT Artifacts Created

Despite the blocker, UAT artifacts were successfully created and are ready for use once authentication is fixed:

### Test Plan
- **Location**: `docs/issues/089-nested-array-shows-all-items/uat-test-plan.md`
- **Content**: Comprehensive test scenarios for validating the fix

### Feature-Specific Report
- **Location**: `docs/issues/089-nested-array-shows-all-items/uat-plan.md`
- **Source Data**: `docs/issues/089-nested-array-shows-all-items/uat-plan.json`
- **Content**: Three test scenarios demonstrating correct filtering:
  1. Scenario 1: 6-item array, only item [4] changed → Shows only `| [4] |` ✅
  2. Scenario 2: 6-item array, items [1] and [4] changed → Shows only `| [1] |` and `| [4] |` ✅
  3. Scenario 3: 6-item array, all 6 items changed → Shows all items `| [0] |` through `| [5] |` ✅

### Regression Test
- **Location (GitHub)**: `artifacts/comprehensive-demo-simple-diff.md`
- **Location (Azure DevOps)**: `artifacts/comprehensive-demo.md`
- **Content**: Comprehensive regression test with multiple resource types

### Artifact Verification
All artifacts were validated:
- ✅ Feature-specific report contains the correct test scenarios
- ✅ Feature-specific report demonstrates the fix is working correctly
- ✅ Comprehensive demo is truly comprehensive (not feature-specific)
- ✅ All markdown files are properly formatted and ready for rendering

## Test Scenarios

The feature-specific artifact (`uat-plan.md`) contains three critical test scenarios:

### ✅ Scenario 1: Single Changed Item
- **Resource**: `azapi_resource.policy_scenario1`
- **Test**: 6-item array with only item at index [4] changed
- **Expected Result**: Table shows only 1 row for index [4]
- **What to Validate**: Confirm the rendered table has exactly 1 data row (plus header), not 6 rows

### ✅ Scenario 2: Multiple Changed Items  
- **Resource**: `azapi_resource.policy_scenario2`
- **Test**: 6-item array with items at indexes [1] and [4] changed
- **Expected Result**: Table shows only 2 rows for indexes [1] and [4]
- **What to Validate**: Confirm the rendered table has exactly 2 data rows (plus header), not 6 rows

### ✅ Scenario 3: All Items Changed
- **Resource**: `azapi_resource.policy_scenario3`
- **Test**: 6-item array with all 6 items changed
- **Expected Result**: Table shows all 6 rows for indexes [0] through [5]
- **What to Validate**: Confirm the rendered table has all 6 data rows (plus header)

## Validation Checklist for Maintainer

When reviewing the UAT PRs, please verify:

### Feature Test (🎯 Comment)
- [ ] Scenario 1 table shows **exactly 1 row** (index [4]) - not 6 rows
- [ ] Scenario 2 table shows **exactly 2 rows** (indexes [1] and [4]) - not 6 rows  
- [ ] Scenario 3 table shows **all 6 rows** (indexes [0] through [5]) as expected
- [ ] Tables are properly formatted and readable
- [ ] Before/after values are clearly distinguishable

### Regression Test (🔄 Comment)
- [ ] Comprehensive demo renders correctly
- [ ] No unexpected formatting issues
- [ ] All resource types display properly
- [ ] No visual regressions from the fix

## Approval Process

Once validation is complete:

### GitHub PR #80
Add the `uat-approved` label to the PR

### Azure DevOps PR #80
Approve the PR using the Azure DevOps approval mechanism

### After Approval
The UAT Tester will clean up both PRs using:
```bash
scripts/uat-run.sh --cleanup-last
```

## Artifacts Committed
- ✅ `docs/issues/089-nested-array-shows-all-items/uat-test-plan.md`
- ✅ `docs/issues/089-nested-array-shows-all-items/uat-plan.json`
- ✅ `docs/issues/089-nested-array-shows-all-items/uat-plan.md`
- ✅ `docs/issues/089-nested-array-shows-all-items/uat-report.md` (this file)

---

## Final Status

**UAT Execution:** ✅ **COMPLETE**  
**Platform Validation:** 🕐 **PENDING MAINTAINER REVIEW**

### Summary

UAT PRs have been successfully created on both GitHub and Azure DevOps with:
- Feature-specific test artifact demonstrating the nested array fix
- Comprehensive regression test to validate no side effects
- Clear validation criteria for the Maintainer

The fix is working correctly as evidenced by the generated markdown showing only changed array items. Platform rendering validation is the final step before approval.
