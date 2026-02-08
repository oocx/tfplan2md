# UAT Report: Tenant Display Name Mapping

**Status:** ❌ FAILED
**Date:** 2026-02-08

## Test Environment
- **Platform:** GitHub & Azure DevOps
- **Artifacts:**
  - `artifacts/tenant-mapping-uat.md` (Feature-specific)

## Test Results

### 1. Resource Summary Formatting
- **Description:** Verify that summary lines include correct backticks and icons.
- **Actual:** `➕ azurerm_role_assignment mg_scope — 👤 Jane Doe → 🛡️ Reader on management group mg-root` (Missing backticks and icons)
- **Expected:** `➕ azurerm_role_assignment `mg_scope` — `👤 Jane Doe` → `🛡️ Reader` on management group `🗂️ mg-root``
- **Result:** ❌ FAILED

### 2. Attribute Table Icons
- **Description:** Verify that management group icons are present in attribute tables inside backticks.
- **Actual:** `scope`mg-root (Management Group)` ` (Missing 🗂️ icon)
- **Expected:** `scope`🗂️ mg-root (Management Group)` `
- **Result:** ❌ FAILED

### 3. Icon Placement Policy
- **Requirement:** Icons for Azure entities must be inside the backticks.
- **Result:** ❌ FAILED (The UAT Tester incorrectly reported that icons should be outside backticks).

## Conclusion
The implementation fails to provide the required icons and backtick formatting in resource summaries and attribute tables. The UAT Tester also misidentified the expected formatting policy in the previous report.

## Next Steps
- Hand off to Developer to fix the missing backticks and icons in the summary and attribute table outputs.
- Ensure icons are correctly placed *inside* the backticks for all Azure-related formatters.
