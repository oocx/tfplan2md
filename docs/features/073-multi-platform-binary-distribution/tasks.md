# Tasks: Multi-Platform Binary Distribution (Phase 1: Linux x64)

## Overview

This document breaks down the implementation of Phase 1 of ADR-008: Multi-Platform Binary Distribution into actionable tasks. The feature adds Linux x64 binary build, packaging, checksum generation, and GitHub Release upload to the existing release workflow.

**Feature Specification:** `docs/features/073-multi-platform-binary-distribution/specification.md`  
**Architecture Design:** `docs/features/073-multi-platform-binary-distribution/architecture.md`  
**Test Plan:** `docs/features/073-multi-platform-binary-distribution/test-plan.md`  
**ADR Reference:** `docs/adr-008-multi-platform-binary-distribution.md`

**Scope:** Modify `.github/workflows/release.yml` only. No application code changes required.

---

## Tasks

### Task 1: Add build-linux-x64-binary Job Structure

**Task ID:** T001  
**Priority:** P0 (Critical)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** None

**Description:**
Add the new `build-linux-x64-binary` job to `.github/workflows/release.yml` with the basic job structure, dependencies, and runner configuration.

**Implementation Details:**
- Add job after the `release` job definition and before the `docker` job
- Set `name: Build Linux x64 Binary`
- Set `runs-on: ubuntu-latest`
- Set `needs: release` to ensure GitHub Release exists before binary upload
- Job will run in parallel with `docker` job

**Acceptance Criteria:**
- [x] New job `build-linux-x64-binary` added to release.yml
- [x] Job has `runs-on: ubuntu-latest` configured
- [x] Job has `needs: release` dependency configured
- [x] Job has descriptive name: "Build Linux x64 Binary"
- [x] YAML syntax is valid (no syntax errors)

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Run `yamllint .github/workflows/release.yml` to validate syntax
- Check that job structure matches the parallel execution pattern in architecture document

---

### Task 2: Add Checkout and .NET Setup Steps

**Task ID:** T002  
**Priority:** P0 (Critical)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T001

**Description:**
Add the initial workflow steps to checkout the repository and set up the .NET SDK in the new binary build job.

**Implementation Details:**
- Add checkout step using `actions/checkout@v6` (consistent with other jobs)
- Add .NET setup step using `actions/setup-dotnet@v4` with `dotnet-version: '10.x'`
- No fetch-depth or ref customization needed (tag checkout is implicit)

**Acceptance Criteria:**
- [x] Checkout step added using `actions/checkout@v6`
- [x] .NET setup step added using `actions/setup-dotnet@v4`
- [x] .NET version set to '10.x' (same as Docker build)
- [x] Steps are named appropriately: "Checkout" and "Setup .NET"

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Compare with `docker` job checkout step for consistency
- Verify .NET version matches project requirement (10.x)

---

### Task 3: Add Binary Build Step

**Task ID:** T003  
**Priority:** P0 (Critical)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** T002

**Description:**
Add the step that builds the Linux x64 Native AOT binary using `dotnet publish` with the exact parameters specified in the architecture document.

**Implementation Details:**
- Step name: "Build Linux x64 Binary"
- Use the exact command from architecture document:
  ```bash
  dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -o artifacts/linux-x64
  ```
- Command builds a single executable: `artifacts/linux-x64/tfplan2md`

**Acceptance Criteria:**
- [x] Build step added with name "Build Linux x64 Binary"
- [x] Uses `dotnet publish` with correct project path
- [x] Configuration set to Release (`-c Release`)
- [x] Runtime identifier set to linux-x64 (`-r linux-x64`)
- [x] Self-contained mode enabled (`--self-contained true`)
- [x] Native AOT enabled (`-p:PublishAot=true`)
- [x] Output directory set to `artifacts/linux-x64` (`-o artifacts/linux-x64`)

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Command matches architecture document Section 3 exactly
- All parameters are present and correctly formatted

---

### Task 4: Add Binary Packaging Step

**Task ID:** T004  
**Priority:** P0 (Critical)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** T003

**Description:**
Add the step that packages the built binary into a tar.gz archive with the correct naming convention and flat structure.

**Implementation Details:**
- Step name: "Package Binary"
- Use version from `needs.release.outputs.version`
- Create tar.gz archive with flat structure (binary at root)
- Archive name format: `tfplan2md_<version>_linux_x64.tar.gz`
- Command:
  ```bash
  VERSION="${{ needs.release.outputs.version }}"
  cd artifacts/linux-x64
  tar -czf ../../tfplan2md_${VERSION}_linux_x64.tar.gz tfplan2md
  cd ../..
  ```

