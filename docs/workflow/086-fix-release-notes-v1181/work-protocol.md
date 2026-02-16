# Work Protocol: Fix Release Notes for v1.18.1

## Issue
Release v1.18.1 was published with incorrect release notes. The published notes describe "Azure DevOps Principal Mapping" (from v1.18.0), but the actual release is based on PR #493, which contains 5 bug fixes.

## Root Cause
The release workflow selects release notes by finding the latest `release-notes.md` file across all work item folders (features, issues, workflow) by highest numeric prefix. Since v1.18.0 was feature 085 (Azure DevOps Principal Mapping), and no dedicated release notes file existed for v1.18.1's bug fixes, the workflow incorrectly used feature 085's release notes.

## Solution
Created workflow folder `086-fix-release-notes-v1181` with corrected release notes describing the 5 bug fixes from PR #493:
1. Parser exception handling (TerraformPlanParseException for null input)
2. CLI argument validation (reject multiple positional arguments)
3. Code formatting edge case (escaped backtick stripping)
4. Principal mapper performance (guard clause ordering)
5. Dead code elimination (redundant ternary)

## Changes Made
- Created `/docs/workflow/086-fix-release-notes-v1181/release-notes.md` with correct bug fix descriptions
- Documented all 11 commits from PR #493 (5 fixes + 6 test additions)

## Required Manual Step
**Maintainer action required**: Update GitHub release v1.18.1 with the corrected notes.

The GitHub token in the coding agent environment does not have `contents: write` permission to edit releases. Please run:

```bash
cat docs/workflow/086-fix-release-notes-v1181/release-notes.md | gh release edit v1.18.1 --notes-file -
```

Or manually edit the release at: https://github.com/oocx/tfplan2md/releases/tag/v1.18.1

## Agent Work Log

### Release Manager (2026-02-16)
**Task**: Fix incorrect release notes for v1.18.1

**Work Completed**:
- Analyzed PR #493 to understand the actual changes in v1.18.1
- Created workflow folder 086 for this release notes correction
- Wrote corrected release notes based on the 5 bug fixes
- Documented all commits from PR #493

**Artifacts Produced**:
- `docs/workflow/086-fix-release-notes-v1181/release-notes.md` - Corrected release notes
- `docs/workflow/086-fix-release-notes-v1181/work-protocol.md` - This document

**Blocked On**:
Maintainer needs to apply the corrected release notes to GitHub release v1.18.1 (agent token lacks release edit permissions).

**Next Steps**:
After maintainer updates the release, verify the corrected notes are displayed correctly on the GitHub releases page.
