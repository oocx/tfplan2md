# Code Review: Cumulative Release Notes

## Addendum (2025-12-27): Tag-Only Release Trigger + Prerelease Safety

This addendum reviews the follow-up workflow change that removes `workflow_dispatch` from the release pipeline, enforces prerelease tagging via Versionize, and adjusts Docker tagging so prereleases do not publish `latest`/major/minor tags.

## Verification Results (Addendum)

- Tests: Pass (317 passed)
- Build: Success (via `dotnet run` demo generation)
- Docker: Builds (`docker build -t tfplan2md:local .`)
- Demo Markdownlint: Pass (0 errors for `artifacts/comprehensive-demo.md`)
- Errors: None observed

## Review Decision (Addendum)

**Status:** Changes Requested

## Issues Found (Addendum)

### Blockers

1. Documentation drift: feature docs referenced `workflow_dispatch`
	- Impact: The cumulative release notes feature documentation required supporting `workflow_dispatch`, but the release workflow is now tag-triggered only.
	- Required fix: Update the cumulative release notes documentation to remove `workflow_dispatch` references and success criteria.

### Major Issues

1. Uncommitted documentation updates
	- Impact: The documentation updates required to reflect the new workflow behavior must be included in the change set.
	- Required fix: Ensure all documentation updates are committed alongside the workflow changes.

### Minor Issues

None

### Suggestions

None

## Summary

The implementation adds cumulative release notes functionality to the release workflow. GitHub releases now include all changes since the last release, ensuring Docker Hub users see complete change history even when intermediate versions are skipped.

## Verification Results

- **Tests**: Pass (8/8 new tests passed, 104/110 total tests passed, 6 skipped)
- **Build**: Success
- **Docker**: Not tested (Docker unavailable in environment)
- **Errors**: None
- **Script Warnings**: Fixed (regex escape sequence warning resolved)

## Review Decision

**Status:** Approved ✅

## Issues Found

### Blockers

None

### Major Issues

None

### Minor Issues

**MI-1**: Fixed during review - Regex escape sequence warning in awk script
- **Location**: [scripts/extract-changelog.sh](scripts/extract-changelog.sh#L27)
- **Issue**: Used `\"` instead of `"` in regex pattern
- **Resolution**: Changed to use unescaped quotes
- **Status**: ✅ Fixed

### Suggestions

None

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ✅ |

### Correctness Details

✅ **All acceptance criteria met:**
- First release (no previous releases) → Only current version extracted
- Consecutive versions → Only current version extracted
- Version gaps → Cumulative extraction works correctly
- Markdown formatting preserved (including complex nested lists, code blocks, links)
- Version prefix handling (with/without 'v') works consistently
- Idempotent execution verified
- Edge cases handled (version not found, last version not in file)
- Workflow handles tag push triggers

✅ **All test cases from test plan implemented and passing:**
- TC-01: No Previous Release ✅
- TC-02: Consecutive Versions ✅
- TC-03: Version Gap (Cumulative) ✅
- TC-04: Formatting Preservation ✅
- TC-05: Version Prefix Handling ✅
- TC-06: Idempotency ✅
- TC-07: Last version not found ✅
- TC-08: Current version not found ✅

### Code Quality Details

✅ **Script follows best practices:**
- Proper error handling with `set -euo pipefail`
- Clear usage message
- Input validation
- Clean awk logic with helper function
- Handles pending anchors correctly

✅ **Test implementation:**
- Uses SkippableFact for platform-specific tests (bash required)
- Clear test naming convention: `MethodName_Scenario_ExpectedResult`
- Process-based testing approach appropriate for bash script
- Comprehensive assertions using AwesomeAssertions

### Architecture Details

✅ **Implementation matches architecture decision:**
- Uses GitHub CLI (`gh release list`) to find last release
- Extracts changelog sections via bash script
- No additional state files or tracking mechanisms
- Maintains backward compatibility
- Works with tag push triggers

✅ **Script is properly executable:**
- Script has execute permissions (`chmod +x`)
- Located in `scripts/` directory
- Invoked from workflow correctly

### Testing Details

✅ **Test coverage is comprehensive:**
- 8 new tests covering all scenarios
- Test data files created (changelog-full.md, changelog-complex.md)
- Edge cases tested
- All tests passing

✅ **Integration with existing tests:**
- No regressions in existing 104 tests
- Follows existing test patterns
- Docker tests skipped gracefully when Docker unavailable

### Documentation Details

✅ **All documentation updated:**
- [docs/features.md](docs/features.md) - Added "CI/CD Integration > Cumulative Release Notes" section
- [docs/spec.md](docs/spec.md) - Updated workflow table and added release notes explanation
- [architecture.md](architecture.md) - Status changed to "Implemented"
- [tasks.md](tasks.md) - All tasks marked complete
- [specification.md](specification.md) - Complete and accurate
- [test-plan.md](test-plan.md) - Complete with all test cases

✅ **Documentation is consistent:**
- No contradictions found
- Clear examples provided
- Implementation aligns with specification

## Next Steps

The implementation is complete and ready for merge:

1. All acceptance criteria met
2. Comprehensive test coverage with all tests passing
3. Documentation fully updated
4. No blockers or major issues
5. Code follows project conventions

**Recommendation**: Merge to main branch.

## Code Review Details

### Files Reviewed
- [.github/workflows/release.yml](.github/workflows/release.yml) - ✅ Workflow changes correct
- [scripts/extract-changelog.sh](scripts/extract-changelog.sh) - ✅ Script logic sound
- [src/tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs](src/tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs) - ✅ Tests comprehensive
- [src/tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md](src/tests/Oocx.TfPlan2Md.Tests/TestData/changelog-full.md) - ✅ Test data appropriate
- [src/tests/Oocx.TfPlan2Md.Tests/TestData/changelog-complex.md](src/tests/Oocx.TfPlan2Md.Tests/TestData/changelog-complex.md) - ✅ Test data appropriate
- All documentation files - ✅ Complete and accurate

### Workflow Changes Analysis

The release workflow changes are minimal and focused:
1. Added "Find previous GitHub release" step using GitHub CLI
2. Modified "Extract changelog for version" → "Extract changelog range" 
3. Invokes new bash script with last version parameter
4. Maintains backward compatibility with fallback message

The changes preserve existing behavior while adding the cumulative functionality.
