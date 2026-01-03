---
name: website-devtools
description: Use Chrome DevTools MCP tools to inspect rendering and troubleshoot website issues with the Maintainer.
---

# Skill Instructions

## Purpose
Provide a repeatable way to use Chrome DevTools during website work to validate rendering, diagnose layout/CSS issues, and troubleshoot together with the Maintainer.

## Hard Rules
### Must
- [ ] Use `io.github.chromedevtools/chrome-devtools-mcp/*` tools when diagnosing rendering/layout issues.
- [ ] Capture concrete findings (console errors, computed styles, DOM structure) and summarize them in the PR/issue.

### Must Not
- [ ] Do not guess at rendering behavior when you can verify it using DevTools.

## Golden Example

```text
Use Chrome DevTools MCP tools to:
- Inspect computed styles and layout for a problematic element
- Check console for errors
- Verify responsive behavior by simulating viewports
```

## Actions
1. Open or serve the website locally (if needed) so it can be inspected.
2. Use Chrome DevTools MCP tools to inspect:
   - DOM structure and element attributes
   - Computed CSS and layout metrics
   - Console logs and runtime errors
   - Network requests (if applicable)
3. Share findings with the Maintainer and propose the smallest fix.
