# UAT Report: Fix Azure AD Group Member Count Summary (Issue #447)

**Issue:** #447  
**Fix PR:** #456  
**Branch:** `copilot/fix-summary-member-counts`  
**UAT Date:** 2026-02-11  
**Tested By:** UAT Tester Agent

---

## Executive Summary

**Status:** ⏸️ **BLOCKED - Manual UAT Required**

**Reason:** UAT automation scripts require GitHub authentication (GITHUB_TOKEN) which is not available in the current GitHub Copilot agent execution environment. The automated UAT workflow (`scripts/uat-run.sh`) cannot push to the UAT repository or create PRs without authentication.

**Recommendation:** **Maintainer must perform manual UAT** using the instructions and artifacts provided below.

---

## Bug Fix Overview

### What Was Fixed

**Issue #447:** Parent-Child Resource Summary Shows Incorrect Member Counts

**Problem:**
- Icon counts (👤 👥 💻) only showed inline members from the `members` attribute
- Missed separate `azuread_group_member` child resources
- Member table showed all members but icon counts didn't match

**Example - Before Fix:**
```
🔄 azuread_group mixed_engineering | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```
(Table has 3 members, but icons show 0 users/0 groups/0 SPs/2 unknown)

**Example - After Fix:**
```
🔄 azuread_group mixed_engineering | 3 👤 1 👥 1 💻 | ➕ 5 members
```
(Icon counts now match member types and table row count)

### Solution Implemented

**Approach:** Simple post-merge update (recommended in `analysis.md`)

**Changes Made:**
1. Added `UpdateAzureAdGroupSummaries()` method in `ReportModelBuilder.ParentChildMerging.cs`
2. Runs after `MergeParentChildRelationships()` completes
3. Extracts member IDs from merged `ChildResourceGroups`
4. Recounts members by type using existing `PrincipalMapper`
5. Updates icon counts in the parent's `SummaryHtml`

**Code Impact:**
- 2 files changed (~196 lines added)
- No new interfaces or architectural changes
- Reuses existing services and patterns
- 4 new test scenarios added
- All 940 tests pass
- 4 snapshot updates to reflect correct counts

---

## Manual UAT Instructions

### Prerequisites

1. **Artifacts Ready:** 
   - ✅ Feature-specific artifact: `artifacts/fix-070-member-counts-uat.md`
   - ✅ Regression artifact (GitHub): `artifacts/comprehensive-demo-simple-diff.md`
   - ✅ Regression artifact (Azure DevOps): `artifacts/comprehensive-demo.md`

2. **Branch:** Ensure you're on `copilot/fix-summary-member-counts`

3. **Authentication:** Ensure GitHub CLI (`gh`) is authenticated to push to UAT repos

### Step 1: Create UAT PRs Manually

#### Option A: Use Automated Script (If You Have Auth)

```bash
# Run the automated workflow
scripts/uat-run.sh artifacts/fix-070-member-counts-uat.md \
  "Fix Azure AD group member count summary (Issue #447)" \
  --create-only

# This will create PRs in both GitHub and Azure DevOps
# Then follow Step 2 below
```

#### Option B: Manual PR Creation (If Script Fails)

##### GitHub UAT PR

```bash
# 1. Navigate to GitHub UAT repo
cd uat-repos/github

# 2. Create UAT branch
git checkout -B copilot/fix-summary-member-counts origin/main

# 3. Create marker commit
mkdir -p .uat
echo "UAT marker commit" > .uat/uat-run.txt
git add .uat/uat-run.txt
git commit -m "chore(uat): Fix Azure AD group member counts"

# 4. Push branch
git push origin HEAD:copilot/fix-summary-member-counts --force

# 5. Create PR using GitHub CLI
cd ../..
gh pr create \
  --repo oocx/tfplan2md-uat \
  --title "UAT: Fix Azure AD Group Member Count Summary (Issue #447)" \
  --body "See PR comments for test artifacts" \
  --base main \
  --head copilot/fix-summary-member-counts

# 6. Get PR number
PR_NUMBER=$(gh pr list --repo oocx/tfplan2md-uat --head copilot/fix-summary-member-counts --json number --jq '.[0].number')

# 7. Post feature-specific artifact as comment
gh pr comment $PR_NUMBER --repo oocx/tfplan2md-uat --body-file artifacts/fix-070-member-counts-uat.md

# 8. Post regression artifact as second comment
gh pr comment $PR_NUMBER --repo oocx/tfplan2md-uat --body-file artifacts/comprehensive-demo-simple-diff.md
```

