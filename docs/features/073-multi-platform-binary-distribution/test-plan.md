# Test Plan: Multi-Platform Binary Distribution (Phase 1: Linux x64)

## Overview

This test plan defines the validation strategy for Phase 1 of ADR-008: Multi-Platform Binary Distribution. Since this feature involves **GitHub Actions workflow changes only** (no application code changes), testing focuses on workflow validation, artifact verification, and end-to-end user scenarios rather than unit/integration tests.

**Feature Specification:** `docs/features/073-multi-platform-binary-distribution/specification.md`  
**Architecture Document:** `docs/features/073-multi-platform-binary-distribution/architecture.md`  
**ADR Reference:** `docs/adr-008-multi-platform-binary-distribution.md`

## Testing Strategy

### Key Principles

1. **No Application Code Tests Needed**: This feature modifies `.github/workflows/release.yml` only. No `.cs` files are changed, so no new unit/integration tests are required.

2. **Workflow-Centric Testing**: Primary testing approach is to execute the release workflow on a test branch and validate outputs.

3. **Manual Artifact Verification**: Built binaries must be downloaded and tested manually to ensure they work in target environments.

4. **Regression Testing**: Ensure existing Docker build and release processes remain unchanged.

5. **Automated Validation Built Into Workflow**: The workflow itself includes validation steps (smoke tests, checksum verification) that act as inline tests.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type | Tester |
|---------------------|--------------|-----------|---------|
| Release workflow successfully builds linux-x64 binary | TC-01, TC-02 | Workflow Validation | Developer |
| Binary is packaged as `tfplan2md_<version>_linux_x64.tar.gz` | TC-03 | Artifact Validation | Developer |
| SHA256SUMS file is generated and correct | TC-04, TC-05 | Artifact Validation | Developer |
| Assets uploaded to GitHub Release | TC-06 | Integration | Developer |
| Binary can be downloaded, verified, extracted, executed on Ubuntu 22.04+ | TC-07, TC-08, TC-09, TC-10 | End-to-End | Developer/Manual |
| Binary produces correct tfplan2md output | TC-11 | Functional | Developer/Manual |
| Docker image build remains unchanged | TC-12 | Regression | Developer |
| Release workflow time ≤ 10 minutes added | TC-13 | Performance | Developer |
| Checksum verification passes | TC-05, TC-08 | Security | Developer/Manual |

## Test Cases

### TC-01: Workflow Executes Without Errors

**Type:** Workflow Validation

**Description:**
Verify that the modified release workflow completes successfully when triggered by a tag push on a test branch.

**Preconditions:**
- Feature branch `copilot/implement-linux-x64-binary` contains workflow changes
- .NET SDK with Native AOT support is available on GitHub Actions `ubuntu-latest` runner
- GitHub Actions has `contents: write` permission

**Test Steps:**
1. Create a test tag on the feature branch (e.g., `v0.0.0-test-linux-x64`)
2. Push the tag to trigger the release workflow
3. Monitor the workflow execution in GitHub Actions UI
4. Check that all jobs complete with success status: `release`, `build-linux-x64-binary`, `docker`

**Expected Result:**
- Workflow completes with green checkmarks on all jobs
- No job failures, timeouts, or errors in logs
- All three jobs (release, build-linux-x64-binary, docker) show "Success" status

**Pass/Fail Criteria:**
- **Pass**: All jobs succeed, workflow badge shows success
- **Fail**: Any job fails or workflow shows error/cancelled status

**Tester:** Developer (during implementation)

---

### TC-02: Binary Build Job Completes in Expected Time

**Type:** Performance

**Description:**
Verify that the `build-linux-x64-binary` job completes within reasonable time (≤ 10 minutes).

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Open the completed workflow run from TC-01
2. Navigate to the `build-linux-x64-binary` job
3. Check the total job duration displayed in GitHub Actions UI

**Expected Result:**
- Job completes in approximately 3-5 minutes
- Maximum acceptable time: 10 minutes

**Pass/Fail Criteria:**
- **Pass**: Job duration ≤ 10 minutes
- **Fail**: Job duration > 10 minutes (indicates performance issue)

