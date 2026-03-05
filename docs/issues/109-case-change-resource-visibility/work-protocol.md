# Work Protocol: Case-Change Resources Appear in Summary Counts

## Workflow Type
Bug Fix

## Issue
Resources with only case-different attribute changes (Azure resource ID casing noise) are excluded
from the rendered report body (`model.Changes`) but still counted in the summary (e.g., "1 to
change"). PR #574 fixed the display suppression but missed the summary-count fix.

## Work Item Folder
`docs/issues/109-case-change-resource-visibility/`

## Agents Involved

| Step | Agent | Status |
|------|-------|--------|
| 1 | Issue Analyst | ✅ Complete |
| 2 | Developer | ⬜ Pending |
| 3 | Code Reviewer | ⬜ Pending |
| 4 | Technical Writer | ⬜ Pending (if needed) |
| 5 | Release Manager | ⬜ Pending |

## Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| Analysis | `docs/issues/109-case-change-resource-visibility/analysis.md` | ✅ Created |
| Work Protocol | `docs/issues/109-case-change-resource-visibility/work-protocol.md` | ✅ Created |
| Fix | _to be implemented by Developer_ | ⬜ Pending |
| Tests | _to be implemented by Developer_ | ⬜ Pending |
| Release Notes | _to be created by Release Manager_ | ⬜ Pending |

## Definition of Done

- [ ] Summary counts (`toChange`) exclude resources where all changes are casing-only Azure ID diffs
- [ ] Resources filtered from display are also filtered from summary counts
- [ ] New test covering `model.Summary.ToChange.Count` for casing-only resources
- [ ] New test covering azapi body-casing scenario (end-to-end)
- [ ] Existing tests still pass
- [ ] `FilteredResourceCount` remains correct

## Agent Work Log

### Issue Analyst (2026-03-05)

**Summary:** Investigated the root cause of casing-only resources appearing in summary counts despite
being excluded from `model.Changes`. Root cause is in `ReportModelBuilder.Build.cs` lines 44–46
where summary counts are computed from `allChanges` (all Terraform resources) before the
`displayChanges` filter (lines 64–73) that suppresses casing-only resources.

**Artifacts produced:**
- `docs/issues/109-case-change-resource-visibility/analysis.md` — full root cause analysis

**Problems encountered:** None. Root cause clear and well-isolated.

**Recommendation:** Hand off to Developer to implement fix in `ReportModelBuilder.Build.cs` and
add regression tests.