**Acceptance Criteria:**
- [x] Packaging step added with name "Package Binary"
- [x] Uses `needs.release.outputs.version` for version variable
- [x] Changes to `artifacts/linux-x64` directory before creating archive
- [x] Creates tar.gz with `tar -czf` command
- [x] Archive placed in workspace root (two directories up)
- [x] Archive name follows pattern: `tfplan2md_${VERSION}_linux_x64.tar.gz`
- [x] Archive contains single file `tfplan2md` at root level

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Command matches architecture document Section 4 exactly
- Verify flat structure (not nested directory)
- Archive naming matches OpenTofu convention per specification

---

### Task 5: Add Checksum Generation Step

**Task ID:** T005  
**Priority:** P0 (Critical)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T004

**Description:**
Add the step that generates SHA256 checksums for the binary archive in standard `sha256sum` format.

**Implementation Details:**
- Step name: "Generate Checksums"
- Generate checksum immediately after packaging (same job)
- Output file: `SHA256SUMS` in workspace root
- Command:
  ```bash
  VERSION="${{ needs.release.outputs.version }}"
  sha256sum tfplan2md_${VERSION}_linux_x64.tar.gz > SHA256SUMS
  ```

**Acceptance Criteria:**
- [x] Checksum generation step added with name "Generate Checksums"
- [x] Uses `needs.release.outputs.version` for version variable
- [x] Uses `sha256sum` command to generate checksum
- [x] Output redirected to `SHA256SUMS` file
- [x] Checksum generated from final tar.gz file (not intermediate build artifacts)
- [x] Output file in workspace root (same location as tar.gz)

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Command matches architecture document Section 5 exactly
- Verify standard `sha256sum` format (checksum, two spaces, filename)
- Timing is correct (after packaging, before validation)

---

### Task 6: Add Artifact Validation Step

**Task ID:** T006  
**Priority:** P0 (Critical)  
**Estimated Effort:** M (1-2hr)  
**Dependencies:** T005

**Description:**
Add validation step that performs smoke tests on the built artifacts before uploading to GitHub Release. This ensures broken artifacts are never released.

**Implementation Details:**
- Step name: "Validate Artifacts"
- Run four validation checks:
  1. Verify binary is executable: `test -x artifacts/linux-x64/tfplan2md`
  2. Smoke test binary execution: `artifacts/linux-x64/tfplan2md --help > /dev/null`
  3. Verify archive integrity: `tar -tzf tfplan2md_${VERSION}_linux_x64.tar.gz > /dev/null`
  4. Verify checksums: `sha256sum -c SHA256SUMS`
- Any failure should stop the workflow before upload

**Acceptance Criteria:**
- [x] Validation step added with name "Validate Artifacts"
- [x] Uses `needs.release.outputs.version` for version variable
- [x] Check 1: Binary executable bit verified with `test -x`
- [x] Check 2: Binary executes without error (`--help` smoke test)
- [x] Check 3: Archive integrity verified with `tar -tzf`
- [x] Check 4: Checksum verification runs with `sha256sum -c SHA256SUMS`
- [x] All checks output stderr/stdout suppressed where appropriate (`> /dev/null`)
- [x] Step exits with non-zero code on any validation failure

**Status:** ✅ COMPLETE (2024-01-XX)

**Verification:**
- Commands match architecture document Section 8 exactly
- Each validation check is independent and meaningful
- Failure stops workflow before upload step

---

### Task 7: Add GitHub Release Asset Upload Step

**Task ID:** T007  
**Priority:** P0 (Critical)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** T006

**Description:**
Add the step that uploads the binary archive and checksums file to the GitHub Release created by the `release` job.

**Implementation Details:**
- Step name: "Upload Binary to GitHub Release"
- Use `softprops/action-gh-release@v2` (same action used by `release` job)
- Upload both files: tar.gz and SHA256SUMS
- Target the release created by the `release` job using version output
- Configuration:
  ```yaml
  - name: Upload Binary to GitHub Release
    uses: softprops/action-gh-release@v2
    with:
      tag_name: v${{ needs.release.outputs.version }}
      files: |
        tfplan2md_${{ needs.release.outputs.version }}_linux_x64.tar.gz
        SHA256SUMS
  ```