**Tester:** Developer

---

### TC-03: Binary Archive Created With Correct Naming

**Type:** Artifact Validation

**Description:**
Verify that the tar.gz archive is created with the correct naming convention following OpenTofu pattern.

**Preconditions:**
- TC-01 passes (workflow executes)
- Test tag version is known (e.g., `0.0.0-test-linux-x64`)

**Test Steps:**
1. Navigate to the GitHub Release page for the test tag
2. Locate the release assets section
3. Find the linux-x64 binary archive in the asset list

**Expected Result:**
- Archive is present with name: `tfplan2md_<version>_linux_x64.tar.gz`
- Example: `tfplan2md_0.0.0-test-linux-x64_linux_x64.tar.gz`
- No other binary archives are present (Phase 1 is linux-x64 only)

**Pass/Fail Criteria:**
- **Pass**: Archive present with exact naming format
- **Fail**: Archive missing, incorrectly named, or unexpected archives present

**Tester:** Developer

---

### TC-04: SHA256SUMS File Generated With Correct Format

**Type:** Artifact Validation

**Description:**
Verify that the SHA256SUMS file is generated in standard `sha256sum` format.

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Navigate to the GitHub Release page for the test tag
2. Download the `SHA256SUMS` file
3. Inspect the file contents

**Expected Result:**
- File contains exactly one line (Phase 1 has one binary)
- Format: `<64-hex-chars>  tfplan2md_<version>_linux_x64.tar.gz`
- Two spaces between checksum and filename
- No extra whitespace or comments

**Pass/Fail Criteria:**
- **Pass**: File format matches expected pattern exactly
- **Fail**: Missing, incorrect format, or extra/missing content

**Tester:** Developer

---

### TC-05: Workflow Validation Step Verifies Checksums

**Type:** Workflow Validation / Security

**Description:**
Verify that the workflow's built-in validation step successfully verifies checksums before upload.

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Open the workflow run logs for `build-linux-x64-binary` job
2. Locate the "Validate Artifacts" step
3. Check the output of the `sha256sum -c SHA256SUMS` command

**Expected Result:**
- Validation step completes successfully
- Output shows: `tfplan2md_<version>_linux_x64.tar.gz: OK`
- No checksum mismatches or failures

**Pass/Fail Criteria:**
- **Pass**: Validation step succeeds, checksums match
- **Fail**: Validation fails, checksum mismatch, or step is missing

**Tester:** Developer

---

### TC-06: Binary Assets Uploaded to GitHub Release

**Type:** Integration

**Description:**
Verify that both the binary archive and SHA256SUMS file are successfully uploaded to the GitHub Release.

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Navigate to the GitHub Release page for the test tag
2. Check the "Assets" section
3. Count and identify all release assets

**Expected Result:**
- Two assets are present:
  1. `tfplan2md_<version>_linux_x64.tar.gz`
  2. `SHA256SUMS`
- Assets have download links
- Assets show file sizes (tar.gz should be several MB, SHA256SUMS should be <1KB)

**Pass/Fail Criteria:**
- **Pass**: Both assets present and downloadable
- **Fail**: Either asset missing or not downloadable

**Tester:** Developer

---

### TC-07: Binary Archive Extraction Succeeds

**Type:** End-to-End

**Description:**
Verify that the tar.gz archive can be downloaded and extracted on a Linux system.

**Preconditions:**
- TC-06 passes (assets uploaded)
- Test environment: Ubuntu 22.04 or later (or compatible glibc-based Linux)

**Test Steps:**
1. Download the tar.gz archive from the GitHub Release
2. Run: `tar -tzf tfplan2md_<version>_linux_x64.tar.gz` to list contents
3. Verify the archive contains only one file: `tfplan2md`
4. Run: `tar -xzf tfplan2md_<version>_linux_x64.tar.gz` to extract
5. Run: `ls -lh tfplan2md` to check file permissions

**Expected Result:**
- Archive lists show single file: `tfplan2md`
- Extraction succeeds without errors
- Extracted binary has execute permissions (`-rwxr-xr-x` or similar)

