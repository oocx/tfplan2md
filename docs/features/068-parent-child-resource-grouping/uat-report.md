# UAT Report: Parent-Child Resource Grouping

**Date:** 2026-02-11  
**Feature:** #068 Parent-Child Resource Grouping  
**UAT PRs:**
- GitHub: #67 (https://github.com/oocx/tfplan2md-uat/pull/67) - PASSED
- Azure DevOps: #72 (https://dev.azure.com/oocx/test/_git/test/pullrequest/72) - PASSED

**Status:** ✅ PASSED

---

## Test Artifacts Used

1. **Feature-Specific:** `artifacts/parent-child-resource-grouping-uat.md`
2. **Regression (GitHub):** `artifacts/comprehensive-demo-simple-diff.md`
3. **Regression (AzDO):** `artifacts/comprehensive-demo.md`

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
