# Feature Specification: Homebrew Installation Support

## Overview

This feature adds support for installing tfplan2md via Homebrew on macOS and Linux (WSL). Users will be able to install tfplan2md with `brew install tfplan2md`, making it accessible through the standard package manager used by macOS and Linux developers.

This builds on top of Feature 047 (Multi-Platform Binary Distribution), which already provides pre-built native binaries for macOS (x64 and ARM64) and Linux platforms via GitHub Releases. The Homebrew integration will leverage these existing release assets to provide a managed installation experience with automatic updates.

## User Goals

### Primary Users

1. **macOS Developers**: Developers on macOS who prefer managing CLI tools through Homebrew for consistent version management and easy updates.

2. **WSL Users**: Windows developers using Windows Subsystem for Linux who rely on Homebrew for Linux package management.

3. **DevOps Engineers**: Teams standardizing on Homebrew for toolchain management across macOS and Linux development environments.

### User Outcomes

- Install tfplan2md with a simple `brew install tfplan2md` command
- Automatically receive updates when running `brew upgrade` or `brew upgrade tfplan2md`
- No need to manually download binaries, verify checksums, or manage PATH
- Consistent installation experience across macOS and Linux (WSL)
- Easy uninstallation with `brew uninstall tfplan2md`

## Scope

### In Scope

1. **Homebrew Tap Setup**: Create a dedicated Homebrew tap repository (e.g., `oocx/homebrew-tfplan2md`) to host the formula
2. **Homebrew Formula**: Create a Ruby formula that downloads pre-built binaries from GitHub Releases
3. **Multi-Platform Support**: Formula supports macOS (x64 and ARM64) and Linux (x64) using platform-specific binary URLs
4. **Automated Formula Updates**: Release workflow automatically updates the formula with new version, SHA256 checksums, and binary URLs
5. **Documentation**: Update README.md and installation docs to include Homebrew as an installation option
6. **Testing**: Local testing on macOS and Linux (WSL) to verify installation, execution, and uninstallation

### Out of Scope

1. **Homebrew Core Submission**: Not submitting to homebrew/core (requires meeting core criteria and maintenance burden). Custom tap is sufficient for this project.
2. **Homebrew Bottles**: Pre-built bottles are not needed since we're distributing pre-compiled native binaries (not building from source)
3. **Linux ARM64 Support**: Feature 047 includes linux-arm64 binaries, but Homebrew on ARM64 Linux is not widely used; may be added in future if demand exists
4. **Automatic CI Testing**: Homebrew formula testing in CI (complex setup requiring brew installation on runners); rely on manual testing pre-release
5. **Windows Support**: Homebrew is not available on native Windows (only WSL)

## User Experience

### Installation

Users install tfplan2md using standard Homebrew commands:

```bash
# Add the tfplan2md tap (one-time setup)
brew tap oocx/tfplan2md

# Install tfplan2md
brew install tfplan2md

# Verify installation
tfplan2md --version
tfplan2md --help
```

### Updating

Users update tfplan2md when new versions are released:

```bash
# Update Homebrew formulae
brew update

# Upgrade tfplan2md to latest version
brew upgrade tfplan2md

# Or upgrade all installed formulae
brew upgrade
```

### Uninstallation

Users can easily remove tfplan2md:

```bash
# Uninstall tfplan2md
brew uninstall tfplan2md

# Optionally remove the tap
brew untap oocx/tfplan2md
```

### Expected Behavior

- **Platform Detection**: Formula automatically selects the correct binary based on the user's platform (macOS x64, macOS ARM64, or Linux x64)
- **Binary Installation**: Binary is installed to Homebrew's bin directory (e.g., `/usr/local/bin/tfplan2md` or `/opt/homebrew/bin/tfplan2md`)
- **PATH Integration**: Homebrew automatically adds the binary to the user's PATH
- **Version Management**: Homebrew tracks installed version and notifies users of available updates via `brew outdated`
- **Self-Contained**: Binary requires no additional dependencies (already Native AOT compiled)

### Error Scenarios

