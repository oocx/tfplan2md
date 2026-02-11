# UAT: Fix Azure AD Group Member Count Summary (Issue #447 / Fix #070)

## 🎯 Bug Fix Validation

This UAT validates the fix for **Issue #447**: Parent-Child Resource Summary Shows Incorrect Member Counts

### What Was Fixed

**Before the fix:**
```
🔄 azuread_group mixed_engineering | 0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members
```
- Icon counts only showed inline members
- Missed separate `azuread_group_member` child resources  
- Member table showed 3 rows but icons only counted 2

**After the fix:**
```
🔄 azuread_group mixed_engineering | 3 👤 1 👥 1 💻 | ➕ 5 members
```
- Icon counts now include BOTH inline and separate members
- Icons correctly show member types (👤 users, 👥 groups, 💻 service principals)
- Icon counts match the number of rows in the member table

### Test Scenarios

This fix handles three scenarios:

1. **Inline members only** - Members defined in the group's `members` attribute
2. **Separate members only** - Members defined as separate `azuread_group_member` resources
3. **Mixed (both)** - The main bug scenario that was broken

---

## 📋 Validation Checklist

Please verify the following in the rendered markdown below:

### ✅ Icon Count Accuracy
- [ ] Icon counts (👤 👥 💻) match the number of rows in the member table
- [ ] Each member type is counted correctly:
  - `👤` = users
  - `👥` = groups  
  - `💻` = service principals
  - `❓` = unknown types (should be rare)

### ✅ Member Table Completeness
- [ ] Member table shows ALL members (inline + separate)
- [ ] Each member row shows correct Terraform Resource column:
  - `members attribute` for inline members
  - `azuread_group_member.<name>` for separate child resources

### ✅ Action Count Accuracy
- [ ] Action counts (e.g., `➕ 5 members`) correctly reflect total member changes
- [ ] Action counts match member table row count

### ✅ Warning for Mixed Configuration
- [ ] Groups with both inline and separate members show the warning:
  > ⚠️ **Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.

---

## 🔬 Test Case: Azure AD Group with Mixed Members

The example below demonstrates the bug fix scenario: an Azure AD group with **both inline members** (in the `members` attribute) **and separate members** (as `azuread_group_member` child resources).

<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">

<summary>➕ azuread_group <b><code>platform_engineers</code></b> — <code>👥 Platform Engineers</code> (<code>🆔 platform-engineers</code>) - Platform engineering team with infrastructure access | <code>3 👤 1 👥 1 💻</code> | ➕ 5 members</summary>
<br>

| Attribute | Value |
| ----------- | ------- |
| description | `Platform engineering team with infrastructure access` |
| display_name | `Platform Engineers` |
| mail_nickname | `platform-engineers` |

#### Members

⚠️ **Warning:** This resource has children managed both inline
and as separate resources. This configuration will cause conflicts.

| Member | Terraform Resource |
| -------- | -------------------- |
| `user-100` | members attribute |
| `user-101` | members attribute |
| `group-200` | members attribute |
| `spn-300` | members attribute |
| `user-102` | azuread_group_member.platform_admin_member |

</details>

---

## 🧪 Expected Results

**Summary Line Analysis:**

```
➕ azuread_group platform_engineers | 3 👤 1 👥 1 💻 | ➕ 5 members
                                      ^^^^^^^^^^^^^^^^^
                                      These icon counts should match:
```

**Icon Counts:**
- `3 👤` = 3 users (`user-100`, `user-101`, `user-102`)
- `1 👥` = 1 group (`group-200`)
- `1 💻` = 1 service principal (`spn-300`)
- **Total:** 5 members

**Member Table:**
- Should show exactly **5 rows**
- 4 rows from `members attribute` (inline)
- 1 row from `azuread_group_member.platform_admin_member` (separate)

**Action Count:**
- `➕ 5 members` matches the 5 rows in the table

**Warning Message:**
- Should appear because the group has both inline and separate members

---

## 🐛 What To Watch For

### ❌ Regressions to Check

If you see any of these, the fix has regressed:

1. **Zero counts when should be non-zero:**
   ```
   | 0 👤 0 👥 0 💻 | ➕ 5 members
   ```
   ❌ Icons show zero but action says 5 members

2. **Count mismatch:**
   ```
   | 2 👤 | ➕ 5 members
   ```
   ❌ Icons only count 2 but table has 5 rows

3. **Missing member types:**
   ```
   | 5 ❓ | ➕ 5 members
   ```
   ❌ All members shown as unknown (❓) instead of resolved types

4. **Missing members from table:**
   - Member table only shows 4 rows instead of 5
   - Missing the separate `azuread_group_member` child resource

---

## 📊 Additional Context

**Related Resources:**
- **Issue:** #447 (Parent-Child Resource Summary Shows Incorrect Member Counts)
- **Fix PR:** #456 (on branch `copilot/fix-summary-member-counts`)
- **Root Cause:** Summaries were built before parent-child merging completed
- **Solution:** Added post-merge update step to rebuild icon counts after children are merged

**Code Changes:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
  - Added `UpdateAzureAdGroupSummaries` method
  - Runs after `MergeParentChildRelationships`
  - Extracts member IDs from merged child rows
  - Recounts by type using `PrincipalMapper`
  - Updates icon counts in `SummaryHtml`

**Test Coverage:**
- 4 new test scenarios in `AzureAdGroupSummaryMemberCountTests.cs`
- All 940 existing tests pass
- 4 snapshot updates to reflect correct counts

---

## ✅ Approval Criteria

This UAT passes if ALL of the following are true:

1. ✅ Icon counts match member table row counts
2. ✅ Member types are correctly identified (👤 👥 💻 not all ❓)
3. ✅ All members appear in the table (both inline and separate)
4. ✅ Warning appears for mixed inline/separate configuration
5. ✅ Action counts match icon counts
6. ✅ Rendering looks correct in both GitHub and Azure DevOps

---

**Tested by:** UAT Tester Agent  
**Date:** 2026-02-11  
**Branch:** copilot/fix-summary-member-counts
