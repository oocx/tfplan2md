# Code Review: Debug Output (Feature 038)

## Summary

This is a re-review of Feature 038 (Debug Output) following the developer's fix for the critical issue identified in the initial review. The fix addressed the primary concern: failed principal resolutions now include resource context as required by the specification.

**Fix Commit:** 9a330e6 - "fix: pass resource address to principal mapper for diagnostic context"

**Overall Assessment:** The critical issue has been addressed with a reasonable solution that enables diagnostic tracking of failed principal resolutions. However, there is one **Blocker** issue related to an unintended behavioral change that must be fixed before approval.

## Verification Results

- **Tests:** 417/419 passed (2 failures)
  - ❌ `Delete_RendersRemoveSummary` - **BLOCKER:** Regression introduced by the fix
  - ⏱️ `Docker_WithVersionFlag_DisplaysVersion` - Timeout (unrelated to changes)
- **Build:** Success (verified via test execution)
- **Docker:** Not fully verified due to network issues during build, but not blocking given test results
- **Workspace Errors:** None observed

## Snapshot Changes

- **Snapshot files changed:** No
- **Commit message token `SNAPSHOT_UPDATE_OK` present:** N/A
- **Justification:** N/A

## Review Decision

**Status:** ⚠️ Changes Requested

## Issues Found

### Blockers

#### 1. Regression in `ResolvePrincipalName` breaks existing functionality

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.Azure.cs` lines 83-99

**Issue:** The fix modified `ResolvePrincipalName` to call `GetName(principalId, null, resourceAddress)`, passing `null` for the principal type parameter. This changes the behavior compared to templates that may rely on type-aware principal resolution.

The failing test `Delete_RendersRemoveSummary` expects:
```
| principal_id | `👤 John Doe (User)` [`33333333-3333-3333-3333-333333333333`] |
```

But now gets:
```
| principal_id | `👤 Security Team (User)` [`33333333-3333-3333-3333-333333333333`] |
```

**Root Cause:** The test data has the same principal ID (`33333333-3333-3333-3333-333333333333`) mapped to two different names depending on type:
- Type "User" → "John Doe"
- Type "Group" → "Security Team"

The StubPrincipalMapper's `GetName(id, type)` method correctly returns "John Doe" when type is "User", but when type is `null` (as now passed), it falls back to the generic lookup which returns "Security Team".

**Impact:** This could affect real-world scenarios where the same principal ID legitimately has different display names based on type (though this is rare in Azure AD/Entra).

**Why This Happened:** The `ResolvePrincipalName` helper function doesn't have access to the principal type, only the principal ID. The Scriban helper registration changed from:
```csharp
// Before (implied)
scriptObject.Import("azure_principal_name", new Func<string?, string>(p => ResolvePrincipalName(p, principalMapper)));

// After
scriptObject.Import("azure_principal_name", new Func<string?, string?, string>((id, addr) => ResolvePrincipalName(id, principalMapper, addr)));
```

The new signature accepts `(id, addr)` but doesn't accept or pass through the principal type.

**Recommendation:** There are two viable solutions:

**Option A (Recommended):** Update the Scriban helper signature to accept principal type as well:
```csharp
scriptObject.Import("azure_principal_name", new Func<string?, string?, string?, string>((id, type, addr) => ResolvePrincipalName(id, type, principalMapper, addr)));
```

Then update `ResolvePrincipalName` to:
```csharp
private static string ResolvePrincipalName(string? principalId, string? principalType, IPrincipalMapper principalMapper, string? resourceAddress = null)
{
    if (principalId is null)
    {
        return string.Empty;
    }

    var name = principalMapper.GetName(principalId, principalType, resourceAddress);
    if (name is null)
    {
        return principalId;
    }

    return $"{name} [{principalId}]";
}
```

**Option B:** Keep the current signature but document that `azure_principal_name` doesn't support type-aware resolution (and update the test to reflect this reality).

**Severity:** Blocker - This is a regression that changes existing behavior and breaks a test.

---

### Major Issues

None identified.

### Minor Issues

None identified.

### Suggestions

#### 1. Consider consolidating helper signatures

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.Registry.cs`

**Observation:** The helpers now have varying signatures:
- `azure_principal_info`: 3 parameters `(id, type, addr)`
- `azure_principal_name`: 2 parameters `(id, addr)` - missing type

**Suggestion:** For consistency, consider making `azure_principal_name` also accept 3 parameters to match `azure_principal_info`. This would make the API more predictable for template authors.

**Impact:** Low - This is a consistency/maintainability suggestion, not a functional issue (aside from the blocker above).

---

## Critical Issue Resolution Verification

### Original Issue
Failed principal resolutions didn't include resource context, making it impossible to know which resource referenced a missing principal ID.

### Fix Analysis
The fix correctly addresses this by:

