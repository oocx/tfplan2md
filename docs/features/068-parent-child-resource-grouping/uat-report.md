# UAT Report: Parent-Child Resource Grouping

**Date:** 2026-02-11 (Initial UAT)  
**Updated:** 2026-02-13 (Inline Diff Fix)  
**Updated:** 2026-02-13 (Comprehensive Fixes - Commit 9f0db75)  
**Feature:** #068 Parent-Child Resource Grouping  

## UAT PRs

### Initial UAT (Core Feature)
- GitHub: #67 (https://github.com/oocx/tfplan2md-uat/pull/67) - PASSED
- Azure DevOps: #72 (https://dev.azure.com/oocx/test/_git/test/pullrequest/72) - PASSED

### Azure RM Batch 2 UAT (Inline Diffs)
- GitHub: #72 (https://github.com/oocx/tfplan2md-uat/pull/72) - UPDATED (inline diff fix)
- Azure DevOps: #74 (https://dev.azure.com/oocx/test/_git/test/pullrequest/74) - UPDATED (inline diff fix)

### Azure RM Batch 2 UAT (Comprehensive Fixes - 2026-02-13)
- GitHub: #72 (https://github.com/oocx/tfplan2md-uat/pull/72) - UPDATED (commit 9f0db75)
- Azure DevOps: #74 (https://dev.azure.com/oocx/test/_git/test/pullrequest/74) - UPDATED (commit 9f0db75)

**Status:** ✅ UPDATED - Fresh artifacts posted with comprehensive fixes

---

## Test Artifacts Used

### Initial UAT
1. **Feature-Specific:** `artifacts/parent-child-resource-grouping-uat.md`
2. **Regression (GitHub):** `artifacts/comprehensive-demo-simple-diff.md`
3. **Regression (AzDO):** `artifacts/comprehensive-demo.md`

### Azure RM Batch 2 UAT (Updated for Inline Diff Fix)
1. **Feature-Specific:** `artifacts/azure-rm-batch-2-uat.md`
2. **Regression (GitHub):** `artifacts/comprehensive-demo-simple-diff.md` (regenerated)
3. **Regression (AzDO):** `artifacts/comprehensive-demo.md` (regenerated)

### Azure RM Batch 2 UAT (Comprehensive Fixes - 2026-02-13)
1. **Feature-Specific:** `artifacts/azure-rm-batch-2-feature-test.md` (328 lines, regenerated with commit 9f0db75)
2. **Regression (GitHub):** `artifacts/comprehensive-demo-simple-diff.md` (regenerated)
3. **Regression (AzDO):** `artifacts/comprehensive-demo.md` (regenerated)

---

## Comprehensive Fixes (2026-02-13) - Commit 9f0db75

### Fresh Artifacts Posted

After the inline diff fix, additional issues were identified and fixed. Fresh test artifacts were regenerated and posted to both UAT PRs to validate all fixes together.

### Fixes Included

**Commit:** `9f0db75` - "fix: bare dash without code tags, newlines instead of br in GitHub diffs (SNAPSHOT_UPDATE_OK)"

1. **Bare Dash Placeholder Without Code Tags**
   - Issue: Dash placeholders (`-`) for empty/missing values were wrapped in backticks, making them look like code
   - Fix: Render bare dash `-` without code formatting for cleaner presentation
   - Impact: Improves readability of empty values in tables

2. **GitHub Diffs Using Newlines Instead of Literal `<br>` Tags**
   - Issue: GitHub simple-diff format was inserting literal `<br>` HTML tags in diff output
   - Fix: Use actual newlines (`\n`) in GitHub diff format instead of HTML tags
   - Impact: Proper line breaks in GitHub PR comments

3. **Raw Value Extraction Before HTML Diff Generation**
   - Issue: HTML escaping was happening before diff generation, causing escaped HTML tags in diffs
   - Fix: Extract raw values first, generate diffs, then apply HTML escaping only where needed
   - Impact: Clean diffs without escaped HTML entities

4. **Backticks on All Non-Diff Values**
   - Issue: Some values weren't consistently formatted as code
   - Fix: Apply backticks to all non-diff values for consistent code formatting
   - Impact: Better visual consistency across all value displays

### Test Results

**Posted to:**
- GitHub PR #72: https://github.com/oocx/tfplan2md-uat/pull/72
  - Before: 19 comments
  - After: 21 comments (verified)
  - 🎯 Feature Test (comment 20)
  - 🔄 Regression Test (comment 21)

- Azure DevOps PR #74: https://dev.azure.com/oocx/test/_git/test/pullrequest/74
  - 🎯 Feature Test (posted successfully)
  - 🔄 Regression Test (posted successfully)

**Artifacts:**
1. Feature-specific: `artifacts/azure-rm-batch-2-feature-test.md` (328 lines)
2. Regression (GitHub): `artifacts/comprehensive-demo-simple-diff.md`
3. Regression (Azure DevOps): `artifacts/comprehensive-demo.md`

**Coverage:**
- 43 Azure RM resources (4 parent-child resource types)
- Virtual Networks with Subnets (inline, separate, mixed)
- DNS Zones with Records (A, AAAA, CNAME, MX, TXT, CAA)
- Route Tables with Routes (inline and separate)
- Network Security Groups with Security Rules (inline, separate, mixed)

**Status:** ✅ Artifacts posted successfully, awaiting maintainer review

---

## Inline Diff Fix (2026-02-13)

### Issue Identified

