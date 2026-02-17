# Feature: Output Display Enhancements

## Overview

Enhance the markdown output of tfplan2md to improve readability and user experience in two specific areas:

1. **Collapsible Debug Section**: Display the debug information section in a collapsed `<details>` block by default to reduce visual clutter while keeping debug information accessible when needed.

2. **No-Changes Summary**: When a Terraform plan contains no changes, display a simple "No changes" message in the Summary section instead of showing an empty summary table, and omit the separate "Resource Changes - no changes" section to reduce redundancy.

These enhancements make tfplan2md reports cleaner, more professional, and easier to scan, especially in PR reviews where many plans may have no changes (e.g., unaffected modules).

## User Goals

- **Cleaner Debug Output**: Users who enable `--debug` flag should not have debug information dominating the visible report. Debug details should be accessible but collapsed by default.
- **Professional No-Changes Display**: When reviewing a plan with no changes, users should see a clean, simple message rather than empty tables or redundant "No changes" sections.
- **Reduced Visual Clutter**: Reports should show only relevant information prominently, with secondary information (like debug details) easily accessible but not distracting.
- **Consistent User Experience**: The formatting should align with the existing tfplan2md style guide and work consistently across GitHub and Azure DevOps rendering platforms.

## Scope

### In Scope

#### 1. Collapsible Debug Section

- Wrap the entire debug section (`## Debug Information` and all its subsections) in a `<details>` block
- The block should be **collapsed by default** (no `open` attribute)
- Use a clear summary line: `<summary>🐛 Debug Information</summary>`
- Add `<br>` spacing after the summary tag (consistent with resource detail blocks in the style guide)
- Apply this formatting automatically when debug output is generated (when `DiagnosticContext.GenerateMarkdownSection()` is called)
- Preserve all existing debug section content (principal mapping diagnostics, template resolution, failed resolutions, etc.)

#### 2. No-Changes Summary Format

When a Terraform plan has **zero resource changes** (no add, change, replace, or destroy actions):

- **Summary Section**: Replace the summary table with a simple message: `No changes`
- **Resource Changes Section**: Do NOT render a separate "No changes" message in the Resource Changes section when Summary already shows "No changes"
- The detection should check if `summary.total_changes` equals 0 (or equivalently, all action counts are 0)

### Out of Scope

- Changing the structure or content of debug diagnostics themselves (only the presentation/collapsibility)
- Making the debug section collapsible state configurable via CLI option (always collapsed by default)
- Changing the format of Summary tables when there ARE changes
- Changing the display of other sections (Resource Changes with actual changes, Code Analysis Summary, etc.)
- Making other sections collapsible (this feature focuses only on debug section and no-changes summary)

## User Experience

### Debug Section Enhancement

**Before (Current Behavior):**
```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 5 users, 3 groups, 2 service principals
...
```

**After (New Behavior):**
```markdown
<details>
<summary>🐛 Debug Information</summary>
<br>

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 5 users, 3 groups, 2 service principals
...

</details>
```

Users can click the `🐛 Debug Information` line to expand and view debug details.

### No-Changes Summary Enhancement

**Before (Current Behavior):**
```markdown
## Summary

| Action | Count | Resource Types |
| -------- | ------- | ---------------- |
| ➕ Add | 0 | |
| 🔄 Change | 0 | |
| ♻️ Replace | 0 | |
| ❌ Destroy | 0 | |
| **Total** | **0** | |

## Resource Changes

No changes
```

**After (New Behavior):**
```markdown
## Summary

No changes
```

The Resource Changes section should not be rendered at all when there are no changes, since the Summary section already indicates this clearly.

## Success Criteria

- [ ] Debug section is wrapped in a `<details>` block with `<summary>🐛 Debug Information</summary>`
- [ ] Debug section is collapsed by default (no `open` attribute on the `<details>` tag)
- [ ] Debug section includes `<br>` spacing after the summary tag
- [ ] All existing debug content (Principal Mapping, Template Resolution, failed resolutions) is preserved and visible when expanded
- [ ] Plans with zero changes show `No changes` in the Summary section instead of an empty table
- [ ] Plans with zero changes do NOT render a "Resource Changes" section with "No changes" (avoid redundancy)
- [ ] Plans with changes continue to show the full summary table as before (no regression)
- [ ] The debug section and no-changes summary render correctly in both GitHub and Azure DevOps markdown renderers
- [ ] Existing tests for debug output and no-changes scenarios are updated to match the new format
- [ ] The report style guide is updated to document the collapsible debug section and no-changes summary format

## Open Questions

None. The requirements are clear based on the user's problem statement.
