# Cleaner PR Reviews: Collapsible Debug and No-Changes Simplification

This release enhances the visual presentation of tfplan2md reports with two focused improvements that reduce clutter in pull request reviews: collapsible debug sections and simplified no-changes summaries.

## ✨ Features

### Collapsible Debug Section

When using the `--debug` flag, debug information now appears in a collapsed `<details>` block instead of dominating your report. The debug section remains fully accessible—just click to expand—but it no longer obscures the actual infrastructure changes you're reviewing.

**What changed:**
- Debug information is wrapped in `<details><summary>🐛 Debug Information</summary>` tags
- Section is collapsed by default (no `open` attribute)
- All debug content (principal mapping diagnostics, template resolution, failed resolutions) is preserved
- Follows the same collapsible pattern used for resource detail blocks

**Why it matters:**
- **Faster reviews**: Scan your plan without scrolling past debug diagnostics
- **Less visual clutter**: Debug info available when needed, hidden when not
- **Consistent UX**: Matches existing collapsible resource sections

### No-Changes Summary Simplification

When a Terraform plan contains zero resource changes (common in multi-module environments where many plans remain stable), you now see a simple `No changes` message instead of an empty summary table. The redundant "Resource Changes" section is also omitted.

**What changed:**
- Summary section shows `No changes` text when `summary.total == 0`
- Resource Changes section is completely omitted (not rendered) for no-changes plans
- Full summary table still appears for plans with changes (no regression)

**Why it matters:**
- **Cleaner no-op plans**: No empty tables cluttering your PR
- **Professional appearance**: Simple, clear messaging for stable infrastructure
- **Reduced redundancy**: One "no changes" statement, not multiple sections

## 📸 Screenshots

### Collapsible Debug Section

The debug section now appears collapsed by default, reducing visual clutter while keeping diagnostics accessible:

![Collapsible Debug Section](https://raw.githubusercontent.com/oocx/tfplan2md/v1.21.0/docs/features/086-output-display-enhancements/debug-section.png)

*Debug information collapsed by default—click to expand when needed*

### No-Changes Summary

Plans with zero changes now show a simple message instead of empty tables:

![No Changes Summary](https://raw.githubusercontent.com/oocx/tfplan2md/v1.21.0/docs/features/086-output-display-enhancements/no-changes-summary.png)

*Clear "No changes" message replaces empty summary table*

## 🔗 Commits

> User-facing commits only (excludes task tracking and internal workflow changes)

- [`995838ad`](https://github.com/oocx/tfplan2md/commit/995838ad) feat: wrap debug section in collapsible details block
- [`2da1f9fe`](https://github.com/oocx/tfplan2md/commit/2da1f9fe) feat: show 'No changes' for zero-change plans
- [`0d5f39ba`](https://github.com/oocx/tfplan2md/commit/0d5f39ba) fix: correct template files with proper non-breaking spaces
- [`052f2b2c`](https://github.com/oocx/tfplan2md/commit/052f2b2c) test: update DiagnosticContext and ProgramMain tests for collapsible debug section
- [`5d99e664`](https://github.com/oocx/tfplan2md/commit/5d99e664) test: update MarkdownRenderer tests for no-changes summary
- [`ae9094f`](https://github.com/oocx/tfplan2md/commit/ae9094f) test: update DebugOutputIntegrationTests for collapsible debug format

## 📚 Documentation

- Updated [Report Style Guide](../../report-style-guide.md) with collapsible debug section pattern and no-changes summary format
- Updated [Features List](../../features.md) with new "Output Display Enhancements" section
- Updated [README.md](../../../README.md) to mention collapsible debug output

## Use Cases

**Scenario 1: Reviewing plans with debug enabled**

Before this release, enabling `--debug` meant scrolling past principal mapping diagnostics, template resolution details, and other debug output before seeing your actual infrastructure changes. Now, debug information is collapsed by default—expand it only when troubleshooting an issue.

**Scenario 2: Multi-module Terraform projects**

In monorepo setups with multiple Terraform modules, many PRs include plans for modules that aren't changing (e.g., networking module untouched while app module changes). These stable modules now show a clean `No changes` message instead of empty tables, making PR reviews faster to scan.

**Scenario 3: GitHub/Azure DevOps PR comments**

Both platforms support `<details>` tags, so collapsible sections work seamlessly. Debug diagnostics and no-changes summaries render consistently across both environments (verified in UAT).

## Technical Details

### Implementation

- **Collapsible Debug**: Modified `DiagnosticContext.GenerateMarkdownSection()` to wrap output in `<details>` tags with `<summary>🐛 Debug Information</summary>` (using non-breaking space U+00A0)
- **No-Changes Summary**: Modified `_summary.sbn` template to conditionally render `No changes` text when `summary.total == 0`
- **Resource Changes Omission**: Modified `default.sbn` template to conditionally render Resource Changes section only when `module_changes.size > 0`

### Backwards Compatibility

- **No breaking changes**: All existing functionality remains intact
- **No CLI changes**: No new flags or options required
- **Template compatibility**: Custom templates can opt into or override these behaviors

### Platform Support

- ✅ GitHub markdown renderer
- ✅ Azure DevOps markdown renderer
- ✅ Verified in UAT with real PRs

## Related Documentation

- **Feature specification**: [specification.md](specification.md)
- **Architecture**: [architecture.md](architecture.md)
- **Test plan**: [test-plan.md](test-plan.md)
- **UAT test plan**: [uat-test-plan.md](uat-test-plan.md)
- **Report style guide**: [docs/report-style-guide.md](../../report-style-guide.md)
- **Features list**: [docs/features.md](../../features.md)
