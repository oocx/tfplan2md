# UAT Report: Azure DevOps Variable Group Template

**Feature:** #039 - Custom template for Azure DevOps variable groups  
**Date:** 2026-01-15  
**Tester:** UAT Tester Agent  
**Environment:** GitHub Actions (Cloud)

## Executive Summary

**Status:** ⚠️ **FAILED** - Critical rendering bug found

The Azure DevOps variable group template successfully displays variables in a unified table format with secret value masking. However, a critical rendering issue was discovered where null/missing "after" values in diff columns show confusing empty "+ " prefixes.

## Test Execution

### Test Artifact

- **Source:** `examples/azuredevops/terraform_plan2.json`
- **Generated:** `/tmp/uat-variable-groups.md`
- **Command:** `dotnet run --project src/Oocx.TfPlan2Md/ -- examples/azuredevops/terraform_plan2.json -o /tmp/uat-variable-groups.md`
- **Result:** Successfully generated 129 lines of markdown

### Test Data Overview

The test plan contains a variable group replace operation (delete+create) with:

**Before state:**
- Regular variables: `APP_VERSION` (1.0.0, enabled=false), `ENVIRONMENT` (development, enabled=false)
- Secret variables: `SECRET_KEY` (supersecret, enabled=false)

**After state:**
- Regular variables: `APP_VERSION` (1.0.0, enabled=null), `ENV` (Production, enabled=null)
- Secret variables: `SECRET_KEY` (supersecret, enabled=null)

**Expected changes:**
- ➕ Added: `ENV`
- 🔄 Modified: `APP_VERSION` (enabled: false → null), `SECRET_KEY` (enabled: false → null)
- ❌ Removed: `ENVIRONMENT`

## Validation Results

### ✅ Passed Checks

1. **Summary Line Format**
   ```markdown
   ♻️ azuredevops_variable_group example — example-variables
   ```
   - ✅ Replace icon (♻️) renders correctly
   - ✅ Resource name `example` is bold and code-formatted
   - ✅ Variable group name displayed with identifier icon
   - ✅ Summary is clear and informative

2. **Variable Group Metadata**
   ```markdown
   **Variable Group:** <code>example-variables</code>
   **Description:** <code>Variable group for example pipeline</code>
   ```
   - ✅ Variable group name is code-formatted
   - ✅ Description is code-formatted
   - ✅ Labels are bold plain text
   - ✅ Clean, readable layout

3. **Variables Table Structure**
   ```markdown
   | Change | Name | Value | Enabled | Content Type | Expires |
   | ------ | ---- | ----- | ------- | ------------ | ------- |
   ```
   - ✅ Table header is plain text (correct per spec)
   - ✅ Table renders without broken rows
   - ✅ All expected columns present
   - ✅ Proper alignment

4. **Change Indicators**
   - ✅ ➕ for added variable (`ENV`)
   - ✅ 🔄 for modified variables (`APP_VERSION`, `SECRET_KEY`)
   - ✅ ❌ for removed variable (`ENVIRONMENT`)
   - ✅ Visually distinct and clear

5. **Secret Value Masking**
   ```markdown
   | 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | ...
   ```
   - ✅ Secret value correctly shown as `(sensitive / hidden)`
   - ✅ Code-formatted (backticks)
   - ✅ No actual secret value exposed
   - ✅ Security requirement met

6. **Regular Variable Values**
   ```markdown
   | ➕ | `ENV` | `Production` | - | - | - |
   | ❌ | `ENVIRONMENT` | `development` | `false` | - | - |
   ```
   - ✅ Added variable shows value without prefix
   - ✅ Removed variable shows value without prefix
   - ✅ Empty/null values shown as `-` (plain text)
   - ✅ Values are code-formatted where appropriate

7. **Markdown Linting**
   - ✅ Zero linting errors from markdownlint-cli2
   - ✅ Valid markdown syntax
   - ✅ No escaped characters visible

### ❌ Failed Checks

#### **CRITICAL: Confusing Null Value Rendering in Diff Columns**

**Issue:** When a variable attribute changes from a value to null/absent, the rendering shows:

```markdown
| 🔄 | `APP_VERSION` | <code>1.0.0</code> | <code>...- false...</span><br>...+ </span></code> | - | - |
```

The "Enabled" column shows:
- Before: `- false` (correct, with red background)
- After: `+ ` (WRONG - empty after the + sign)

**Expected Behavior:**

According to the specification (specification.md, line 29-30):
> For modified variables: show before/after values with `-` and `+` prefixes for changed attributes
> For modified variables: show single value without prefix for unchanged attributes

And from the test plan (uat-test-plan.md, lines 89-92):
> **Expected:**
> - Value column shows before/after diff with `-` and `+` prefixes
> - Empty attributes show as `-` (plain text, not code-formatted)

**What Should Render:**

Option A (Show null explicitly):
```markdown
- false
+ (null)
```

Option B (Show dash for null in after):
```markdown
- false
+ -
```

