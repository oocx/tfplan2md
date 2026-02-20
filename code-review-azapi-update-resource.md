# Code Review: Apply Attribute Grouping to `azapi_update_resource`

## Summary

This review covers a targeted fix that extends Feature 034 (Improved AzAPI Attribute Grouping and Array Rendering) to `azapi_update_resource`. The original feature specification stated that the grouping should apply to "all azapi resources that work with JSON body attributes (azapi_resource, azapi_update_resource, and potentially others)", but `azapi_update_resource` was not included in the initial implementation. This fix addresses that oversight by creating a template file that delegates to the existing `azapi/resource` template.

**Overall Assessment:** ✅ The implementation is correct, minimal, and follows the established pattern. The fix successfully applies attribute grouping to `azapi_update_resource`.

## Verification Results

- **Tests:** Pass (15/15 in AzapiResourceTemplateTests, all tests pass)
- **Build:** Success
- **Docker:** Could not verify (Alpine CDN network issue - infrastructure problem, not code issue)
- **Comprehensive Demo:** Generated successfully (only version/timestamp changes)
- **Markdownlint:** Pre-existing MD024 error (unrelated to these changes)
- **Workspace Errors:** None

## Specification Compliance

| Requirement from Feature 034 Spec | Implemented | Tested | Notes |
|-----------------------------------|-------------|--------|-------|
| Apply to azapi_resource | ✅ (existing) | ✅ | Already implemented in Feature 034 |
| Apply to azapi_update_resource | ✅ | ✅ | **This fix** |
| Apply to "potentially others" | N/A | N/A | No other azapi resource types identified |

**Spec Alignment:** ✅ The specification (docs/features/034-azapi-attribute-grouping/specification.md line 11) explicitly states:

> The solution will apply to all azapi resources that work with JSON body attributes (azapi_resource, azapi_update_resource, and potentially others).

This fix completes the implementation to match the specification.

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Template resolution for `azapi_update_resource` | ✅ Pass | Manually verified by renaming template - fallback to `_resource` confirmed |
| Metadata table rendering | ✅ Pass | Shows `name`, `parent_id`, `location` |
| Body Changes heading | ✅ Pass | Shows "Body Changes" instead of generic "Attribute" table |
| API documentation link | ✅ Pass | Generated correctly from resource type |
| Attribute de-prefixing | ✅ Pass | `disableLocalAuth` instead of `body.properties.disableLocalAuth` |
| Update diff rendering | ✅ Pass | Before/After columns shown correctly |

**Manual verification performed:**
1. Generated markdown with template → Shows azapi-specific rendering
2. Generated markdown without template → Falls back to generic `_resource` template
3. Comparison confirms the template is being used correctly

## Review Decision

**Status:** ✅ **Approved with Minor Suggestions**

## Snapshot Changes

- **Snapshot files changed:** No
- **Commit message token `SNAPSHOT_UPDATE_OK` present:** N/A
- **Why the snapshot diff is correct:** N/A - No snapshot changes

## Issues Found

### Blockers

**None**

### Major Issues

**None**

### Minor Issues

1. **Missing work-protocol.md file**
   - **Location:** `docs/features/034-azapi-attribute-grouping/work-protocol.md`
   - **Issue:** The work protocol file is missing for this work item
   - **Impact:** Moderate - This is a follow-up fix to an already-released feature (034), so the missing work protocol is less critical than it would be for a new feature. However, it should exist to document this additional work.
   - **Recommendation:** Since Feature 034 has already gone through the full workflow (Requirements → Architect → Developer → Code Review → UAT → Release), and this is a small follow-up fix, the maintainer should decide whether to create a minimal work protocol or document this as a hotfix/patch to the existing feature.
   - **Note:** The feature has retrospective.md and uat-report.md from the original implementation, indicating it went through the full workflow previously.

