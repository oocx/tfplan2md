# Tasks: Homebrew Installation Support

## Overview

This document breaks down Feature 089 (Homebrew Installation Support) into actionable implementation tasks. The feature enables macOS and Linux users to install tfplan2md via Homebrew package manager, leveraging existing multi-platform binaries from Feature 047.

**Specification:** [specification.md](./specification.md)

**Architecture Decisions:**
- [ADR-001: Platform Build Fixes](./adr-001-platform-build-fixes.md)
- [ADR-002: Homebrew Formula Design](./adr-002-homebrew-formula-design.md)  
- [ADR-003: Release Workflow Integration](./adr-003-release-workflow-integration.md)

**Test Plan:** [test-plan.md](./test-plan.md)

## Tasks

### TASK-001: Fix macOS x64 Platform Build

**Priority:** P0 (Critical - Required for Homebrew)

**Description:**
Fix the macOS x64 (Intel) build failure by installing Xcode Command Line Tools on the GitHub Actions macos-13 runner. The .NET NativeAOT compiler requires Xcode toolchain (clang, linker, macOS SDK) to compile native binaries on macOS.

**Files to Modify:**
- `.github/workflows/release.yml`

**Implementation Details:**
Add an Xcode Command Line Tools installation step in the build-binaries job before the "Setup .NET" step (after line 264). The step should:
- Check if we're on a macOS platform using `startsWith(matrix.platform, 'macos-')`
- Run `sudo xcode-select --install` with error suppression (`|| true`)
- Verify installation succeeded by checking `xcode-select -p`
- Exit with error if installation failed

**Acceptance Criteria:**
- [ ] Xcode installation step added to release workflow
- [ ] Step only runs for macos-x64 and macos-arm64 platforms (conditional check)
- [ ] Workflow verifies Xcode tools are installed successfully
- [ ] macos-x64 build completes successfully in GitHub Actions
- [ ] Binary archive `tfplan2md_<version>_macos-x64.tar.gz` is created
- [ ] Build time increases by ~2-3 minutes (acceptable overhead)
- [ ] Test case BUILD-MAC-001 passes

**Dependencies:** None

**Notes:**
- GitHub Actions macos-13 runners do NOT have Xcode CLT pre-installed
- The `xcode-select --install` command may start a GUI installer but will succeed in CI
- Verification with `xcode-select -p` ensures tools are available before .NET setup

**Estimated Effort:** S (1-2h)

---

### TASK-002: Fix macOS ARM64 Platform Build

**Priority:** P0 (Critical - Required for Homebrew)

**Description:**
Fix the macOS ARM64 (Apple Silicon) build failure by installing Xcode Command Line Tools on the GitHub Actions macos-14 runner. This is the same fix as TASK-001 but for ARM64 architecture.

**Files to Modify:**
- `.github/workflows/release.yml` (same file as TASK-001)

**Implementation Details:**
The Xcode installation step from TASK-001 will automatically apply to macos-arm64 since it uses `startsWith(matrix.platform, 'macos-')` condition. No additional changes needed beyond TASK-001.

**Acceptance Criteria:**
- [ ] macos-arm64 build completes successfully in GitHub Actions
- [ ] Binary archive `tfplan2md_<version>_macos-arm64.tar.gz` is created
- [ ] Binary is correct architecture (ARM64, not x64)
- [ ] Build time increases by ~2-3 minutes (acceptable overhead)
- [ ] Test case BUILD-MAC-002 passes

**Dependencies:** TASK-001 (shares the same Xcode installation step)

**Notes:**
- macos-14 runners use Apple Silicon (M1) processors
- Same Xcode installation logic applies to both x64 and ARM64 macOS platforms

**Estimated Effort:** XS (<1h - included in TASK-001)

---

### TASK-003: Remove windows-arm64 from Build Matrix

**Priority:** P0 (Critical - Simplifies workflow)

**Description:**
Remove the `windows-arm64` platform from the build matrix in the release workflow. This platform has been failing due to cross-compilation complexity and represents <1% of potential users. GitHub Actions does not provide native ARM64 Windows runners.

**Files to Modify:**
- `.github/workflows/release.yml`

**Implementation Details:**
Delete the windows-arm64 matrix entry (lines 239-245) from the build-binaries job:
```yaml
# DELETE THIS BLOCK:
- platform: windows-arm64
  os: windows-latest
  rid: win-arm64
  archive_ext: zip
  binary_name: tfplan2md.exe
  container: ''
  needs_clang: false
```

