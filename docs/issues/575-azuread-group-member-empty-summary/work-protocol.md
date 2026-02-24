# Work Protocol: azuread_group_member Resources Render with Empty Summary and No Details Table

**Work Item:** `docs/issues/575-azuread-group-member-empty-summary/`
**Branch:** `copilot/fix-empty-summary-details`
**Workflow Type:** Bug Fix
**Created:** 2025-01-27

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Developer
- **Date:** 2025-01-27
- **Summary:** Implemented fixes for both root causes identified in the analysis. Fix 1: Updated `BuildGroupMemberSummaryHtml` to show `"(known after apply)"` when `groupId` is empty, and added `JsonStateReader.PropertyExists()` helper to distinguish absent `member_object_id` (don't show arrow) from null/unknown `member_object_id` (show `→ (known after apply)`). Fix 2: Updated `BuildAttributeChanges` to consult `change.AfterUnknown` — attributes with null after-values AND `after_unknown=true` are now displayed as `"(known after apply)"` instead of being skipped. Extracted `IsKeyComputedAfterApply` and `DetermineAfterDisplay` helpers to stay within cyclomatic complexity limits.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — Bug 2 fix + helper methods
  - `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs` — Bug 1 fix
  - `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/JsonStateReader.cs` — `PropertyExists()` helper
  - `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberTemplateTests.cs` — 2 new tests
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-group-member-all-unknown-plan.json` — new test fixture
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/ephemeral-open.md` — updated snapshot (null_resource.app_config id now shows as "(known after apply)")
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/no-configuration-block.md` — updated snapshot (azuread_group_member now shows correct summary and attribute table)
- **Tests:** All 4 AzureAdGroupMemberTemplateTests pass (2 new, 2 existing). All snapshot tests pass. Build clean, no warnings or CodeQL alerts.
- **Problems Encountered:** The `update-test-snapshots.sh` script deleted more snapshots than it regenerated (it deleted all but only ran snapshot test classes for AzureAD/Ephemeral/AzureDevOps). Resolved by restoring non-affected snapshots via `git checkout` and manually regenerating only the two affected snapshots.

