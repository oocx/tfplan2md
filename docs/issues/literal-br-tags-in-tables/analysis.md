# Issue: Line Breaks in Tables Render as Literal `<br>` Tags

## Problem Description

Line breaks in markdown tables are not rendering correctly. Instead of displaying as actual line breaks, the literal text `<br>` (or escaped `\<br\>`) appears in the rendered output. This specifically affects the firewall rules template where modified rules display before/after values using `format_diff`, which generates strings containing `<br>` tags to separate the values.

## Steps to Reproduce

1. Generate a markdown report for a Terraform plan containing modified firewall rules
2. View the generated markdown in any markdown renderer
3. Observe that modified rule attributes show escaped `\<br\>` instead of line breaks

**Example from comprehensive-demo/report.md (line 159):**
```markdown
| 🔄 | allow-dns | UDP | - 10.1.1.0/24<br>+ 10.1.1.0/24, 10.1.2.0/24 | 168.63.129.16 | 53 | DNS to Azure |
```

When rendered, the `<br>` appears as literal text instead of creating a line break.

## Expected Behavior

The `<br>` HTML tags should render as actual line breaks within table cells, displaying the before and after values on separate lines:

```
- 10.1.1.0/24
+ 10.1.1.0/24, 10.1.2.0/24
```

## Actual Behavior

The table cells display the escaped HTML tags as literal text:

```
- 10.1.1.0/24\<br\>+ 10.1.1.0/24, 10.1.2.0/24
```

Or in raw markdown (unescaped):

```
- 10.1.1.0/24<br>+ 10.1.1.0/24, 10.1.2.0/24
```

Which renders as a single line with visible `<br>` text.

## Root Cause Analysis

### Affected Components

