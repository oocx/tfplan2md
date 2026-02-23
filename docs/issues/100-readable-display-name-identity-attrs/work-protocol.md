# Work Protocol: Issue 100 - Readable Display Name Incorrectly Applied to Resource's Own Identity Attributes

## Workflow Type

**Bug Fix**

## Issue Reference

- **Issue Number:** 100
- **Branch:** `copilot/fix-readable-display-name-issue-again`
- **Related Documentation:** 
  - `docs/features/019-azure-resource-id-formatting/specification.md`
  - `docs/features/024-visual-report-enhancements/specification.md`
  - `docs/features/061-extensible-provider-registry/specification.md`

## Problem Statement

The "readable display name" feature is incorrectly applied to a resource's own identity attributes (`id`, `name`) when rendering attribute tables. This causes redundant output where a resource's own identifier is decorated with semantic icons AND additional context that should only be used when referencing other resources.

Expected: Resource's own `id`/`name` should only show semantic icon (🆔)  
Actual: May show full "Type `name` in resource group `rg` of subscription `sub`" format

## Workflow Participants

- **Issue Analyst** (current): Investigate and document the bug
- **Developer** (next): Implement the fix based on analysis
- **Code Reviewer** (after dev): Review the implementation
- **UAT Tester** (after review): Validate the fix in rendered output

## Agent Work Log

### Issue Analyst - 2024-02-23

**Task:** Investigate the bug and create analysis document

**Investigation Approach:**
1. ✅ Searched for "readable display name" and related formatting code
2. ✅ Located `AzureResourceIdFormatter` as the primary component
3. ✅ Traced the formatting pipeline from templates through helpers to formatters
4. ✅ Identified the root cause: formatter applies to ALL attributes without distinguishing own identity from references
5. ✅ Reviewed existing tests and snapshot files
6. ✅ Examined current behavior in test snapshots

**Key Findings:**
- The `AzureResourceIdFormatter` is registered with a match pattern that applies to ALL azurerm attributes
- No logic exists to distinguish between a resource's own `id`/`name` and references to other resources
- The registration uses `MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null)` which matches everything
- Current test snapshots show correct behavior for `name` attributes (only icon), suggesting the bug might be specific to certain scenarios or `id` attributes

**Root Cause:**
File: `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRmValueFormatterRegistration.cs` (lines 30-32)
- Registers `AzureResourceIdFormatter` without excluding identity attributes
- The formatter in `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AzureResourceIdFormatter.cs` has no context about whether an attribute is the resource's own identity

**Proposed Solutions:**
1. **Option 1 (Recommended):** Modify match pattern to exclude `id` and `name` attributes using regex negative lookahead
2. **Option 2:** Add context parameter to indicate "own identity" 
3. **Option 3:** Create separate `IdentityAttributeFormatter` with higher priority

**Artifacts Created:**
- ✅ `docs/issues/100-readable-display-name-identity-attrs/analysis.md` - Comprehensive analysis document

**Problems Encountered:**
- None - investigation was straightforward

**Recommendation:**
Hand off to **Developer** agent to implement Option 1 (match pattern exclusion) as it's the simplest solution with minimal changes required.

---

### Developer - 2024-02-23

**Task:** Implement the fix based on analysis

**Actions Taken:**
- ✅ Modified `AzureResourceIdFormatter.TryFormat()` to exclude `id` and `name` attributes
- ✅ Added 4 comprehensive test cases (2 for azurerm, 2 for azapi)
- ✅ Verified all existing tests continue to pass
- ✅ Added XML documentation to explain the exclusion logic

**Implementation Details:**
The fix uses a simple early-return pattern in `TryFormat()`:
```csharp
if (context.AttributeName is "id" or "name")
{
    return null;
}
```
This ensures identity attributes are not formatted by `AzureResourceIdFormatter`, allowing them to fall through to semantic formatting which adds only the icon decoration.

**Problems Encountered:**
- None - the implementation was straightforward

**Artifacts Created/Modified:**
- Modified: `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AzureResourceIdFormatter.cs`
- Modified: `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiModule.cs` (comment update)
- Modified: `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRmValueFormatterRegistration.cs` (comment update)
- Created: 4 new test methods in `src/tests/Oocx.TfPlan2Md.TUnit/Providers/ProviderValueFormatterRegistryTests.cs`

**Next Steps:**
- Technical Writer to create release notes and update documentation

---

### Technical Writer - 2024-02-23

**Task:** Create release notes and update documentation for the bug fix

**Actions Taken:**
- ✅ Reviewed the implementation and test changes
- ✅ Examined the issue analysis document
- ✅ Checked existing feature documentation (Feature 015 - Universal Azure Resource ID Formatting)
- ✅ Created comprehensive release notes following project conventions
- ✅ Verified docs/features.md and README.md don't need updates (they describe the feature correctly at a high level)

**Documentation Analysis:**
- **docs/features.md**: Line 648-652 describes "Universal Azure Resource ID Formatting" - no update needed as it describes the feature generically without specifying it applies to identity attributes
- **README.md**: Line 51 mentions "Readable Azure Resource IDs" - no update needed as it's accurate
- **Feature 015 specification**: Documents the feature correctly; the bug was in implementation, not design

**Problems Encountered:**
- None - the documentation already describes the feature correctly

**Artifacts Created/Modified:**
- ✅ Created: `docs/issues/100-readable-display-name-identity-attrs/release-notes.md`
- ✅ Updated: `docs/issues/100-readable-display-name-identity-attrs/work-protocol.md` (this file)

**Next Steps:**
- Code Reviewer to review the implementation and documentation

---

### [Next Agent] - [Date]

**Task:** [To be filled by next agent]

**Actions Taken:**
- [To be filled]

**Problems Encountered:**
- [To be filled]

**Artifacts Created/Modified:**
- [To be filled]

**Next Steps:**
- [To be filled]

---

## Handoff Checklist

- [x] Analysis document created and saved to disk
- [x] Root cause identified with file paths and line numbers
- [x] Suggested fix approaches documented with pros/cons
- [x] Related tests identified
- [x] Work protocol created
- [x] Ready to commit analysis and hand off to Developer

## Notes

The investigation found that the current test snapshots actually show CORRECT behavior for `name` attributes (they only display the semantic icon without the expanded format). This suggests the bug might be:
1. Specific to `id` attributes in certain scenarios
2. Related to how computed values are handled
3. Less prevalent than initially described

The Developer should verify the actual bug occurrence by:
1. Creating a test case with a known `id` value (not "known after apply")
2. Testing with update/delete operations where `id` appears in "before" state
3. Confirming whether the issue exists before implementing the fix

If the bug doesn't actually occur in practice, this issue analysis still provides value as preventive documentation and could lead to explicit test coverage for this edge case.
