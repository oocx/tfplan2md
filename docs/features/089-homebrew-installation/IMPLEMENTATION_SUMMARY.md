# Homebrew Installation Support - Implementation Summary

## Completed Tasks

### ✅ TASK-001 & TASK-002: Fix macOS Platform Builds
**Status:** Implementation complete, awaiting CI validation

**Changes Made:**
- Added Xcode Command Line Tools installation step to release workflow
- Step runs before .NET setup for both macOS x64 (macos-13) and macOS ARM64 (macos-14) platforms
- Includes validation to ensure Xcode tools are installed before proceeding
- Build time will increase by ~2-3 minutes per macOS platform

**File Modified:**
- `.github/workflows/release.yml` (lines 266-279)

**Verification:**
- Workflow step only runs for macOS platforms using conditional: `startsWith(matrix.platform, 'macos-')`
- Installation verified with `xcode-select -p` before continuing
- Exits with error if Xcode tools not found

---

### ✅ TASK-003: Remove windows-arm64 from Build Matrix
**Status:** Complete

**Changes Made:**
- Removed `windows-arm64` matrix entry from build-binaries job
- Build matrix now has 5 platforms instead of 6:
  - linux-x64 ✅
  - linux-arm64 ✅
  - windows-x64 ✅
  - macos-x64 ✅
  - macos-arm64 ✅
  - ~~windows-arm64~~ ❌ (removed)

**Rationale:**
- No native ARM64 Windows runners available in GitHub Actions
- Cross-compilation is complex and fragile
- Low usage (<1% of Windows market share)
- Can be re-added later if native runners become available

**File Modified:**
- `.github/workflows/release.yml` (lines 239-245 removed)

---

### ✅ TASK-007: Create Formula Update Script
**Status:** Complete and tested

**Changes Made:**
- Created `scripts/update-homebrew-formula.sh` with:
  - Checksum extraction from SHA256SUMS file
  - Validation of checksum presence and format (64 hex characters)
  - Placeholder replacement using sed
  - Verbose output for debugging
  - Proper error handling with exit codes

**File Created:**
- `scripts/update-homebrew-formula.sh` (executable)

**Testing:**
- ✅ Successfully updates formula template with sample data
- ✅ Validates checksums are present (fails if missing)
- ✅ Validates checksum format (fails if invalid)
- ✅ Replaces all 4 placeholders correctly:
  - `{{VERSION}}`
  - `{{MACOS_X64_SHA256}}`
  - `{{MACOS_ARM64_SHA256}}`
  - `{{LINUX_X64_SHA256}}`

---

### ✅ TASK-008: Add Formula Update Job to Release Workflow
**Status:** Implementation complete, awaiting tap repository creation

**Changes Made:**
- Added `update-homebrew-formula` job to release workflow after `consolidate-checksums`
- Job only runs for stable releases (skips prereleases)
- Workflow steps:
  1. Checkout main repository (to access update script)
  2. Download consolidated checksums artifact
  3. Checkout tap repository (`oocx/homebrew-tfplan2md`) with `HOMEBREW_TAP_TOKEN`
  4. Run update script with version, checksums, and formula path
  5. Commit and push changes to tap repository
- Includes idempotent commit check (only commits if formula changed)
- Added artifact upload step for consolidated checksums

**Files Modified:**
- `.github/workflows/release.yml` (new job and artifact upload step)

**Validation:**
- ✅ YAML syntax valid
- ✅ Job dependencies correct (`needs: [release, consolidate-checksums]`)
- ✅ Conditional logic correct (`is_prerelease != 'true'`)
- ✅ Uses `HOMEBREW_TAP_TOKEN` secret for authentication

---

## Manual Tasks Required (Maintainer)

### 🔧 TASK-005: Create Homebrew Tap Repository

**Action Required:** Create a new public GitHub repository

**Repository Details:**
- **Name:** `homebrew-tfplan2md`
- **Owner:** `oocx` organization
- **Visibility:** Public (required for Homebrew)
- **Description:** "Homebrew tap for tfplan2md - Convert Terraform plan JSON to Markdown"

**Repository Structure:**

```
oocx/homebrew-tfplan2md/
├── Formula/
│   └── tfplan2md.rb
├── README.md
├── LICENSE
└── .gitignore
```

**Steps to Create:**

1. **Create Repository:**
   ```bash
   # Via GitHub web UI or CLI:
   gh repo create oocx/homebrew-tfplan2md \
     --public \
     --description "Homebrew tap for tfplan2md" \
     --clone
   ```

2. **Initialize Structure:**
   ```bash
   cd homebrew-tfplan2md
   mkdir Formula
   ```

