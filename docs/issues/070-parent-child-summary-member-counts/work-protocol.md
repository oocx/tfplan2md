# Work Protocol: Parent-Child Resource Summary Shows Incorrect Member Counts

**Work Item:** `docs/issues/070-parent-child-summary-member-counts/`
**Branch:** `fix/070-parent-child-summary-member-counts`
**Workflow Type:** Bug Fix
**Created:** 2026-02-11

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst
- **Date:** 2026-02-11
- **Summary:** Investigated root cause of incorrect member counts in Azure AD group parent-child summaries. Reviewed closed PR #453 to understand previous fix attempt. Identified timing issue where summaries are built before parent-child merging completes. Documented two fix approaches: simple post-merge update (recommended) vs complex interface-based rebuilder pattern.
- **Artifacts Produced:**
  - `docs/issues/070-parent-child-summary-member-counts/analysis.md` - Comprehensive root cause analysis
  - `docs/issues/070-parent-child-summary-member-counts/work-protocol.md` - This work protocol document
- **Problems Encountered:**
  - PR #453 was closed without clear documentation of why it was rejected. Had to infer reasons from the PR diff showing significant architectural complexity (13 files, 628 additions) for what might be a simpler problem.
  - Could not access PR comments via GitHub API or gh CLI in the CI environment to understand maintainer feedback on PR #453.
  - Currently on wrong branch (`copilot/fix-summary-member-counts`) instead of the proper numbered branch (`fix/070-parent-child-summary-member-counts`). Need to create proper branch with issue number.

### Developer
- **Date:** 2026-02-11
- **Summary:** Implemented simple post-merge update approach to fix Azure AD group member counts. Added `UpdateAzureAdGroupSummaries` method that runs after parent-child merging, extracts member IDs from merged child rows, recounts by type using PrincipalMapper, and updates icon counts in SummaryHtml. Wrote comprehensive tests for all scenarios (inline-only, separate-only, mixed, no-members). Updated test snapshots to reflect correct member counts.
- **Artifacts Produced:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs` - Added `UpdateAzureAdGroupSummaries` method and helper methods for member extraction and counting
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` - Added call to `UpdateAzureAdGroupSummaries` after `MergeParentChildRelationships`
  - `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupSummaryMemberCountTests.cs` - New test class with 4 test scenarios
  - Updated 4 snapshot files to reflect correct member counts
  - Commits:
    - `12d4fd4` - Implementation with tests
    - `c008961` - Snapshot updates with `SNAPSHOT_UPDATE_OK` token
- **Problems Encountered:**
  - Initial member ID extraction logic didn't handle backtick formatting (`` `user-1` ``). Fixed by adding backtick removal in `ExtractMemberIdFromFormattedValue`.
  - Test timeout script cut off at 120s, but build verified 940 tests passed with 0 failures.
- **Implementation Notes:**
  - Followed the simple post-merge update approach from analysis.md (Option 1)
  - Minimal code changes: 2 production files changed, ~200 lines added (mostly comments)
  - No new interfaces or architectural patterns - just a straightforward update method
  - Reused existing `PrincipalMapper` service for type resolution
  - Member ID extraction handles both "Name [id]" and `` `id` `` formats
  - Regex pattern with timeout (1 second) to find and replace icon counts in SummaryHtml
  - All existing tests pass, 4 new tests cover the bug scenarios