**Pass/Fail Criteria:**
- **Pass**: Archive extracts successfully, binary has execute bit set
- **Fail**: Extraction fails, binary missing, or no execute permission

**Tester:** Developer (manual verification)

---

### TC-08: Checksum Verification Succeeds

**Type:** End-to-End / Security

**Description:**
Verify that the downloaded binary archive passes checksum verification using the SHA256SUMS file.

**Preconditions:**
- TC-07 passes (archive extracted)
- Both tar.gz and SHA256SUMS files downloaded to same directory

**Test Steps:**
1. Ensure both files are in the same directory
2. Run: `sha256sum -c SHA256SUMS --ignore-missing`
3. Check the command output and exit code

**Expected Result:**
- Command outputs: `tfplan2md_<version>_linux_x64.tar.gz: OK`
- Command exits with code 0 (success)
- No warnings or errors

**Pass/Fail Criteria:**
- **Pass**: Checksum verification succeeds
- **Fail**: Checksum mismatch or command fails

**Tester:** Developer (manual verification)

---

### TC-09: Binary Executes and Shows Help Message

**Type:** End-to-End / Smoke Test

**Description:**
Verify that the extracted binary is a valid executable and displays the help message.

**Preconditions:**
- TC-07 passes (binary extracted)
- Test environment: Ubuntu 22.04 or later

**Test Steps:**
1. Navigate to the directory containing the extracted `tfplan2md` binary
2. Run: `./tfplan2md --help`
3. Check the output and exit code

**Expected Result:**
- Binary executes without "cannot execute binary" or "file not found" errors
- Help message is displayed showing usage information
- Exit code is 0 (success)
- No runtime errors (e.g., missing dependencies, glibc version issues)

**Pass/Fail Criteria:**
- **Pass**: Binary executes and displays help message
- **Fail**: Binary fails to execute, shows errors, or displays no output

**Tester:** Developer (manual verification)

---

### TC-10: Binary Runs Without .NET Runtime Dependency

**Type:** End-to-End / Functional

**Description:**
Verify that the binary is self-contained and does not require .NET runtime installation.

**Preconditions:**
- TC-09 passes (binary executes)
- Test environment: Fresh Ubuntu 22.04 VM/container **without** .NET SDK/runtime installed

**Test Steps:**
1. Prepare a clean Ubuntu 22.04 environment with no .NET SDK or runtime
2. Transfer only the `tfplan2md` binary to this environment
3. Run: `./tfplan2md --help`
4. Verify execution succeeds

**Expected Result:**
- Binary executes successfully without .NET runtime
- No "dotnet not found" or similar errors
- Help message displays correctly

**Pass/Fail Criteria:**
- **Pass**: Binary runs in environment without .NET
- **Fail**: Binary requires .NET runtime or fails to execute

**Tester:** Developer (manual verification on clean environment)

**Note:** This test validates the Native AOT self-contained requirement.

---

### TC-11: Binary Processes Terraform Plan JSON Correctly

**Type:** Functional / End-to-End

**Description:**
Verify that the binary produces correct markdown output when given a valid Terraform plan JSON file.

**Preconditions:**
- TC-09 passes (binary executes)
- Sample Terraform plan JSON available (use `examples/azure_cdn.json` or similar from test data)

**Test Steps:**
1. Copy a known Terraform plan JSON file to the test environment (e.g., `examples/azure_cdn.json`)
2. Run: `./tfplan2md azure_cdn.json > output.md`
3. Inspect the generated `output.md` file
4. Compare output structure to expected markdown format (resources, changes, summaries)

**Expected Result:**
- Command executes successfully with exit code 0
- `output.md` file is created
- Output contains expected markdown sections:
  - Summary tables (e.g., "Resources to be created/updated/destroyed")
  - Resource details (formatted Terraform resources)
  - No error messages or stack traces in output
- Output matches the format produced by the Docker version (can use Docker for comparison)

**Pass/Fail Criteria:**
- **Pass**: Output is valid markdown with expected content
- **Fail**: Command fails, output is empty/malformed, or contains errors

