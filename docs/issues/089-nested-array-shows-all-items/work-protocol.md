# Work Protocol: Fix Nested Array Rendering

## Workflow Type
Bug Fix

## Issue Number
089

## Issue Title
Nested Array Changes Show All Items Instead of Only Changed Items

## Current Branch
`copilot/fix-html-rendering-issue`

## Related Documentation
- Feature Specification: docs/features/034-azapi-attribute-grouping/specification.md
- Architecture: docs/architecture.md

## Agent Work Log

### Issue Analyst - 2024
**Duration:** ~1 hour
**Status:** ✅ Complete

**Work Completed:**
1. ✅ Loaded coding-agent-workflow skill
2. ✅ Verified current branch (`copilot/fix-html-rendering-issue`)
3. ✅ Determined next issue number (089) using `scripts/next-issue-number.sh`
4. ✅ Created issue directory: `docs/issues/089-nested-array-shows-all-items/`
5. ✅ Investigated codebase to understand nested array change detection and rendering
6. ✅ Identified root cause in `SelectUpdateGroupsAndMainProps` function
7. ✅ Traced through related code in:
   - `AzApi.Rendering.Update.cs` - Main update rendering logic
   - `AzApi.Rendering.Array.cs` - Array extraction logic
   - `AzApi.Grouping.cs` - Grouping detection logic
   - `AzApi.Data.cs` - JSON comparison logic
8. ✅ Reviewed feature specification (034-azapi-attribute-grouping)
9. ✅ Created comprehensive issue analysis document at `docs/issues/089-nested-array-shows-all-items/analysis.md`
10. ✅ Committed analysis document with conventional commit message

**Artifacts Produced:**
- `docs/issues/089-nested-array-shows-all-items/analysis.md` - Comprehensive issue analysis with root cause, suggested fixes, and test recommendations

**Root Cause Identified:**
The `SelectUpdateGroupsAndMainProps` function in `AzApi.Rendering.Update.cs` (lines 163-176) uses an all-or-nothing approach: if ANY property in an array group has changed, it renders the ENTIRE group (all array items). This was an intentional MVP design decision documented in the feature specification, but creates poor user experience with excessive output.

**Recommended Fix:**
Implement filtering in `ExtractArrayItems` or `SelectUpdateGroupsAndMainProps` to include only array items that have at least one changed property, rather than including all items when any item changes.

**Problems Encountered:**
None - investigation proceeded smoothly.

**Next Agent Recommendation:**
Developer agent to implement the fix based on Option 1 in the analysis (filter array items to show only changed items).

**Ready for Handoff:** Yes ✅

---

### Developer - [Date TBD]
**Duration:** 
**Status:** 🔄 Pending

**Assigned Tasks:**
1. Implement filtering logic to show only changed array items
2. Add unit tests for new filtering behavior
3. Update existing tests if needed
4. Add integration test with sample Azure Policy Definition resource
5. Verify fix resolves the original bug report scenario

**Reference:** See analysis.md for detailed implementation guidance and test requirements.

---

## Notes

- Issue number 089 was determined using `scripts/next-issue-number.sh` (required to prevent conflicts)
- The bug stems from an intentional MVP design decision in feature 034, now being reconsidered based on user feedback
- The change summary correctly identifies specific changes, but detailed rendering shows entire groups
- This issue affects all AzAPI resources with nested array structures (e.g., Azure Policy Definitions)