1. **Unsupported Platform**: If a user tries to install on an unsupported platform (e.g., Windows without WSL), Homebrew will display an error indicating the platform is not supported.

2. **Download Failure**: If GitHub Releases is unavailable or the release asset is missing, Homebrew will report a download error and suggest retrying.

3. **Checksum Mismatch**: If the downloaded binary's SHA256 checksum doesn't match the formula, Homebrew will abort installation and report a checksum error (security protection).

4. **Tap Not Found**: If the user forgets to run `brew tap oocx/tfplan2md`, the `brew install tfplan2md` command will fail with "No available formula with the name tfplan2md".

## Requirements

### Functional Requirements

**FR1: Homebrew Tap Repository**
- A dedicated GitHub repository MUST be created at `oocx/homebrew-tfplan2md` following Homebrew tap naming conventions
- The repository MUST be public for Homebrew to access it
- The repository MUST contain a `Formula/` directory for formula files

**FR2: Homebrew Formula**
- A Ruby formula file MUST be created at `Formula/tfplan2md.rb` in the tap repository
- The formula MUST specify:
  - Project description and homepage
  - License (MIT)
  - Binary download URLs for each supported platform (macOS x64, macOS ARM64, Linux x64)
  - SHA256 checksums for each binary archive
  - Version number matching the GitHub Release tag
- The formula MUST use platform-specific URL and SHA256 values based on detected OS and architecture

**FR3: Binary Installation**
- The formula MUST download the appropriate pre-built binary archive from GitHub Releases (from Feature 047)
- The formula MUST extract the binary and install it to Homebrew's bin directory
- The formula MUST NOT attempt to build from source (binary-only formula)

**FR4: Platform Support**
- Formula MUST support macOS x64 (Intel)
- Formula MUST support macOS ARM64 (Apple Silicon M1/M2/M3)
- Formula MUST support Linux x64 (WSL and native Linux)
- Formula SHOULD detect platform and select appropriate binary automatically

**FR5: Automated Formula Updates**
- The release workflow MUST update the Homebrew formula when a new version is released
- Updates MUST include:
  - New version number
  - Updated SHA256 checksums for all platform binaries
  - Updated binary download URLs
- Formula updates MUST be committed and pushed to the tap repository automatically

**FR6: Documentation**
- README.md MUST include Homebrew installation instructions
- Homebrew SHOULD be listed as a recommended installation method alongside Docker
- Installation docs MUST explain the tap setup step

### Non-Functional Requirements

**NFR1: Installation Speed**
- Installation via Homebrew SHOULD complete in under 1 minute (network-dependent)
- Binary download is the primary time factor; extraction and installation are fast

**NFR2: Version Update Latency**
- Formula SHOULD be updated within 5 minutes of a new GitHub Release being published
- Users running `brew update` SHOULD see the new version within 10 minutes of release

**NFR3: Security**
- Formula MUST verify SHA256 checksums before installing binaries
- Checksums MUST be generated from actual release artifacts, not intermediate builds
- Binary download URLs MUST use HTTPS

**NFR4: Compatibility**
- Formula MUST work with current Homebrew stable versions (4.x+)
- Formula syntax MUST follow Homebrew style guidelines
- Formula MUST pass `brew audit tfplan2md` and `brew style tfplan2md` validation

**NFR5: Maintainability**
- Formula updates MUST be automated (no manual editing required for releases)
- Release workflow SHOULD fail gracefully if formula update fails (not block release)
- Formula repository SHOULD include clear README with development instructions

## Technical Approach

### Homebrew Tap Repository Structure

Create a new repository `oocx/homebrew-tfplan2md` with the following structure:

```
homebrew-tfplan2md/
├── Formula/
│   └── tfplan2md.rb
├── README.md
└── .github/
    └── workflows/
        └── (optional: formula testing workflow)
```

### Formula Template

The `Formula/tfplan2md.rb` file will follow this structure:

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

**Placeholders** (`{{VERSION}}`, `{{MACOS_X64_SHA256}}`, etc.) will be replaced by the automated update script during releases.