3. **Create README.md:**
   ```markdown
   # Homebrew Tap for tfplan2md

   Official Homebrew tap for [tfplan2md](https://github.com/oocx/tfplan2md).

   ## Installation

   ```bash
   # Add the tap
   brew tap oocx/tfplan2md

   # Install tfplan2md
   brew install tfplan2md

   # Verify installation
   tfplan2md --version
   ```

   ## Updating

   ```bash
   brew update
   brew upgrade tfplan2md
   ```

   ## Uninstallation

   ```bash
   brew uninstall tfplan2md
   brew untap oocx/tfplan2md
   ```

   ## Supported Platforms

   - macOS x64 (Intel)
   - macOS ARM64 (Apple Silicon)
   - Linux x64 (including WSL)

   ## License

   This tap is maintained by the tfplan2md project and is distributed under the MIT license.
   ```

4. **Create LICENSE:**
   - Copy MIT license from main tfplan2md repository

5. **Create Formula Template (`Formula/tfplan2md.rb`):**
   ```ruby
   class Tfplan2md < Formula
     desc "Convert Terraform plan JSON files into human-readable Markdown reports"
     homepage "https://github.com/oocx/tfplan2md"
     license "MIT"
     version "{{VERSION}}"

     on_macos do
       if Hardware::CPU.intel?
         url "https://github.com/oocx/tfplan2md/releases/download/v{{VERSION}}/tfplan2md_{{VERSION}}_macos-x64.tar.gz"
         sha256 "{{MACOS_X64_SHA256}}"
       elsif Hardware::CPU.arm?
         url "https://github.com/oocx/tfplan2md/releases/download/v{{VERSION}}/tfplan2md_{{VERSION}}_macos-arm64.tar.gz"
         sha256 "{{MACOS_ARM64_SHA256}}"
       end
     end

     on_linux do
       if Hardware::CPU.intel?
         url "https://github.com/oocx/tfplan2md/releases/download/v{{VERSION}}/tfplan2md_{{VERSION}}_linux-x64.tar.gz"
         sha256 "{{LINUX_X64_SHA256}}"
       end
     end

     def install
       bin.install "tfplan2md"
     end

     test do
       system "#{bin}/tfplan2md", "--version"
       system "#{bin}/tfplan2md", "--help"
     end
   end
   ```

6. **Commit and Push:**
   ```bash
   git add .
   git commit -m "chore: initialize Homebrew tap with formula template"
   git push origin main
   ```

7. **Verify Repository:**
   - Repository is public: https://github.com/oocx/homebrew-tfplan2md
   - Formula template is at: `Formula/tfplan2md.rb`
   - Placeholders are intact (not replaced yet)

---

### 🔧 TASK-009: Verify GitHub Secret Configuration

**Action Required:** Confirm `HOMEBREW_TAP_TOKEN` secret exists

According to the Maintainer comment in tasks.md, the `HOMEBREW_TAP_TOKEN` secret has already been created. Please verify:

1. **Check Secret Exists:**
   - Go to: https://github.com/oocx/tfplan2md/settings/secrets/actions
   - Verify `HOMEBREW_TAP_TOKEN` is listed

2. **Verify Token Permissions:**
   The token should have:
   - **Classic PAT:** `repo` scope
   - **OR Fine-grained PAT:** 
     - Repository access: `oocx/homebrew-tfplan2md`
     - Permissions: `Contents: Read and write`

3. **Test Token (Optional):**
   ```bash
   # Test with gh CLI
   export GITHUB_TOKEN=<paste-token-value>
   gh repo view oocx/homebrew-tfplan2md
   ```

If the secret needs to be recreated or updated:
1. Generate new PAT: https://github.com/settings/tokens
2. Update repository secret: https://github.com/oocx/tfplan2md/settings/secrets/actions

---

## Pending Tasks (Not Implemented)

### ⏸️ TASK-004: Verify Platform Build Regression Testing
**Status:** Pending CI workflow execution

**What Happens Next:**
- When the next release is triggered, the workflow will build all 5 platforms
- macOS x64 and macOS ARM64 should build successfully (with Xcode CLT installation)
- linux-x64, linux-arm64, and windows-x64 should continue working as before
- windows-arm64 will NOT be built (removed from matrix)

**Manual Validation Required After First Release:**
1. Check GitHub Actions workflow run
2. Verify all 5 platform builds complete successfully
3. Download artifacts and verify binaries are correct architecture
4. Confirm checksums are generated for all 5 platforms

---

### ⏸️ TASK-006: Create Homebrew Formula Template
**Status:** Included in TASK-005 instructions above

The formula template is provided in the TASK-005 repository setup instructions. Once the repository is created with the formula template, this task is complete.

---

### ⏸️ TASK-010: End-to-End Automation Testing
**Status:** Pending tap repository creation and first release

**What Happens Next:**
1. After tap repository is created (TASK-005)
2. And HOMEBREW_TAP_TOKEN is verified (TASK-009)
3. The next stable release will trigger the `update-homebrew-formula` job
4. The job will update the formula in the tap repository
5. Users can then install tfplan2md via Homebrew