**Acceptance Criteria:**
- [ ] windows-arm64 entry removed from matrix
- [ ] Build matrix shows 5 platforms (not 6): linux-x64, linux-arm64, windows-x64, macos-x64, macos-arm64
- [ ] No windows-arm64 build job runs in GitHub Actions
- [ ] No `tfplan2md_<version>_windows-arm64.zip` artifact created
- [ ] Release workflow completes faster (~3-5 minutes saved)
- [ ] Test case BUILD-WIN-001 passes

**Dependencies:** None

**Notes:**
- This simplifies the workflow and removes a consistently failing build
- Windows ARM64 can be re-added later if native runners become available
- Existing Windows x64 support remains unchanged

**Estimated Effort:** XS (<1h)

---

### TASK-004: Verify Platform Build Regression Testing

**Priority:** P0 (Critical - Quality gate)

**Description:**
After making platform build changes (TASK-001, TASK-002, TASK-003), verify that existing platform builds (linux-x64, linux-arm64, windows-x64) continue to work correctly and that the new macOS builds produce correct binaries.

**Files to Modify:**
None (testing task)

**Implementation Details:**
Trigger a test release workflow run and validate:
1. All 5 platforms build successfully
2. Each platform produces correct architecture binary
3. Checksums are generated for all platforms
4. No regressions in existing platforms

**Acceptance Criteria:**
- [ ] linux-x64 build completes successfully (test case BUILD-REG-001)
- [ ] linux-arm64 build completes successfully
- [ ] windows-x64 build completes successfully (test case BUILD-REG-002)
- [ ] macos-x64 build completes successfully (test case BUILD-MAC-001)
- [ ] macos-arm64 build completes successfully (test case BUILD-MAC-002)
- [ ] All binaries have correct architecture (test cases BUILD-VAL-001, BUILD-VAL-002, BUILD-VAL-003)
- [ ] All binaries are executable and run `--version` successfully
- [ ] consolidate-checksums job completes with 5 platform checksums

**Dependencies:** TASK-001, TASK-002, TASK-003

**Notes:**
- This task validates that platform build fixes don't break existing functionality
- Can be performed as part of the first test release
- Use test tag like `v0.0.0-brew-test` to trigger workflow without creating production release

**Estimated Effort:** S (1-2h)

---

### TASK-005: Create Homebrew Tap Repository

**Priority:** P0 (Critical - Infrastructure prerequisite)

**Description:**
Create the GitHub repository `oocx/homebrew-tfplan2md` to host the Homebrew formula. This is a one-time setup task that must be completed before formula creation and workflow integration.

**Files to Create:**
- Repository: `oocx/homebrew-tfplan2md`
- `Formula/` directory (empty initially)
- `README.md`
- `LICENSE` (MIT, same as main project)
- `.gitignore` (standard GitHub template)

**Implementation Details:**
1. Create new public repository at `https://github.com/oocx/homebrew-tfplan2md`
2. Initialize with README and LICENSE
3. Create `Formula/` directory
4. Populate README with installation instructions (see ADR-002 for template)

**Acceptance Criteria:**
- [ ] Repository `oocx/homebrew-tfplan2md` exists and is public
- [ ] Repository has `Formula/` directory
- [ ] README.md includes installation instructions (tap, install, verify, update, uninstall)
- [ ] LICENSE file contains MIT license
- [ ] Repository is accessible via `brew tap oocx/tfplan2md`
- [ ] Repository follows Homebrew naming conventions (homebrew-* prefix)

**Dependencies:** None (can be done in parallel with other tasks)

**Notes:**
- Repository MUST be public for Homebrew to access it
- Repository name MUST follow Homebrew convention: `homebrew-<tap-name>`
- This is a manual task requiring GitHub repository creation permissions

**Estimated Effort:** XS (<1h)

---

### TASK-006: Create Homebrew Formula Template

**Priority:** P0 (Critical - Core deliverable)

**Description:**
Create the Homebrew formula file `Formula/tfplan2md.rb` with template placeholders for version and checksums. The formula uses conditional platform detection to select the correct binary for macOS x64, macOS ARM64, or Linux x64.

**Files to Create:**
- `Formula/tfplan2md.rb` (in homebrew-tfplan2md repository)

