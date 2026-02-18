# Test Plan: Homebrew Installation Support

## Overview

This test plan covers Feature 089: Homebrew Installation Support for tfplan2md. The feature enables macOS and Linux (WSL) users to install tfplan2md via Homebrew package manager using a custom tap repository.

**Feature Specification**: [specification.md](./specification.md)

**Architecture Decisions**:
- [ADR-001: Platform Build Fixes](./adr-001-platform-build-fixes.md)
- [ADR-002: Homebrew Formula Design](./adr-002-homebrew-formula-design.md)
- [ADR-003: Release Workflow Integration](./adr-003-release-workflow-integration.md)

**Key Components**:
1. **Platform Build Fixes**: Fix macOS x64/ARM64 builds, remove windows-arm64
2. **Homebrew Formula**: Multi-platform formula with conditional binary selection
3. **Release Workflow**: Automated formula updates via GitHub Actions
4. **Tap Repository**: `oocx/homebrew-tfplan2md` structure and maintenance

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type | Priority |
|---------------------|--------------|-----------|----------|
| macOS x64 builds successfully | BUILD-MAC-001 | Integration | Critical |
| macOS ARM64 builds successfully | BUILD-MAC-002 | Integration | Critical |
| windows-arm64 removed from matrix | BUILD-WIN-001 | Integration | High |
| Existing platforms still work | BUILD-REG-001, BUILD-REG-002 | Integration | Critical |
| Binaries are correct architecture | BUILD-VAL-001, BUILD-VAL-002, BUILD-VAL-003 | Integration | High |
| Formula syntax is valid | BREW-SYNTAX-001, BREW-SYNTAX-002 | Manual | Critical |
| Formula installs on macOS x64 | BREW-INSTALL-001 | Manual | Critical |
| Formula installs on macOS ARM64 | BREW-INSTALL-002 | Manual | Critical |
| Formula installs on Linux x64 | BREW-INSTALL-003 | Manual | High |
| Platform detection works | BREW-PLATFORM-001 | Manual | Critical |
| Checksum verification works | BREW-CHECKSUM-001, BREW-CHECKSUM-002 | Manual | Critical |
| Upgrade path works | BREW-UPGRADE-001 | Manual | High |
| Uninstall works | BREW-UNINSTALL-001 | Manual | Medium |
| Formula updates automatically | WORKFLOW-UPDATE-001 | Integration | Critical |
| Checksums extracted correctly | WORKFLOW-UPDATE-002 | Integration | Critical |
| Placeholders replaced correctly | WORKFLOW-UPDATE-003 | Integration | Critical |
| Tap repository updated | WORKFLOW-UPDATE-004 | Integration | Critical |
| Prerelease skipped | WORKFLOW-UPDATE-005 | Integration | High |
| Formula failure doesn't block release | WORKFLOW-UPDATE-006 | Integration | High |
| Missing checksums handled | WORKFLOW-ERROR-001 | Integration | High |
| Invalid checksums handled | WORKFLOW-ERROR-002 | Integration | High |
| Push failures handled | WORKFLOW-ERROR-003 | Integration | Medium |
| End-to-end release workflow | INTEGRATION-E2E-001 | Manual | Critical |
| Prerelease doesn't update formula | INTEGRATION-E2E-002 | Integration | High |

## Test Cases

### Platform Build Testing

#### BUILD-MAC-001: macOS x64 Build Success

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Verify that macOS x64 (Intel) binaries build successfully with Xcode Command Line Tools installed.

**Preconditions:**
- Feature branch with ADR-001 changes merged
- GitHub Actions workflow includes Xcode installation step for macOS platforms

**Test Steps:**
1. Trigger release workflow with test tag (e.g., `v0.0.0-brew-test`)
2. Monitor `build-binaries` job for `macos-x64` matrix entry
3. Verify Xcode Command Line Tools installation step succeeds
4. Verify .NET setup completes
5. Verify `dotnet publish -p:PublishAot=true` succeeds
6. Verify binary archive created: `tfplan2md_<version>_macos-x64.tar.gz`

**Expected Result:**
- Xcode installation step completes successfully (exit code 0)
- Build completes without NativeAOT compilation errors
- Archive uploaded to GitHub Releases
- Build time: ~5-8 minutes (including Xcode installation overhead)

**Test Data:**
- GitHub Actions runner: `macos-13` (Intel)
- .NET RID: `osx-x64`

---

#### BUILD-MAC-002: macOS ARM64 Build Success

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Verify that macOS ARM64 (Apple Silicon) binaries build successfully with Xcode Command Line Tools installed.

**Preconditions:**
- Feature branch with ADR-001 changes merged
- GitHub Actions workflow includes Xcode installation step for macOS platforms

**Test Steps:**
1. Trigger release workflow with test tag (e.g., `v0.0.0-brew-test`)
2. Monitor `build-binaries` job for `macos-arm64` matrix entry
3. Verify Xcode Command Line Tools installation step succeeds
4. Verify .NET setup completes
5. Verify `dotnet publish -p:PublishAot=true` succeeds
6. Verify binary archive created: `tfplan2md_<version>_macos-arm64.tar.gz`

**Expected Result:**
- Xcode installation step completes successfully (exit code 0)
- Build completes without NativeAOT compilation errors
- Archive uploaded to GitHub Releases
- Build time: ~5-8 minutes (including Xcode installation overhead)

**Test Data:**
- GitHub Actions runner: `macos-14` (Apple Silicon)
- .NET RID: `osx-arm64`

---

#### BUILD-WIN-001: Windows ARM64 Removed from Matrix

**Type:** Integration (GitHub Actions)

**Priority:** High

**Description:**
Verify that `windows-arm64` platform has been removed from the build matrix and no longer runs.

**Preconditions:**
- Feature branch with ADR-001 changes merged
- Release workflow updated to remove `windows-arm64` matrix entry

**Test Steps:**
1. Trigger release workflow with test tag
2. Monitor `build-binaries` job matrix
3. Verify matrix includes: `linux-x64`, `linux-arm64`, `windows-x64`, `macos-x64`, `macos-arm64`
4. Verify matrix does NOT include: `windows-arm64`
5. Verify no build artifacts for `windows-arm64`

**Expected Result:**
- Build matrix shows 5 platforms (not 6)
- No `windows-arm64` job runs
- No `tfplan2md_<version>_windows-arm64.zip` artifact created
- Total build time reduced by ~3-5 minutes

**Test Data:**
- Expected platforms: 5 (linux-x64, linux-arm64, windows-x64, macos-x64, macos-arm64)

---

