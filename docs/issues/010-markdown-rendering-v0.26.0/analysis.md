# Issue: Markdown Rendering Errors in v0.26.0

## Problem Description

Version 0.26.0 was released with the goal of implementing markdown quality validation and linting (feature: docs/features/009-markdown-quality-validation/specification.md), but it still produces markdown that fails validation and renders incorrectly.

**Two distinct issues identified:**
1. Multiple consecutive blank lines (MD012 error at line 365)
2. Role assignment tables have blank lines between rows, breaking table rendering

## Steps to Reproduce

```bash
docker run --rm oocx/tfplan2md:0.26.0 /examples/comprehensive-demo/plan.json \
  --principals /examples/comprehensive-demo/demo-principals.json | markdownlint -s
```

**Observe:**
```
stdin:365 error MD012/no-multiple-blanks Multiple consecutive blank lines [Expected: 1; Actual: 2]
```

Additionally, view the generated markdown and observe role assignment tables with blank lines between rows.

## Expected Behavior

- Markdown output should pass `markdownlint` validation without errors
- Tables should render correctly with NO blank lines between rows
- Maximum 1 blank line between sections

## Actual Behavior

### Issue 1: MD012 Error - Multiple Consecutive Blank Lines

**Location:** Line 365 in comprehensive demo output

**Pattern:**
```
</details>
<blank line>
---
<blank line>
```

This creates 2 consecutive blank lines which violates MD012 rule.

### Issue 2: Role Assignment Table Rendering

**User Report:** "a table that is not rendered correctly because of additional new lines in the table of a role assignment"

**Example Output:**
```markdown
#### ❌ module.security.azurerm_role_assignment.obsolete

**Summary:** remove `Reader` on `rg-old` from `00000000-0000-0000-0000-000000000005`

<details>
| Attribute | Value |
| ----------- | ------- |

| `scope` | rg-old in subscription 12345678-1234-1234-1234-123456789012 |

| `role_definition_id` | Reader \(acdd72a7-3385-48ef-bd42-f606fba81ae7\) |

| `principal_id` | 00000000-0000-0000-0000-000000000005 \[00000000-0000-0000-0000-000000000005\] |

</details>
```

**Problem:** Blank lines appear between each table row, completely breaking markdown table rendering.

## Root Cause Analysis

### Affected Components

#### Issue 1: Blank Line Normalization

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L367-L385](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L367-L385)

**Method:** `NormalizeHeadingSpacing()`

**Line 370:**
```csharp
markdown = Regex.Replace(markdown, @"(\n[ \t]*){2,}", "\n\n");
```

**What's Broken:**
- Regex pattern `(\n[ \t]*){2,}` is supposed to collapse multiple blank lines to single blank line
- Pattern matches: 2+ occurrences of (newline followed by optional spaces/tabs)
- Replacement: exactly 2 newlines (1 blank line)

**Why It's Failing:**
The pattern doesn't catch the case at line 365 because there's CONTENT between the newlines (`---`):
```
</details>\n
\n
---\n
\n
```

The pattern expects consecutive newlines with only whitespace between them, but `---` is actual content. So the regex sees:
- After `</details>`: one blank line (valid)
- After `---`: one blank line (valid)
- Total: 2 blank lines with content in between (not caught by regex)

#### Issue 2: Role Assignment Table Template

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn#L60-L69](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn#L60-L69)

**Template Code:**
```scriban
{{ else }}

| Attribute | Value |
| ----------- | ------- |
{{~ for attr in attributes ~}}
{{~ if attr == "role_definition_name" ~}}{{ continue }}{{~ end ~}}
{{- value = format_value(attr, attribute_value(after_json, attr), after_json, principal_mapper) -}}
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |
{{~ end }}
```

**What's Broken:**
- Line 61: `{{ else }}` outputs nothing
- Line 62: **BLANK LINE IN TEMPLATE SOURCE** - outputs `\n` to final markdown
- Line 63: Table header - outputs `| Attribute | Value |\n`
- Line 64: Table separator - outputs `| ----------- | ------- |\n`
- Line 65: `{{~ for attr in attributes ~}}` - SHOULD strip whitespace before `{{~`
- Lines 66-67: Logic and variable assignment
- Line 68: Table row - outputs `| row data |\n`
- Line 69: `{{~ end }}` - SHOULD strip whitespace before `{{~`