##### Azure DevOps UAT PR

```bash
# Similar steps for Azure DevOps using `az devops` CLI
# (See scripts/uat-azdo.sh for reference)
```

### Step 2: Review Rendering in UAT PRs

#### What to Verify

Visit the created PRs in both platforms:
- **GitHub:** https://github.com/oocx/tfplan2md-uat/pulls
- **Azure DevOps:** https://dev.azure.com/oocx/test/_git/test/pullrequests

Review **both** PR comments in each platform:
1. **🎯 Feature Test** (first comment) - Validates the specific bug fix
2. **🔄 Regression Test** (second comment) - Ensures no side effects

#### Validation Checklist

For the Azure AD group example in the feature test artifact:

```
➕ azuread_group platform_engineers | 3 👤 1 👥 1 💻 | ➕ 5 members
```

**Icon Count Accuracy:**
- [ ] Icon counts match member table row count (5 members shown)
- [ ] Icon types are correct:
  - `3 👤` = 3 users (user-100, user-101, user-102)
  - `1 👥` = 1 group (group-200)
  - `1 💻` = 1 service principal (spn-300)
- [ ] No unknown type icons (❓) for known types

**Member Table Completeness:**
- [ ] Member table shows ALL 5 members
- [ ] Inline members show "members attribute" source
- [ ] Separate members show "azuread_group_member.xxx" source
- [ ] Warning appears about mixed inline/separate configuration

**Action Count Accuracy:**
- [ ] Action count `➕ 5 members` matches icon total and table rows

**Rendering Quality:**
- [ ] Tables render correctly with proper alignment
- [ ] Icons display correctly (not showing as text codes)
- [ ] Code formatting is preserved
- [ ] Links work and are formatted properly
- [ ] No escaped characters displaying incorrectly

**Regression Check (Comprehensive Demo):**
- [ ] All existing functionality still works
- [ ] No layout issues or broken rendering
- [ ] Other Azure AD resources render correctly
- [ ] Non-Azure AD resources unaffected

### Step 3: Approve or Reject

#### If UAT Passes ✅

**GitHub:**
```bash
# Apply approval label
gh pr edit $PR_NUMBER --repo oocx/tfplan2md-uat --add-label "uat-approved"
```

**Azure DevOps:**
- Approve the PR in the Azure DevOps UI

#### If UAT Fails ❌

**GitHub:**
```bash
# Apply rejection label
gh pr edit $PR_NUMBER --repo oocx/tfplan2md-uat --add-label "uat-rejected"

# Add comment with specific issues
gh pr comment $PR_NUMBER --repo oocx/tfplan2md-uat --body "❌ UAT Failed\n\n**Issues Found:**\n1. [Describe issue]\n2. [Describe issue]"
```

**Azure DevOps:**
- Request changes in the PR with specific feedback

### Step 4: Cleanup

After approval/rejection:

```bash
# Close the UAT PRs
gh pr close $PR_NUMBER --repo oocx/tfplan2md-uat

# (Repeat for Azure DevOps PR)

# Clean up local UAT branches
cd uat-repos/github
git checkout main
git branch -D copilot/fix-summary-member-counts

cd ../azdo
git checkout main
git branch -D copilot/fix-summary-member-counts
```

---

## Test Artifacts

### Feature-Specific Test