#### BUILD-REG-001: Linux x64 Build Still Works

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Regression test to ensure existing linux-x64 builds continue to work after macOS changes.

**Preconditions:**
- Feature branch with ADR-001 changes merged

**Test Steps:**
1. Trigger release workflow with test tag
2. Monitor `build-binaries` job for `linux-x64` matrix entry
3. Verify container setup succeeds
4. Verify clang installation succeeds
5. Verify `dotnet publish -p:PublishAot=true` succeeds
6. Verify binary archive created: `tfplan2md_<version>_linux-x64.tar.gz`

**Expected Result:**
- Build completes successfully (no regressions from macOS changes)
- Archive uploaded to GitHub Releases
- Binary is executable and correct architecture (x86_64)

**Test Data:**
- GitHub Actions runner: `ubuntu-latest` with container `mcr.microsoft.com/dotnet/sdk:10.0-noble`
- .NET RID: `linux-x64`

---

#### BUILD-REG-002: Windows x64 Build Still Works

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Regression test to ensure existing windows-x64 builds continue to work after macOS changes.

**Preconditions:**
- Feature branch with ADR-001 changes merged

**Test Steps:**
1. Trigger release workflow with test tag
2. Monitor `build-binaries` job for `windows-x64` matrix entry
3. Verify .NET setup completes
4. Verify `dotnet publish -p:PublishAot=true` succeeds
5. Verify binary archive created: `tfplan2md_<version>_windows-x64.zip`

**Expected Result:**
- Build completes successfully (no regressions from macOS changes)
- Archive uploaded to GitHub Releases
- Binary is executable (.exe) and correct architecture (x64)

**Test Data:**
- GitHub Actions runner: `windows-latest`
- .NET RID: `win-x64`

---

#### BUILD-VAL-001: macOS x64 Binary Architecture Validation

**Type:** Integration (GitHub Actions or Manual)

**Priority:** High

**Description:**
Verify that the macOS x64 binary is actually x64 architecture (not ARM64 built on wrong runner).

**Preconditions:**
- macOS x64 build completed successfully (BUILD-MAC-001)
- Binary archive downloaded

**Test Steps:**
1. Download `tfplan2md_<version>_macos-x64.tar.gz` from GitHub Releases
2. Extract archive: `tar -xzf tfplan2md_<version>_macos-x64.tar.gz`
3. Check binary architecture: `file tfplan2md`
4. Verify output contains: `Mach-O 64-bit executable x86_64`
5. Attempt to run on macOS Intel machine (if available): `./tfplan2md --version`

**Expected Result:**
- File type shows `x86_64` architecture
- Binary runs natively on Intel Macs (no Rosetta required)
- Binary does NOT run on ARM64-only systems

**Test Data:**
- Expected output: `tfplan2md: Mach-O 64-bit executable x86_64`

---

#### BUILD-VAL-002: macOS ARM64 Binary Architecture Validation

**Type:** Integration (GitHub Actions or Manual)

**Priority:** High

**Description:**
Verify that the macOS ARM64 binary is actually ARM64 architecture (not x64).

**Preconditions:**
- macOS ARM64 build completed successfully (BUILD-MAC-002)
- Binary archive downloaded

**Test Steps:**
1. Download `tfplan2md_<version>_macos-arm64.tar.gz` from GitHub Releases
2. Extract archive: `tar -xzf tfplan2md_<version>_macos-arm64.tar.gz`
3. Check binary architecture: `file tfplan2md`
4. Verify output contains: `Mach-O 64-bit executable arm64`
5. Attempt to run on Apple Silicon Mac (if available): `./tfplan2md --version`

**Expected Result:**
- File type shows `arm64` architecture
- Binary runs natively on Apple Silicon Macs
- Binary does NOT run on Intel-only systems (without Rosetta)

**Test Data:**
- Expected output: `tfplan2md: Mach-O 64-bit executable arm64`

---

#### BUILD-VAL-003: Linux x64 Binary Architecture Validation

**Type:** Integration (GitHub Actions or Manual)

**Priority:** High

**Description:**
Verify that the Linux x64 binary is correct architecture and links to glibc (not musl).

**Preconditions:**
- Linux x64 build completed successfully (BUILD-REG-001)
- Binary archive downloaded

**Test Steps:**
1. Download `tfplan2md_<version>_linux-x64.tar.gz` from GitHub Releases
2. Extract archive: `tar -xzf tfplan2md_<version>_linux-x64.tar.gz`
3. Check binary architecture: `file tfplan2md`
4. Verify output contains: `ELF 64-bit LSB executable, x86-64`
5. Check dynamic linking: `ldd tfplan2md`
6. Verify glibc dependency (not musl)
7. Run binary: `./tfplan2md --version`

**Expected Result:**
- File type shows `x86-64` architecture
- Binary is dynamically linked to glibc (standard Linux)
- Binary runs on Ubuntu 24.04+ and similar distributions

**Test Data:**
- Expected file output: `ELF 64-bit LSB executable, x86-64`
- Expected ldd output: Contains `libc.so.6` (glibc)

---

### Homebrew Formula Testing

#### BREW-SYNTAX-001: Formula Audit Validation

**Type:** Manual (Homebrew CLI)

**Priority:** Critical

**Description:**
Verify that the Homebrew formula passes `brew audit` validation, ensuring correct syntax, naming conventions, and metadata completeness.

**Preconditions:**
- Tap repository `oocx/homebrew-tfplan2md` created
- `Formula/tfplan2md.rb` created with template
- Homebrew installed on test system (macOS or Linux)

**Test Steps:**
1. Add tap: `brew tap oocx/tfplan2md`
2. Run audit: `brew audit --new-formula tfplan2md`
3. Review output for errors or warnings
4. If errors found, fix formula and re-run audit

**Expected Result:**
- Audit completes with zero errors
- All required fields present: `desc`, `homepage`, `license`, `version`, `url`, `sha256`
- URL format is valid HTTPS GitHub Releases URL
- SHA256 format is valid (64 hex characters)
- Class name matches formula name (`Tfplan2md`)

**Test Data:**
- Formula file: `oocx/homebrew-tfplan2md/Formula/tfplan2md.rb`

---

#### BREW-SYNTAX-002: Formula Style Validation

**Type:** Manual (Homebrew CLI)

**Priority:** Critical

**Description:**
Verify that the Homebrew formula passes `brew style` validation, ensuring compliance with Homebrew Ruby style guidelines.

**Preconditions:**
- Tap repository `oocx/homebrew-tfplan2md` created
- `Formula/tfplan2md.rb` created with template
- Homebrew installed on test system

