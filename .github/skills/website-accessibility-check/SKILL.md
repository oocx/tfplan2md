---
name: website-accessibility-check
description: Run a focused accessibility pass for website changes (WCAG 2.1 AA-oriented).
---

# Skill Instructions

## Purpose
Provide a focused, repeatable accessibility checklist for changes to Eleventy-authored website pages and shared UI building blocks.

## Hard Rules
### Must
- [ ] For any changed page/component, confirm semantic structure and a sensible heading hierarchy.
- [ ] Ensure interactive elements are keyboard reachable and have visible focus.
- [ ] Ensure images/icons that convey meaning have appropriate alt text (decorative images should not add noise).
- [ ] Ensure links have descriptive text (avoid “click here”).
- [ ] For any forms/inputs, ensure there are labels (or equivalent accessible names).
- [ ] Run `scripts/website-verify.sh --all` and fix failures before claiming done.
- [ ] When shared authored sources change (`_includes`, `_data`, `styles`, `site-assets`, or `examples`), validate at least one representative consuming page in the rendered `website/dist/` output.

### Must Not
- [ ] Do not claim “done” if basic keyboard navigation is broken.
- [ ] Do not introduce new UI patterns without considering accessibility impact.

## Quick Checklist (per changed page)
- Structure: one clear `h1`, no heading level jumps where avoidable
- Landmarks: use semantic elements (`header`, `nav`, `main`, `footer`) where appropriate
- Keyboard: tab order is logical; focus is visible; no traps
- Images: meaningful images have alt; decorative images don’t distract screen readers
- Links: text is descriptive and unique enough in context
- Color/contrast: avoid low-contrast text and “color-only” meaning

## Suggested Workflow (VS Code)
1. Open the changed page via the VS Code preview server (`http://127.0.0.1:3000/website/dist/`), then use the `browser/*` tools or the `website-devtools` skill to inspect DOM, focus order, and console state.
2. Use keyboard only (Tab/Shift+Tab/Enter/Space) to navigate key flows.
3. Spot-check headings and landmark structure in the DOM.
4. If shared components or layouts changed, repeat the checks on multiple pages that use them.
5. If available, use the `website-devtools` skill to validate focus states, responsive behavior, and console errors.
