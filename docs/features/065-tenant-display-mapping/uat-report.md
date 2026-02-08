# UAT Report: Tenant Display Name Mapping

**Status:** ❌ FAILED
**Date:** 2026-02-08

## Test Environment
- **Platform:** GitHub & Azure DevOps
- **Artifacts:**
  - `artifacts/tenant-mapping-uat.md` (Feature-specific)
  - `artifacts/comprehensive-demo.md` (Regression)

## Test Results

### 1. Tenant Mapping (Attribute Tables)
- **Requirement:** `tenant_id` displayed as 🏢 `Name (GUID)`
- **Result:** ❌ FAILED
- **Issue:** Icon was wrapped inside the backticks (`🏢 Name (GUID)`).

### 2. Management Group Icons
- **Requirement:** 🗂️ icon outside backticks for MG IDs.
- **Result:** ❌ FAILED
- **Issue:** Icons were wrapped inside backticks or missing in summary context.

### 3. Registry Icons
- **Requirement:** Icons resolved from the registry should follow the same "icon outside backticks" rule.
- **Result:** ❌ FAILED
- **Issue:** Registry-resolved icons were included in the code span.

## Conclusion
The mapping logic is working correctly (GUIDs are resolved to names), but the visual formatting violates the presentation specification.

## Next Steps
- Hand off to Developer to refactor `ScribanHelpers` and Azure-specific formatters to move icon concatenation outside of the `FormatCodeTable` and `FormatCodeSummary` calls.