**Implementation Details:**
Create formula file following the structure from ADR-002:
- Class name: `Tfplan2md`
- Metadata: desc, homepage, license, version (with {{VERSION}} placeholder)
- Platform detection: `on_macos` and `on_linux` blocks
- Architecture detection: `Hardware::CPU.intel?` and `Hardware::CPU.arm?`
- URL and SHA256 placeholders for each platform
- Install method: `bin.install "tfplan2md"`
- Test method: verify `--version` and `--help` work

**Acceptance Criteria:**
- [ ] Formula file `Formula/tfplan2md.rb` created in tap repository
- [ ] Formula has correct class name: `Tfplan2md`
- [ ] Metadata fields populated: desc, homepage, license
- [ ] Version placeholder: `{{VERSION}}`
- [ ] URL placeholders for 3 platforms with correct download URLs
- [ ] SHA256 placeholders: `{{MACOS_X64_SHA256}}`, `{{MACOS_ARM64_SHA256}}`, `{{LINUX_X64_SHA256}}`
- [ ] Install method correctly installs binary to bin directory
- [ ] Test method verifies binary runs
- [ ] Formula passes basic Ruby syntax check: `ruby -c Formula/tfplan2md.rb`

**Dependencies:** TASK-005 (tap repository must exist)

**Notes:**
- Placeholders will be replaced by update script in TASK-007
- Formula template should be committed with placeholders intact
- See ADR-002 for complete formula template

**Estimated Effort:** S (1-2h)

---

### TASK-007: Create Formula Update Script

**Priority:** P0 (Critical - Automation prerequisite)

**Description:**
Create the shell script `scripts/update-homebrew-formula.sh` that extracts checksums from the consolidated SHA256SUMS file and updates the Homebrew formula by replacing placeholders with actual version and checksum values.

**Files to Create:**
- `scripts/update-homebrew-formula.sh`

**Implementation Details:**
Create bash script that:
1. Accepts 3 parameters: VERSION, CHECKSUMS_FILE, FORMULA_FILE
2. Extracts SHA256 checksums for macos-x64, macos-arm64, linux-x64 from CHECKSUMS_FILE
3. Validates checksums are present and in correct format (64 hex characters)
4. Uses `sed` to replace placeholders in FORMULA_FILE
5. Provides clear success/error messages

See ADR-003 for complete script implementation.

**Acceptance Criteria:**
- [ ] Script file `scripts/update-homebrew-formula.sh` created
- [ ] Script has executable permissions (`chmod +x`)
- [ ] Script accepts 3 required parameters
- [ ] Script extracts checksums correctly from SHA256SUMS format
- [ ] Script validates all checksums are present (exits with error if missing)
- [ ] Script validates checksum format (64 hex characters)
- [ ] Script replaces all 4 placeholders: {{VERSION}}, {{MACOS_X64_SHA256}}, {{MACOS_ARM64_SHA256}}, {{LINUX_X64_SHA256}}
- [ ] Script provides verbose output for debugging
- [ ] Script exits with code 0 on success, 1 on error
- [ ] Manual test: script successfully updates a test formula file

**Dependencies:** None (can be developed in parallel)

**Notes:**
- Script will be run from GitHub Actions ubuntu-latest runner
- Uses `sed -i` for in-place replacement (Linux syntax, not macOS)
- Validation prevents pushing bad checksums to tap repository

**Estimated Effort:** S (1-2h)

---

### TASK-008: Add Formula Update Job to Release Workflow

**Priority:** P0 (Critical - Automation integration)

**Description:**
Add a new job `update-homebrew-formula` to the release workflow that runs after checksums are consolidated. The job clones the tap repository, runs the update script, and commits/pushes the updated formula.

**Files to Modify:**
- `.github/workflows/release.yml`

**Implementation Details:**
Add new job after line 395 (after consolidate-checksums job) with:
- Job name: `update-homebrew-formula`
- Runs on: `ubuntu-latest`
- Needs: `[release, consolidate-checksums]`
- Conditional: only stable releases (skip prereleases)
- Steps: checkout main repo, download checksums artifact, checkout tap repo, run update script, commit and push

See ADR-003 for complete workflow job specification.

