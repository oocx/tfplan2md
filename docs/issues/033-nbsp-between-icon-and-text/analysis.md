# Issue: Icon + label uses regular spaces (should be NBSP)

## Problem Description

tfplan2md should keep “semantic icon + label” tokens together (e.g. `⬆️\u00A0Outbound`) so the icon never wraps onto a different line than its label.

While some icon formatting already uses non‑breaking spaces (NBSP, U+00A0), the generated markdown still contains multiple icon+label strings separated by regular spaces (U+0020). This can cause unwanted line breaks in narrow layouts and is visible on the website examples.

## Steps to Reproduce

1. Inspect generated markdown in the repo artifacts, e.g.:
   - `artifacts/comprehensive-demo.md` contains `| ➕ Add | …` (regular space).
   - `artifacts/comprehensive-demo.md` contains `### 📦 Module: …` (regular space).
2. Confirm the space type:
   - In `artifacts/comprehensive-demo.md`, the separator between `➕` and `Add` is U+0020 (not U+00A0).
3. (Website symptom) Open `website/features/nsg-rules.html`:
   - It contains multiple icon+text strings like `` `⬆️ Outbound` `` and `` `✅ Allow` `` that use U+0020.

## Expected Behavior

All “icon + following text” tokens emitted by templates should use a non‑breaking space:

- `➕\u00A0Add`
- `🔄\u00A0Change`
- `♻️\u00A0Replace`
- `❌\u00A0Destroy`
- `📦\u00A0Module:`

## Actual Behavior

Templates emit regular spaces between icon and label.

## Root Cause Analysis

### What’s already correct

The semantic formatting helpers already enforce NBSP for many attribute values:

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.SemanticFormatting.cs` defines `NonBreakingSpace = "\u00A0"`.
- `EnsureNonBreakingIconSpacing()` replaces the first regular space after an icon with NBSP.
- Direction/access/protocol formatting uses `NonBreakingSpace` directly.

So values like `⬆️\u00A0Outbound` and `✅\u00A0Allow` are produced correctly *when those helper paths are used*.

### What’s broken

Several Scriban templates embed icon+label text directly with a literal space (U+0020), bypassing the semantic helper logic:

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
  - Summary rows: `➕ Add`, `🔄 Change`, `♻️ Replace`, `❌ Destroy`
  - Module heading: `### 📦 Module: …`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/summary.sbn`
  - Summary rows: `➕ Add`, `🔄 Change`, `♻️ Replace`, `❌ Destroy`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_summary.sbn`
  - Summary rows: `➕ Add`, `🔄 Change`, `♻️ Replace`, `❌ Destroy`

Additionally, the website example page `website/features/nsg-rules.html` contains code-block examples with regular spaces after icons; these appear to be static content (not generated on-demand), and should be regenerated/updated after the core fix.

## Suggested Fix Approach (High Level)

1. Update the templates to use NBSP between icon and label.
   - Minimal approach: replace the literal spaces with a literal NBSP character (U+00A0) in the `.sbn` files.
   - More maintainable approach: expose a Scriban helper like `icon_label(icon, text)` (or `nbsp()`) from C# that returns `${icon}\u00A0${text}`, and use it consistently in templates.
2. Update tests and snapshots that assert the old strings.
   - `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererTests.cs` currently asserts strings containing `"➕ Add"` and module headings like `"### 📦 Module:"`.
   - Snapshot files under `tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/` include the old spacing.
   - `tests/Oocx.TfPlan2Md.HtmlRenderer.Tests/MarkdownToHtmlRendererTests.cs` uses `"### 📦 Module: …"`.
3. Regenerate website example snippets (or update the HTML source) so the examples reflect actual tfplan2md output.

## Related Tests

- `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererTests.cs`
- `tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/ComprehensiveDemoTests.cs`
- `tests/Oocx.TfPlan2Md.HtmlRenderer.Tests/MarkdownToHtmlRendererTests.cs`
- Snapshots: `tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/*.md`

## Additional Context

- The user-reported example `⬆️ Outbound` is already NBSP-correct when produced via `ScribanHelpers` semantic formatting; the template-driven summary/module labels are the primary confirmed source of U+0020 icon spacing in generated output.
- The website example page currently contains U+0020 after many icons; this is a separate (but user-visible) symptom and may be due to static/manual example content.
