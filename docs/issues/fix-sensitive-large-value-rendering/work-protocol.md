# Work Protocol: Sensitive + Large Output Value Rendering Bugs

**Work Item:** `docs/issues/fix-sensitive-large-value-rendering/`
**Branch:** `copilot/fix-sensitive-large-value-rendering`
**Workflow Type:** Bug Fix
**Created:** 2025-07-17

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-07-17
- **Summary:** Investigated two bugs in the output value rendering pipeline in `ReportRenderer.cs`. Bug 1 (critical security): sensitive+large output values leak the raw secret in the below-table code block because the second rendering loop has no `IsMasked` guard, and the table-row condition checks `IsLargeOutputValue` before `IsMasked`. Bug 2 (formatting): large JSON output values are rendered as compact single-line strings because `JsonElement.ToString()` returns raw (unformatted) JSON; the below-table code path bypasses the existing pretty-print logic. Both bugs are localized to `RenderOutputTable` in `ReportRenderer.cs`. Proposed minimal fixes documented in analysis.
- **Artifacts Produced:**
  - `docs/issues/fix-sensitive-large-value-rendering/analysis.md`
  - `docs/issues/fix-sensitive-large-value-rendering/work-protocol.md`
- **Problems Encountered:** None

### Developer
- **Date:** 2025-07-18
- **Summary:** Implemented all three fixes using a test-first approach. Wrote 3 new tests in `ReportRendererTests.cs` that confirmed failure before fixes and success after. Applied minimal surgical changes to `ReportRenderer.RenderOutputTable`.
  - **Fix 1A (Bug 1A):** Swapped `IsMasked`/`IsLargeOutputValue` check order in the table-row loop so sensitive values always show `(sensitive value)` regardless of size.
  - **Fix 1B (Bug 1B):** Added `|| output.IsMasked` guard in the below-table loop so masked/sensitive large values are never rendered verbatim.
  - **Fix 2 (Bug 2):** Added `FormatLargeOutputValueContent` private helper using `Utf8JsonWriter` with `Indented = true` to pretty-print `JsonElement` objects/arrays (mirrors existing `TryFormatJsonOutputValue` pattern). Replaced `.ToString()` call with this helper.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (bug fixes + helper method)
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportRendererTests.cs` (3 new tests)
- **Test Results:** Full suite: 1201 passed, 0 failed, 0 skipped
- **Security:** CodeQL scan: 0 alerts. Code review: no issues found.
- **Problems Encountered:** None

### Technical Writer
- **Date:** 2025-07-18
- **Summary:** Updated `docs/features.md` to accurately document the now-correct behavior for both bug fixes. Expanded the "Sensitive Value Protection" subsection under "Terraform Outputs Support" to explicitly state that masked outputs are never emitted in below-table blocks regardless of size (Bug 1 fix). Added a new "Large Output Value Formatting" subsection documenting that large JSON objects/arrays are pretty-printed in below-table blocks (Bug 2 fix). No changes were required in `docs/spec.md` (no output-rendering content) or `README.md` (no user-facing CLI changes).
- **Artifacts Produced:**
  - `docs/features.md` — updated Sensitive Value Protection section + new Large Output Value Formatting section
  - `docs/issues/fix-sensitive-large-value-rendering/work-protocol.md` (this entry)
- **Problems Encountered:** None

### Code Reviewer
- **Date:** 2025-07-18
- **Summary:** Reviewed both bug fixes end-to-end. All 1201 tests pass. Comprehensive demo regenerated with 0 markdownlint errors. No snapshot files changed. Security fix is complete and correct: `IsMasked` is checked first in the table-cell path (Bug 1A), and the `|| output.IsMasked` guard prevents below-table rendering for masked values (Bug 1B). The `FormatLargeOutputValueContent` helper correctly pretty-prints JSON objects/arrays using `Utf8JsonWriter` with `Indented = true` (Bug 2). Two minor gaps identified (no test for `--show-sensitive` + large sensitive value, no test for string-embedded JSON path) — neither blocks approval. Review decision: **Approved**.
- **Artifacts Produced:**
  - `docs/issues/fix-sensitive-large-value-rendering/code-review.md`
  - `docs/issues/fix-sensitive-large-value-rendering/work-protocol.md` (this entry)
- **Problems Encountered:** None

### UAT Tester
- **Date:** 2025-07-18
- **Summary:** Executed technical UAT validation of both bug fixes on branch `copilot/fix-sensitive-large-value-rendering`. Validated the three new unit tests and the full test suite. All 1201 tests passed with 0 failures. Source code inspection confirmed all three fix sites are correctly implemented.
  
  **Bug 1 (Critical Security) — VALIDATED ✅**
  - Fix 1A (table cell): `IsMasked` check now precedes `IsLargeOutputValue` at line 175 in `ReportRenderer.cs`. Sensitive+large values show `(sensitive value)` in the table cell, never `_(see below)_`.
  - Fix 1B (below-table block): Guard `|| output.IsMasked` at line 225 prevents the below-table loop from rendering any masked value verbatim.
  - Test `Render_SensitiveLargeOutput_TableCellShowsSensitiveValue`: PASSED — asserts table cell contains `(sensitive value)` and not `_(see below)_`.
  - Test `Render_SensitiveLargeOutput_BelowTableBlockOmitted`: PASSED — asserts raw secret string and `\`\`\`json` block are absent from rendered markdown.

  **Bug 2 (Formatting) — VALIDATED ✅**
  - Fix: `FormatLargeOutputValueContent` helper at line 292 uses `Utf8JsonWriter` with `Indented = true` to pretty-print JSON objects/arrays. Replaces the plain `.ToString()` call that produced compact single-line JSON.
  - Test `Render_LargeJsonArrayOutput_BelowTableIsPrettyPrinted`: PASSED — asserts rendered markdown contains `\`\`\`json` block with indented line breaks (`\n  {`).

  **Full Test Suite:** 1201 passed, 0 failed, 0 skipped (2m 52s).
- **Artifacts Produced:**
  - `docs/issues/fix-sensitive-large-value-rendering/work-protocol.md` (this entry)
- **Problems Encountered:** None — all validations passed cleanly.
