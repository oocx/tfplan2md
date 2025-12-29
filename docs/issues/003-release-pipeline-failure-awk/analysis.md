# Issue Analysis: Release Pipeline Failure - AWK Syntax Incompatibility

**Issue ID:** release-pipeline-failure-awk  
**Date:** December 20, 2025  
**Reporter:** Maintainer  
**Severity:** Blocker  
**Status:** Fixed  

## Problem Summary

The release workflow failed on December 19, 2025 at 22:41:18Z during the Docker build phase. Eight tests in `ChangelogExtractionTests` failed with AWK syntax errors, preventing the Docker image from being built and published.

## Error Details

```
awk: line 3: syntax error at or near ,
awk: line 6: syntax error at or near return
```

**Failed Workflow Run:** https://github.com/oocx/tfplan2md/actions/runs/20384419278

**Failed Tests:**
1. `Preserves_complex_markdown_formatting`
2. `Extracts_cumulative_sections_when_versions_were_skipped`
3. `Returns_empty_output_when_current_version_not_found`
4. `Extracts_only_current_version_when_no_previous_release_exists`
5. `Handles_versions_with_or_without_v_prefix_consistently`
6. `Extracts_only_current_version_when_previous_is_consecutive`
7. `Extracts_until_end_when_last_version_not_found`
8. `Is_idempotent_for_same_inputs`

All 8 failures occurred in `dotnet test` during the Docker build step.

## Root Cause Analysis

### Immediate Cause

The [scripts/extract-changelog.sh](../../../scripts/extract-changelog.sh) script uses GNU AWK-specific syntax for declaring function-local variables:

```awk
function header_version(line,   m) {
    if (match(line, /^##[[:space:]]+\[?v?([0-9]+\.[0-9]+\.[0-9]+)\]?/, m)) {
        return m[1];
    }
    return "";
}
```

This syntax has two GNU AWK-specific features:
1. **Function-local variable declaration**: The extra spaces before `m` in the parameter list indicate it's a local variable (GNU AWK convention)
2. **match() with capture array**: The third argument to `match()` that captures subexpressions is a GNU AWK extension

### Why It Wasn't Caught Earlier

- **Local development environment** has GNU AWK 5.3.2 which supports this syntax ✅
- **Docker build environment** (`mcr.microsoft.com/dotnet/sdk:10.0`) has a different AWK implementation (likely mawk or BusyBox awk) that doesn't support these GNU-specific features ❌
- **Tests passed locally** but failed in CI/CD pipeline

### Contributing Factors

1. The script was created as part of the Cumulative Release Notes feature (commit d3d89a8)
2. Tests were written and validated only on GNU AWK
3. No POSIX AWK compatibility testing was in place

## Impact Assessment

**Severity:** Blocker  
**Scope:** Release workflow completely blocked  
**User Impact:** No Docker image releases possible until fixed  
**Duration:** Since December 19, 2025 22:41Z (approximately 24 hours)

## Solution Implemented

### Changes Made

**1. Modified [scripts/extract-changelog.sh](../../../scripts/extract-changelog.sh)** to use POSIX-compatible AWK syntax:

```awk
function header_version(line) {
    if (line ~ /^##[[:space:]]+\[?v?[0-9]+\.[0-9]+\.[0-9]+\]?/) {
        match(line, /[0-9]+\.[0-9]+\.[0-9]+/);
        return substr(line, RSTART, RLENGTH);
    }
    return "";
}
```

**Changes:**
- Removed function-local variable declaration (just `line` parameter now)
- Replaced `match()` with capture array with two-step approach:
  1. First check if line matches the pattern using `~`
  2. Then use `match()` without capture array
  3. Extract matched text using `substr(line, RSTART, RLENGTH)`

**2. Added regression test** in [tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs](../../../tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs):

```csharp
[Fact]
public void Works_with_posix_awk()
{
    SkipIfBashUnavailable();

    var output = RunExtraction("changelog-full.md", "0.12.0", "0.11.0", usePosixAwk: true);

    var expected = """
<a name="0.12.0"></a>
## [0.12.0] - 2025-12-18

### ✨ Features
* feature twelve

### 🐛 Bug Fixes
* fix twelve
""";

    Normalize(output).Should().Be(Normalize(expected));
}
```

The test runs the script with `POSIXLY_CORRECT=1` environment variable to enforce POSIX-only AWK behavior.

### Verification

**All tests pass locally:**
```
Test Run Successful.
Total tests: 111
     Passed: 105
     Skipped: 6 (Docker not available)
 Total time: 0.7s
```

**POSIX AWK compatibility test passes:**
```
Test Run Successful.
Total tests: 1
     Passed: 1
 Total time: 0.5s
```

**Code formatting compliant:** `dotnet format --verify-no-changes` passes

## Lessons Learned

### What Went Well
- Comprehensive test suite caught the issue in CI/CD before production deployment
- Root cause was quickly identified from error messages
- Fix was straightforward once the problem was understood

### What Could Be Improved
1. **POSIX compatibility testing should be standard** for all shell scripts
2. **Docker-based testing** should be part of local development workflow
3. **Pre-commit hooks** could include AWK syntax validation

### Preventive Measures

**Short-term:**
- ✅ Added POSIX AWK compatibility test
- ✅ Modified script to use POSIX-only features

**Long-term recommendations:**
1. Add `shellcheck` to pre-commit hooks for shell script linting
2. Consider running a subset of tests in Docker during local development
3. Document AWK compatibility requirements in contribution guidelines
4. Add CI check that runs with `POSIXLY_CORRECT=1` for all shell script tests

## Related Files

- [scripts/extract-changelog.sh](../../../scripts/extract-changelog.sh) - Fixed AWK script
- [tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs](../../../tests/Oocx.TfPlan2Md.Tests/Workflows/ChangelogExtractionTests.cs) - Test suite with new POSIX compatibility test
- [Dockerfile](../../../Dockerfile) - Build configuration that runs tests
- [.github/workflows/release.yml](../../../.github/workflows/release.yml) - Release workflow that uses the script

## References

- Failed workflow run: https://github.com/oocx/tfplan2md/actions/runs/20384419278
- GNU AWK vs POSIX AWK differences: https://www.gnu.org/software/gawk/manual/html_node/POSIX.html
- Feature that introduced the script: Cumulative Release Notes (commit d3d89a8)