**Location:** `artifacts/fix-070-member-counts-uat.md`

**Purpose:** Validates the specific bug fix for Azure AD group member counts

**Test Scenario:** Azure AD group with mixed members (both inline and separate)

**Expected Results:**
- Icon counts: `3 👤 1 👥 1 💻`
- Member table: 5 rows
- Action count: `➕ 5 members`
- Warning about mixed configuration

### Regression Test

**GitHub:** `artifacts/comprehensive-demo-simple-diff.md`  
**Azure DevOps:** `artifacts/comprehensive-demo.md`

**Purpose:** Ensures no regressions in existing functionality

**Coverage:** All resource types, all rendering scenarios, full feature set

---

## Known Limitations

### Environment Issue

The automated UAT workflow (`scripts/uat-run.sh`) expects to run in an environment with GitHub authentication configured. Specifically:

1. **GitHub Token Required:** The scripts use `git push` to the UAT repository, which requires authentication
2. **Current Environment:** GitHub Copilot agent execution environment does not expose `GITHUB_TOKEN`
3. **Git Credential Helper:** Configured but references undefined `$GITHUB_TOKEN` variable

**Impact:** Cannot automate UAT PR creation and management from this agent context.

**Workaround:** Manual UAT using the instructions above, or run the UAT workflow from a different environment (e.g., local machine with GitHub auth, or GitHub Actions workflow with proper token permissions).

### Future Improvement

**Recommendation:** Enhance the agent execution environment to provide GitHub authentication for UAT workflows, or create a dedicated GitHub Actions workflow for UAT that can be triggered by the agent.

---

## Test Results

**Status:** ⏸️ **AWAITING MANUAL UAT**

### Automated Checks ✅

- [x] All unit tests pass (940 tests, 0 failures)
- [x] Build succeeds with 0 warnings
- [x] Snapshot updates verified
- [x] Code review approved
- [x] Test artifacts generated successfully
- [x] Comprehensive demo validates correctly

### Manual UAT 🔄

- [ ] GitHub UAT PR created
- [ ] GitHub UAT PR reviewed and approved
- [ ] Azure DevOps UAT PR created
- [ ] Azure DevOps UAT PR reviewed and approved
- [ ] No rendering issues found
- [ ] Regression test passed

---

## Approval Decision

**Pending Maintainer Review**

Once manual UAT is complete, update this section with:

- **GitHub UAT PR:** #XXX (Status: APPROVED/REJECTED)
- **Azure DevOps UAT PR:** #YYY (Status: APPROVED/REJECTED)
- **Issues Found:** (List any rendering problems)
- **Final Decision:** PASS / FAIL
- **Next Step:** Release Manager (if passed) / Developer (if failed)

---

## Related Documentation

- **Issue Analysis:** `docs/issues/070-parent-child-summary-member-counts/analysis.md`
- **Code Review:** `docs/issues/070-parent-child-summary-member-counts/code-review.md`
- **Work Protocol:** `docs/issues/070-parent-child-summary-member-counts/work-protocol.md`
- **Test Plan:** N/A (no formal test plan existed; validation steps derived from bug analysis)

---

## Appendix: What Changed

### Files Modified

1. **`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`**
   - Added `UpdateAzureAdGroupSummaries()` method
   - Added helper methods for member extraction and counting
   - ~180 lines added

2. **`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`**
   - Added call to `UpdateAzureAdGroupSummaries()` after parent-child merging
   - ~1 line added

### Tests Added

1. **`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupSummaryMemberCountTests.cs`**
   - New test class with 4 scenarios:
     - Group with inline members only
     - Group with separate members only
     - Group with mixed members (the bug scenario)
     - Group with no members

### Snapshots Updated

- 4 snapshot files updated to reflect correct member counts
- Justified with `SNAPSHOT_UPDATE_OK` commit message

---

**Report Generated:** 2026-02-11  
**Agent:** UAT Tester  
**Status:** Blocked pending manual UAT execution