**Test Steps:**
1. Add tap (if not already added): `brew tap oocx/tfplan2md`
2. Run style check: `brew style tfplan2md`
3. Review output for style violations
4. If violations found, fix formula and re-run style check

**Expected Result:**
- Style check completes with zero violations
- Indentation is correct (2 spaces)
- Ruby syntax follows Homebrew conventions
- Comments (if any) are properly formatted

**Test Data:**
- Formula file: `oocx/homebrew-tfplan2md/Formula/tfplan2md.rb`

---

#### BREW-INSTALL-001: Fresh Installation on macOS x64

**Type:** Manual (Homebrew on Intel Mac or GitHub Actions)

**Priority:** Critical

**Description:**
Verify that tfplan2md installs successfully via Homebrew on macOS x64 (Intel) systems.

**Preconditions:**
- Formula updated with real version and checksums (not template placeholders)
- Homebrew installed on macOS x64 system
- tfplan2md NOT already installed

**Test Steps:**
1. Add tap: `brew tap oocx/tfplan2md`
2. Install formula: `brew install tfplan2md`
3. Verify installation completed without errors
4. Check binary location: `which tfplan2md`
5. Verify version: `tfplan2md --version`
6. Test help command: `tfplan2md --help`
7. Test functionality with example: `tfplan2md examples/azure_cdn.json > output.md`
8. Verify output.md is valid markdown

**Expected Result:**
- Installation completes successfully
- Binary installed to Homebrew bin directory (e.g., `/usr/local/bin/tfplan2md` or `/opt/homebrew/bin/tfplan2md`)
- `tfplan2md --version` displays correct version
- `tfplan2md --help` displays usage information
- Binary can process Terraform plan JSON files correctly

**Test Data:**
- Platform: macOS x64 (Intel)
- Expected binary path: `/usr/local/bin/tfplan2md` (Intel) or `/opt/homebrew/bin/tfplan2md` (if M1 with Rosetta)

---

#### BREW-INSTALL-002: Fresh Installation on macOS ARM64

**Type:** Manual (Homebrew on Apple Silicon Mac or GitHub Actions)

**Priority:** Critical

**Description:**
Verify that tfplan2md installs successfully via Homebrew on macOS ARM64 (Apple Silicon) systems.

**Preconditions:**
- Formula updated with real version and checksums
- Homebrew installed on macOS ARM64 system
- tfplan2md NOT already installed

**Test Steps:**
1. Add tap: `brew tap oocx/tfplan2md`
2. Install formula: `brew install tfplan2md`
3. Verify installation completed without errors
4. Check binary location: `which tfplan2md`
5. Verify version: `tfplan2md --version`
6. Test help command: `tfplan2md --help`
7. Verify binary architecture: `file $(which tfplan2md)` (should show `arm64`, not `x86_64`)
8. Test functionality with example: `tfplan2md examples/azure_cdn.json > output.md`

**Expected Result:**
- Installation completes successfully
- Binary installed to Homebrew bin directory (e.g., `/opt/homebrew/bin/tfplan2md`)
- Binary is ARM64 architecture (native, not x64 via Rosetta)
- `tfplan2md --version` displays correct version
- Binary can process Terraform plan JSON files correctly

**Test Data:**
- Platform: macOS ARM64 (Apple Silicon M1/M2/M3)
- Expected binary path: `/opt/homebrew/bin/tfplan2md`
- Expected architecture: `arm64`

---

#### BREW-INSTALL-003: Fresh Installation on Linux x64

**Type:** Manual (Homebrew on Linux or WSL)

**Priority:** High

**Description:**
Verify that tfplan2md installs successfully via Homebrew on Linux x64 systems (including WSL).

**Preconditions:**
- Formula updated with real version and checksums
- Homebrew installed on Linux x64 system (native or WSL)
- tfplan2md NOT already installed

**Test Steps:**
1. Add tap: `brew tap oocx/tfplan2md`
2. Install formula: `brew install tfplan2md`
3. Verify installation completed without errors
4. Check binary location: `which tfplan2md`
5. Verify version: `tfplan2md --version`
6. Test help command: `tfplan2md --help`
7. Verify binary architecture: `file $(which tfplan2md)` (should show `ELF 64-bit ... x86-64`)
8. Test functionality with example: `tfplan2md examples/azure_cdn.json > output.md`

**Expected Result:**
- Installation completes successfully
- Binary installed to Homebrew bin directory (e.g., `~/.linuxbrew/bin/tfplan2md` or `/home/linuxbrew/.linuxbrew/bin/tfplan2md`)
- Binary is x86-64 architecture
- `tfplan2md --version` displays correct version
- Binary can process Terraform plan JSON files correctly

**Test Data:**
- Platform: Linux x64 (Ubuntu 24.04+ or WSL)
- Expected binary path: `~/.linuxbrew/bin/tfplan2md` or `/home/linuxbrew/.linuxbrew/bin/tfplan2md`

---

#### BREW-PLATFORM-001: Platform Detection and Binary Selection

**Type:** Manual (Homebrew on Multiple Platforms)

**Priority:** Critical

**Description:**
Verify that Homebrew formula correctly detects platform (macOS vs Linux) and architecture (Intel vs ARM64) and selects the appropriate binary.

**Preconditions:**
- Formula updated with real version and checksums for all platforms
- Access to test systems: macOS x64, macOS ARM64, Linux x64

**Test Steps:**
1. On **macOS x64** system:
   - Run: `brew install oocx/tfplan2md/tfplan2md`
   - Verify downloaded file: Check Homebrew download cache for `tfplan2md_<version>_macos-x64.tar.gz`
   - Verify binary architecture: `file $(which tfplan2md)` → should show `x86_64`

2. On **macOS ARM64** system:
   - Run: `brew install oocx/tfplan2md/tfplan2md`
   - Verify downloaded file: Check Homebrew download cache for `tfplan2md_<version>_macos-arm64.tar.gz`
   - Verify binary architecture: `file $(which tfplan2md)` → should show `arm64`

3. On **Linux x64** system:
   - Run: `brew install oocx/tfplan2md/tfplan2md`
   - Verify downloaded file: Check Homebrew download cache for `tfplan2md_<version>_linux-x64.tar.gz`
   - Verify binary architecture: `file $(which tfplan2md)` → should show `x86-64`

