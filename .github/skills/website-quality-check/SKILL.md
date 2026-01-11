---
name: website-quality-check
description: Run a lightweight, repeatable quality checklist for the website (including style guide adherence).
---

# Skill Instructions

## Purpose
Provide a lightweight, repeatable quality checklist for website changes.

## Hard Rules
### Must
- [ ] Verify the change follows `website/_memory/style-guide.md`.
- [ ] Verify the change follows `website/_memory/non-functional-requirements.md`.
- [ ] If any HTML/CSS/JS files changed under `website/`, run `scripts/website-verify.sh` and fix failures.
- [ ] If local preview is available, open the changed pages and ensure the browser console has no errors.
- [ ] Do quick link/navigation sanity checks on changed pages.
- [ ] Do basic accessibility spot checks (headings, aria labels, keyboard navigation where relevant).

### Must Not
- [ ] Do not merge or hand off for PR without completing the checklist for the changed pages.

## Golden Example

```text
Checklist (per changed page):
- Style guide: containers, typography, nav consistency
- NFRs: accessibility basics, supported browsers
- Links: no broken relative links introduced
- DevTools: verify layout and console is clean
```

## Actions
1. Identify which pages/assets changed under `website/`.
2. For each changed page, verify:
   - Style guide adherence (`website/_memory/style-guide.md`)
   - NFR adherence (`website/_memory/non-functional-requirements.md`)
   - Link/navigation sanity
   - Accessibility basics
3. If issues are found, fix them or record them with a clear plan.