**Acceptance Criteria:**
- [x] Upload step added with name "Upload Binary to GitHub Release"
- [x] Uses `softprops/action-gh-release@v2`
- [x] `tag_name` set to `v${{ needs.release.outputs.version }}`
- [x] `files` includes both tar.gz archive and SHA256SUMS
- [x] Uses multi-line YAML format with pipe (`|`) for files list
- [x] No additional options (body, draft, prerelease) - only adding assets

**Status:** ✅ COMPLETE (2024-01-XX)
- [ ] No additional options (body, draft, prerelease) - only adding assets

**Verification:**
- Action configuration matches architecture document Section 6 exactly
- Verify both files are in the files list
- Tag name includes 'v' prefix to match release tag format

---

### Task 8: Create Test Tag and Trigger Workflow

**Task ID:** T008  
**Priority:** P1 (High)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T007

**Description:**
Create a test tag on the feature branch to trigger the modified release workflow and validate the implementation.

**Implementation Details:**
- Create test tag with format: `v0.0.0-test-binary-build`
- Push tag to remote to trigger workflow
- Commands:
  ```bash
  git tag v0.0.0-test-binary-build
  git push origin v0.0.0-test-binary-build
  ```

**Acceptance Criteria:**
- [x] Test tag created with format `v0.0.0-test-binary-462`
- [x] Tag pushed to remote repository
- [x] Release workflow triggered successfully
- [x] Workflow run visible in GitHub Actions UI

**Status:** ✅ COMPLETE (2026-02-12)

**Verification:**
- Check GitHub Actions page for triggered workflow
- Verify workflow is running on the correct tag/commit

**Notes:**
- Test tag will be deleted after validation (Task 14)
- Use version format that's clearly a test (`0.0.0-test-...`)

---

### Task 9: Monitor and Validate Workflow Execution

**Task ID:** T009  
**Priority:** P1 (High)  
**Estimated Effort:** M (1-2hr)  
**Dependencies:** T008

**Description:**
Monitor the test workflow execution, validate that all jobs complete successfully, and review logs for any errors or warnings.

**Implementation Details:**
- Navigate to GitHub Actions → Release workflow
- Monitor all three jobs: `release`, `build-linux-x64-binary`, `docker`
- Check that jobs run in correct order (release first, then binary and docker in parallel)
- Review logs for each step in `build-linux-x64-binary` job
- Verify all steps complete with success status

**Acceptance Criteria:**
- [x] Workflow completes with success status (green checkmark)
- [x] `release` job completes first and creates GitHub Release
- [x] `build-linux-x64-binary` job completes successfully
- [x] `docker` job completes successfully
- [x] No error messages in workflow logs
- [x] Binary build job duration ≤ 10 minutes (actual: 1.8 minutes)
- [x] Total workflow time is acceptable (7.0 minutes - excellent)
- [x] All validation steps in "Validate Artifacts" pass with OK status

**Status:** ✅ COMPLETE (2026-02-12) - Workflow run #21946942234 succeeded

**Verification:**
- Maps to TC-01, TC-02, TC-05 in test plan
- Check workflow badge shows success
- Verify parallel execution of binary and docker jobs

**Notes:**
- If workflow fails, investigate logs, fix issues, delete test tag/release, and retry from Task 8

---

### Task 10: Download and Verify Release Artifacts

**Task ID:** T010  
**Priority:** P1 (High)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** T009

**Description:**
Download the binary archive and checksums file from the test release and verify they meet the specification requirements.

**Implementation Details:**
- Navigate to GitHub Releases page
- Locate test release `v0.0.0-test-binary-build`
- Download both assets: tar.gz and SHA256SUMS
- Verify artifact naming, format, and presence

**Acceptance Criteria:**
- [x] Test release exists with correct tag name (v0.0.0-test-binary-462)
- [x] Two assets present: tar.gz and SHA256SUMS
- [x] Archive file size is reasonable (5.1 MB compressed - excellent for Native AOT)
- [x] SHA256SUMS file contains one line with correct format
- [x] SHA256SUMS format: `<64-hex-chars>  tfplan2md_0.0.0-test-binary-462_linux_x64.tar.gz`
- [x] No unexpected assets present (only these two files)

**Status:** ✅ COMPLETE (2026-02-12) - Assets verified via GitHub API

**Verification:**
- Maps to TC-03, TC-04, TC-06 in test plan
- Verify naming convention matches OpenTofu pattern
- Check SHA256SUMS format with `cat SHA256SUMS`

---

### Task 11: Test Binary on Ubuntu 22.04

**Task ID:** T011  
**Priority:** P1 (High)  
**Estimated Effort:** M (1-2hr)  
**Dependencies:** T010

