# UAT Report: Parent-Child Resource Grouping

**Date:** 2026-02-11  
**Feature:** #068 Parent-Child Resource Grouping  
**UAT PRs:**
- GitHub: #65 (https://github.com/oocx/tfplan2md-uat/pull/65) - CLOSED
- Azure DevOps: #70 (https://dev.azure.com/oocx/test/_git/test/pullrequest/70) - ABANDONED

**Status:** ❌ FAILED

---

## Test Artifacts Used

1. **Feature-Specific:** `artifacts/parent-child-resource-grouping-uat.md`
2. **Regression:** `artifacts/comprehensive-demo-simple-diff.md`

---

## Critical Issues Found

### Issue 1: Members Tables Missing for Create Operations

**Resource:** `azuread_group.inline_engineering`

**Expected Behavior:**
- Summary line shows: `➕ 3 members`
- Should display a "Members" table with 3 member rows

**Actual Behavior:**
- Summary line correctly shows: `0 👤 0 👥 0 💻 3 ❓ | ➕ 3 members`
- **No members table is rendered**
- Members are completely missing from the output

**Impact:** Critical - Primary feature functionality is broken for CREATE operations

---

### Issue 2: Members Missing for Update Operations (Separate Resources)

**Resource:** `azuread_group.separate_engineering`

**Expected Behavior:**
- Summary line shows: `➕ 1 members | ❌ 1 members`
- Should display a "Members" table with 2 rows (1 addition, 1 removal)

**Actual Behavior:**
- Summary line shows: `0 👤 0 👥 0 💻 | ➕ 1 members | ❌ 1 members`
- **No members table is rendered**
- Discrepancy between member counts (0 in first part vs. +1/-1 in second part)

**Additional Observation:**
The title shows: `👥 Engineering Team Engineering team members - updated`
- This appears to be concatenating `display_name` + `description` without proper separator

**Impact:** Critical - Configuration reference matching not working for UPDATE operations

---

### Issue 3: Mixed Management Members Missing

**Resource:** `azuread_group.mixed_engineering`

**Expected Behavior:**
- Summary line shows: `➕ 2 members`
- Should display a "Members" table with 2 member rows (mixed inline + separate)
- Should show warning: "⚠️ **Warning:** This resource has children managed both inline and as separate resources"

**Actual Behavior:**
- Summary line shows: `0 👤 0 👥 0 💻 2 ❓ | ➕ 2 members`
- **No members table is rendered**
- **No mixed management warning displayed**

**Impact:** Critical - Mixed management detection and warning system not functioning

---

### Issue 4: Contractors Group Members Missing

**Resource:** `azuread_group.contractors`

**Expected Behavior:**
- Summary line shows: `➕ 1 members`
- Should display a "Members" table with 1 member row

**Actual Behavior:**
- Summary line shows: `0 👤 0 👥 0 💻 | ➕ 1 members`
- **No members table is rendered**

**Impact:** Critical - Even simple cases with single members are not rendering

---

## Pattern Analysis

All four test cases show the same failure pattern:

1. **Summary counters are being calculated correctly** (showing correct member counts)
2. **Member tables are completely missing** from the rendered output
3. **Member type breakdown shows zeros** (`0 👤 0 👥 0 💻`) but then shows correct additions
4. This suggests:
   - The parent-child detection logic is identifying members
   - The summary aggregation is working
   - **The rendering/output logic is failing to display the member tables**

---

## Test Plan Coverage

| Test Case | Status | Notes |
|-----------|--------|-------|
| Configuration Reference Matching (known after apply) | ❌ FAILED | Tables not rendered |
| Value-Based Matching (known ID) | ❌ FAILED | Tables not rendered |
| Mixed Management Warning | ❌ FAILED | No warning, no table |
| Change Summary with Counts | ⚠️ PARTIAL | Counts present but incorrect breakdown |
| Cross-Platform Rendering | ⚠️ NOT TESTED | Cannot test without tables |

---

## Root Cause Hypothesis

The issue appears to be in the **rendering phase** rather than the detection/aggregation phase:

1. Parent-child relationships are being detected (summary shows member counts)
2. Children are being aggregated (member type counts attempted)
3. **Rendering logic is not outputting the member tables**

Possible causes:
- Template condition preventing table rendering
- Missing template section for member tables
- Logic error in the rendering pathway deciding when to show tables
- Child resources being filtered out before rendering

---

## Reproduction Steps

1. Generate artifact: `tfplan2md --plan examples/parent-child-grouping/plan.json --output artifacts/parent-child-resource-grouping-uat.md`
2. Create UAT PR with artifact
3. Observe resource sections in rendered markdown
4. Expected: Member tables below each group resource
5. Actual: No member tables rendered

---

## Required Fixes

1. **Investigate rendering logic** for parent-child tables
2. **Fix table output** for all parent-child scenarios (create, update, mixed)
3. **Fix member type breakdown** (showing all zeros despite having members)
4. **Implement mixed management warning** (currently not displayed)
5. **Fix title formatting** (remove description duplication in title)

---

## Next Steps

☐ Developer: Investigate why member tables are not being rendered despite correct summary counts  
☐ Developer: Add/fix template logic to output member tables  
☐ Developer: Add test coverage for rendering output (not just summary calculation)  
☐ Re-run UAT after fixes

---

## Evidence Files

- Feature artifact: `artifacts/parent-child-resource-grouping-uat.md`
- Comprehensive demo: `artifacts/comprehensive-demo.md`
- Test plan: `docs/features/068-parent-child-resource-grouping/uat-test-plan.md`