**Why It's Failing:**
The `{{~` operator in Scriban strips whitespace **to the left** (before the tag). However:
1. After line 64 (table separator), there's a `\n`
2. Line 65 with `{{~ for` should strip that `\n`
3. But it's NOT stripping it, resulting in a blank line after the separator

Similarly, between iterations:
1. Line 68 outputs table row with `\n`
2. Next iteration or `{{~ end }}` should strip that `\n`
3. But it's NOT stripping it, resulting in blank lines between rows

**Hypothesis:** Scriban's `{{~` operator may only strip horizontal whitespace (spaces, tabs), NOT newlines. This would explain why blank lines persist.

**Counter-evidence:** The firewall_network_rule_collection.sbn template uses identical pattern and works correctly:
```scriban
| header |
{{~ for rule in after_json.rule ~}}
| {{ rule.name }} |
{{~ end ~}}
```

This suggests the issue is specific to role_assignment.sbn, possibly due to:
- The blank line on line 62 in the `{{ else }}` branch
- Interaction between variable assignment on line 67 and table row output on line 68
- How `attributes` collection is populated or iterated

### Why It Happened

1. **Incomplete Testing:** v0.26.0 added `MarkdownValidationTests.cs` but did NOT:
   - Run actual `markdownlint` command on generated output
   - Test comprehensive demo example with linting
   - Test role assignment template specifically

2. **Regex Pattern Too Specific:** The `NormalizeHeadingSpacing()` regex only collapses consecutive newlines with whitespace between them, not newlines separated by content like `---`

3. **Template Whitespace Issues:** Role assignment template has subtle whitespace/newline handling bugs that weren't caught by tests

## Suggested Fix Approach

### Fix 1: Improve Blank Line Normalization

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L370](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L370)

**Current:**
```csharp
markdown = Regex.Replace(markdown, @"(\n[ \t]*){2,}", "\n\n");
```

**Proposed:**
```csharp
// Collapse runs of 3+ newlines (2+ blank lines) to exactly 2 newlines (1 blank line)
markdown = Regex.Replace(markdown, @"\n\n\n+", "\n\n");
```

**Rationale:** Simpler pattern that catches ALL cases of multiple blank lines, regardless of content between them.

### Fix 2: Fix Role Assignment Template