**Acceptance Criteria:**
- [ ] Job `update-homebrew-formula` added to release workflow
- [ ] Job depends on both `release` and `consolidate-checksums` jobs
- [ ] Job only runs for stable releases: `needs.release.outputs.is_prerelease != 'true'`
- [ ] Job checks out main repository to access update script
- [ ] Job downloads `checksums` artifact to get SHA256SUMS file
- [ ] Job checks out tap repository with HOMEBREW_TAP_TOKEN authentication
- [ ] Job runs `scripts/update-homebrew-formula.sh` with correct parameters
- [ ] Job commits changes with conventional commit message: `chore: update formula to v<version>`
- [ ] Job pushes changes to tap repository
- [ ] Job only commits if formula changed (idempotent check)
- [ ] Test case WORKFLOW-UPDATE-001, WORKFLOW-UPDATE-002, WORKFLOW-UPDATE-003, WORKFLOW-UPDATE-004 pass

**Dependencies:** TASK-005 (tap repository), TASK-006 (formula template), TASK-007 (update script)

**Notes:**
- Requires `HOMEBREW_TAP_TOKEN` secret to be configured (see TASK-009)
- Job failure does NOT block GitHub Release creation (graceful degradation)
- Job logs provide debugging information for troubleshooting

**Estimated Effort:** M (2-4h)

---

### TASK-009: Configure GitHub Secret for Tap Repository Access

**Priority:** P0 (Critical - Security prerequisite)

**Description:**
Create a GitHub Personal Access Token (PAT) with write access to the tap repository and add it to the main repository secrets as `HOMEBREW_TAP_TOKEN`. This enables the release workflow to push formula updates to the tap repository.

**Files to Modify:**
None (GitHub settings task)

**Implementation Details:**
1. Create GitHub Personal Access Token (classic or fine-grained):
   - Classic: `repo` scope
   - Fine-grained: Repository access to `oocx/homebrew-tfplan2md`, Permission: `Contents: Read and write`
2. Add token to repository secrets:
   - Go to main repository Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `HOMEBREW_TAP_TOKEN`
   - Value: (paste the PAT)

**Acceptance Criteria:**
- [ ] GitHub Personal Access Token created with appropriate permissions
- [ ] Token has write access to `oocx/homebrew-tfplan2md` repository
- [ ] Secret `HOMEBREW_TAP_TOKEN` added to main repository
- [ ] Secret is accessible in GitHub Actions (test with workflow run)
- [ ] Workflow can successfully checkout and push to tap repository using token

**Dependencies:** TASK-005 (tap repository must exist to grant access)

**Notes:**
- This is a manual task requiring repository admin permissions
- Token should be stored securely (only in GitHub Secrets, not in code)
- Token can be fine-grained for better security (preferred over classic PAT)
- Token may need to be rotated periodically based on organization security policy

**Estimated Effort:** XS (<1h)

---

### TASK-010: Test Formula Update Automation End-to-End

**Priority:** P1 (High - Validation)

**Description:**
Perform end-to-end testing of the complete formula update automation by triggering a test release and verifying that the formula is updated correctly in the tap repository.

**Files to Modify:**
None (testing task)

**Implementation Details:**
1. Create a test tag (e.g., `v0.0.0-brew-test`) to trigger release workflow
2. Monitor workflow execution through all jobs
3. Verify formula update job runs and succeeds
4. Check tap repository for committed formula update
5. Verify formula has correct version and checksums
6. Test installation using Homebrew on macOS or Linux

**Acceptance Criteria:**
- [ ] Test release workflow runs successfully for all jobs
- [ ] Platform builds complete for all 5 platforms
- [ ] Checksums consolidated correctly
- [ ] Formula update job runs (not skipped)
- [ ] Formula update job completes successfully
- [ ] Tap repository has new commit with formula update
- [ ] Commit message follows format: `chore: update formula to v<version>`
- [ ] Formula file has correct version (no {{VERSION}} placeholder)
- [ ] Formula file has valid SHA256 checksums (no placeholder text)
- [ ] Checksums match values in SHA256SUMS file
- [ ] Formula passes syntax check: `brew audit --new-formula tfplan2md`
- [ ] Test case WORKFLOW-UPDATE-001 through WORKFLOW-UPDATE-004 pass
- [ ] Test case INTEGRATION-E2E-001 passes

**Dependencies:** TASK-001 through TASK-009 (all automation must be in place)

**Notes:**
- Use a test tag that can be deleted after testing
- Consider creating a test tap repository for initial testing
- This validates the complete automation pipeline before production use

**Estimated Effort:** M (2-4h)

---

### TASK-011: Test Prerelease Skip Behavior

**Priority:** P1 (High - Validation)

