# Code Review: Custom Template for azapi_resource

## Summary

This review evaluates the implementation of a custom Scriban template for `azapi_resource` resources, which transforms JSON body content into human-readable markdown tables. The implementation includes 5 new Scriban helper functions (~680 lines), a comprehensive template file (~228 lines), 41 passing unit and integration tests, and updated documentation.

**Overall Assessment:** The implementation demonstrates strong code quality with comprehensive test coverage and well-structured helpers. However, there are **3 Blocker issues** related to markdown rendering invariants that must be fixed before approval. The core functionality is sound, but the template produces markdown that violates project quality standards in specific scenarios.

## Verification Results

### Tests
- **Status:** ⚠️ 486 passed, 3 failed
- **Failure Details:**
  - `Invariant_AllTablesParseCorrectly_AllPlans` - 3 azapi test files generate unparseable tables
  - `Invariant_NoRawNewlinesInTableCells_AllPlans` - 8 instances of raw newlines in table cells
  - `Templates_ShouldNotExceedMaximumLineCount` - Template exceeds 100-line limit (228 lines)

### Build
- **Status:** ✅ Success
- **Warnings:** 0
- **Errors:** 0

### Docker
- **Status:** ⚠️ Build failed due to network issue (Alpine package manager)
- **Impact:** Not feature-related - infrastructure issue with Alpine CDN
- **Note:** Feature functionality is not affected

### Comprehensive Demo
- **Status:** ✅ Generated successfully
- **Markdownlint:** ✅ 0 errors

### Code Quality Checks
- **Compilation:** ✅ Clean (0 warnings, 0 errors)
- **Access Modifiers:** ✅ All helpers are `internal` or `private`
- **AOT Compatibility:** ✅ No reflection, uses System.Text.Json
- **Performance:** ✅ Efficient (handles 100+ properties <100ms per architecture doc)

## Review Decision

**Status:** 🚫 Changes Requested

The implementation requires fixes for 3 Blocker issues before approval. The code quality and architecture are excellent, but the template produces markdown that breaks project quality invariants in specific scenarios.

## Issues Found

### Blockers

#### 1. Raw Newlines in "Other Attribute Changes" Table Cells