2. **No TemplateResolver unit test for `azapi_update_resource`**
   - **Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/TemplateResolverTests.cs`
   - **Issue:** While integration tests verify end-to-end behavior, there's no unit test explicitly verifying that `ResolveTemplate("azapi_update_resource")` returns `"azapi/update_resource"`
   - **Impact:** Low - The integration tests provide coverage, but explicit unit-level testing would improve test granularity and make the resolution mechanism more obvious
   - **Recommendation:** Add a test case like:
     ```csharp
     [Test]
     public void ResolveTemplate_AzapiUpdateResource_ReturnsAzapiUpdateResourceTemplate()
     {
         // Arrange
         var result = _resolver.ResolveTemplate("azapi_update_resource");
         
         // Assert
         result.Should().Be("azapi/update_resource");
     }
     ```

### Suggestions

1. **Consider documenting the template delegation pattern**
   - The `{{~ include "/azapi/resource" ~}}` pattern is elegant but not immediately obvious to new contributors
   - Could add a comment in the template file explaining why this delegation approach is used:
     ```scriban
     {{~## Template for azapi_update_resource
          Delegates to azapi/resource to ensure consistent attribute grouping behavior.
          Related feature: docs/features/034-azapi-attribute-grouping/specification.md
     ~}}
     {{~ include "/azapi/resource" ~}}
     ```

2. **Test naming could reference the spec requirement**
   - The tests could include a comment referencing the spec line that requires this behavior
   - Example: `// Spec requirement: "apply to all azapi resources" (specification.md:11)`

## Critical Questions Answered

- **What could make this code fail?**
  - If the `azapi/resource` template is renamed or moved, the include would break
  - If the ResourceTypeParser logic changes, template resolution might fail
  - **Mitigation:** Integration tests would catch both scenarios

- **What edge cases might not be handled?**
  - All edge cases are delegated to the `azapi/resource` template, which already has comprehensive coverage from Feature 034
  - No new edge cases introduced by this change

- **Are all error paths tested?**
  - Template not found: Would fall back to `_resource` (verified manually)
  - Invalid JSON in test data: Would fail at parsing level (existing coverage)
  - ✅ All paths covered

## Checklist Summary

| Category | Status | Notes |
|----------|--------|-------|
| Correctness | ✅ | Template resolution works, grouping applied correctly |
| Code Quality | ✅ | Minimal, clean implementation |
| Access Modifiers | ✅ | N/A - only template file changes |
| Code Comments | ⚠️ | Template could benefit from comment explaining delegation |
| Architecture | ✅ | Follows established template delegation pattern |
| Testing | ✅ | Integration tests cover behavior; unit test would be nice-to-have |
| Documentation | ⚠️ | work-protocol.md missing (minor for follow-up fix) |

## Detailed Findings

### Correctness ✅

**Implementation Approach:**
The fix uses template delegation (`{{~ include "/azapi/resource" ~}}`), which is the correct approach because:
1. **DRY Principle:** Reuses existing, tested logic from `azapi/resource` template
2. **Consistency:** Ensures `azapi_update_resource` behaves identically to `azapi_resource`
3. **Maintainability:** Changes to grouping logic automatically apply to both resource types

**Template Resolution Flow:**
1. Resource type: `azapi_update_resource`
2. ResourceTypeParser splits on `_`: provider=`azapi`, resource=`update_resource`
3. TemplateResolver checks if `azapi/update_resource` template exists: **YES** ✅
4. Template includes `/azapi/resource` which renders with grouping logic

**Verification Evidence:**
- Created test plan with `azapi_update_resource` → Renders with metadata table and "Body Changes" heading
- Removed template → Falls back to generic `_resource` template (no metadata, full dotted paths)
- Restored template → Grouping behavior returns

### Code Quality ✅

