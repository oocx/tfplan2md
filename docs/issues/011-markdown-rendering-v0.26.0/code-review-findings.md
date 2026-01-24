# Code Review: Markdown Rendering Fix (v0.26.0)

## Executive Summary

**Status:** ⚠️ **INCOMPLETE - Critical Issues Remain**

The fix addresses some issues but **fails to meet the core requirement** of generating valid markdown. Markdownlint validation still fails with MD012 error.

## Issues Found

### 🔴 BLOCKER: MD012 Violation Still Exists

**Test:** `markdownlint` on comprehensive demo output  
**Result:** FAIL at line 359  
**Error:** `MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]`

**Root Cause:**
- [default.sbn#L58-L61](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn#L58-L61) has a blank line at EOF
- This creates: `---\n\n` at the end of the document
- `NormalizeHeadingSpacing()` does `TrimEnd()` then adds `\n`, but the blank line before the trim creates the double newline

**Impact:** The core requirement "we must absolutely ensure that we do not produce invalid markdown" is NOT MET.

### 🟡 Test Gap: No Actual Linting Integration

**Current State:**
- `Render_ComprehensiveDemo_NoMultipleBlankLines` uses regex `\n{3,}` to detect multiple blank lines
- This regex does NOT match the MD012 pattern because it requires 3+ **consecutive** newlines without content

**The Gap:**
- MD012 checks for 2+ consecutive **blank lines** (lines with only whitespace)
- `---` is content, so `</details>\n\n---\n\n` is NOT caught by `\n{3,}`
- The test passes but `markdownlint` fails

**Impact:** False sense of security - tests pass but markdown is still invalid.

## What Works

### ✅ Table Row Blank Line Removal
- Regex `(?<=\|[^\n]*)\n\s*\n(?=[ \t]*\|)` successfully removes blank lines between table rows
- [role_assignment.sbn](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn) template fix (removed blank line after `{{ else }}`) prevents the issue at the source
- Test `Render_ComprehensiveDemo_NoBlankLinesInTables` correctly validates this

### ✅ Table Indentation Removal
- Regex `\n[ \t]+(\|)` removes indentation from table rows
- Prevents tables from being treated as code blocks
- Good defensive measure

### ✅ Table Count Validation
- `Render_ComprehensiveDemo_TableCountMatchesResources` ensures all resource tables are parsed
- Catches cases where tables break into text blocks
- Effective structural validation

## Required Fixes

### Fix 1: Remove Blank Line from default.sbn Template

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn#L61](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn#L61)

**Change:**
```scriban
---
{{ end }}
{{ end }}

```

To:
```scriban
---
{{ end }}
{{ end }}
```

Remove the trailing blank line at EOF.

### Fix 2: Update Blank Line Detection Test

**File:** [src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs](../../../src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs)

**Current:**
```csharp
var matches = Regex.Matches(markdown, @"\n{3,}");
```

**Problem:** This only catches 3+ consecutive newlines, missing the MD012 case.

**Proposed Fix:**
```csharp
// Check for 2+ consecutive blank lines (MD012 rule)
var lines = markdown.Split('\n');
var consecutiveBlanks = 0;
var maxConsecutiveBlanks = 0;
for (var i = 0; i < lines.Length; i++)
{
    if (string.IsNullOrWhiteSpace(lines[i]))
    {
        consecutiveBlanks++;
        maxConsecutiveBlanks = Math.Max(maxConsecutiveBlanks, consecutiveBlanks);
    }
    else
    {
        consecutiveBlanks = 0;
    }
}

maxConsecutiveBlanks.Should().BeLessThan(2, 
    "because multiple consecutive blank lines violate MD012");
```

### Fix 3: Add Actual markdownlint Integration Test

**File:** [src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs](../../../src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs)

**Add:**
```csharp
[Fact]
public void Render_ComprehensiveDemo_PassesMarkdownLint()
{
    // This test requires markdownlint-cli2 to be installed
    // Skip if not available
    var hasMarkdownLint = CheckMarkdownLintAvailable();
    if (!hasMarkdownLint)
    {
        return; // Skip test
    }
    
    var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
    var builder = new ReportModelBuilder();
    var model = builder.Build(plan);
    var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

    var markdown = renderer.Render(model);
    
    // Write to temp file
    var tempFile = Path.GetTempFileName() + ".md";
    File.WriteAllText(tempFile, markdown);
    
    try
    {
        // Run markdownlint
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "markdownlint-cli2",
            Arguments = $"\"{tempFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var errors = process.StandardError.ReadToEnd();
        
        process.ExitCode.Should().Be(0, 
            $"markdownlint should pass but found errors:\n{output}\n{errors}");
    }
    finally
    {
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }
}
```

## Test Coverage Assessment

### Current Tests

| Test | Purpose | Effectiveness |
|------|---------|---------------|
| `Render_BreakingPlan_EscapesPipesAndAsterisks` | Character escaping | ✅ Good |
| `Render_BreakingPlan_ReplacesNewlinesInTableCells` | Newline handling | ✅ Good |
| `Render_BreakingPlan_ParsesTablesWithMarkdig` | Table structure | ⚠️ Too lenient |
| `Render_DefaultPlan_HeadingsAreParsed` | Heading structure | ✅ Good |
| `Render_ComprehensiveDemo_RendersToHtmlWithoutRawMarkdown` | HTML conversion | ✅ Good |
| `Render_ComprehensiveDemo_NoMultipleBlankLines` | MD012 detection | ❌ **FAILS** - Wrong regex |
| `Render_ComprehensiveDemo_NoBlankLinesInTables` | Table blank lines | ✅ Good |
| `Render_ComprehensiveDemo_TableCountMatchesResources` | Table counting | ✅ Excellent |

### Missing Tests

1. **Actual `markdownlint` integration** - Currently no test runs the real linter
2. **Template-specific isolation tests** - No tests for individual templates in isolation
3. **Section separator spacing** - No test for correct spacing around `---`
4. **End-of-file handling** - No test for correct EOF newline handling

## Recommendations

### Immediate (Before Merge)

1. ✅ **Fix default.sbn template** - Remove trailing blank line (2 min)
2. ✅ **Fix blank line detection test** - Use correct MD012 logic (10 min)  
3. ✅ **Verify with markdownlint** - Run manual validation (2 min)

### Short Term (Same Sprint)

4. **Add markdownlint integration test** - Real linter validation (30 min)
5. **Add EOF handling test** - Verify single newline at EOF (15 min)
6. **Document template guidelines** - How to avoid spacing issues (30 min)

### Long Term (Next Sprint)

7. **Template isolation framework** - Test each template independently
8. **CI/CD linting enforcement** - Fail builds on invalid markdown
9. **Template validator tool** - Pre-commit hook for template changes

## Conclusion

The current fix is **incomplete and does not meet requirements**. While it addresses table-specific issues successfully, it fails the core requirement of producing valid markdown.

**Before this can be merged:**
- [ ] Fix default.sbn template (remove trailing blank)
- [ ] Fix blank line detection test (use correct MD012 logic)
- [ ] Verify markdownlint passes on comprehensive demo
- [ ] All 170 tests must pass

**Estimated time to complete:** 20-30 minutes

**Risk if merged as-is:** HIGH - Will produce invalid markdown in production, violating the stated absolute requirement.
