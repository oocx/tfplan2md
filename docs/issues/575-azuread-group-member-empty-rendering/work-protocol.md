# Work Protocol: azuread_group_member Empty Rendering

**Work Item:** `docs/issues/575-azuread-group-member-empty-rendering/`
**Branch:** `copilot/fix-empty-summary-details-azuread-group-member`
**Workflow Type:** Bug Fix
**Created:** 2025-07-15

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Developer
- **Date:** 2025-07-15
- **Summary:** Implemented the fix for the `azuread_group_member` empty rendering bug. Updated `BuildAttributeChanges` in `ReportModelBuilder.ResourceChanges.cs` to include attributes from `change.AfterUnknown` in the attribute table, displaying them as `(known after apply)`. Added a test fixture and snapshot test for the all-unknown case. Updated all affected snapshots and demo artifacts.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — core fix
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-group-member-all-unknown-plan.json` — new test fixture
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzureAdSnapshotTests.cs` — new snapshot test
  - `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/azuread-group-member-all-unknown.md` — new snapshot baseline
  - Multiple existing snapshots updated with `(known after apply)` rows
  - All demo artifacts regenerated
- **Test Results:** 1233/1233 non-Docker tests pass (0 skipped)
- **Problems Encountered:** The `update-test-snapshots.sh` script doesn't cover `EphemeralSnapshotTests`, so `ephemeral-open.md` had to be manually regenerated (it has a `null_resource` with `after_unknown: {id: true}`).

### Technical Writer
- **Date:** 2025-07-15
- **Summary:** Created release notes for the fix and updated `docs/features.md` to accurately reflect the new `(known after apply)` rendering behaviour for fully-computed resources.
- **Artifacts Produced:**
  - `docs/issues/575-azuread-group-member-empty-rendering/release-notes.md` — new release notes file
  - `docs/features.md` — corrected inaccurate statement ("Null or unknown attributes are omitted") to reflect that attributes in `after_unknown` are now shown as `(known after apply)` rather than silently dropped
- **Problems Encountered:** None

### Code Reviewer
- **Date:** 2025-07-15
- **Summary:** Reviewed the fix for `azuread_group_member` empty rendering. All acceptance criteria implemented and tested. 1237 non-Docker tests pass. Snapshot changes are justified and `SNAPSHOT_UPDATE_OK` is present. Approved with two minor notes: (1) release-notes test method name mismatch (documentation inaccuracy only) and (2) pre-existing `(sensitive)` in Before column for attributes absent from `before` is now newly visible for `primary_access_key`.
- **Artifacts Produced:** `docs/issues/575-azuread-group-member-empty-rendering/code-review.md`
- **Problems Encountered:** markdownlint error on `artifacts/comprehensive-demo.md:710` (MD024 duplicate heading) — confirmed pre-existing, not introduced by this fix.

### Issue Analyst
- **Date:** 2025-07-15
- **Summary:** Investigated why `azuread_group_member` resources render with an empty attribute table when all values are "known after apply". Identified the root cause in `BuildAttributeChanges` — it only looks at `change.After` and ignores `change.AfterUnknown`. When `change.After` is `null` (all attributes computed), no keys are found, and the table is empty.
- **Artifacts Produced:** `docs/issues/575-azuread-group-member-empty-rendering/analysis.md`, `docs/issues/575-azuread-group-member-empty-rendering/work-protocol.md`
- **Problems Encountered:** None