**Description:**
Verify that the formula update job correctly skips prerelease versions and does NOT update the Homebrew formula. This ensures Homebrew users only see stable releases.

**Files to Modify:**
None (testing task)

**Implementation Details:**
1. Create a prerelease tag (e.g., `v0.0.0-beta.1`)
2. Trigger release workflow
3. Verify release job runs and marks release as prerelease
4. Verify formula update job is skipped (conditional check)
5. Verify tap repository has no new commits

**Acceptance Criteria:**
- [ ] Prerelease workflow creates GitHub Release with prerelease flag
- [ ] Formula update job is skipped (shown as "skipped" in Actions UI)
- [ ] Tap repository has no new commits after prerelease workflow
- [ ] Stable release (non-prerelease) still updates formula correctly
- [ ] Test case WORKFLOW-UPDATE-005 passes
- [ ] Test case INTEGRATION-E2E-002 passes

**Dependencies:** TASK-008 (workflow integration with conditional)

**Notes:**
- Conditional check: `needs.release.outputs.is_prerelease != 'true'`
- Ensures Homebrew users don't get beta/RC versions automatically
- Users can still manually install prerelease binaries from GitHub Releases

**Estimated Effort:** S (1-2h)

---

### TASK-012: Validate Formula on macOS Intel (x64)

**Priority:** P1 (High - Platform validation)

**Description:**
Manually test Homebrew formula installation on macOS Intel (x64) platform to verify correct binary selection, installation, and functionality.

**Files to Modify:**
None (testing task)

**Implementation Details:**
On a macOS Intel machine (or GitHub Actions macos-13 runner):
1. Add tap: `brew tap oocx/tfplan2md`
2. Install: `brew install tfplan2md`
3. Verify installation location and binary architecture
4. Run functionality tests
5. Test upgrade path
6. Test uninstallation

**Acceptance Criteria:**
- [ ] Formula syntax validates: `brew audit --new-formula tfplan2md`
- [ ] Formula style validates: `brew style tfplan2md`
- [ ] Installation completes successfully
- [ ] Binary installed to Homebrew bin directory (e.g., `/usr/local/bin/tfplan2md`)
- [ ] Binary is correct architecture (x86_64)
- [ ] `tfplan2md --version` displays correct version
- [ ] `tfplan2md --help` displays help text
- [ ] Binary processes example Terraform plan JSON correctly
- [ ] `brew upgrade tfplan2md` works for newer version
- [ ] `brew uninstall tfplan2md` removes binary cleanly
- [ ] Test cases BREW-SYNTAX-001, BREW-INSTALL-001, BREW-PLATFORM-001, BREW-CHECKSUM-001, BREW-UPGRADE-001, BREW-UNINSTALL-001 pass

**Dependencies:** TASK-010 (formula must be published to tap repository)

**Notes:**
- Requires access to macOS Intel machine or GitHub Actions runner
- Test with actual released version (not test version)
- Verify checksum validation works (intentionally corrupt checksum should fail)

**Estimated Effort:** M (2-4h)

---

### TASK-013: Validate Formula on macOS Apple Silicon (ARM64)

**Priority:** P1 (High - Platform validation)

**Description:**
Manually test Homebrew formula installation on macOS Apple Silicon (ARM64) platform to verify correct binary selection, installation, and functionality.

**Files to Modify:**
None (testing task)

**Implementation Details:**
On a macOS Apple Silicon machine (or GitHub Actions macos-14 runner):
1. Add tap: `brew tap oocx/tfplan2md`
2. Install: `brew install tfplan2md`
3. Verify installation location and binary architecture
4. Run functionality tests
5. Verify binary is ARM64 (not x64 via Rosetta)

**Acceptance Criteria:**
- [ ] Formula syntax validates: `brew audit --new-formula tfplan2md`
- [ ] Formula style validates: `brew style tfplan2md`
- [ ] Installation completes successfully
- [ ] Binary installed to Homebrew bin directory (e.g., `/opt/homebrew/bin/tfplan2md`)
- [ ] Binary is correct architecture (arm64)
- [ ] Binary is NOT running via Rosetta translation
- [ ] `tfplan2md --version` displays correct version
- [ ] `tfplan2md --help` displays help text
- [ ] Binary processes example Terraform plan JSON correctly
- [ ] Test cases BREW-SYNTAX-002, BREW-INSTALL-002, BREW-PLATFORM-001, BREW-CHECKSUM-001 pass