Option C (Don't show diff if after is null - treat as single value):
```markdown
false
```

**Impact:** HIGH
- Users see incomplete diffs with confusing empty "+ " prefixes
- Unclear whether value was deleted, set to null, or rendering bug
- Affects both regular and secret variables
- Reduces confidence in the report accuracy

**Affected Variables:**
- `APP_VERSION` - enabled: false → null
- `SECRET_KEY` - enabled: false → null

**Root Cause:**
The template's diff rendering logic doesn't handle null/missing values in the "after" state. The styling code generates `+ ` but no value text follows.

### ⚠️ Warning Items

1. **Verbose Styled Code Blocks in Table Cells**
   
   The diff rendering uses extensive inline HTML styling:
   ```html
   <code style="display:block; white-space:normal; padding:0; margin:0;">
     <span style="background-color: #fff5f5; border-left: 3px solid #d73a49; ...">
       - <span style="background-color: #ffc0c0; ...">false</span>
     </span><br>
     <span style="background-color: #f0fff4; border-left: 3px solid #28a745; ...">
       + 
     </span>
   </code>
   ```
   
   **Concern:** This is significantly more complex than other templates which use simpler formatting:
   ```markdown
   - `value1`<br>+ `value2`
   ```
   
   **Impact:** MEDIUM
   - May not render consistently across all markdown viewers
   - Increases output size considerably
   - Not tested in Azure DevOps rendering (UAT environment limitation)
   
   **Recommendation:** Consider simplifying to match other templates unless advanced styling is required.

## Platform-Specific Validation

### GitHub (Markdown Preview)

✅ **Validation performed via markdown linting**
- Markdown syntax is valid
- No HTML escaping issues detected
- Tables structure is correct

⚠️ **Visual rendering not tested**
- Cannot create actual GitHub PR in sandboxed environment
- Cannot verify collapsible sections expand/collapse
- Cannot verify HTML styling renders as expected
- Cannot verify emoji display consistency

### Azure DevOps

❌ **Not tested**
- Cannot access Azure DevOps from sandboxed environment
- Cannot verify HTML `<code>` tag rendering
- Cannot verify table rendering without horizontal scrolling
- Cannot verify before/after diff display with `<br>` tags

## Comparison with Specification

### Coverage Analysis

| Requirement | Status | Notes |
|------------|--------|-------|
| Specialized template for azuredevops_variable_group | ✅ Pass | Template exists and activates |
| Display variable group metadata | ✅ Pass | Name and description shown |
| Unified table for all variables | ✅ Pass | Both regular and secret vars in one table |
| Secret value masking | ✅ Pass | Shows "(sensitive / hidden)" |
| Secret metadata visible | ⚠️ Partial | Visible but diff rendering broken for null |
| Change indicators (➕🔄❌⏺️) | ✅ Pass | All indicators present |
| Semantic matching by name | ✅ Pass | Correctly identified add/modify/remove |
| Before/after diffs with -/+ | ❌ Fail | Null values show empty "+ " |
| Empty values as `-` | ⚠️ Partial | Works for initially empty, not for null in diff |
| Support create/update/delete | ⚠️ Unknown | Only replace (delete+create) tested |
| Large value handling | ⚠️ Not tested | No large values in test data |
| Key Vault blocks | ⚠️ Not tested | No Key Vault data in test plan |

### Success Criteria Review

From specification.md lines 299-322:

- [x] Template created at correct path
- [x] All variables displayed in unified table
- [x] Secret variables show metadata with "(sensitive / hidden)" for value
- [x] Variables categorized correctly (Added/Modified/Removed)
- [ ] ❌ Modified variables show before/after correctly (FAILED for null)
- [ ] ⚠️ Large variable values handled (NOT TESTED)
- [x] Empty/null initial values displayed as `-`
- [ ] ⚠️ Create/update/delete operations (only replace tested)
- [x] Variable group metadata displayed
- [x] Summary line includes variable group name
- [ ] ⚠️ Key Vault blocks displayed (NOT TESTED)
- [x] Template follows Report Style Guide formatting

**Overall Coverage:** 8/12 criteria passed, 3 not tested, 1 failed

## Issues Found

### Issue #1: Empty After Values in Diff Columns (CRITICAL)

**Severity:** HIGH  
**Type:** Rendering Bug  
**Location:** Variable group template - diff column rendering for null/missing values

**Description:**
When a variable attribute exists in "before" but is null/missing in "after", the diff rendering shows an incomplete "+ " prefix with no value text.

**Example:**
```markdown
- false
+ 
```

**Expected:**
```markdown
- false
+ (null)
```
or
```markdown
- false
+ -
```

**Reproduction:**
1. Use terraform_plan2.json which has enabled=false before, enabled=null after
2. Generate markdown
3. Observe "Enabled" column for APP_VERSION and SECRET_KEY

**Impact:**
- Users cannot understand what the "after" state is
- Confusing and looks like rendering error
- Reduces trust in the report

**Recommendation:**
Fix the template to handle null/missing values in diff rendering. Options:
1. Show "(null)" or "-" for null values in after state
2. Don't show diff styling if after value is null - just show before value
3. Treat null as unchanged and show single value without diff

### Issue #2: Incomplete Test Coverage (MEDIUM)

**Severity:** MEDIUM  
**Type:** Test Gap  
**Location:** Test plan artifact selection

**Description:**
The test plan only exercises replace (delete+create) operations. The following scenarios are not tested:

- Pure update operation (modify existing variable group)
- Pure create operation (new variable group)
- Pure delete operation (remove variable group)
- Large variable values (>100 chars or multi-line)
- Key Vault integration blocks

**Impact:**
- Cannot verify template works correctly for all operations
- Unknown if create/delete operations render as specified
- Large values might have rendering issues
- Key Vault feature completely untested

**Recommendation:**
1. Create additional test artifacts or enhance terraform_plan2.json
2. Add test case with large connection string variable
3. Add test case with Key Vault block
4. Test pure create/update/delete operations

## Code Review Cross-Check

Comparing with approved code review (code-review.md):

✅ **Code review identified same concerns:**
- Line 85: Null value handling flagged as potential issue - **CONFIRMED BUG**
- Line 168: HTML styling complexity noted - **CONFIRMED WARNING**

✅ **Code review did not flag:**
- Security of secret masking - **VALIDATED as working correctly**
- Table structure - **VALIDATED as correct**

## Recommendations

### Immediate Actions Required (Before Release)

1. **FIX CRITICAL BUG**: Resolve null value rendering in diff columns
   - Implement proper null/missing value display
   - Test with terraform_plan2.json
   - Verify visually in actual PR

2. **ENHANCE TEST COVERAGE**: Add missing test scenarios
   - Create test with pure update operation
   - Create test with large variable values
   - Create test with Key Vault blocks
   - Document in test-plan.md

3. **VISUAL VALIDATION**: Perform real PR testing
   - Create GitHub PR with generated markdown
   - Create Azure DevOps PR with generated markdown
   - Verify rendering on both platforms
   - Document screenshots in uat-report.md (this file)

### Nice-to-Have Improvements

1. **Simplify Styling**: Consider using simpler markdown formatting
   - Replace verbose HTML spans with `- <code>value</code><br>+ <code>value</code>`
   - Test if simpler format renders acceptably on both platforms

2. **Add Empty State Messages**: Improve clarity for edge cases
   - "No variables defined" if variable arrays are empty
   - "No Key Vault integration" if key_vault blocks absent

## Decision

**UAT Status:** ❌ **FAILED**

**Blocker Issues:**
1. Critical rendering bug with null values in diff columns (Issue #1)

**Recommendation:**
- **DO NOT RELEASE** in current state
- Hand off to Developer to fix Issue #1
- Re-run UAT after fix with enhanced test coverage
- Perform visual validation in real PRs before final approval

**Next Steps:**
1. Developer fixes null value rendering
2. Developer adds test cases for missing scenarios (optional but recommended)
3. UAT Tester re-runs full UAT with enhanced artifacts
4. UAT Tester validates in real GitHub + Azure DevOps PRs
5. If passed, hand off to Release Manager

## Appendix: Generated Output Sample

### Variable Group Section (Full)

```markdown
<details open style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
<summary>♻️ azuredevops_variable_group <b><code>example</code></b> — <code>🆔 example-variables</code></summary>
<br>

### ♻️ azuredevops_variable_group.example

**Variable Group:** <code>example-variables</code>

**Description:** <code>Variable group for example pipeline</code>

#### Variables

| Change | Name | Value | Enabled | Content Type | Expires |
| ------ | ---- | ----- | ------- | ------------ | ------- |
| ➕ | `ENV` | `Production` | - | - | - |
| 🔄 | `APP_VERSION` | <code>1.0.0</code> | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- <span style="background-color: #ffc0c0; color: #24292e;">false</span></span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ </span></code> | - | - |
| 🔄 | `SECRET_KEY` | `(sensitive / hidden)` | <code style="display:block; white-space:normal; padding:0; margin:0;"><span style="background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">- <span style="background-color: #ffc0c0; color: #24292e;">false</span></span><br><span style="background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: inline-block; padding-left: 8px; margin-left: 0;">+ </span></code> | - | - |
| ❌ | `ENVIRONMENT` | `development` | `false` | - | - |

</details>
```

### Key Observations

**What Works:**
- Clean summary line
- Proper table structure
- Secret masking working
- Change indicators clear
- Metadata formatting correct

**What's Broken:**
- Enabled column diff: `- false<br>+ ` (empty after +)
- Both APP_VERSION and SECRET_KEY affected

---

**Report Generated:** 2026-01-15 09:25 UTC  
**Generated By:** UAT Tester Agent  
**Test Duration:** ~5 minutes  
**Exit Code:** 1 (FAILED)