During Azure RM Batch 2 UAT testing, inline diffs were completely missing for UPDATE operations on child resources (subnets, routes, NSG rules, DNS records). The summary counts showed changes, but the actual before/after diffs were not visible in the child tables.

### Root Cause

Two bugs were identified and fixed in commit `4ef994e`:

1. **Diff Detection Bug**: The system wasn't properly detecting value changes in nested child resources. The diff detection logic was only checking top-level attributes.

2. **Diff Extraction Bug**: When diffs were detected, they weren't being extracted and passed to the rendering layer. The rendering context didn't include the diff information needed for inline display.

### Fix Applied

**Commit:** `4ef994e`

**Changes:**
- Fixed diff detection to properly traverse nested child resource attributes
- Fixed diff extraction to pass before/after values to the rendering layer
- Verified inline diffs now work for all child resource types

**Test Results:**
- ✅ All 994 tests pass
- ✅ Inline diffs render correctly in UPDATE resources
- ✅ Character-level and word-level diffs display properly

### Examples of Fixed Inline Diffs

1. **Subnet Address Prefix Change:** `/24` → `/23` now shows character-level diff
2. **Route Next Hop Type Change:** `VirtualAppliance` → `VnetLocal` now shows highlighted changes
3. **NSG Rule Source Addition:** Single source → multiple sources now shows added values highlighted
4. **NSG Rule Description Change:** Word-level diff now displays for description updates

### UAT PR Updates

Both UAT PRs (#72 GitHub, #74 Azure DevOps) were updated with:
1. Explanation comment documenting the fix
2. Regenerated feature-specific artifact showing working inline diffs
3. Regenerated comprehensive demo for regression testing

---

## Test Results

### ✅ Configuration Reference Matching (Known After Apply)

**Resource:** `azuread_group.platform_engineers`

**Verified:**
- ✅ Single section for parent resource (no separate child sections)
- ✅ Members table present with both inline and separate members
- ✅ Members show correct Terraform resource addresses
- ✅ Change indicators (➕, 🔄, ❌) display correctly

**Status:** PASSED

---

### ✅ Mixed Management Warning

**Resources:** Multiple groups with both inline and separate children

**Verified:**
- ✅ Warning message displays: "⚠️ **Warning:** This resource has children managed both inline and as separate resources"
- ✅ All children appear in the same table
- ✅ Warning is clearly visible and helpful

**Status:** PASSED

---

### ✅ Change Summary

**Verified:**
- ✅ Parent resource headers include child counts
- ✅ Summary format is clear (e.g., `➕ 4 members`)
- ✅ Counts aggregate all child changes

**Status:** PASSED

---

### ✅ Cross-Platform Layout

**GitHub:**
- ✅ Tables have proper markdown headers
- ✅ Change indicators display correctly
- ✅ Resource addresses are formatted as monospace code
- ✅ Warning messages display with emoji

**Azure DevOps:**
- ✅ Tables render cleanly (no broken markdown)
- ✅ Change indicators display correctly
- ✅ No layout issues or overflow
- ✅ Warning messages are visible

**Status:** PASSED

---

## Minor Issues Found (Non-Blocking)

### Issue #447: Incorrect Member Counts in Summary

**Description:**
The summary line shows incorrect member counts in some scenarios:

1. **Zero count when should be 1:**
   ```
   0 👤 0 👥 0 💻 | ➕ 1 members | ❌ 1 members
   ```
   Should show `1` in the icon counts, not `0`.

2. **Count mismatch between summary and table:**
   ```
   🔄 azuread_group mixed_engineering — 👥 Engineering Mixed | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
   ```
   But the members table has 3 entries (appears to only count separate resources, not inline members).

**Impact:** Minor - Does not affect core functionality or table rendering. Summary counts are inconsistent but table content is correct.

**Tracking:** GitHub Issue #447

**Decision:** Fix separately, does not block feature #068 merge.

---

## Test Plan Coverage

| Test Case | Status | Notes |
|-----------|--------|-------|
| Configuration Reference Matching (known after apply) | ✅ PASSED | Children correctly merged into parent sections |
| Value-Based Matching (known ID) | ✅ PASSED | Update scenarios work correctly |
| Mixed Management Warning | ✅ PASSED | Warning displays and tables render |
| Change Summary with Counts | ⚠️ PASSED (minor issue) | See issue #447 |
| Cross-Platform Rendering | ✅ PASSED | Both GitHub and AzDO render correctly |

---

## Previous UAT Attempts

### Attempt 1 (GitHub #65, AzDO #70)
**Date:** 2026-02-11 (earlier)  
**Status:** ❌ FAILED  
**Issue:** Member tables not rendering despite correct summary counts. Rendering logic bug fixed before this UAT.

---

## Conclusion

The parent-child resource grouping feature is **ready for release**. All critical functionality works as expected:

- Configuration reference matching works correctly for `(known after apply)` scenarios
- Children are properly merged into parent sections with inline tables
- Mixed management warnings display correctly
- Cross-platform rendering is clean and consistent

Minor issue #447 (summary count discrepancies) does not block release and will be fixed separately.

---

## Evidence Files

- Feature artifact: `artifacts/parent-child-resource-grouping-uat.md`
- Regression artifacts: `artifacts/comprehensive-demo-simple-diff.md`, `artifacts/comprehensive-demo.md`
- Test plan: `docs/features/068-parent-child-resource-grouping/uat-test-plan.md`
- Bug report: GitHub Issue #447