**Dependencies:** TASK-010 (formula must be published to tap repository)

**Notes:**
- Requires access to Apple Silicon Mac or GitHub Actions macos-14 runner
- Verify binary architecture with `file` command or `arch` command
- Most important test is confirming ARM64 binary (not x64 emulated via Rosetta)

**Estimated Effort:** M (2-4h)

---

### TASK-014: Validate Formula on Linux x64

**Priority:** P1 (High - Platform validation)

**Description:**
Manually test Homebrew formula installation on Linux x64 platform (Ubuntu or WSL) to verify correct binary selection, installation, and functionality.

**Files to Modify:**
None (testing task)

**Implementation Details:**
On a Linux x64 machine (Ubuntu 24.04+ native or WSL):
1. Ensure Homebrew is installed (`/home/linuxbrew/.linuxbrew/bin/brew`)
2. Add tap: `brew tap oocx/tfplan2md`
3. Install: `brew install tfplan2md`
4. Verify installation location and binary architecture
5. Run functionality tests

**Acceptance Criteria:**
- [ ] Homebrew is available on Linux test system
- [ ] Installation completes successfully
- [ ] Binary installed to Homebrew bin directory
- [ ] Binary is correct architecture (x86_64)
- [ ] Binary has glibc dependency satisfied (Ubuntu 24.04+ or equivalent)
- [ ] `tfplan2md --version` displays correct version
- [ ] `tfplan2md --help` displays help text
- [ ] Binary processes example Terraform plan JSON correctly
- [ ] Test case BREW-INSTALL-003 passes

**Dependencies:** TASK-010 (formula must be published to tap repository)

**Notes:**
- Linux testing is lower priority than macOS (Homebrew primarily for macOS)
- Requires Linux system with Homebrew installed (not as common)
- Can test in WSL environment on Windows
- Verify glibc compatibility (Ubuntu 24.04+, Debian 13+, RHEL 10+)

**Estimated Effort:** M (2-4h)

---

### TASK-015: Update README with Homebrew Installation Instructions

**Priority:** P1 (High - Documentation)

**Description:**
Update the main project README.md to include Homebrew as a recommended installation method alongside Docker and direct binary download.

**Files to Modify:**
- `README.md`

**Implementation Details:**
Add a new "Installation" section (or update existing) with:
1. Homebrew installation (recommended for macOS/Linux)
2. Docker installation (recommended for CI/CD)
3. Direct binary download (all platforms)

Include clear instructions for:
- Adding the tap (`brew tap oocx/tfplan2md`)
- Installing tfplan2md (`brew install tfplan2md`)
- Verifying installation (`tfplan2md --version`)
- Updating (`brew upgrade tfplan2md`)

**Acceptance Criteria:**
- [ ] README.md has Installation section
- [ ] Homebrew installation instructions included
- [ ] Instructions show tap command: `brew tap oocx/tfplan2md`
- [ ] Instructions show install command: `brew install tfplan2md`
- [ ] Instructions show verification: `tfplan2md --version`
- [ ] Homebrew listed as recommended method for macOS and Linux
- [ ] Instructions mention supported platforms (macOS x64, macOS ARM64, Linux x64)
- [ ] Update/upgrade instructions included
- [ ] Installation section is easy to find (near top of README)

**Dependencies:** TASK-010 (Homebrew installation must work before documenting)

**Notes:**
- Keep instructions concise and clear
- Include link to tap repository for advanced users
- Mention that Docker is still recommended for CI/CD environments

**Estimated Effort:** S (1-2h)

---

### TASK-016: Document Formula Update Process in Tap Repository

**Priority:** P2 (Medium - Documentation)

**Description:**
Update the Homebrew tap repository README to document how formula updates work, including the automated process and manual fallback procedures.

**Files to Modify:**
- `README.md` (in homebrew-tfplan2md repository)

**Implementation Details:**
Add sections to tap README explaining:
1. How formula updates work (automated via release workflow)
2. Expected update latency (within 5 minutes of release)
3. Manual update procedure (if automation fails)
4. Troubleshooting common issues

**Acceptance Criteria:**
- [ ] Tap repository README documents automated update process
- [ ] README explains formula is updated automatically on tfplan2md releases
- [ ] README includes expected update latency (5 minutes)
- [ ] README provides manual update instructions for maintainers
- [ ] README links to main tfplan2md repository
- [ ] README explains how to report issues
- [ ] README mentions supported platforms clearly