**Tester:** Developer (manual verification)

**Test Data:** `examples/azure_cdn.json` (or any valid plan JSON from repo)

---

### TC-12: Docker Build Remains Unchanged and Succeeds

**Type:** Regression

**Description:**
Verify that the existing Docker build process is unaffected by the binary build workflow changes.

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Open the workflow run from TC-01
2. Navigate to the `docker` job
3. Check the job status and duration
4. Navigate to Docker Hub (or GitHub Container Registry)
5. Verify the Docker image was pushed with correct tags

**Expected Result:**
- `docker` job completes successfully
- Job duration is similar to previous releases (~5-7 minutes, no significant increase)
- Docker image is available on Docker Hub with version tag
- Docker image size remains approximately 14.7MB (Alpine Native AOT)

**Pass/Fail Criteria:**
- **Pass**: Docker job succeeds, image pushed, size unchanged
- **Fail**: Docker job fails or image is not available/incorrect

**Tester:** Developer

---

### TC-13: Parallel Execution Keeps Total Workflow Time Acceptable

**Type:** Performance

**Description:**
Verify that adding the binary build does not significantly increase total release workflow time due to parallel execution.

**Preconditions:**
- TC-01 passes (workflow executes)

**Test Steps:**
1. Open the completed workflow run from TC-01
2. Check the total workflow duration (from start of `release` job to completion of last job)
3. Compare to expected time:
   - `release` job: ~1-2 minutes
   - `build-linux-x64-binary` and `docker` jobs: run in parallel, max(3-5 min, 5-7 min) = ~5-7 minutes
   - Total expected: ~6-9 minutes

**Expected Result:**
- Total workflow time is ≤ 10 minutes
- Binary and Docker jobs overlap in execution (run in parallel)

**Pass/Fail Criteria:**
- **Pass**: Total workflow time ≤ 10 minutes
- **Fail**: Total time > 10 minutes (indicates jobs are not running in parallel or performance issue)

**Tester:** Developer

---

## Edge Cases and Error Scenarios

### EC-01: Binary Build Failure Does Not Block Docker Build

**Description:** Verify independent job failure handling.

**Test Approach:**
- Intentionally break the binary build (e.g., invalid RID or missing file)
- Verify that Docker build still completes successfully
- Confirm release exists with Docker image but no binary assets

**Expected Behavior:**
- Docker job succeeds independently
- Release is created with Docker image only
- Binary build job shows failure but does not cancel other jobs

**Tester:** Developer (optional, can be tested by introducing temporary error)

---

### EC-02: Checksum Mismatch Prevents Upload

**Description:** Verify that workflow validation catches corrupted archives.

**Test Approach:**
- This is inherently tested by TC-05 (validation step in workflow)
- If checksum validation fails, the workflow step should exit with error before upload

**Expected Behavior:**
- Workflow stops at validation step if checksums don't match
- No assets are uploaded to GitHub Release
- Job shows failure status

**Tester:** Developer (verified via TC-05)

---

### EC-03: Archive Extraction Failure

**Description:** Verify behavior when tar.gz is corrupted.

**Test Approach:**
- Simulate by manually corrupting the downloaded archive
- Attempt extraction with `tar -xzf`

**Expected Behavior:**
- `tar` command fails with clear error message (e.g., "corrupted archive")
- User can re-download from GitHub Release

**Tester:** Developer (manual, optional validation)

---

### EC-04: Binary Execution on Non-x64 Architecture

**Description:** Verify appropriate error when binary is run on incompatible architecture (e.g., ARM).

**Test Approach:**
- Transfer linux-x64 binary to a linux-arm64 system
- Attempt to execute: `./tfplan2md --help`

**Expected Behavior:**
- Clear error message from OS: `cannot execute binary file: Exec format error`
- This is expected behavior (architecture mismatch)

**Tester:** Developer (optional, out of scope for Phase 1)

---

### EC-05: Binary Execution on Alpine Linux (musl vs glibc)

**Description:** Verify expected failure on musl-based systems (Alpine Linux).