**Testing Plan:**
- Monitor the `update-homebrew-formula` job in the first release
- Check that the formula is updated in the tap repository
- Test installation on macOS and Linux:
  ```bash
  brew tap oocx/tfplan2md
  brew install tfplan2md
  tfplan2md --version
  ```

---

## Next Steps

### For Maintainer:

1. **Create Tap Repository** (TASK-005)
   - Follow instructions above to create `oocx/homebrew-tfplan2md`
   - Initialize with Formula template, README, and LICENSE

2. **Verify Secret** (TASK-009)
   - Confirm `HOMEBREW_TAP_TOKEN` exists and has correct permissions

3. **Review and Approve Pull Request**
   - Review code changes in feature branch
   - Approve for merge to main

4. **Trigger Test Release** (Optional)
   - Create a test tag (e.g., `v0.0.0-brew-test`)
   - Verify workflow completes successfully
   - Check that macOS builds work
   - Verify formula update job runs (will skip for prerelease tag)

5. **Create Stable Release**
   - Once testing is complete, create a stable release
   - Formula will be automatically updated for stable releases

### For Code Reviewer:

- Review implementation changes in `.github/workflows/release.yml`
- Review update script `scripts/update-homebrew-formula.sh`
- Verify workflow YAML syntax and job dependencies
- Check that error handling is appropriate

### For Technical Writer:

- Update README.md to include Homebrew installation option
- Add Homebrew to installation docs
- Ensure installation instructions are clear and accurate

---

## Implementation Notes

### Why macOS Builds Failed Before

GitHub Actions macOS runners (macos-13 and macos-14) do NOT have Xcode Command Line Tools pre-installed. The .NET NativeAOT compiler requires:
- Xcode clang compiler for native code generation
- macOS SDK headers for system library linking
- Apple linker (ld) for creating the final executable

Without these tools, `dotnet publish -p:PublishAot=true` fails during the native compilation phase.

### Why windows-arm64 Was Removed

- GitHub Actions does not provide native ARM64 Windows runners
- Cross-compilation from x64 to ARM64 on Windows is complex and fragile
- Windows ARM64 represents <1% of the Windows market share
- No user demand for Windows ARM64 binaries
- Can be re-added if GitHub adds native ARM64 Windows runners

### Workflow Automation Design

The `update-homebrew-formula` job is designed to:
- Run AFTER checksums are consolidated (depends on `consolidate-checksums`)
- Run ONLY for stable releases (skips prereleases using conditional)
- Use the consolidated SHA256SUMS artifact as the single source of truth
- Fail gracefully (does NOT block GitHub Release if formula update fails)
- Be idempotent (only commits if formula actually changed)

### Security Considerations

- `HOMEBREW_TAP_TOKEN` is stored as a GitHub secret (encrypted)
- Token is only used by the `update-homebrew-formula` job (least privilege)
- Token is NOT exposed in workflow logs (GitHub masks secrets)
- Token only grants access to tap repository (not main repository)

---

## File Changes Summary

**Modified Files:**
- `.github/workflows/release.yml` (platform fixes, workflow automation)
- `docs/features/089-homebrew-installation/tasks.md` (task status updates)

**Created Files:**
- `scripts/update-homebrew-formula.sh` (formula update automation)
- `docs/features/089-homebrew-installation/IMPLEMENTATION_SUMMARY.md` (this file)

**Repository to Create:**
- `oocx/homebrew-tfplan2md` (Homebrew tap repository)

---

## Verification Checklist

Before merging:
- [x] Workflow YAML syntax is valid
- [x] Update script is executable and tested
- [x] Build completes without errors
- [x] Task status is updated in tasks.md
- [ ] Tap repository created (Maintainer action)
- [ ] HOMEBREW_TAP_TOKEN verified (Maintainer action)
- [ ] Code review approved
- [ ] CI validation passes (after merge)

---

## Questions for Maintainer

1. **Tap Repository Owner:** Should the tap repository be under the `oocx` organization or a personal account?
   - **Recommendation:** Use `oocx` organization for consistency

2. **HOMEBREW_TAP_TOKEN:** Is the existing secret still valid and has correct permissions?
   - **Required Permissions:** `repo` scope (classic) OR `Contents: Read and write` (fine-grained)

3. **Testing Strategy:** Should we create a test release first, or proceed directly to production release?
   - **Recommendation:** Create test tag `v0.0.0-brew-test` to validate macOS builds first

4. **Formula Testing:** Do you have access to macOS (Intel and Apple Silicon) for testing Homebrew installation?
   - **Alternatives:** Can use GitHub Actions macos-13 and macos-14 runners for testing

---

## References

- **Feature Specification:** `docs/features/089-homebrew-installation/specification.md`
- **ADR-001:** Platform Build Fixes
- **ADR-002:** Homebrew Formula Design
- **ADR-003:** Release Workflow Integration
- **Test Plan:** `docs/features/089-homebrew-installation/test-plan.md`
- **Homebrew Documentation:** https://docs.brew.sh/How-to-Create-and-Maintain-a-Tap