### Release Workflow Integration

Modify `.github/workflows/release.yml` to add a new job `update-homebrew-formula` that runs after `consolidate-checksums`:

```yaml
update-homebrew-formula:
  name: Update Homebrew Formula
  runs-on: ubuntu-latest
  needs: [release, consolidate-checksums]
  if: needs.release.outputs.version != '' && needs.release.outputs.is_prerelease != 'true'
  
  steps:
    - name: Checkout tfplan2md repository
      uses: actions/checkout@v6

    - name: Download consolidated checksums
      uses: actions/download-artifact@v4
      with:
        name: checksums
        path: checksums

    - name: Checkout Homebrew tap repository
      uses: actions/checkout@v6
      with:
        repository: oocx/homebrew-tfplan2md
        token: ${{ secrets.HOMEBREW_TAP_TOKEN }}
        path: homebrew-tap

    - name: Update formula
      run: |
        # Script to update Formula/tfplan2md.rb with new version and checksums
        # Parse SHA256SUMS file to extract checksums for each platform
        # Replace placeholders in formula template
        # Commit and push changes to tap repository
      
    - name: Commit and push formula update
      run: |
        cd homebrew-tap
        git config user.name "github-actions[bot]"
        git config user.email "github-actions[bot]@users.noreply.github.com"
        git add Formula/tfplan2md.rb
        git commit -m "chore: update formula to v${{ needs.release.outputs.version }}"
        git push
```

**Key Requirements:**
- Job runs ONLY for stable releases (not prereleases)
- Job has access to consolidated `SHA256SUMS` artifact from `consolidate-checksums` job
- Requires a GitHub Personal Access Token (`HOMEBREW_TAP_TOKEN`) with write access to the tap repository
- Update script parses checksums from `SHA256SUMS` and updates formula placeholders
- Commits and pushes changes to tap repository with conventional commit message

### Formula Update Script

A shell script `scripts/update-homebrew-formula.sh` will handle the formula update logic:

```bash
#!/bin/bash
set -e

VERSION="$1"
CHECKSUMS_FILE="$2"
FORMULA_FILE="$3"

# Extract checksums from SHA256SUMS for each platform
MACOS_X64_SHA=$(grep "macos-x64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')
MACOS_ARM64_SHA=$(grep "macos-arm64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')
LINUX_X64_SHA=$(grep "linux-x64.tar.gz" "$CHECKSUMS_FILE" | awk '{print $1}')

# Update formula file (using sed or template engine)
sed -i "s/{{VERSION}}/$VERSION/g" "$FORMULA_FILE"
sed -i "s/{{MACOS_X64_SHA256}}/$MACOS_X64_SHA/g" "$FORMULA_FILE"
sed -i "s/{{MACOS_ARM64_SHA256}}/$MACOS_ARM64_SHA/g" "$FORMULA_FILE"
sed -i "s/{{LINUX_X64_SHA256}}/$LINUX_X64_SHA/g" "$FORMULA_FILE"

echo "Formula updated to version $VERSION"
```

### Testing Strategy

**Local Testing (Pre-Release)**:
1. Create a test formula with a test version (e.g., `v0.0.0-brew-test`)
2. Test installation on macOS x64, macOS ARM64, and Linux x64 (WSL)
3. Verify:
   - Formula syntax: `brew audit --new-formula tfplan2md`
   - Style compliance: `brew style tfplan2md`
   - Installation: `brew install tfplan2md`
   - Execution: `tfplan2md --version`, `tfplan2md --help`
   - Functionality: `tfplan2md examples/azure_cdn.json > output.md`
   - Uninstallation: `brew uninstall tfplan2md`

**Post-Release Validation**:
1. After each release, manually test installation from the tap on at least one platform
2. Verify formula update was applied correctly (check version and checksums in formula)
3. Confirm users can discover updates via `brew outdated`

## Success Criteria