**Test Approach:**
- Transfer linux-x64 binary to Alpine Linux container
- Attempt to execute: `./tfplan2md --help`

**Expected Behavior:**
- Error: `/bin/sh: ./tfplan2md: not found` (glibc dependency not found)
- This is expected behavior per specification (glibc required, musl support is Phase 3)

**Tester:** Developer (optional, out of scope for Phase 1)

---

## Test Data Requirements

**No new test data files required.** This feature tests workflow and artifacts, not application logic.

**Test Data for Functional Validation (TC-11):**
- Use existing Terraform plan JSON files from `examples/` directory:
  - `examples/azure_cdn.json`
  - `examples/simple_diff.json`
  - Any other valid plan JSON

**Test Environments Needed:**
- **GitHub Actions**: ubuntu-latest runner (provided by GitHub)
- **Manual Testing**: Ubuntu 22.04 VM/container for end-to-end validation (TC-07 through TC-11)

---

## Non-Functional Tests

### Performance

- **TC-02**: Binary build completes in ≤ 10 minutes
- **TC-13**: Total workflow time remains acceptable (≤ 10 minutes added)

### Compatibility

- **TC-10**: Binary runs without .NET runtime (self-contained verification)
- **TC-11**: Binary produces correct output (functional compatibility)

### Security

- **TC-04**: Checksums generated in standard format
- **TC-05**: Workflow validates checksums before upload
- **TC-08**: User can verify checksums with standard tools

### Reliability

- **EC-01**: Binary build failure does not affect Docker build (independent jobs)

---

## Workflow Testing Strategy

### Pre-Merge Testing (Developer Responsibility)

**Test Workflow on Feature Branch:**

1. **Create a test tag on the feature branch:**
   ```bash
   git checkout copilot/implement-linux-x64-binary
   git tag v0.0.0-test-binary-build
   git push origin v0.0.0-test-binary-build
   ```

2. **Monitor workflow execution:**
   - Go to GitHub Actions → Release workflow
   - Verify all jobs succeed (release, build-linux-x64-binary, docker)

3. **Validate artifacts:**
   - Check GitHub Releases for the test tag
   - Download `tfplan2md_0.0.0-test-binary-build_linux_x64.tar.gz`
   - Download `SHA256SUMS`
   - Run TC-07 through TC-11 (extraction, checksum, execution, functional test)

4. **Clean up test tag and release:**
   ```bash
   # Delete test tag locally and remotely
   git tag -d v0.0.0-test-binary-build
   git push origin :refs/tags/v0.0.0-test-binary-build
   
   # Delete test release via GitHub UI or gh CLI
   gh release delete v0.0.0-test-binary-build --yes
   ```

### Post-Merge Testing (First Real Release)

**After PR Merge:**

1. **Merge to main and wait for automatic release workflow (triggered by CI)**
2. **Validate real release:**
   - Verify workflow succeeds on main branch
   - Check GitHub Release for proper version tag (e.g., `v1.12.0`)
   - Validate binary assets are present
   - Perform end-to-end validation (TC-07 through TC-11)

3. **Announce to users:**
   - Release notes should mention Linux x64 binary availability
   - Link to ADR-008 for Phase 2/3 roadmap

---

## Test Execution Plan

### Phase 1: Developer Testing (During Implementation)

**Executor:** Developer agent (during implementation)

**Tests:**
- All workflow validation tests (TC-01, TC-02, TC-05)
- All artifact validation tests (TC-03, TC-04, TC-06)
- Regression test (TC-12)
- Performance test (TC-13)

**Method:**
- Trigger release workflow on feature branch with test tag
- Monitor GitHub Actions logs and outputs
- Verify workflow completes without errors

**Success Criteria:**
- All workflow tests pass
- Artifacts are generated and uploaded
- Docker build is unaffected

---

### Phase 2: Manual Verification (Developer-Executed)

**Executor:** Developer (manual testing after workflow succeeds)

**Tests:**
- All end-to-end tests (TC-07 through TC-11)
- Security test (TC-08)

**Method:**
- Download artifacts from test release
- Execute validation steps on Ubuntu 22.04 system
- Verify binary functionality

