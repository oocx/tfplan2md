# Work Protocol: azuread_group_member Empty Rendering

**Work Item:** `docs/issues/575-azuread-group-member-empty-rendering/`
**Branch:** `copilot/fix-empty-summary-details-azuread-group-member`
**Workflow Type:** Bug Fix
**Created:** 2025-07-15

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-07-15
- **Summary:** Investigated why `azuread_group_member` resources render with an empty attribute table when all values are "known after apply". Identified the root cause in `BuildAttributeChanges` — it only looks at `change.After` and ignores `change.AfterUnknown`. When `change.After` is `null` (all attributes computed), no keys are found, and the table is empty.
- **Artifacts Produced:** `docs/issues/575-azuread-group-member-empty-rendering/analysis.md`, `docs/issues/575-azuread-group-member-empty-rendering/work-protocol.md`
- **Problems Encountered:** None