**Dependencies:** TASK-005 (tap repository), TASK-008 (automation)

**Notes:**
- This is mainly for maintainers and curious users
- Keep it concise but informative
- Include link to release workflow for transparency

**Estimated Effort:** S (1-2h)

---

### TASK-017: Add Error Handling Tests for Formula Update

**Priority:** P2 (Medium - Validation)

**Description:**
Test error handling scenarios in the formula update automation to ensure failures are handled gracefully and don't block releases.

**Files to Modify:**
None (testing task)

**Implementation Details:**
Test scenarios:
1. Missing checksums in SHA256SUMS file
2. Invalid checksum format (not 64 hex characters)
3. Git push failure (network error simulation)
4. Authentication failure (invalid token)

For each scenario, verify:
- Error is detected and logged
- Workflow job fails appropriately
- GitHub Release still succeeds (graceful degradation)
- Manual fix procedure works

**Acceptance Criteria:**
- [ ] Missing macos-x64 checksum causes script to exit with error
- [ ] Invalid checksum format causes script to exit with error
- [ ] Script provides clear error messages for debugging
- [ ] Formula update job failure does NOT prevent GitHub Release creation
- [ ] Manual formula update can be performed as fallback
- [ ] Test cases WORKFLOW-ERROR-001, WORKFLOW-ERROR-002, WORKFLOW-ERROR-003 pass

**Dependencies:** TASK-008 (workflow with error handling)

**Notes:**
- These are edge case tests to validate robustness
- Can simulate errors by temporarily modifying scripts or workflows
- Ensures the "graceful degradation" design works as intended

**Estimated Effort:** M (2-4h)

---

## Implementation Order

Tasks are organized into phases based on dependencies and logical implementation flow:

### Phase 1: Platform Build Fixes (P0 - Foundation)
**Goal:** Enable macOS binary builds required for Homebrew support

1. **TASK-001** - Fix macOS x64 Platform Build  
   *Reason:* Unblocks macOS x64 binaries for Homebrew formula
   
2. **TASK-002** - Fix macOS ARM64 Platform Build  
   *Reason:* Unblocks macOS ARM64 binaries for Homebrew formula (shares implementation with TASK-001)
   
3. **TASK-003** - Remove windows-arm64 from Build Matrix  
   *Reason:* Simplifies workflow, removes failing build
   
4. **TASK-004** - Verify Platform Build Regression Testing  
   *Reason:* Quality gate before proceeding to Homebrew infrastructure

### Phase 2: Homebrew Infrastructure Setup (P0 - Parallel with Phase 1)
**Goal:** Create tap repository and formula structure

5. **TASK-005** - Create Homebrew Tap Repository  
   *Reason:* Prerequisite for formula creation (can be done while fixing builds)
   
6. **TASK-006** - Create Homebrew Formula Template  
   *Reason:* Defines formula structure with placeholders (depends on TASK-005)
   
7. **TASK-007** - Create Formula Update Script  
   *Reason:* Automation logic for formula updates (can be developed in parallel)

### Phase 3: Workflow Automation (P0 - Integration)
**Goal:** Automate formula updates in release workflow

8. **TASK-009** - Configure GitHub Secret for Tap Repository Access  
   *Reason:* Security prerequisite for workflow (can be done early)
   
9. **TASK-008** - Add Formula Update Job to Release Workflow  
   *Reason:* Integrates update script into release workflow (depends on TASK-007, TASK-009)

### Phase 4: End-to-End Testing (P0 - Validation)
**Goal:** Validate complete automation pipeline

10. **TASK-010** - Test Formula Update Automation End-to-End  
    *Reason:* Validates entire automation pipeline works correctly
    
11. **TASK-011** - Test Prerelease Skip Behavior  
    *Reason:* Validates conditional logic for stable-only updates

### Phase 5: Platform Validation (P1 - Quality)
**Goal:** Verify formula works on all supported platforms

12. **TASK-012** - Validate Formula on macOS Intel (x64)  
    *Reason:* Primary platform validation
    
13. **TASK-013** - Validate Formula on macOS Apple Silicon (ARM64)  
    *Reason:* Secondary macOS platform validation
    
14. **TASK-014** - Validate Formula on Linux x64  
    *Reason:* Linux platform validation (lower priority)

### Phase 6: Documentation (P1/P2 - User-Facing)
**Goal:** Document installation and update processes