**Success Criteria:**
- Binary extracts, executes, and produces correct output
- Checksum verification succeeds

---

### Phase 3: Maintainer Approval (Before Merge)

**Executor:** Maintainer (review and approval)

**Tests:**
- Review test results from Phases 1 and 2
- Spot-check: Download binary from test release and run smoke test
- Review workflow changes in PR

**Success Criteria:**
- Developer reports all tests passing
- Maintainer confirms binary works as expected
- PR approved for merge

---

## Open Questions

1. **Test Release Cleanup**: Should test releases be deleted after validation, or kept for historical reference?
   - **Recommendation**: Delete test releases to avoid cluttering the releases page

2. **Binary Testing in CI**: Should we add automated end-to-end testing for the binary in CI (Phase 2)?
   - **Recommendation**: Defer to Phase 2 when multiple platforms are available (can test matrix of binaries)

3. **Version Naming for Test Tags**: What version format should be used for test tags?
   - **Recommendation**: Use `v0.0.0-test-<description>` format (e.g., `v0.0.0-test-binary-build`)

---

## Definition of Done

The test plan is complete and testing is successful when:

- [ ] Test plan document is approved by Maintainer
- [ ] Developer has executed workflow validation tests (TC-01 through TC-06) on feature branch
- [ ] Developer has executed manual end-to-end tests (TC-07 through TC-11) on Ubuntu 22.04
- [ ] All test cases pass (100% pass rate)
- [ ] Regression test confirms Docker build is unaffected (TC-12)
- [ ] Performance test confirms acceptable workflow time (TC-13)
- [ ] Test results are documented (pass/fail status recorded)
- [ ] Any test failures are resolved before merge
- [ ] Maintainer has reviewed and approved test results

---

## Test Results Summary

_This section will be populated during implementation by the Developer agent._

| Test Case | Status | Date | Notes |
|-----------|--------|------|-------|
| TC-01: Workflow Executes Without Errors | ⏳ Pending | - | Awaiting implementation |
| TC-02: Binary Build Job Completes in Expected Time | ⏳ Pending | - | - |
| TC-03: Binary Archive Created With Correct Naming | ⏳ Pending | - | - |
| TC-04: SHA256SUMS File Generated With Correct Format | ⏳ Pending | - | - |
| TC-05: Workflow Validation Step Verifies Checksums | ⏳ Pending | - | - |
| TC-06: Binary Assets Uploaded to GitHub Release | ⏳ Pending | - | - |
| TC-07: Binary Archive Extraction Succeeds | ⏳ Pending | - | - |
| TC-08: Checksum Verification Succeeds | ⏳ Pending | - | - |
| TC-09: Binary Executes and Shows Help Message | ⏳ Pending | - | - |
| TC-10: Binary Runs Without .NET Runtime Dependency | ⏳ Pending | - | - |
| TC-11: Binary Processes Terraform Plan JSON Correctly | ⏳ Pending | - | - |
| TC-12: Docker Build Remains Unchanged and Succeeds | ⏳ Pending | - | - |
| TC-13: Parallel Execution Keeps Total Workflow Time Acceptable | ⏳ Pending | - | - |

**Legend:**
- ⏳ Pending: Not yet executed
- ✅ Pass: Test passed successfully
- ❌ Fail: Test failed (see notes)
- ⚠️ Warning: Test passed with minor issues (see notes)

---

## References

- **Feature Specification**: `docs/features/073-multi-platform-binary-distribution/specification.md`
- **Architecture Document**: `docs/features/073-multi-platform-binary-distribution/architecture.md`
- **ADR-008**: `docs/adr-008-multi-platform-binary-distribution.md`
- **Current Release Workflow**: `.github/workflows/release.yml`
- **Testing Strategy**: `docs/testing-strategy.md`
- **Issue #073**: [Feature]: Publish pre-built binaries for multiple architectures

---

## Approval

**Quality Engineer**: Test plan complete and ready for Maintainer review.  
**Maintainer**: _[Pending approval]_

Once approved, handoff to **Developer** agent for implementation and testing execution.