- [ ] Homebrew tap repository `oocx/homebrew-tfplan2md` is created and public
- [ ] Formula `Formula/tfplan2md.rb` is created with correct structure and platform support
- [ ] Formula successfully installs tfplan2md on macOS x64 (Intel)
- [ ] Formula successfully installs tfplan2md on macOS ARM64 (Apple Silicon)
- [ ] Formula successfully installs tfplan2md on Linux x64 (WSL or native)
- [ ] Formula correctly selects platform-specific binary based on detected OS and architecture
- [ ] SHA256 checksum verification works correctly (installation fails if checksum mismatches)
- [ ] Release workflow automatically updates formula with new version, checksums, and URLs
- [ ] Formula passes `brew audit` and `brew style` validation
- [ ] Binary is installed to Homebrew's bin directory and is executable
- [ ] `tfplan2md --version` and `tfplan2md --help` work after installation
- [ ] Binary can process Terraform plan JSON files correctly after Homebrew installation
- [ ] Users can update tfplan2md with `brew upgrade tfplan2md`
- [ ] Users can uninstall tfplan2md with `brew uninstall tfplan2md`
- [ ] README.md includes Homebrew installation instructions
- [ ] Formula update does not block GitHub Release if it fails (graceful degradation)

## Dependencies

### Blockers

- **Feature 047 (Multi-Platform Binary Distribution)**: MUST be complete and verified. Homebrew formula depends on GitHub Release assets from Feature 047 (binaries for macos-x64, macos-arm64, linux-x64).
  - **Current Status**: ✅ Feature 047 is COMPLETE and released (confirmed in work-protocol.md)

### Prerequisites

- **GitHub Personal Access Token**: A GitHub PAT with write access to the `oocx/homebrew-tfplan2md` repository must be added to GitHub Secrets as `HOMEBREW_TAP_TOKEN`
- **Tap Repository**: The `oocx/homebrew-tfplan2md` repository must be created before the release workflow can update the formula
- **SHA256SUMS Artifact**: The `consolidate-checksums` job from the release workflow must complete successfully to provide checksums

### External Dependencies

- **Homebrew**: Users must have Homebrew installed on their system (macOS or Linux)
- **GitHub Releases**: Formula downloads binaries from GitHub Releases (already provided by Feature 047)
- **Internet Access**: Users need internet access to add tap, download formula, and install binary

## Risks and Mitigations

### Risk 1: Homebrew Tap Access
**Description**: Homebrew requires the tap repository to be public. If the tfplan2md repository becomes private, the tap repository should remain public for Homebrew users.  
**Impact**: Medium - Users cannot install via Homebrew if tap is private or inaccessible.  
**Likelihood**: Low - No plan to make tap private; Homebrew formulae are typically public.  
**Mitigation**: Document in tap repository README that it must remain public for Homebrew compatibility. If tfplan2md repository becomes private, tap remains public with formula referencing public GitHub Releases.

### Risk 2: Formula Update Failure
**Description**: The automated formula update might fail due to network issues, authentication problems, or incorrect checksums.  
**Impact**: Medium - Formula is not updated, users cannot install new version via Homebrew immediately.  
**Likelihood**: Medium - Network and credential issues are common in CI/CD.  
**Mitigation**: 
- Formula update job should NOT block GitHub Release creation (independent job)
- Add retry logic and detailed error logging to formula update script
- Manual fallback: Maintainer can manually update formula if automation fails
- Monitor workflow runs to catch failures quickly

### Risk 3: Platform Detection Issues
**Description**: Homebrew's platform detection might not work correctly on all systems (especially newer macOS or Linux variants).  
**Impact**: Low - Users may get incorrect binary or installation fails.  
**Likelihood**: Low - Homebrew's `Hardware::CPU` and OS detection are well-tested.  
**Mitigation**: 
- Use standard Homebrew platform detection APIs (`on_macos`, `on_linux`, `Hardware::CPU.intel?`, `Hardware::CPU.arm?`)
- Test formula on multiple platforms before releasing
- Document supported platforms in formula and README