15. **TASK-015** - Update README with Homebrew Installation Instructions  
    *Reason:* User-facing documentation for new installation method
    
16. **TASK-016** - Document Formula Update Process in Tap Repository  
    *Reason:* Maintainer documentation for tap repository

### Phase 7: Error Handling Validation (P2 - Robustness)
**Goal:** Validate error scenarios and graceful degradation

17. **TASK-017** - Add Error Handling Tests for Formula Update  
    *Reason:* Validates robustness and error handling

## Critical Path

The critical path for minimum viable Homebrew support:

```
TASK-001 (macOS x64 fix)
  ↓
TASK-002 (macOS ARM64 fix) ← shares implementation
  ↓
TASK-003 (remove windows-arm64)
  ↓
TASK-004 (regression testing)
  ↓
[Parallel: TASK-005 (tap repo) + TASK-007 (update script) + TASK-009 (secret)]
  ↓
TASK-006 (formula template)
  ↓
TASK-008 (workflow integration)
  ↓
TASK-010 (end-to-end testing)
  ↓
TASK-012 (macOS x64 validation)
  ↓
TASK-015 (README update)
```

**Minimum viable delivery:** Tasks 1-12, 15 (13 tasks)  
**Complete delivery:** All 17 tasks

## Open Questions

1. **Homebrew Testing Environment:** Do we have access to macOS Apple Silicon hardware for testing (TASK-013), or should we rely on GitHub Actions macos-14 runner?
   - **Recommendation:** Use GitHub Actions macos-14 runner if physical hardware is unavailable

2. **Test Tag Cleanup:** Should test tags (e.g., `v0.0.0-brew-test`) be deleted after testing, or kept for reference?
   - **Recommendation:** Delete test tags to avoid confusion; they're not production releases

3. **Formula Versioning:** Should we add version constraints (e.g., minimum macOS version) to the formula?
   - **Recommendation:** Add constraints matching Feature 047 platform requirements (macOS 10.15+ for x64, macOS 11+ for ARM64)

4. **Linux ARM64 Timeline:** When should we implement Linux ARM64 support (Phase 2 from ADR-001)?
   - **Recommendation:** Defer to post-MVP; add only if user demand exists

5. **Homebrew Core Submission:** Should we plan to submit to homebrew-core in the future?
   - **Recommendation:** Defer until project meets homebrew-core criteria (notable user base, stable maintenance)

6. **Token Rotation Policy:** How often should `HOMEBREW_TAP_TOKEN` be rotated for security?
   - **Recommendation:** Use fine-grained token with minimum permissions; rotate every 12 months or per organization policy

## Risk Mitigation

### High-Priority Risks

**Risk:** Platform build fixes fail in production  
**Mitigation:** TASK-004 validates all platforms before proceeding; test release catches issues early

**Risk:** Formula update automation fails silently  
**Mitigation:** TASK-010 validates end-to-end; TASK-017 tests error scenarios; workflow logs provide debugging

**Risk:** Checksum mismatch prevents installation  
**Mitigation:** TASK-007 validates checksum format; TASK-010 verifies checksums match; use consolidated SHA256SUMS as single source of truth

### Medium-Priority Risks

**Risk:** `HOMEBREW_TAP_TOKEN` expires or becomes invalid  
**Mitigation:** TASK-009 documents token creation; use fine-grained token with clear expiration; monitor workflow failures

**Risk:** Platform detection fails on newer macOS versions  
**Mitigation:** Use standard Homebrew APIs (`on_macos`, `Hardware::CPU`); TASK-12/13 validate on current macOS versions

**Risk:** Users expect instant formula updates  
**Mitigation:** TASK-016 documents expected latency (~5 minutes); automation is fast enough for reasonable expectations

## Success Metrics

- [ ] All 5 platforms build successfully (linux-x64, linux-arm64, windows-x64, macos-x64, macos-arm64)
- [ ] Formula installs successfully on macOS x64, macOS ARM64, and Linux x64
- [ ] Formula passes `brew audit` and `brew style` validation
- [ ] Formula updates automatically within 5 minutes of stable release
- [ ] Installation instructions in README are clear and complete
- [ ] Test plan coverage: 35 test cases with 80%+ pass rate
- [ ] Zero checksum mismatches in production
- [ ] Formula update automation has <5% failure rate

---

**Document Version:** 1.0  
**Created By:** Task Planner Agent  
**Date:** 2025-02-18  
**Status:** Ready for Maintainer Review