1. ✅ Updated `RoleAssignmentViewModelFactory.GetPrincipalInfo` to accept and pass `change.Address`
2. ✅ Updated `ScribanHelpers.GetPrincipalInfo` to accept optional `resourceAddress` parameter
3. ✅ Updated `ScribanHelpers.ResolvePrincipalName` to accept optional `resourceAddress` parameter
4. ✅ Updated Scriban helper registration to support the resource address parameter
5. ✅ Maintained backward compatibility with default parameters
6. ✅ Test TC-12 (`PrincipalMapper_FailedResolution_RecordsResourceContext`) passes and validates the fix

### Code Quality Assessment

**Positive aspects:**
- Clean use of optional parameters (`string? resourceAddress = null`) for backward compatibility
- Proper XML documentation added to modified methods
- Default interface implementations in `IPrincipalMapper` maintain backward compatibility
- The fix is focused and minimal - only touches what's necessary

**Areas of concern:**
- The `ResolvePrincipalName` regression (Blocker #1 above)
- The comment on line 90-91 suggests some uncertainty about the design ("GetPrincipalName has a 2-parameter overload...but we want to use the 3-parameter one")

##Checklist Summary

| Category | Status | Notes |
|----------|--------|-------|
| Correctness | ⚠️ Partial | Critical issue fixed, but regression introduced |
| Code Quality | ✅ Pass | Clean, well-documented code |
| Access Modifiers | ✅ Pass | Appropriate use of private/internal |
| Code Comments | ✅ Pass | XML docs present and accurate |
| Architecture | ✅ Pass | Aligns with diagnostic context pattern |
| Testing | ⚠️ Partial | TC-12 passes, but regression in `Delete_RendersRemoveSummary` |
| Documentation | ✅ Pass | No documentation changes needed for this fix |

## Detailed Test Analysis

### Passing Tests
- ✅ **TC-12** (`PrincipalMapper_FailedResolution_RecordsResourceContext`): **CRITICAL** - This test validates the core fix and it passes
- ✅ 416 other tests pass, indicating no widespread regressions

### Failing Tests
1. **Delete_RendersRemoveSummary** (Blocker)
   - **Type:** Regression
   - **Root Cause:** Loss of principal type information in `ResolvePrincipalName`
   - **Must Fix:** Yes

2. **Docker_WithVersionFlag_DisplaysVersion** (Not Blocking)
   - **Type:** Timeout
   - **Root Cause:** Unrelated to feature changes (Docker test infrastructure)
   - **Must Fix:** No (pre-existing or environmental issue)

## Backward Compatibility

✅ **Excellent backward compatibility**:
- Optional parameters with defaults (`resourceAddress = null`)
- Default interface implementations in `IPrincipalMapper`
- Existing code calling methods without the new parameter continues to work
- No breaking changes to public APIs

## Next Steps

### Required Changes (Blocker)

1. **Fix the `ResolvePrincipalName` regression:**
   - Update the Scriban helper registration for `azure_principal_name` to accept 3 parameters: `(id, type, addr)`
   - Update `ResolvePrincipalName` method signature to accept `principalType` parameter
   - Pass the principal type through to `principalMapper.GetName()`
   - Update any template calls to `azure_principal_name` to pass the type parameter (or pass `null` if type is unavailable)
   - Verify `Delete_RendersRemoveSummary` test passes

2. **Update test stub mappers:**
   - Ensure `StubPrincipalMapper` and `UnmappedPrincipalMapper` in test files properly implement the 3-parameter `GetName` overload (they inherit the default implementation, but explicit implementation would be clearer)

### Verification Steps After Fix

1. Run the full test suite and confirm all tests pass (except known Docker timeouts)
2. Specifically verify:
   - `Delete_RendersRemoveSummary` passes
   - `TC-12` (`PrincipalMapper_FailedResolution_RecordsResourceContext`) still passes
3. Generate comprehensive demo output and verify it's correct
4. Run markdownlint on the demo output

## Recommendation

**Do not approve** until the blocker issue is resolved. The fix is very close to being correct - it successfully enables resource context tracking for failed principal resolutions (the critical requirement). However, the unintended regression in `ResolvePrincipalName` must be addressed to avoid breaking existing functionality.

Once the blocker is fixed:
- The implementation will be solid
- Backward compatibility will remain excellent
- All acceptance criteria will be met
- The feature will be ready for approval

## Additional Context

### Why This Matters

The debug output feature is critical for troubleshooting principal mapping issues in production environments. Users need to know:
1. ✅ **Which principals failed to resolve** - Already working
2. ✅ **Which resources referenced those principals** - Fixed by this commit
3. ⚠️ **That existing principal resolution behavior isn't broken** - Regression introduced

The fix successfully addresses #2, but we cannot accept a regression in existing behavior (#3).

### Complexity Assessment

The required fix is straightforward:
- **Estimated effort:** 15-30 minutes
- **Risk:** Low - confined to helper function signatures
- **Testing:** Existing tests will validate the fix