**Primary Files:**
- [src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs#L145) - `FormatDiff` method generates strings with `<br>` tags
- [src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs#L48-L49) - `EscapeMarkdown` method escapes `<` and `>` characters
- [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn#L22) - Template applies `escape_markdown` after `format_diff`

**Test Files:**
- [tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/firewall-rules.md](../../../tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/firewall-rules.md#L28) - Snapshot shows escaped `\<br\>` tags

### What's Broken

The issue occurs due to a pipeline of transformations:

1. **`FormatDiff` generates HTML** (line 145 of ScribanHelpers.cs):
   ```csharp
   return $"- {beforeValue}<br>+ {afterValue}";
   ```

2. **Template applies both functions in sequence** (line 22 of firewall template):
   ```scriban
   {{ format_diff (...) | escape_markdown }}
   ```

3. **`EscapeMarkdown` escapes angle brackets** (lines 48-49 of ScribanHelpers.cs):
   ```csharp
   value = value.Replace("<", "\\<");
   value = value.Replace(">", "\\>");
   ```

4. **Result:** `<br>` becomes `\<br\>`, which renders as literal text instead of a line break.

### Why It Happened

The `escape_markdown` function was designed to protect against markdown injection by escaping special characters. However, when used after `format_diff`, it also escapes the intentional HTML `<br>` tags, preventing them from functioning as line breaks.

This is a **template usage issue** - the template applies `escape_markdown` to the entire output of `format_diff`, which already contains HTML that should not be escaped.

### Why Wasn't This Caught Earlier?

1. **Tests validate escaped output:** The existing tests check for the presence of escaped `\<br\>` in the generated markdown string, which is technically correct as raw markdown content. Example from MarkdownRendererResourceTemplateTests.cs line 41:
   ```csharp
   result.Should().Contain(Escape("- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24"));
   ```
   This test expects the `<br>` to be escaped by `Escape()` (which calls `EscapeMarkdown`).

2. **No rendering validation:** Tests verify the markdown *source* but don't validate how it *renders*. The escaped `\<br\>` is valid markdown syntax, but doesn't produce the intended visual result.

3. **Snapshot tests capture escaped output:** The snapshot files contain the escaped tags, so regression tests pass. See firewall-rules.md line 28.

4. **Markdown quality tests focus on structure:** Recent markdown validation tests (MarkdownLintIntegrationTests) check for lint errors and structural issues, but don't validate that HTML tags within tables render correctly.

## Suggested Fix Approach

There are three potential approaches to fix this issue:

### Option 1: Make `FormatDiff` Return Pre-Escaped Safe HTML (Recommended)

**Approach:** Modify `FormatDiff` to return a string that's already been through `EscapeMarkdown` for the data values, but preserves the `<br>` tags unescaped.

**Implementation:**
1. Update `FormatDiff` to escape the before/after values individually before inserting them
2. Keep the `<br>` tag unescaped in the output
3. Update the template to NOT apply `escape_markdown` to the `format_diff` output
4. Update all affected tests and snapshots

**Example change in ScribanHelpers.cs:**
```csharp
public static string FormatDiff(string? before, string? after)
{
    var beforeValue = before ?? string.Empty;
    var afterValue = after ?? string.Empty;

    if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
    {
        return EscapeMarkdown(afterValue);  // Single value still needs escaping
    }

    // Escape each value individually, but leave <br> unescaped
    var escapedBefore = EscapeMarkdown(beforeValue);
    var escapedAfter = EscapeMarkdown(afterValue);
    
    return $"- {escapedBefore}<br>+ {escapedAfter}";
}
```

**Template change in firewall_network_rule_collection.sbn:**
```scriban
{{~ for item in diff.modified ~}}
| 🔄 | {{ item.after.name | escape_markdown }} | {{ format_diff (item.before.protocols | array.join ", ") (item.after.protocols | array.join ", ") }} | ...
{{~ end ~}}
```
(Remove `| escape_markdown` from `format_diff` calls)

**Pros:**
- Clean separation of concerns: `FormatDiff` handles all escaping internally
- Template becomes simpler and less error-prone
- Consistent with the principle that helper functions should return safe output
- Maintains backwards compatibility for unchanged values (they still get escaped)

**Cons:**
- Breaking change to `FormatDiff` behavior (now returns partially-escaped HTML)
- Requires updating all call sites in templates
- Need to update all tests that verify escaped output

### Option 2: Use a Different Line Break Approach

**Approach:** Instead of HTML `<br>` tags, use markdown-native approaches or Unicode characters.

**Options:**
- Two spaces + newline (markdown line break)
- Unicode newline character
- Multiple rows in the table (one for before, one for after)

**Pros:**
- Avoids HTML entirely
- Pure markdown solution

**Cons:**
- Two-space line breaks are fragile and often don't work in tables
- Unicode characters may not render consistently
- Multiple table rows would significantly complicate the template

### Option 3: Modify `EscapeMarkdown` to Preserve HTML Tags

**Approach:** Update `EscapeMarkdown` to detect and preserve certain safe HTML tags like `<br>`, `<br/>`.

**Pros:**
- Centralized fix in one function

**Cons:**
- Complex logic to distinguish safe vs unsafe HTML
- Security implications (what other tags should be allowed?)
- Goes against the purpose of the function (preventing HTML injection)

## Recommended Solution: Option 1

**Rationale:**
- `FormatDiff` is a specialized helper that generates formatted output for display
- It should be responsible for producing safe, ready-to-use HTML
- Templates shouldn't need to know which parts of the output to escape
- This follows the security principle: escape data at the point of use, but preserve intentional markup

## Implementation Checklist

### Code Changes

- [ ] Update `FormatDiff` in ScribanHelpers.cs to escape before/after values individually
- [ ] Keep `<br>` tags unescaped in `FormatDiff` output
- [ ] Handle the case where values are equal (still needs escaping)
- [ ] Update firewall template to remove `escape_markdown` from `format_diff` calls
- [ ] Check if any other templates use `format_diff` (currently only firewall template)

### Test Updates

- [ ] Update `ScribanHelpersFormatDiffTests` to verify that:
  - Different values return `"- {escaped_before}<br>+ {escaped_after}"`
  - Equal values return single escaped value
  - Special characters in values are properly escaped
  - `<br>` tags are NOT escaped in diff output
  
- [ ] Update `MarkdownRendererResourceTemplateTests` to verify:
  - `<br>` tags appear unescaped in modified rule output
  - Data values are still escaped (pipes, backticks, etc.)
  
- [ ] Update firewall snapshot tests:
  - `firewall-rules.md` snapshot should show `<br>` not `\<br\>`
  - `comprehensive-demo.md` snapshot should show `<br>` not `\<br\>`

- [ ] Add new rendering validation test to verify `<br>` tags actually create line breaks when rendered

### Documentation Updates

- [ ] Update [docs/features.md](../../features.md#L378) to clarify that `format_diff` returns pre-escaped HTML with unescaped `<br>` tags
- [ ] Update [docs/features.md](../../features.md#L404) documentation for `format_diff` helper
- [ ] Update example output in feature documentation to show correct rendering

## Related Tests

Tests that should pass after the fix:

### Must Update (Currently Expect Escaped `<br>`)

- [ ] `ScribanHelpersFormatDiffTests.FormatDiff_DifferentStrings_ReturnsDiffFormat`
- [ ] `ScribanHelpersFormatDiffTests.FormatDiff_NullBefore_ReturnsDiffFormat`
- [ ] `ScribanHelpersFormatDiffTests.FormatDiff_NullAfter_ReturnsDiffFormat`
- [ ] `ScribanHelpersFormatDiffTests.FormatDiff_EmptyStrings_HandledCorrectly`
- [ ] `MarkdownRendererResourceTemplateTests.RenderResourceChange_FirewallRuleCollection_ShowsModifiedRules`
- [ ] `MarkdownRendererResourceTemplateTests.Render_FirewallRuleCollection_UsesResourceSpecificTemplate`

### Must Update (Snapshots Contain Escaped `<br>`)

- [ ] `MarkdownSnapshotTests.Snapshot_FirewallRules_MatchesBaseline`
- [ ] `MarkdownSnapshotTests.Snapshot_ComprehensiveDemo_MatchesBaseline`
- [ ] Snapshot files:
  - `TestData/Snapshots/firewall-rules.md`
  - `TestData/Snapshots/comprehensive-demo.md`

### Should Still Pass (Don't Check for `<br>` Escaping)

- [ ] `MarkdownRendererResourceTemplateTests.RenderResourceChange_FirewallRuleCollection_ReturnsResourceSpecificMarkdown`
- [ ] `MarkdownRendererResourceTemplateTests.RenderResourceChange_FirewallRuleCollection_ShowsAddedRules`
- [ ] `MarkdownRendererResourceTemplateTests.RenderResourceChange_FirewallRuleCollection_ShowsRemovedRules`
- [ ] `MarkdownRendererResourceTemplateTests.RenderResourceChange_FirewallRuleCollection_ShowsUnchangedRules`

## Additional Context

### Related Issues

This issue was introduced when the firewall before/after display feature was implemented. See:
- [docs/features/firewall-rule-before-after-display/specification.md](../../features/firewall-rule-before-after-display/specification.md)
- [docs/features/firewall-rule-before-after-display/architecture.md](../../features/firewall-rule-before-after-display/architecture.md)

### Verification Steps

After implementing the fix, verify:

1. **Visual Inspection:** Render the comprehensive-demo report in a markdown viewer and confirm that modified firewall rules show before/after values on separate lines
2. **Manual Test:** Create a plan with modified firewall rules and verify the output looks correct
3. **Automated Tests:** All tests pass with updated expectations
4. **Snapshot Review:** Compare old vs new snapshots to confirm only `<br>` escaping changed

## Security Considerations

The fix should maintain security by:
- Still escaping all user data (values before/after the `<br>` tags)
- Only allowing the specific `<br>` tag that `FormatDiff` intentionally generates
- Not opening the door to arbitrary HTML injection

The recommended approach (Option 1) achieves this by having `FormatDiff` escape the data values individually before inserting them into the HTML string.

---

## Implementation Status

✅ **RESOLVED** - December 22, 2025

### Changes Made

**Code Changes:**
- Updated `FormatDiff` in [ScribanHelpers.cs](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers.cs#L129-L158) to escape values internally while preserving `<br>` tags
- Modified [firewall_network_rule_collection.sbn](../../../src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn#L23) template to remove `escape_markdown` from `format_diff` outputs

**Test Updates:**
- Updated all unit tests in [ScribanHelpersFormatDiffTests.cs](../../../tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ScribanHelpersFormatDiffTests.cs) to verify escaping behavior
- Updated integration tests in [MarkdownRendererResourceTemplateTests.cs](../../../tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererResourceTemplateTests.cs) to expect unescaped `<br>` tags
- Updated snapshots: [firewall-rules.md](../../../tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/firewall-rules.md) and [comprehensive-demo.md](../../../tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/comprehensive-demo.md)
- Added new test `FormatDiff_EscapesValuesAndPreservesLineBreakTags` to verify markdown-sensitive characters are escaped while `<br>` remains intact

**Documentation Updates:**
- Updated [docs/features.md](../../features.md) to clarify `format_diff` escaping behavior
- Updated [docs/features/resource-specific-templates.md](../resource-specific-templates.md) to document internal escaping

### Verification

All tests pass (259/259):
```
dotnet test
Test summary: total: 259, failed: 0, succeeded: 259, skipped: 0
```

The `<br>` tags now render correctly as line breaks in markdown tables while maintaining security by escaping all user data.

---

**Analysis completed by:** Documentation Author Agent  
**Date:** December 22, 2025  
**Implementation completed by:** Developer Agent  
**Date:** December 22, 2025