**Changes Made:**
1. **New file:** `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn` (1 line)
2. **New file:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-update-resource-plan.json` (test data)
3. **Modified:** Added 2 test methods to `AzapiResourceTemplateTests.cs`

**Positive Aspects:**
- ✅ Minimal change - only adds what's necessary
- ✅ No code duplication - delegates to existing template
- ✅ Test data is realistic (update action with before/after body changes)
- ✅ Tests follow naming convention: `MethodName_Scenario_ExpectedResult`
- ✅ Tests use AAA pattern (Arrange-Act-Assert)
- ✅ Inline comments explain intent

**Comparison to Alternatives:**
| Approach | Pros | Cons | Selected |
|----------|------|------|----------|
| Template delegation (current) | DRY, consistent, maintainable | Requires understanding of include mechanism | ✅ Yes |
| Duplicate template logic | Self-contained | Code duplication, inconsistency risk | ❌ No |
| Modify TemplateResolver | Programmatic mapping | More complex, harder to discover | ❌ No |

### Testing ✅

**Test Coverage:**

**Integration Tests (AzapiResourceTemplateTests.cs):**
1. `Render_AzapiUpdateResource_UsesAzapiTemplate` - Verifies azapi-specific rendering
   - Checks for "Body Changes" heading (not present in generic template)
   - Checks for Before/After columns (update operation)
   
2. `Render_AzapiUpdateResource_ShowsMetadata` - Verifies metadata table
   - Checks for `| Attribute | Value |` table
   - Checks for `name` and `parent_id` fields

**Test Data (azapi-update-resource-plan.json):**
- ✅ Valid Terraform plan format (format_version 1.2)
- ✅ Update action (not create/delete)
- ✅ Realistic before/after body changes
- ✅ Contains nested JSON body (`properties.disableLocalAuth`, etc.)
- ✅ Uses actual Azure resource type (`Microsoft.Automation/automationAccounts@2021-06-22`)

**Coverage Gaps:**
- ⚠️ No unit test at TemplateResolver level (minor - integration tests provide coverage)
- ✅ All functional behavior is tested through integration tests

**Test Execution Results:**
```
Test run summary: Passed!
  total: 15
  failed: 0
  succeeded: 15
  skipped: 0
  duration: 3s 425ms
```

### Architecture ✅

**Alignment with Feature 034:**
The fix follows the same architectural pattern established in Feature 034:
- Template files in `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/`
- Template resolution via `ResourceTypeParser` + `TemplateResolver`
- Scriban `include` directive for code reuse

**Design Pattern:**
The template delegation pattern used here is consistent with other providers in the codebase. The `include` directive is a standard Scriban feature for template composition.

**No Scope Creep:**
- ✅ Does not add new features beyond what specification requires
- ✅ Does not modify existing grouping logic
- ✅ Focused solely on extending feature to `azapi_update_resource`

## Next Steps

Since this is a targeted fix to an already-released feature, and all tests pass:

1. **Minor Issue Resolution (Optional):**
   - Maintainer decision: Create work-protocol.md or document as hotfix
   - Consider adding TemplateResolver unit test (optional improvement)
   - Consider adding template comment (optional documentation enhancement)

2. **Approval Path:**
   - ✅ Code Review: Approved
   - ➡️ **Next Agent:** Release Manager (this is a bug fix/completion of Feature 034, no UAT needed since the rendering behavior is identical to azapi_resource which was already UAT-tested)

## Summary

This is a well-executed, minimal fix that completes Feature 034 by extending attribute grouping to `azapi_update_resource` as originally specified. The implementation:

✅ Correctly uses template delegation to reuse existing logic
✅ Passes all tests (15/15 in test class, full suite passes)
✅ Follows established architectural patterns
✅ Has appropriate test coverage via integration tests
✅ Makes no unnecessary changes or additions
✅ Completes the specification requirement

**The fix is functionally correct and ready for release.** The minor suggestions are optional improvements that can be addressed in future work if desired.

**Recommendation:** Approved for merge. Hand off to Release Manager to include in next release as part of Feature 034 completion.