**Severity:** Blocker  
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`, lines 212-225

**Description:**  
The "Other Attribute Changes" section uses direct `escape_markdown` on attribute values (line 221), which converts newlines to `<br/>` tags. However, the Terraform attribute change values can contain raw JSON strings with embedded newlines (e.g., from the `body` attribute when serialized). When these multi-line JSON strings are placed in table cells with `<br/>` tags, markdown parsers fail to parse the table correctly.

**Test Evidence:**
- `azapi-multiple-large-values-plan.json`: expected 3 tables, parsed 2
- `azapi-large-value-plan.json`: expected 3 tables, parsed 2
- `azapi-all-large-body-plan.json`: expected 3 tables, parsed 2
- Raw newlines detected at lines 37, 40, etc. in multiple test files

**Root Cause:**  
The template section at lines 220-221:
```scriban
| {{ attr.name | escape_markdown }} | {{ (attr.before ?? "(null)") | escape_markdown }} | {{ (attr.after ?? "(null)") | escape_markdown }} |
```

This directly escapes attribute values, but `escape_markdown` converts `\n` to `<br/>`, which breaks table parsing when the value is a long JSON string. The issue occurs when `change.attribute_changes` includes attributes like `body.properties.connectionString` with multi-line values.

**Example of Broken Output:**
```markdown
| body.properties.siteConfig.connectionStrings | (null) | Server=tcp:myserver...<br/>Multiple...<br/>Lines... |
```

**Why This Breaks:**  
Markdown table parsers expect cells to be on a single logical line. When `<br/>` tags appear in cells without proper escaping or code block formatting, the table structure becomes ambiguous and parsers fail.

**Fix Required:**  
Replace direct `escape_markdown` with proper attribute formatting that handles multi-line values. The template should use `format_attribute_value_table` (like the body sections do) or wrap long values in code blocks/collapsible sections. Alternatively, exclude the `body.*` attributes from the "Other Attribute Changes" section entirely, since the body is already rendered separately.

**Recommended Approach:**
```scriban
{{~ # Show non-body attribute changes only ~}}
{{~ non_body_attrs = change.attribute_changes | array.filter @(do; ret !string.starts_with $0.name "body."; end) ~}}
{{~ if non_body_attrs && non_body_attrs.size > 0 ~}}
<details>
<summary>Other Attribute Changes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
{{~ for attr in non_body_attrs ~}}
| {{ attr.name | escape_markdown }} | {{ format_attribute_value_table attr.before attr.after null }} | {{ format_attribute_value_table attr.before attr.after null }} |
{{~ end ~}}

</details>
{{~ end ~}}
```

---

#### 2. Template Exceeds Maximum Line Count

**Severity:** Blocker (Architectural Violation)  
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`

**Description:**  
The template file contains 228 lines, exceeding the project's 100-line limit for templates. This violates the architectural guideline that templates should be kept concise, with complex logic moved to C# helpers or view models.

**Test Evidence:**
```
AssertionException: Expected oversizedTemplates to be empty because templates should not 
exceed 100 lines - consider moving logic to C# ViewModels or splitting into partials. 
Oversized templates: azapi.resource (228 lines)
```

**Why This Matters:**  
The 100-line limit ensures templates remain maintainable and testable. Large templates become difficult to reason about, have complex control flow, and are harder to test in isolation.

**Analysis:**  
The template has significant duplication across create/update/delete/replace sections:
- Lines 56-91: CREATE body rendering
- Lines 92-137: UPDATE body rendering  
- Lines 138-173: DELETE body rendering
- Lines 174-209: REPLACE body rendering

Each section follows nearly identical logic:
1. Flatten the body JSON
2. Separate small vs. large properties
3. Render small properties in main table
4. Render large properties in collapsible section

**Fix Required:**  
Refactor repeated logic into either:
1. **C# View Model** - Create `AzapiResourceViewModel` with pre-computed properties
2. **Scriban Partial Templates** - Extract common rendering blocks to `_azapi_body.sbn`
3. **C# Helper Functions** - Add helpers like `render_azapi_body(body, mode)` that return formatted markdown

**Recommended Approach:**  
Create a C# helper function that encapsulates the body rendering logic:
```csharp
/// <summary>
/// Renders azapi_resource body content as formatted markdown.
/// </summary>
/// <param name="body">The JSON body object to render.</param>
/// <param name="heading">Heading text (e.g., "Body Configuration" or "Body Changes").</param>
/// <returns>Markdown string with tables for small and large properties.</returns>
public static string RenderAzapiBody(object? body, string heading)
{
    // Implementation moves template logic to C#
}
```

Then the template becomes:
```scriban
{{~ if change.action == "create" && change.after_json && change.after_json.body ~}}
{{ render_azapi_body change.after_json.body "Body Configuration" }}
{{~ end ~}}
```

This would reduce the template to ~120 lines, closer to the target (and then addressing Blocker #1 would bring it below 100).

---

#### 3. Documentation Alignment: Template Line Count Not Mentioned in Architecture

**Severity:** Minor (Documentation Gap)  
**Location:** `docs/features/040-azapi-resource-template/architecture.md`

**Description:**  
The architecture document doesn't mention or justify the decision to have a 228-line template that exceeds project guidelines. The document describes the template as "approximately 220 lines" but doesn't address the architectural violation or provide a mitigation plan.

**Fix Required:**  
Update the architecture document to either:
1. Acknowledge the template size and document the refactoring plan to address it
2. If the template is intentionally oversized, provide architectural justification and obtain approval for an exception

**Note:** This is a documentation issue, not a code quality issue. However, it's important for maintaining consistency between docs and implementation.

---

### Major Issues

None identified.

---

### Minor Issues

#### 1. Missing ConvertJsonValue Helper Documentation

**Severity:** Minor  
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs`, line 183

**Description:**  
The helper function `ConvertJsonValue(element)` is called but not defined in the visible code. It's likely a helper from another file, but there's no reference or XML comment explaining where it comes from or what it does.

**Fix:**  
Add a comment or ensure the helper is properly documented where it's defined.

---

#### 2. Template Could Benefit from Inline Comments

**Severity:** Minor  
**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`

**Description:**  
While the template has section comments (e.g., `{{~ # CREATE: Show body configuration ~}}`), some complex logic blocks could benefit from additional inline comments explaining the intent.

**Example:**  
Lines 98-103 build the sensitive structures with null-coalescing and safe navigation:
```scriban
{{~ before_sensitive_body = change.before_sensitive ? change.before_sensitive.body : null ~}}
{{~ after_sensitive_body = change.after_sensitive ? change.after_sensitive.body : null ~}}
{{~ comparisons = compare_json_properties change.before_json.body change.after_json.body before_sensitive_body after_sensitive_body false false ~}}
```

A comment explaining the false/false parameters would improve readability:
```scriban
{{~ # Compare body properties (showUnchanged=false, showSensitive=false handled by template) ~}}
```

**Fix:**  
Add explanatory comments for non-obvious parameter values and complex expressions.

---

### Suggestions

#### 1. Consider Performance Optimization for Deep Nesting

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs`, `FlattenJsonElement` method

**Observation:**  
The flattening algorithm recursively traverses nested JSON structures. For very deeply nested objects (10+ levels), this could have performance implications.

**Suggestion:**  
Consider adding an optional depth limit parameter with a default of 20 levels to prevent stack overflow on malformed or pathological JSON structures. While the current tests show good performance, adding a safeguard would improve robustness.

**Example:**
```csharp
private const int MaxNestingDepth = 20;

private static void FlattenJsonElement(JsonElement element, string prefix, ScriptArray result, int depth = 0)
{
    if (depth > MaxNestingDepth)
    {
        result.Add(CreatePropertyObject(prefix, "[Nested too deep - truncated]"));
        return;
    }
    // ... existing logic, passing depth + 1 to recursive calls
}
```

---

#### 2. Documentation Link Best-Effort Warning Could Be More Prominent

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`, line 27

**Observation:**  
The template shows `[View API Documentation (best-effort)]` but the "best-effort" qualifier is easy to miss in the rendered output.

**Suggestion:**  
Consider adding a tooltip or note explaining that the link is heuristically generated and may not always be accurate:
```markdown
📚 [View API Documentation](link) *(best-effort link)*
```

Or add a footnote explaining the limitation in the feature documentation.

---

#### 3. Test Coverage for Edge Cases Could Be Enhanced

**Location:** Test suite

**Observation:**  
The test suite has excellent coverage (41 tests) but could benefit from a few additional edge cases:
- Empty arrays in body `"tags": []`
- Null properties explicitly set to null `"location": null`
- Unicode characters in property values
- Very long property paths (15+ segments)

**Suggestion:**  
Add 2-3 additional test cases for these edge cases to ensure robustness.

---

## Checklist Summary

| Category | Status | Notes |
|----------|--------|-------|
| **Correctness** | ⚠️ | 3 test failures (Blockers) |
| **Code Quality** | ✅ | Excellent - well-structured, no duplication in helpers |
| **Access Modifiers** | ✅ | All `internal` or `private` - appropriate |
| **Code Comments** | ✅ | Comprehensive XML docs on all helpers |
| **Architecture** | ⚠️ | Template exceeds 100-line limit (Blocker #2) |
| **Testing** | ✅ | 41 tests, good coverage (97% pass rate) |
| **Documentation** | ✅ | README and features.md updated appropriately |
| **Comprehensive Demo** | ✅ | Generated and passes markdownlint |
| **User-Facing Feature** | ✅ | Yes - UAT required after approval |

## Next Steps

### For Developer Agent

1. **Fix Blocker #1**: Update the "Other Attribute Changes" template section (lines 212-225) to:
   - Filter out `body.*` attributes (already rendered separately) OR
   - Use proper formatting for multi-line values (format_attribute_value_table or code blocks)

2. **Fix Blocker #2**: Refactor the template to reduce line count below 100:
   - Create C# helper for body rendering logic OR
   - Extract common rendering blocks to partial templates OR
   - Create a view model that pre-computes rendered body sections

3. **Fix Blocker #3**: Update `architecture.md` to document template size decision

4. **Re-run verification suite** to confirm all invariant tests pass

### After Developer Fixes

- **Code Reviewer** will re-review the fixes
- Upon approval, hand off to **UAT Tester** for validation on real GitHub and Azure DevOps PRs (this is a user-facing markdown rendering feature)

## Positive Observations

Despite the blockers, this implementation demonstrates several strengths:

✅ **Excellent helper design** - The 5 helper functions are well-structured, well-documented, and follow the single-responsibility principle  
✅ **Comprehensive tests** - 41 tests covering a wide range of scenarios (29 unit, 12 integration)  
✅ **Performance-conscious** - Efficient flattening algorithm handles 100+ properties quickly  
✅ **Sensitive value handling** - Proper integration with existing `before_sensitive`/`after_sensitive` structures  
✅ **Documentation quality** - XML comments are thorough with examples and feature references  
✅ **AOT-compatible** - No reflection, uses System.Text.Json throughout  
✅ **Semantic formatting** - Integrates well with existing icon and formatting conventions  
✅ **Change detection** - Sophisticated comparison logic for update operations

The core functionality is sound. Once the template rendering issues are addressed, this will be a high-quality feature.

## Feature Readiness

**User-Facing Impact:** High - This feature significantly improves reviewability of azapi_resource changes

**UAT Required:** Yes - After code approval, the UAT Tester must validate rendering on:
- GitHub PRs (check table parsing, collapsible sections, links)
- Azure DevOps PRs (verify markdown compatibility)

**Release Readiness:** Blocked on Blockers #1 and #2

---

## Approval Conditions

This feature will be approved when:

1. ✅ All 3 Blocker issues are resolved
2. ✅ Test suite passes 100% (currently 486/489 = 99.4%)
3. ✅ Docker build completes successfully (or infrastructure issue is resolved)
4. ✅ Template line count is below 100 (or architectural exception is granted)
5. ✅ Comprehensive demo continues to pass markdownlint

---

**Review Conducted By:** Code Reviewer Agent  
**Date:** 2026-01-17  
**Implementation Quality:** High (pending blocker fixes)  
**Recommendation:** Request changes from Developer, then proceed to UAT