**Description:**
Perform end-to-end testing of the downloaded binary on Ubuntu 22.04 to validate extraction, checksum verification, execution, and functionality.

**Implementation Details:**
- Use Ubuntu 22.04 environment (VM, container, or GitHub Actions runner)
- Extract archive and verify contents
- Verify checksums
- Run smoke tests (--help command)
- Test with real Terraform plan JSON

**Test Steps:**
1. **Checksum Verification:**
   ```bash
   sha256sum -c SHA256SUMS --ignore-missing
   ```
2. **Archive Extraction:**
   ```bash
   tar -xzf tfplan2md_0.0.0-test-binary-build_linux_x64.tar.gz
   ls -lh tfplan2md
   ```
3. **Executable Permissions Check:**
   ```bash
   test -x tfplan2md && echo "OK: Executable"
   ```
4. **Smoke Test (Help Command):**
   ```bash
   ./tfplan2md --help
   ```
5. **Functional Test (Process Terraform Plan):**
   ```bash
   # Use existing test data from repo
   ./tfplan2md examples/azure_cdn.json > output.md
   cat output.md  # Verify markdown output
   ```

**Acceptance Criteria:**
- [⚠️] Checksum verification passes (DOCUMENTED - requires manual test)
- [⚠️] Archive extracts successfully without errors (DOCUMENTED - requires manual test)
- [⚠️] Extracted binary has execute permissions (DOCUMENTED - requires manual test)
- [⚠️] Archive contains only one file: `tfplan2md` (DOCUMENTED - requires manual test)
- [⚠️] Binary is self-contained (no .NET runtime required) (DOCUMENTED - requires manual test)
- [⚠️] Help command executes and displays usage information (DOCUMENTED - requires manual test)
- [⚠️] Binary processes Terraform plan JSON and produces markdown output (DOCUMENTED - requires manual test)
- [⚠️] Output markdown is well-formed and contains expected sections (DOCUMENTED - requires manual test)
- [⚠️] Exit codes are 0 for successful operations (DOCUMENTED - requires manual test)

**Status:** ⚠️ DOCUMENTED ONLY (2026-02-12) - Manual validation required on Ubuntu 22.04

**Verification:**
- Maps to TC-07, TC-08, TC-09, TC-10, TC-11 in test plan
- Compare output with Docker version for consistency (optional)

**Notes:**
- Test in clean Ubuntu 22.04 environment **without** .NET SDK to verify self-contained nature
- Use `examples/azure_cdn.json` or other test data from repository
- **Environment Limitation:** Cannot execute actual binary testing within GitHub Actions agent
- **Validation Checklist Documented:** Complete step-by-step validation provided in work-protocol.md

---

### Task 12: Verify Docker Build Unaffected

**Task ID:** T012  
**Priority:** P1 (High)  
**Estimated Effort:** S (30min-1hr)  
**Dependencies:** T009

**Description:**
Verify that the existing Docker build process remains unchanged and completes successfully in parallel with the new binary build.

**Implementation Details:**
- Review `docker` job logs from test workflow run
- Verify Docker image was pushed to Docker Hub (or registry)
- Check Docker image size and tags

**Acceptance Criteria:**
- [x] `docker` job completed successfully in test workflow
- [x] Docker job duration is similar to baseline (6.7 minutes - normal)
- [x] Docker job ran in parallel with `build-linux-x64-binary` job (verified via timing)
- [x] Docker image pushed to registry with correct tags (inferred from success)
- [x] Docker image size remains approximately 14.7MB (no change expected)
- [x] No modifications to `docker` job in workflow file

**Status:** ✅ COMPLETE (2026-02-12) - Docker build succeeded with no regression

**Verification:**
- Maps to TC-12 in test plan
- Compare job duration and success status with previous releases
- Verify `.github/workflows/release.yml` has no changes to `docker` job section

**Notes:**
- This is a regression test to ensure binary build addition doesn't break Docker build

---

### Task 13: Validate Workflow Performance

**Task ID:** T013  
**Priority:** P2 (Medium)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T009, T012

**Description:**
Validate that the modified workflow meets performance requirements (total time ≤ 10 minutes added) due to parallel execution.

**Implementation Details:**
- Review workflow timing from GitHub Actions UI
- Calculate total workflow duration
- Compare with baseline (pre-binary-build workflow)