### Risk 4: Checksum Mismatch
**Description**: If checksums in formula don't match downloaded binaries (due to update script bug or corrupted download), Homebrew will reject installation.  
**Impact**: High - Users cannot install tfplan2md via Homebrew.  
**Likelihood**: Low - Checksums are sourced from the same `SHA256SUMS` file used for GitHub Releases.  
**Mitigation**: 
- Use the consolidated `SHA256SUMS` artifact from the release workflow as the single source of truth
- Add validation in update script to verify checksums are non-empty and correct format (64 hex characters)
- Test formula installation after each update to catch mismatches early

### Risk 5: Homebrew Core Confusion
**Description**: Users may expect tfplan2md to be available in Homebrew core (`brew install tfplan2md` without tapping), leading to confusion.  
**Impact**: Low - User confusion, support burden.  
**Likelihood**: Medium - Many users are unfamiliar with custom taps.  
**Mitigation**: 
- Clearly document in README that tfplan2md requires adding the custom tap first
- Use consistent messaging: "Install via Homebrew (custom tap)"
- Consider submitting to Homebrew core in the future if project meets core criteria and usage grows

### Risk 6: Binary Compatibility Issues
**Description**: Pre-built binaries may not be compatible with all macOS or Linux versions (e.g., older macOS versions, musl-based Linux).  
**Impact**: Medium - Users on unsupported platforms cannot use Homebrew installation.  
**Likelihood**: Low - Feature 047 binaries are tested on supported platforms (macOS 10.15+, glibc-based Linux).  
**Mitigation**: 
- Document minimum supported versions in formula and README
- For macOS: Match Feature 047 minimum versions (macOS 10.15+ for x64, macOS 11+ for ARM64)
- For Linux: Specify glibc requirement (Ubuntu 24.04+, Debian 13+, RHEL 10+)
- Users on unsupported platforms can use Docker or build from source

## Open Questions

1. **Tap Repository Owner**: Should the tap repository be under the `oocx` GitHub organization, or should it be a personal repository?  
   → **Recommendation**: Use `oocx` organization for consistency with the main tfplan2md repository.

2. **Formula Naming**: Should the formula be named `tfplan2md` or `tfplan2md-cli`?  
   → **Recommendation**: Use `tfplan2md` (matches project name and binary name).

3. **Prerelease Handling**: Should prerelease versions (e.g., `v1.2.0-beta.1`) be installable via Homebrew, or only stable releases?  
   → **Recommendation**: Only update formula for stable releases. Prereleases can be installed manually via direct binary download.

4. **Multiple Formula Versions**: Should we maintain multiple formula versions (e.g., `tfplan2md@1.x` for major version pinning)?  
   → **Recommendation**: Start with a single formula for the latest version. Add versioned formulae later if users request major version pinning.

5. **Homebrew Tap Token**: Does the Maintainer have a GitHub Personal Access Token with write access to create the tap repository and configure `HOMEBREW_TAP_TOKEN` secret?  
   → **Action**: Maintainer to confirm or create token with `repo` scope for the tap repository.

6. **Testing Environment**: Do we have access to macOS ARM64 (Apple Silicon) and Linux x64 (WSL) environments for testing the formula?  
   → **Action**: Identify available testing environments or use GitHub Actions macOS runners for testing.

## References

- **Feature 047**: Multi-Platform Binary Distribution - [docs/features/047-multi-platform-binary-distribution/](../047-multi-platform-binary-distribution/)
- **Current Release Workflow**: [.github/workflows/release.yml](../../../.github/workflows/release.yml)
- **Homebrew Formula Cookbook**: https://docs.brew.sh/Formula-Cookbook
- **Homebrew Platform Detection**: https://docs.brew.sh/Formula-Cookbook#platform-specific-dependencies
- **Homebrew Tap Documentation**: https://docs.brew.sh/How-to-Create-and-Maintain-a-Tap
- **Example Tap Repository**: https://github.com/opentofu/homebrew-tap (reference for similar tools)

---

## Approval

**Requirements Engineer**: Specification complete and ready for Maintainer review.  
**Maintainer**: _[Pending approval]_  

Once approved, handoff to **Architect** agent for technical design and implementation planning.