**File:** [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn#L60-L69](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn#L60-L69)

**Option A: Remove blank line in template source**

Change lines 61-65 from:
```scriban
{{ else }}

| Attribute | Value |
| ----------- | ------- |
{{~ for attr in attributes ~}}
```

To:
```scriban
{{ else }}
| Attribute | Value |
| ----------- | ------- |
{{~ for attr in attributes ~}}
```

**Option B: Use stricter whitespace control**

Change line 68 from:
```scriban
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |
```

To:
```scriban
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |
{{~ end ~}}
```

Wait, line 69 already has `{{~ end }}`. The issue is we need to strip the newline AFTER line 68. Change to:
```scriban
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |{{~ "" ~}}
{{~ end }}
```

Actually, simpler approach - use `~}}` at end of line 68:
```scriban
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |~}}
```

No wait, that's not valid Scriban. The correct approach is:

```scriban
{{- value = format_value(attr, attribute_value(after_json, attr), after_json, principal_mapper) -}}
| `{{ attr | escape_markdown }}` | {{ string.strip(value) | escape_markdown }} |{{~""~}}
```

This outputs the row, then `{{~""~}}` strips whitespace on both sides while outputting nothing.

**Option C: Post-process template output (preferred)**

In [MarkdownRenderer.cs#L289-L293](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs#L289-L293), the `RenderResourceWithTemplate` method already has:

```csharp
// Post-process: collapse blank lines that appear before table rows (leading '|')
rendered = Regex.Replace(rendered, @"\n\s*\n(?=\|)", "\n");
```

This removes blank lines BEFORE table rows. We need to ALSO remove blank lines WITHIN tables:

```csharp
// Post-process: collapse blank lines that appear before table rows (leading '|')
rendered = Regex.Replace(rendered, @"\n\s*\n(?=\|)", "\n");

// Remove blank lines between table rows
rendered = Regex.Replace(rendered, @"(?<=\|[^\n]*)\n\s*\n(?=\|)", "\n");
```

This pattern:
- `(?<=\|[^\n]*)` - lookbehind for a line ending with `|` (table row)
- `\n\s*\n` - blank line (newline, optional whitespace, newline)
- `(?=\|)` - lookahead for line starting with `|` (next table row)

### Fix 3: Add Comprehensive Linting Test

**File:** [tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs](../../../tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs)

**Add new test:**
```csharp
[Fact]
public void Render_ComprehensiveDemo_PassesMarkdownLint()
{
    var json = File.ReadAllText("../../examples/comprehensive-demo/plan.json");
    var principals = File.ReadAllText("../../examples/comprehensive-demo/demo-principals.json");
    var principalMapper = new PrincipalMapper(principals);
    
    var parser = new TerraformPlanParser();
    var plan = parser.Parse(json);
    var builder = new ReportModelBuilder();
    var model = builder.Build(plan);
    
    var renderer = new MarkdownRenderer(principalMapper);
    var markdown = renderer.Render(model);
    
    // Run markdownlint on output
    var result = RunMarkdownLint(markdown);
    result.Should().Pass("comprehensive demo output must pass markdownlint");
}
```

## Related Tests

Tests that should pass after the fix:

- [x] Existing: `MarkdownValidationTests.Render_BreakingPlan_EscapesPipesAndAsterisks()`
- [x] Existing: `MarkdownValidationTests.Render_BreakingPlan_ReplacesNewlinesInTableCells()`
- [ ] **NEW:** `MarkdownValidationTests.Render_ComprehensiveDemo_PassesMarkdownLint()`
- [ ] **NEW:** `MarkdownValidationTests.Render_NormalizesMultipleBlankLines()`
- [ ] **NEW:** `RoleAssignmentTemplateTests.Render_HasNoBlankLinesBetweenTableRows()`

## Additional Context

- **Release:** [v0.26.0](https://github.com/oocx/tfplan2md/releases/tag/v0.26.0)
- **Feature Spec:** [docs/features/009-markdown-quality-validation/specification.md](../../features/009-markdown-quality-validation/specification.md)
- **Markdownlint Config:** [.markdownlint.json](../../../.markdownlint.json)
- **Related Commit:** [7cc7632](https://github.com/oocx/tfplan2md/commit/7cc7632d2f7e3b40385db6bb88e32c8c83035e7d) - feat: implement markdown quality validation and linting

## Investigation Notes

### Verified Issues

1. **MD012 Error Confirmed**
   - Reproduced with: Docker v0.26.0 and local build from main
   - Error at line 365: Pattern is `</details>` → blank → `---` → blank
   - Root cause: `NormalizeHeadingSpacing()` regex doesn't catch this pattern

2. **Role Assignment Table Confirmed**
   - User provided exact example showing blank lines between rows
   - Template uses `{{~` whitespace control but it's not working
   - Comparison with firewall template suggests issue is specific to role_assignment.sbn

3. **Summary Table Is Correct**
   - Initial investigation showed summary table appeared broken
   - Hex dump analysis confirmed rows ARE on separate lines
   - Terminal word-wrap caused confusion - summary table is actually fine

### Recommended Fix Order

1. **Fix blank line normalization regex** (simple, catches both issues)
2. **Add test that runs markdownlint** (prevents regression)
3. **Fix role assignment template** (if regex fix doesn't resolve it)
4. **Add specific tests for role assignment tables**

### Test Data

- Test output: `/tmp/local-output.md` and `/tmp/output.md`
- Both show identical issues
- Comprehensive demo plan: `examples/comprehensive-demo/plan.json`
