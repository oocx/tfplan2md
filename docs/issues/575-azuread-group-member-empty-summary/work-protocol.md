# Work Protocol: azuread_group_member Resources Render with Empty Summary and No Details Table

**Work Item:** `docs/issues/575-azuread-group-member-empty-summary/`
**Branch:** `copilot/fix-empty-summary-details`
**Workflow Type:** Bug Fix
**Created:** 2025-01-27

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2025-01-27
- **Summary:** Investigated the root cause of `azuread_group_member` resources rendering with an empty summary line (just `— `) and no attributes table when both `group_object_id` and `member_object_id` are unknown at plan time (computed from other resources). Identified two distinct bugs: (1) `BuildGroupMemberSummaryHtml` produces empty detail text when `groupId` is an empty string from a null JSON property, and (2) `BuildAttributeChanges` skips attributes where both `before` and `after` values are null—treating "null before (create) + null after (unknown)" as unchanged—because `AfterUnknown` is never consulted.
- **Artifacts Produced:** `docs/issues/575-azuread-group-member-empty-summary/analysis.md`
- **Problems Encountered:** None