**Expected Result:**
- Each platform downloads the correct platform-specific archive
- No cross-platform contamination (e.g., ARM64 Mac doesn't download x64 binary)
- Binary architecture matches system architecture
- No Rosetta translation warnings on Apple Silicon

**Test Data:**
- Expected archives:
  - macOS x64: `tfplan2md_<version>_macos-x64.tar.gz`
  - macOS ARM64: `tfplan2md_<version>_macos-arm64.tar.gz`
  - Linux x64: `tfplan2md_<version>_linux-x64.tar.gz`

---

#### BREW-CHECKSUM-001: Valid Checksum Acceptance

**Type:** Manual (Homebrew Installation)

**Priority:** Critical

**Description:**
Verify that Homebrew successfully validates SHA256 checksums during installation when checksums are correct.

**Preconditions:**
- Formula updated with correct SHA256 checksums from `SHA256SUMS` file
- Homebrew installed on test system
- tfplan2md NOT already installed

**Test Steps:**
1. Add tap: `brew tap oocx/tfplan2md`
2. Install formula: `brew install tfplan2md`
3. Monitor installation output for checksum validation messages
4. Verify installation completes successfully

**Expected Result:**
- Installation completes without checksum errors
- Homebrew downloads binary archive
- Homebrew validates SHA256 checksum silently (no errors)
- Binary is installed to bin directory

**Test Data:**
- Checksums sourced from `SHA256SUMS` artifact (generated by `consolidate-checksums` job)

---

#### BREW-CHECKSUM-002: Invalid Checksum Rejection

**Type:** Manual (Homebrew Installation with Modified Formula)

**Priority:** Critical

**Description:**
Verify that Homebrew rejects installation when SHA256 checksum does not match the downloaded binary.

**Preconditions:**
- Test tap repository with intentionally incorrect checksum in formula
- Homebrew installed on test system
- tfplan2md NOT already installed

**Test Steps:**
1. Create test formula with intentionally incorrect SHA256 (e.g., change one character)
2. Add test tap: `brew tap oocx/tfplan2md-test`
3. Attempt to install: `brew install tfplan2md`
4. Observe error message from Homebrew
5. Verify installation aborted (no binary installed)

**Expected Result:**
- Installation fails with checksum mismatch error
- Error message includes expected vs actual SHA256 values
- No binary installed to bin directory
- Homebrew cache may contain downloaded (but rejected) archive

**Test Data:**
- Modified checksum: Change last character of valid SHA256
- Expected error: "SHA256 mismatch" or similar

---

#### BREW-UPGRADE-001: Upgrade from Previous Version

**Type:** Manual (Homebrew Upgrade)

**Priority:** High

**Description:**
Verify that users can upgrade tfplan2md from an older version to a newer version via Homebrew.

**Preconditions:**
- Two releases available (e.g., v1.0.0 and v1.1.0)
- Formula updated for both versions in tap repository
- tfplan2md v1.0.0 already installed via Homebrew

**Test Steps:**
1. Verify current version: `tfplan2md --version` → should show v1.0.0
2. Update Homebrew formulae: `brew update`
3. Check for outdated packages: `brew outdated` → should list tfplan2md v1.0.0 → v1.1.0
4. Upgrade tfplan2md: `brew upgrade tfplan2md`
5. Verify new version: `tfplan2md --version` → should show v1.1.0
6. Test functionality: `tfplan2md examples/azure_cdn.json > output.md`

**Expected Result:**
- Upgrade completes successfully
- Old version (v1.0.0) replaced by new version (v1.1.0)
- `tfplan2md --version` shows v1.1.0
- Binary still works correctly after upgrade

**Test Data:**
- Old version: v1.0.0
- New version: v1.1.0

---

#### BREW-UNINSTALL-001: Clean Uninstallation

**Type:** Manual (Homebrew Uninstall)

**Priority:** Medium

**Description:**
Verify that tfplan2md can be cleanly uninstalled via Homebrew, removing all installed files.

**Preconditions:**
- tfplan2md installed via Homebrew
- Binary present in Homebrew bin directory

**Test Steps:**
1. Verify binary exists: `which tfplan2md` → should return path
2. Uninstall: `brew uninstall tfplan2md`
3. Verify binary removed: `which tfplan2md` → should return nothing (exit code 1)
4. Check bin directory: `ls -l $(brew --prefix)/bin/tfplan2md` → should not exist
5. (Optional) Remove tap: `brew untap oocx/tfplan2md`
6. Verify tap removed: `brew tap` → should not list `oocx/tfplan2md`

**Expected Result:**
- Uninstall completes successfully
- Binary removed from Homebrew bin directory
- No orphaned files left behind
- `tfplan2md` command no longer available in PATH
- Tap can be removed cleanly

**Test Data:**
- Expected binary path before uninstall: `$(brew --prefix)/bin/tfplan2md`
- Expected result after uninstall: File not found

---

### Release Workflow Testing

#### WORKFLOW-UPDATE-001: Formula Update Job Execution

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Verify that the `update-homebrew-formula` job runs successfully after a stable release and updates the tap repository formula.

**Preconditions:**
- Feature branch with ADR-003 changes merged
- `scripts/update-homebrew-formula.sh` script created
- `HOMEBREW_TAP_TOKEN` secret configured
- Tap repository `oocx/homebrew-tfplan2md` exists with template formula

**Test Steps:**
1. Create stable release tag: `git tag v0.0.1-brew-test && git push origin v0.0.1-brew-test`
2. Monitor release workflow in GitHub Actions
3. Verify `release` job completes successfully
4. Verify `build-binaries` jobs complete for all platforms
5. Verify `consolidate-checksums` job completes
6. Verify `update-homebrew-formula` job runs (not skipped)
7. Check tap repository for new commit
8. Verify commit message: `chore: update formula to v0.0.1-brew-test`
9. Review formula changes in commit diff

**Expected Result:**
- `update-homebrew-formula` job runs and completes successfully
- Formula file updated in tap repository
- Commit pushed to tap repository with conventional commit message
- Job completes in ~30-60 seconds

**Test Data:**
- Test tag: `v0.0.1-brew-test`
- Expected commit in tap repository: `chore: update formula to v0.0.1-brew-test`

---

#### WORKFLOW-UPDATE-002: Checksum Extraction from SHA256SUMS

**Type:** Integration (GitHub Actions + Script)

**Priority:** Critical

**Description:**
Verify that the update script correctly extracts SHA256 checksums from the consolidated `SHA256SUMS` artifact for each platform.

**Preconditions:**
- `scripts/update-homebrew-formula.sh` script created
- Sample `SHA256SUMS` file available for testing

**Test Steps:**
1. Create test `SHA256SUMS` file with known checksums:
   ```
   aabbccdd11223344...  tfplan2md_1.0.0_linux-x64.tar.gz
   eeff0011aabbccdd...  tfplan2md_1.0.0_macos-arm64.tar.gz
   11223344aabbccdd...  tfplan2md_1.0.0_macos-x64.tar.gz
   ```
2. Create test formula template with placeholders
3. Run script locally: `bash scripts/update-homebrew-formula.sh 1.0.0 test-SHA256SUMS test-formula.rb`
4. Verify script extracts checksums correctly:
   - `MACOS_X64_SHA=11223344aabbccdd...`
   - `MACOS_ARM64_SHA=eeff0011aabbccdd...`
   - `LINUX_X64_SHA=aabbccdd11223344...`
5. Verify script validates checksum format (64 hex characters)
6. Verify script updates formula file with extracted checksums

**Expected Result:**
- Script successfully extracts all three checksums
- Validation passes for all checksums (64 hex characters)
- Formula file updated with correct SHA256 values in correct platform sections
- Script exits with code 0 (success)

**Test Data:**
- Test checksums: 64-character hex strings
- Expected output: "✅ Formula updated to version 1.0.0"

---

#### WORKFLOW-UPDATE-003: Template Placeholder Replacement

**Type:** Integration (Script Testing)

**Priority:** Critical

**Description:**
Verify that the update script correctly replaces all template placeholders in the formula file with actual values.

**Preconditions:**
- `scripts/update-homebrew-formula.sh` script created
- Test formula template with placeholders: `{{VERSION}}`, `{{MACOS_X64_SHA256}}`, `{{MACOS_ARM64_SHA256}}`, `{{LINUX_X64_SHA256}}`

**Test Steps:**
1. Create test formula template (as defined in ADR-002)
2. Create test `SHA256SUMS` file with known checksums
3. Run script: `bash scripts/update-homebrew-formula.sh 1.2.3 test-SHA256SUMS test-formula.rb`
4. Inspect updated formula file
5. Verify all placeholders replaced:
   - `version "{{VERSION}}"` → `version "1.2.3"`
   - `sha256 "{{MACOS_X64_SHA256}}"` → `sha256 "<actual_checksum>"`
   - `sha256 "{{MACOS_ARM64_SHA256}}"` → `sha256 "<actual_checksum>"`
   - `sha256 "{{LINUX_X64_SHA256}}"` → `sha256 "<actual_checksum>"`
6. Verify URLs updated:
   - `v{{VERSION}}` → `v1.2.3`
   - `tfplan2md_{{VERSION}}_macos-x64.tar.gz` → `tfplan2md_1.2.3_macos-x64.tar.gz`
7. Verify no placeholders remain (grep for `{{`)

**Expected Result:**
- All 4 unique placeholders replaced correctly
- Version appears in multiple locations (version field + URLs) - all updated
- No leftover `{{` or `}}` in formula file
- Formula syntax remains valid Ruby

**Test Data:**
- Test version: `1.2.3`
- Expected version occurrences: 4 (1 in version field, 3 in URLs)

---

#### WORKFLOW-UPDATE-004: Tap Repository Commit and Push

**Type:** Integration (GitHub Actions)

**Priority:** Critical

**Description:**
Verify that the workflow successfully commits and pushes formula updates to the tap repository using `HOMEBREW_TAP_TOKEN`.

**Preconditions:**
- `HOMEBREW_TAP_TOKEN` secret configured with write access to tap repository
- Tap repository exists and is accessible
- Test release triggered

**Test Steps:**
1. Trigger test release
2. Monitor `update-homebrew-formula` job logs
3. Verify checkout of tap repository succeeds (using token)
4. Verify formula file updated by script
5. Verify git config sets user.name and user.email (github-actions[bot])
6. Verify git add command runs
7. Verify git commit command runs (or skips if no changes)
8. Verify git push command succeeds
9. Check tap repository on GitHub for new commit
10. Verify commit author is `github-actions[bot]`

**Expected Result:**
- Tap repository successfully cloned using `HOMEBREW_TAP_TOKEN`
- Formula changes committed locally
- Commit pushed to remote tap repository
- Commit visible on GitHub within ~10 seconds
- Commit message follows conventional format: `chore: update formula to v<version>`

**Test Data:**
- Tap repository: `oocx/homebrew-tfplan2md`
- Expected commit author: `github-actions[bot]`

---

#### WORKFLOW-UPDATE-005: Prerelease Skips Formula Update

**Type:** Integration (GitHub Actions)

**Priority:** High

**Description:**
Verify that the `update-homebrew-formula` job is skipped when releasing a prerelease version (e.g., beta, rc).

**Preconditions:**
- Release workflow with ADR-003 changes merged
- `if` condition in `update-homebrew-formula` job checks `is_prerelease != 'true'`

**Test Steps:**
1. Create prerelease tag: `git tag v1.0.0-beta.1 && git push origin v1.0.0-beta.1`
2. Monitor release workflow in GitHub Actions
3. Verify `release` job completes and creates prerelease
4. Verify `release` job outputs `is_prerelease=true`
5. Verify `build-binaries` jobs run (prereleases still build binaries)
6. Verify `consolidate-checksums` job runs
7. Verify `update-homebrew-formula` job is **SKIPPED** (not run)
8. Check tap repository - should have NO new commit for this prerelease

**Expected Result:**
- Prerelease created successfully on GitHub
- Binaries built and uploaded to prerelease
- `update-homebrew-formula` job shows as "Skipped" in workflow run
- Tap repository formula NOT updated (remains at previous stable version)

**Test Data:**
- Prerelease tags: `v1.0.0-beta.1`, `v1.0.0-rc.2`, `v2.0.0-alpha.1`
- Expected job status: Skipped

---

#### WORKFLOW-UPDATE-006: Formula Update Failure Doesn't Block Release

**Type:** Integration (GitHub Actions)

**Priority:** High

**Description:**
Verify that if the `update-homebrew-formula` job fails, the GitHub Release still succeeds (graceful degradation).

**Preconditions:**
- Test scenario with intentional formula update failure (e.g., invalid token, missing checksums, or network error simulation)

**Test Steps:**
1. Temporarily misconfigure `HOMEBREW_TAP_TOKEN` secret (e.g., use invalid token)
2. Create test release tag
3. Monitor release workflow
4. Verify `release` job completes successfully
5. Verify `build-binaries` jobs complete successfully
6. Verify `consolidate-checksums` job completes successfully
7. Verify `update-homebrew-formula` job **FAILS** (due to auth error)
8. Verify GitHub Release is created despite formula job failure
9. Verify release assets (binaries, checksums) are uploaded
10. Restore valid token

**Expected Result:**
- GitHub Release created successfully
- All binaries and checksums uploaded to release
- `update-homebrew-formula` job fails (expected)
- Release workflow overall status: **Partial success** (some jobs failed)
- Users can still install via direct binary download
- Maintainer can manually update formula or re-run workflow

**Test Data:**
- Expected release status: Created with assets
- Expected formula job status: Failed
- Expected overall workflow status: Completed (with some failures)

---

#### WORKFLOW-ERROR-001: Missing Checksums in SHA256SUMS

**Type:** Integration (Script Testing)

**Priority:** High

**Description:**
Verify that the update script properly detects and reports errors when checksums are missing from the `SHA256SUMS` file.

**Preconditions:**
- `scripts/update-homebrew-formula.sh` script created
- Test `SHA256SUMS` file with one or more missing platform checksums

**Test Steps:**
1. Create test `SHA256SUMS` file missing `macos-x64` checksum:
   ```
   aabbccdd11223344...  tfplan2md_1.0.0_linux-x64.tar.gz
   eeff0011aabbccdd...  tfplan2md_1.0.0_macos-arm64.tar.gz
   # macos-x64 intentionally missing
   ```
2. Create test formula template
3. Run script: `bash scripts/update-homebrew-formula.sh 1.0.0 test-SHA256SUMS test-formula.rb`
4. Verify script detects missing checksum
5. Verify script prints error message: "❌ Error: Missing checksums in ..."
6. Verify script exits with code 1 (failure)
7. Verify formula file NOT updated (remains unchanged)

**Expected Result:**
- Script detects missing checksum(s) during extraction
- Clear error message printed to stderr
- Script exits with non-zero code
- Formula file NOT modified
- Workflow job fails (expected behavior)

**Test Data:**
- Test scenario: Missing `macos-x64` checksum
- Expected error: "❌ Error: Missing checksums in test-SHA256SUMS"

---

#### WORKFLOW-ERROR-002: Invalid Checksum Format

**Type:** Integration (Script Testing)

**Priority:** High

**Description:**
Verify that the update script properly validates checksum format and rejects invalid checksums.

**Preconditions:**
- `scripts/update-homebrew-formula.sh` script created
- Test `SHA256SUMS` file with invalid checksum format

**Test Steps:**
1. Create test `SHA256SUMS` file with invalid checksum (wrong length or non-hex):
   ```
   aabbccdd11223344INVALID  tfplan2md_1.0.0_linux-x64.tar.gz
   eeff0011aabbccdd...      tfplan2md_1.0.0_macos-arm64.tar.gz
   11223344aabbccdd...      tfplan2md_1.0.0_macos-x64.tar.gz
   ```
2. Run script: `bash scripts/update-homebrew-formula.sh 1.0.0 test-SHA256SUMS test-formula.rb`
3. Verify script detects invalid format
4. Verify script prints error: "❌ Error: Invalid checksum format: ..."
5. Verify script exits with code 1
6. Verify formula file NOT updated

**Expected Result:**
- Script validates checksum format (64 hex characters)
- Invalid checksums rejected
- Clear error message identifying which checksum is invalid
- Script exits with non-zero code
- Formula file NOT modified

**Test Data:**
- Invalid formats: 
  - Too short: `aabbccdd` (16 chars instead of 64)
  - Too long: `aabbccdd...` (65 chars)
  - Non-hex: `aabbccddzz...` (contains 'zz')

---

#### WORKFLOW-ERROR-003: Git Push Failure Handling

**Type:** Integration (GitHub Actions - Simulated Failure)

**Priority:** Medium

**Description:**
Verify that the workflow handles git push failures gracefully (e.g., network errors, authentication failures).

**Preconditions:**
- Ability to simulate push failure (e.g., invalid token, network disconnection)

**Test Steps:**
1. Set up scenario that will cause push to fail:
   - Option A: Use invalid/expired `HOMEBREW_TAP_TOKEN`
   - Option B: Modify tap repository to reject pushes (protected branch, requires review)
2. Trigger test release
3. Monitor `update-homebrew-formula` job
4. Verify formula update script runs successfully (updates local file)
5. Verify git commit succeeds (local)
6. Verify git push **FAILS** (authentication or permission error)
7. Verify job fails with clear error message
8. Verify GitHub Release still succeeds
9. Check workflow logs for error details

**Expected Result:**
- Formula updated locally but not pushed remotely
- Git push failure logged with error message (e.g., "Authentication failed", "Permission denied")
- Job fails (expected)
- GitHub Release succeeds (graceful degradation)
- Maintainer notified of failure (workflow email)
- Manual fix: Update token or re-run workflow

**Test Data:**
- Expected error patterns:
  - "fatal: Authentication failed"
  - "fatal: unable to access 'https://github.com/oocx/homebrew-tfplan2md.git/'"
  - "error: failed to push some refs"

---

### Integration Testing

#### INTEGRATION-E2E-001: End-to-End Release Workflow

**Type:** Manual (Full Release Cycle)

**Priority:** Critical

**Description:**
Verify the complete end-to-end workflow: create tag → build binaries → consolidate checksums → update formula → install via Homebrew.

**Preconditions:**
- All feature changes merged to release branch
- `HOMEBREW_TAP_TOKEN` configured
- Tap repository exists with template formula
- Access to macOS or Linux system with Homebrew

**Test Steps:**
1. **Create Release Tag**:
   - `git tag v0.0.2-brew-test`
   - `git push origin v0.0.2-brew-test`

2. **Monitor Workflow**:
   - Watch GitHub Actions workflow run
   - Verify all jobs complete successfully:
     - `release` ✅
     - `build-binaries` (all 5 platforms) ✅
     - `consolidate-checksums` ✅
     - `update-homebrew-formula` ✅

3. **Verify GitHub Release**:
   - Check release exists: `https://github.com/oocx/tfplan2md/releases/tag/v0.0.2-brew-test`
   - Verify assets uploaded:
     - `tfplan2md_0.0.2-brew-test_linux-x64.tar.gz`
     - `tfplan2md_0.0.2-brew-test_macos-x64.tar.gz`
     - `tfplan2md_0.0.2-brew-test_macos-arm64.tar.gz`
     - `tfplan2md_0.0.2-brew-test_windows-x64.zip`
     - `SHA256SUMS`

4. **Verify Formula Update**:
   - Check tap repository: `https://github.com/oocx/homebrew-tfplan2md`
   - Verify latest commit: `chore: update formula to v0.0.2-brew-test`
   - Review `Formula/tfplan2md.rb` changes:
     - Version updated
     - All checksums updated
     - URLs updated

5. **Test Installation**:
   - Uninstall previous version (if installed): `brew uninstall tfplan2md`
   - Update tap: `brew update`
   - Install: `brew install oocx/tfplan2md/tfplan2md`
   - Verify version: `tfplan2md --version` → should show `v0.0.2-brew-test`
   - Test functionality: `tfplan2md examples/azure_cdn.json > output.md`
   - Verify output.md is valid

**Expected Result:**
- Complete workflow executes successfully (tag → release → build → update → install)
- Total workflow time: ~15-20 minutes (depending on runners)
- Formula available via Homebrew within 5 minutes of release
- Users can install and use tfplan2md via Homebrew
- Binary works correctly after installation

**Test Data:**
- Test tag: `v0.0.2-brew-test`
- Expected platforms in release: 4 (linux-x64, macos-x64, macos-arm64, windows-x64)
- Expected formula version: `0.0.2-brew-test`

---

#### INTEGRATION-E2E-002: Prerelease Does Not Update Formula

**Type:** Integration (GitHub Actions)

**Priority:** High

**Description:**
Verify that creating a prerelease does NOT trigger formula updates, but binaries are still built.

**Preconditions:**
- Release workflow with formula update conditional logic

**Test Steps:**
1. Create prerelease tag: `git tag v1.0.0-rc.1 && git push origin v1.0.0-rc.1`
2. Monitor workflow run
3. Verify `release` job creates prerelease (with "This is a pre-release" badge)
4. Verify `build-binaries` jobs run and upload binaries
5. Verify `consolidate-checksums` job runs
6. Verify `update-homebrew-formula` job **SKIPPED**
7. Check tap repository - verify NO new commit
8. Check Homebrew formula - verify version still shows previous stable release (not rc.1)
9. Test installation: `brew install tfplan2md` → should install previous stable version, not rc.1

**Expected Result:**
- Prerelease created on GitHub
- Binaries available for manual download from prerelease
- Formula NOT updated (remains at previous stable version)
- Homebrew users install stable version, not prerelease
- Prerelease users can download binaries manually

**Test Data:**
- Prerelease tag: `v1.0.0-rc.1`
- Expected formula version: Previous stable (e.g., `1.0.0`, NOT `1.0.0-rc.1`)

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| No internet during brew install | Installation fails with download error | Not automated (network-dependent) |
| GitHub Releases rate limited | Formula update may fail, manual retry needed | WORKFLOW-ERROR-003 (covers push failures) |
| Tap repository deleted | Users cannot add tap, error message shown | Manual validation (destructive) |
| Formula has syntax error | `brew audit` and `brew install` fail with Ruby error | BREW-SYNTAX-001, BREW-SYNTAX-002 |
| Checksum mismatch (corrupted download) | Homebrew rejects installation, shows checksum error | BREW-CHECKSUM-002 |
| Multiple versions installed | `brew link` manages symlinks, latest version active | BREW-UPGRADE-001 (covers upgrade path) |
| User has no write access to tap | Cannot `brew tap`, error shown | Manual validation (permissions) |
| Binary not executable (chmod issue) | Homebrew sets +x during install, should work | BREW-INSTALL-001/002/003 (validation) |
| Homebrew version too old | May not support `on_macos`/`on_linux` syntax | Document minimum Homebrew version in formula README |
| Xcode CLT installation fails | macOS build fails, workflow logs error | BUILD-MAC-001, BUILD-MAC-002 (implicit) |
| linux-arm64 build still failing | Build job fails, but macOS/Linux x64 succeed | BUILD-REG-001 (linux-x64 still works) |

---

## Test Data Requirements

### Test Environments

1. **GitHub Actions Runners** (Automated Tests):
   - `ubuntu-latest` (for Linux x64 builds and workflow scripts)
   - `macos-13` (for macOS x64 builds)
   - `macos-14` (for macOS ARM64 builds)
   - `windows-latest` (for Windows x64 builds)

2. **Local Test Systems** (Manual Tests):
   - macOS x64 (Intel Mac) with Homebrew installed
   - macOS ARM64 (Apple Silicon Mac) with Homebrew installed
   - Linux x64 (Ubuntu 24.04 or WSL) with Homebrew installed

### Test Data Files

- **Sample Terraform plan JSON**: `examples/azure_cdn.json` (already exists in repository)
- **Test SHA256SUMS file**: Create for local script testing with known checksums
- **Test formula template**: Copy from tap repository for local validation
- **Invalid checksums**: For negative testing (wrong format, missing entries)

### Required Secrets and Tokens

- **`HOMEBREW_TAP_TOKEN`**: GitHub Personal Access Token with write access to `oocx/homebrew-tfplan2md`
  - Scope: `repo` (classic token) or `Contents: Read and write` (fine-grained)
  - Must be configured in repository secrets before testing

### Test Tap Repository

- **Primary**: `oocx/homebrew-tfplan2md` (production)
- **Test**: `oocx/homebrew-tfplan2md-test` (optional, for pre-production testing)

---

## Non-Functional Tests

### Performance

| Aspect | Requirement | Test Case |
|--------|-------------|-----------|
| macOS build time | <10 minutes (including Xcode installation) | BUILD-MAC-001, BUILD-MAC-002 |
| Formula update latency | <5 minutes from release publish | WORKFLOW-UPDATE-001 |
| Homebrew install time | <1 minute (network-dependent) | BREW-INSTALL-001/002/003 |
| Script execution time | <10 seconds | WORKFLOW-UPDATE-002 |

### Security

| Aspect | Requirement | Test Case |
|--------|-------------|-----------|
| Checksum verification | SHA256 validated before install | BREW-CHECKSUM-001, BREW-CHECKSUM-002 |
| Token security | `HOMEBREW_TAP_TOKEN` not exposed in logs | WORKFLOW-UPDATE-004 (verify logs masked) |
| HTTPS downloads | All binary URLs use HTTPS | BREW-SYNTAX-001 (audit checks URL scheme) |

### Compatibility

| Aspect | Requirement | Test Case |
|--------|-------------|-----------|
| Homebrew version | Works with Homebrew 4.x+ | BREW-SYNTAX-001 (syntax compatibility) |
| macOS version | macOS 10.15+ (x64), 11+ (ARM64) | BUILD-VAL-001, BUILD-VAL-002 |
| Linux distribution | Ubuntu 24.04+, Debian 13+, RHEL 10+ (glibc) | BUILD-VAL-003 |

---

## Open Questions

1. **Q: Should we test linux-arm64 builds even though they're Phase 2?**
   - **A**: No. Phase 2 is out of scope for this feature. linux-arm64 can be added in a future feature/fix if demand exists.

2. **Q: Do we need automated testing for Homebrew installation, or is manual testing sufficient?**
   - **A**: Manual testing is sufficient for Homebrew installation tests (BREW-INSTALL-*, BREW-UPGRADE-*, etc.). Homebrew itself is complex to automate in CI. Focus automation on workflow integration (WORKFLOW-* tests).

3. **Q: How do we test Xcode Command Line Tools installation without actually running it repeatedly?**
   - **A**: BUILD-MAC-001 and BUILD-MAC-002 test this implicitly by running full macOS builds in GitHub Actions. The workflow logs will show Xcode installation step output. Manual verification of logs is acceptable.

4. **Q: Should we create a test tap repository for pre-production validation?**
   - **A**: Recommended. Create `oocx/homebrew-tfplan2md-test` for testing formula updates before using production tap. This allows testing with test tags (e.g., `v0.0.0-brew-test`) without polluting production tap history.

5. **Q: What if Homebrew audit/style checks find issues we can't fix immediately?**
   - **A**: Treat audit/style failures as blockers for merging. The formula MUST pass `brew audit --new-formula` and `brew style` before release. If issues are found, fix them in the formula template before proceeding.

6. **Q: How do we handle rollback if a bad formula is pushed to production tap?**
   - **A**: Manual rollback process (see ADR-003 "Rollback Strategy"):
     - Option 1: `git revert HEAD` in tap repository and push
     - Option 2: Manually edit formula with correct values and commit fix
     - Prevention: WORKFLOW-UPDATE-002 validates checksums before pushing

---

## Test Execution Schedule

### Phase 1: Pre-Development (Before Implementation)
- **BREW-SYNTAX-001**: Validate formula template syntax locally
- **BREW-SYNTAX-002**: Validate formula template style locally
- **WORKFLOW-UPDATE-002**: Test update script locally with mock data

### Phase 2: During Development (CI/CD Integration)
- **BUILD-MAC-001**: Test on first commit with Xcode installation step
- **BUILD-MAC-002**: Test on first commit with Xcode installation step
- **BUILD-WIN-001**: Verify windows-arm64 removed from matrix
- **BUILD-REG-001**: Regression test for linux-x64 (every commit)
- **BUILD-REG-002**: Regression test for windows-x64 (every commit)

### Phase 3: Pre-Release (Before Merging to Main)
- **BUILD-VAL-001**: Validate macOS x64 binary architecture
- **BUILD-VAL-002**: Validate macOS ARM64 binary architecture
- **BUILD-VAL-003**: Validate Linux x64 binary architecture
- **WORKFLOW-UPDATE-001**: Test formula update job with test tag
- **WORKFLOW-UPDATE-003**: Verify placeholder replacement
- **WORKFLOW-UPDATE-004**: Verify tap repository push
- **WORKFLOW-UPDATE-005**: Test prerelease skipping formula update
- **WORKFLOW-ERROR-001**: Test missing checksums error handling
- **WORKFLOW-ERROR-002**: Test invalid checksum error handling
- **INTEGRATION-E2E-001**: Full end-to-end test with test tag

### Phase 4: Post-Release (After First Production Release)
- **BREW-INSTALL-001**: Test installation on macOS x64
- **BREW-INSTALL-002**: Test installation on macOS ARM64
- **BREW-INSTALL-003**: Test installation on Linux x64
- **BREW-PLATFORM-001**: Verify platform detection across systems
- **BREW-CHECKSUM-001**: Verify valid checksum acceptance
- **BREW-UPGRADE-001**: Test upgrade path (after second release)
- **BREW-UNINSTALL-001**: Test uninstallation

### Ongoing (Per Release)
- **INTEGRATION-E2E-002**: Verify prerelease behavior
- **Spot-check**: BREW-INSTALL-001 or BREW-INSTALL-002 (every 3-5 releases)

---

## Success Criteria

The test plan is considered complete when:

- [x] All critical test cases defined with clear steps and expected results
- [x] Test coverage matrix maps all acceptance criteria to test cases
- [x] Both automated (CI/CD) and manual test cases specified
- [x] Edge cases and error scenarios covered
- [x] Non-functional requirements (performance, security, compatibility) addressed
- [x] Test data requirements identified (environments, files, secrets)
- [x] Test execution schedule defined
- [x] Open questions documented for Maintainer clarification

---

## Notes for Developer Agent

### Implementation Priority

1. **Critical Path** (blocks Homebrew feature):
   - Fix macOS builds (ADR-001): BUILD-MAC-001, BUILD-MAC-002
   - Remove windows-arm64 (ADR-001): BUILD-WIN-001
   - Create update script (ADR-002, ADR-003): WORKFLOW-UPDATE-002
   - Add workflow job (ADR-003): WORKFLOW-UPDATE-001

2. **Validation** (before merge):
   - Binary architecture validation: BUILD-VAL-001, BUILD-VAL-002, BUILD-VAL-003
   - Formula syntax validation: BREW-SYNTAX-001, BREW-SYNTAX-002
   - End-to-end test: INTEGRATION-E2E-001

3. **Post-Release** (after first production release):
   - Installation testing: BREW-INSTALL-001, BREW-INSTALL-002, BREW-INSTALL-003
   - Platform detection: BREW-PLATFORM-001

### Testing Infrastructure

- **No new test framework required**: Homebrew tests are primarily manual or workflow-based
- **Script testing**: Use bash directly for `update-homebrew-formula.sh` validation
- **CI/CD testing**: Leverage existing GitHub Actions workflows
- **Manual testing**: Requires access to macOS and Linux systems with Homebrew

### Test Automation Limits

Due to Homebrew's nature, the following tests **cannot be easily automated** in CI/CD:
- BREW-INSTALL-* (requires Homebrew installed, complex to set up in CI)
- BREW-PLATFORM-001 (requires multiple physical/virtual systems)
- BREW-UPGRADE-001 (requires two sequential releases)
- BREW-CHECKSUM-002 (requires modifying formula, not suitable for CI)

**Recommendation**: Focus automation on workflow integration (WORKFLOW-* tests) and build validation (BUILD-* tests). Manual testing for Homebrew-specific scenarios is acceptable and expected.

---

## Definition of Done

Testing is complete when:

- [ ] All **Critical** priority test cases executed and passed
- [ ] All **High** priority test cases executed and passed
- [ ] macOS x64 and ARM64 builds work in CI/CD
- [ ] Formula updated successfully in tap repository via workflow
- [ ] At least one successful end-to-end release cycle completed (INTEGRATION-E2E-001)
- [ ] Installation tested on at least 2 platforms (macOS x64 or ARM64, Linux x64)
- [ ] No blocking issues identified
- [ ] All test results documented in work protocol
- [ ] Maintainer approves test results before release