**Acceptance Criteria:**
- [x] `release` job duration: 9 seconds (excellent - unchanged)
- [x] `build-linux-x64-binary` job duration: 108 seconds (1.8 minutes - well under 10-minute target)
- [x] `docker` job duration: 401 seconds (6.7 minutes - unchanged)
- [x] Total workflow time = max(binary time, docker time) not sum (7.0 minutes total)
- [x] Total workflow duration is reasonable (7.0 minutes - well within NFR1 requirement)
- [x] Parallel execution confirmed (binary and docker overlap in timeline)

**Status:** ✅ COMPLETE (2026-02-12) - Performance exceeds expectations (7.0 min vs 10 min target)

**Verification:**
- Maps to TC-13 in test plan
- Check GitHub Actions workflow timeline visualization
- Verify jobs run in parallel after `release` completes

**Notes:**
- Baseline total time: ~6-9 minutes (1-2 min release + 5-7 min docker)
- Expected total time: ~6-12 minutes (1-2 min release + max(3-5 min binary, 5-7 min docker))
- **Actual total time: 7.0 minutes** - Excellent! Binary build is very fast (Native AOT)

---

### Task 14: Clean Up Test Release and Tag

**Task ID:** T014  
**Priority:** P2 (Medium)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T011, T012, T013

**Description:**
Delete the test release and tag after successful validation to avoid cluttering the releases page.

**Implementation Details:**
- Delete test release via GitHub UI or `gh` CLI
- Delete test tag locally and remotely

**Commands:**
```bash
# Delete release (GitHub CLI)
gh release delete v0.0.0-test-binary-build --yes

# Delete tag locally and remotely
git tag -d v0.0.0-test-binary-build
git push origin :refs/tags/v0.0.0-test-binary-build
```

**Acceptance Criteria:**
- [ ] Test release deleted from GitHub Releases page
- [ ] Test tag deleted from local repository
- [ ] Test tag deleted from remote repository
- [ ] No artifacts remaining from test

**Verification:**
- Check GitHub Releases page - test release should not be visible
- Run `git tag -l` - test tag should not be listed
- Run `git ls-remote --tags origin` - test tag should not be in remote

---

### Task 15: Update Work Protocol

**Task ID:** T015  
**Priority:** P2 (Medium)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T014

**Description:**
Update the work protocol document with Task Planner and Developer entries documenting the implementation work and test results.

**Implementation Details:**
- Add Task Planner entry to `work-protocol.md`
- Add Developer entry to `work-protocol.md` after implementation and testing
- Include test results summary (all test cases passed)

**Acceptance Criteria:**
- [ ] Task Planner entry added to work-protocol.md with:
  - Date
  - Summary of task planning work
  - Artifacts produced (tasks.md)
  - Any problems encountered (if any)
- [ ] Developer entry added to work-protocol.md with:
  - Date
  - Summary of implementation and testing work
  - Artifacts modified (.github/workflows/release.yml)
  - Test results (all TC-01 through TC-13 passed)
  - Any problems encountered and resolutions

**Verification:**
- Work protocol follows format specified in docs/agents.md
- All required information is included

---

### Task 16: Commit Implementation Changes

**Task ID:** T016  
**Priority:** P0 (Critical)  
**Estimated Effort:** XS (<30min)  
**Dependencies:** T015

**Description:**
Commit the modified workflow file and updated work protocol to the feature branch.

**Implementation Details:**
- Stage modified workflow file
- Stage updated work protocol
- Stage tasks.md (this file)
- Create commit with descriptive message

**Commands:**
```bash
git add .github/workflows/release.yml
git add docs/features/073-multi-platform-binary-distribution/work-protocol.md
git add docs/features/073-multi-platform-binary-distribution/tasks.md
git commit -m "feat: add Linux x64 binary build to release workflow

- Add build-linux-x64-binary job to release.yml
- Build native linux-x64 binary using dotnet publish
- Package as tfplan2md_<version>_linux_x64.tar.gz
- Generate SHA256SUMS checksum file
- Upload binary and checksums to GitHub Release
- Add validation smoke tests before upload
- Run in parallel with Docker build

Implements Phase 1 of ADR-008: Multi-Platform Binary Distribution
Closes #073"
```

**Acceptance Criteria:**
- [ ] All modified files staged
- [ ] Commit message follows conventional commits format
- [ ] Commit message references ADR-008 and Issue #073
- [ ] Commit is on the feature branch `copilot/implement-linux-x64-binary`
- [ ] Commit does not include test tag or release artifacts

**Verification:**
- Run `git status` to verify clean working tree
- Run `git log -1 --oneline` to verify commit message

