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
