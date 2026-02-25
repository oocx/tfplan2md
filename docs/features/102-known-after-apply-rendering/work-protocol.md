# Work Protocol: Known-After-Apply Rendering

**Work Item:** `docs/features/575-known-after-apply-rendering/`
**Branch:** `feature/575-known-after-apply-rendering`
**Workflow Type:** Feature
**Created:** 2026-02-25

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2026-02-25
- **Summary:** Analysed two existing Copilot branches (`copilot/fix-empty-summary-details` and `copilot/fix-empty-summary-details-azuread-group-member`) that each independently address the same underlying bug. Synthesised their approaches into a single feature specification covering all use-case scenarios (five group-member scenarios + generic resource scenarios + update scenario) and three explicit decision points (A/B/C) with rendered output examples for each option. Created the feature branch and specification document.
- **Artifacts Produced:** `docs/features/575-known-after-apply-rendering/specification.md`, `docs/features/575-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** The two copilot branches conflict on three design decisions (which attributes to show in tables, whether to show config references in table values, and sensitivity vs. computed priority). These are presented as Decision A, B, and C in the specification with rendered examples for Maintainer review before the Architect begins implementation planning.