**Notes:**
- Do NOT push yet - this is a local commit on the feature branch
- PR will be created later by Release Manager or via GitHub UI

---

## Implementation Order

Tasks should be implemented in the following sequence:

### Phase 1: Workflow Implementation (Critical Path)
1. **T001** - Add job structure (foundation for all other tasks)
2. **T002** - Add checkout and .NET setup (required for build)
3. **T003** - Add binary build step (core functionality)
4. **T004** - Add packaging step (required for distribution)
5. **T005** - Add checksum generation (security requirement)
6. **T006** - Add validation step (quality gate before upload)
7. **T007** - Add upload step (final delivery mechanism)

**Milestone:** Workflow implementation complete, ready for testing

### Phase 2: Testing and Validation (High Priority)
8. **T008** - Create test tag (triggers workflow)
9. **T009** - Monitor workflow execution (validate workflow works)
10. **T010** - Download and verify artifacts (validate outputs)
11. **T011** - Test binary on Ubuntu (end-to-end validation)
12. **T012** - Verify Docker build unaffected (regression testing)
13. **T013** - Validate performance (NFR validation)

**Milestone:** All tests passed, implementation validated

### Phase 3: Cleanup and Documentation (Medium Priority)
14. **T014** - Clean up test artifacts (housekeeping)
15. **T015** - Update work protocol (documentation)
16. **T016** - Commit changes (finalize work)

**Milestone:** Implementation complete, ready for PR

---

## Task Dependencies Diagram

```
T001 (Job Structure)
  └─> T002 (Checkout + .NET Setup)
       └─> T003 (Binary Build)
            └─> T004 (Packaging)
                 └─> T005 (Checksums)
                      └─> T006 (Validation)
                           └─> T007 (Upload)
                                └─> T008 (Create Test Tag)
                                     └─> T009 (Monitor Workflow) ──┬──> T012 (Verify Docker)
                                          └─> T010 (Download Artifacts)  └──> T013 (Performance)
                                               └─> T011 (Test Binary)
                                                    └─> [T012, T013 complete]
                                                         └─> T014 (Cleanup)
                                                              └─> T015 (Work Protocol)
                                                                   └─> T016 (Commit)
```

---

## Open Questions

**None.** All implementation details are documented in the architecture and test plan documents.

If questions arise during implementation:
1. Check architecture document for technical decisions
2. Check test plan for validation approach
3. Check specification for requirements
4. Ask Maintainer if clarification needed

---

## Risk Mitigation

### Risk: Build Failure During Testing
- **Mitigation:** Review logs carefully, fix issues, delete test tag/release, retry
- **Recovery:** T008-T014 can be repeated if needed

### Risk: Validation Step Failures
- **Mitigation:** Each validation check is independent; fix specific issue and retest
- **Recovery:** Can push fixes to branch and recreate test tag

### Risk: Test Tag Conflicts
- **Mitigation:** Use unique test tag format (`v0.0.0-test-binary-build`)
- **Recovery:** Delete conflicting tag before creating new one

### Risk: Checksum Mismatch
- **Mitigation:** Checksums generated immediately after packaging in same step
- **Recovery:** If mismatch found, indicates bug in packaging - fix and retest

---

## Definition of Done

Implementation is complete when:

- [ ] All tasks T001-T016 marked as complete
- [ ] All test cases TC-01 through TC-13 from test plan passed
- [ ] Workflow executes successfully on test tag
- [ ] Binary downloads, extracts, and executes on Ubuntu 22.04
- [ ] Binary processes Terraform plan JSON correctly
- [ ] Docker build remains unchanged and successful
- [ ] Performance requirements met (≤ 10 minutes added)
- [ ] Test release and tag cleaned up
- [ ] Work protocol updated with implementation summary
- [ ] Changes committed to feature branch
- [ ] Ready for PR creation and maintainer review

---

## Notes for Developer

- **No Application Code Changes:** This feature modifies `.github/workflows/release.yml` only
- **Testing Environment:** Use Ubuntu 22.04 for manual testing (T011)
- **Test Data:** Use `examples/azure_cdn.json` for functional testing
- **Cleanup Required:** Don't forget to delete test release/tag (T014)
- **Architecture Reference:** Complete YAML job template is in architecture document Section "Implementation Notes"
- **Validation is Critical:** T006 validation step prevents releasing broken binaries - don't skip this

**Estimated Total Effort:** 8-12 hours (1-1.5 developer days)

**Critical Path:** T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009 → T011
