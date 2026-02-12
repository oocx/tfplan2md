# Feature Specification: Multi-Platform Binary Distribution (Phase 1: Linux x64)

## Overview

This feature implements **Phase 1** of ADR-008: Multi-Platform Binary Distribution, focusing exclusively on Linux x64 (glibc) binary distribution. The goal is to provide pre-built native binaries as GitHub Release assets alongside the existing Docker image, enabling tfplan2md usage in environments without container runtime support.

**Phase 1 Scope:**
- Build linux-x64 native binary using .NET Native AOT
- Package as `tfplan2md_<version>_linux_x64.tar.gz` following OpenTofu naming convention
- Generate SHA256SUMS file for checksum verification
- Upload binary and checksums to GitHub Release as supplementary assets
- Keep existing Docker image build unchanged (remains primary distribution method)

**Out of Phase 1 Scope:**
- Other platforms (linux-arm64, darwin-x64, darwin-arm64, win-x64, musl variants) → Phase 2/3 per ADR-008
- NuGet package distribution
- Installation scripts or package manager integration
- Automated testing on linux-x64 target (deferred to Phase 2)

This addresses Issue #461 which requested OpenTofu-style pre-built binaries for closed/air-gapped systems, non-containerized environments, and local development use cases.

## User Goals

### Primary Users
1. **DevOps Engineers in Closed/Air-Gapped Systems**: Organizations with strict compliance requirements that cannot pull images from public Docker registries need downloadable binaries for internal use.

2. **Local Developers**: Developers who want to test tfplan2md quickly without Docker overhead or container runtime setup.

3. **CI/CD System Operators**: Teams running CI/CD pipelines in environments without container runtime (e.g., minimal build agents, legacy systems) need native executables.

### User Outcomes
- Download a single, self-contained linux-x64 binary from GitHub Releases
- Verify binary integrity using SHA256 checksums
- Execute tfplan2md directly without Docker or .NET runtime dependencies
- Use tfplan2md in restricted environments where container images cannot be pulled

## User Experience

### Downloading the Binary

Users navigate to the tfplan2md GitHub Releases page and find:
- `tfplan2md_<version>_linux_x64.tar.gz` (e.g., `tfplan2md_1.12.0_linux_x64.tar.gz`)
- `SHA256SUMS` file containing checksums for all release binaries

### Verification and Extraction

```bash
# Download binary and checksums
wget https://github.com/<org>/tfplan2md/releases/download/v1.12.0/tfplan2md_1.12.0_linux_x64.tar.gz
wget https://github.com/<org>/tfplan2md/releases/download/v1.12.0/SHA256SUMS

# Verify checksum
sha256sum -c SHA256SUMS --ignore-missing

# Extract
tar -xzf tfplan2md_1.12.0_linux_x64.tar.gz

# Execute
./tfplan2md --help
terraform plan -no-color -out=plan.tfplan
terraform show -json plan.tfplan > plan.json
./tfplan2md plan.json > plan.md
```

### Expected Binary Characteristics

- **Single executable file** named `tfplan2md` (no file extension)
- **Self-contained**: No external dependencies (glibc is system-provided)
- **Native AOT compiled**: Fast startup, no .NET runtime required
- **Executable permission**: Must be set via `chmod +x tfplan2md` after extraction
- **File size**: Approximately 15-30MB (uncompressed), typical for Native AOT glibc binaries

### Error Scenarios

1. **Checksum Mismatch**: If SHA256 verification fails, the user should re-download the binary or report a potential security issue.

2. **Incompatible Architecture**: If the user attempts to run the linux-x64 binary on a non-x64 system (e.g., ARM), they will see a `cannot execute binary file: Exec format error` message. The error is clear from the OS level.

3. **Missing glibc**: If the user's Linux distribution lacks glibc (e.g., Alpine Linux), they will see a `No such file or directory` error. This is expected behavior; musl-based Linux support is deferred to Phase 3.

## Requirements

### Functional Requirements

**FR1: Linux x64 Binary Build**
- The release workflow MUST build a native linux-x64 binary using `dotnet publish` with Native AOT enabled
- Runtime Identifier (RID): `linux-x64`
- Build configuration: Release
- Self-contained: true
- PublishAot: true
- Output: Single executable file named `tfplan2md`

**FR2: Packaging**
- The binary MUST be packaged as a tar.gz archive
- Archive name format: `tfplan2md_<version>_linux_x64.tar.gz`
- Archive MUST contain a single file: `tfplan2md` (the executable)
- The executable MUST have execute permissions preserved in the archive

**FR3: Checksum Generation**
- Generate a `SHA256SUMS` file containing SHA256 checksums for all release binaries
- Format: Standard `sha256sum` output format (checksum, two spaces, filename)
- Example line: `a3d5e8f7...  tfplan2md_1.12.0_linux_x64.tar.gz`

**FR4: GitHub Release Upload**
- Upload `tfplan2md_<version>_linux_x64.tar.gz` as a GitHub Release asset
- Upload `SHA256SUMS` as a GitHub Release asset
- Both files MUST be attached to the same release as the Docker image

**FR5: Docker Image Unchanged**
- Existing Docker image build process MUST remain unchanged
- Docker image remains the primary and recommended distribution method
- Binary distribution is supplementary

### Non-Functional Requirements

**NFR1: Build Performance**
- Binary build SHOULD complete within 10 minutes on GitHub Actions ubuntu-latest runner
- Binary build MAY run in parallel with Docker build to minimize total release time

**NFR2: Binary Size**
- Compressed archive size SHOULD be under 15MB
- Uncompressed binary size expected to be 15-30MB (acceptable for standalone use)

**NFR3: Compatibility**
- Binary MUST be compatible with glibc-based Linux distributions (Ubuntu, Debian, RHEL, CentOS, Fedora, etc.)
- Binary MUST work on x86_64 (x64) architecture
- Binary MUST NOT require .NET runtime installation

**NFR4: Release Integrity**
- All release builds MUST be reproducible from the tagged commit
- Checksums MUST be generated from the actual release artifacts, not intermediate builds

**NFR5: Workflow Reliability**
- Release workflow failure MUST NOT occur due to missing dependencies or toolchain issues
- If binary build fails, Docker build SHOULD still succeed (independent job)

## Technical Approach

### Workflow Modification Strategy

Modify `.github/workflows/release.yml` to add a new job `build-linux-x64-binary` that runs in parallel with the existing `docker` job:

```yaml
jobs:
  release:
    # ... existing release job (creates GitHub Release)

  build-linux-x64-binary:
    name: Build Linux x64 Binary
    runs-on: ubuntu-latest
    needs: release
    steps:
      # Build, package, checksum, upload
      
  docker:
    # ... existing docker job (unchanged)
```

### Build Command

Use `dotnet publish` with Native AOT targeting linux-x64:

```bash
dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishAot=true \
  -o artifacts/linux-x64
```

**Output**: `artifacts/linux-x64/tfplan2md` (single executable)

### Packaging Command

Create tar.gz archive preserving executable permissions:

```bash
cd artifacts/linux-x64
tar -czf ../../tfplan2md_${VERSION}_linux_x64.tar.gz tfplan2md
cd ../..
```

### Checksum Generation

Generate SHA256SUMS file for all binaries in the release:

```bash
sha256sum tfplan2md_${VERSION}_linux_x64.tar.gz > SHA256SUMS
```

**Note**: In Phase 2/3, this file will be appended with checksums for additional platforms.

### Asset Upload to GitHub Release

Use `softprops/action-gh-release@v2` action (already in use for release creation) to upload assets:

```yaml
- name: Upload Binary Assets
  uses: softprops/action-gh-release@v2
  with:
    tag_name: v${{ needs.release.outputs.version }}
    files: |
      tfplan2md_${{ needs.release.outputs.version }}_linux_x64.tar.gz
      SHA256SUMS
```

### Workflow Job Dependencies

```
release (create GitHub Release)
  ├─> build-linux-x64-binary (parallel)
  └─> docker (parallel)
```

Both `build-linux-x64-binary` and `docker` jobs depend on `release` to ensure the GitHub Release exists before uploading assets.

## Out of Scope

The following items are explicitly **excluded** from Phase 1:

1. **Other Platforms**: linux-arm64, darwin-x64, darwin-arm64, win-x64, musl variants (Phase 2/3 per ADR-008)
2. **Automated Testing**: Running tests on the linux-x64 binary in CI (deferred to Phase 2 when multiple platforms are available)
3. **Installation Scripts**: No `install.sh` or package manager integration
4. **Cross-platform Matrix Builds**: Single-platform build for Phase 1; matrix approach deferred to Phase 2
5. **NuGet Packages**: Not applicable for Native AOT use case
6. **Binary Signing**: Code signing or GPG signatures (future enhancement)
7. **Homebrew/APT/RPM Packages**: Package manager support (future enhancement)
8. **Documentation Updates**: Detailed download/usage documentation (handled by Technical Writer in next phase)

## Success Criteria

Phase 1 is successful when:

- [ ] Release workflow successfully builds linux-x64 binary without errors
- [ ] Binary is packaged as `tfplan2md_<version>_linux_x64.tar.gz`
- [ ] SHA256SUMS file is generated and contains checksum for the linux-x64 archive
- [ ] Both tar.gz and SHA256SUMS files are uploaded to GitHub Release as assets
- [ ] Binary can be downloaded, verified, extracted, and executed on Ubuntu 22.04+ without errors
- [ ] Binary produces correct tfplan2md output when given a valid Terraform plan JSON file
- [ ] Docker image build remains unchanged and continues to succeed
- [ ] Release workflow total time does not increase by more than 10 minutes (due to parallel execution)
- [ ] Checksum verification using `sha256sum -c SHA256SUMS --ignore-missing` passes

## Acceptance Scenarios

### Scenario 1: Download and Execute on Ubuntu 22.04

**Given** a new release v1.12.0 has been published  
**When** a user downloads `tfplan2md_1.12.0_linux_x64.tar.gz` and `SHA256SUMS` from GitHub Releases  
**And** the user verifies the checksum with `sha256sum -c SHA256SUMS --ignore-missing`  
**And** the user extracts the archive with `tar -xzf tfplan2md_1.12.0_linux_x64.tar.gz`  
**And** the user executes `./tfplan2md --help`  
**Then** the checksum verification passes  
**And** the help message is displayed without errors  

### Scenario 2: Process Terraform Plan JSON

**Given** the linux-x64 binary has been extracted to `./tfplan2md`  
**And** a valid Terraform plan JSON file exists at `plan.json`  
**When** the user executes `./tfplan2md plan.json > plan.md`  
**Then** a markdown file `plan.md` is created  
**And** the markdown file contains expected Terraform plan content  
**And** the binary exits with code 0  

### Scenario 3: Parallel Builds Complete Successfully

**Given** a new tag `v1.12.0` triggers the release workflow  
**When** the release workflow executes  
**Then** the `release` job completes first and creates the GitHub Release  
**And** the `build-linux-x64-binary` job completes successfully in parallel with `docker`  
**And** the `docker` job completes successfully  
**And** the total workflow time is not significantly increased (≤ 10 minutes added)  

### Scenario 4: Docker Image Unaffected

**Given** a new release v1.12.0 has been built  
**When** a user pulls the Docker image `<dockerhub-user>/tfplan2md:1.12.0`  
**Then** the Docker image is available and unchanged from previous release process  
**And** the Docker image size remains approximately 14.7MB  

## Dependencies

### Blockers
- None identified. All tooling and infrastructure are already in place:
  - .NET 10 SDK with Native AOT support (already used for Docker builds)
  - GitHub Actions ubuntu-latest runner (supports linux-x64 builds)
  - GitHub Release creation workflow (already implemented)

### Prerequisites
- ADR-008 has been approved (Status: Proposed → needs Maintainer approval)
- Native AOT compilation is already functional for Docker builds (proven working)
- Release workflow structure is stable and tested

### External Dependencies
- `dotnet` SDK 10.x on GitHub Actions ubuntu-latest runner
- `softprops/action-gh-release@v2` GitHub Action for asset upload
- glibc on target user systems (system-provided, not a build dependency)

## Risks and Mitigations

### Risk 1: Build Time Increase
**Description**: Native AOT compilation may add significant time to the release workflow.  
**Impact**: Medium - Slower releases could delay deployments.  
**Likelihood**: Low - Native AOT already used for Docker (14.7MB image builds quickly).  
**Mitigation**: Run binary build in parallel with Docker build. If build time exceeds 10 minutes, investigate caching strategies.

### Risk 2: Binary Size Larger Than Expected
**Description**: Glibc-based binaries may be larger than the 14.7MB Alpine Docker image.  
**Impact**: Low - Download time slightly increased, but acceptable for binary distribution.  
**Likelihood**: Medium - Glibc binaries typically 15-30MB.  
**Mitigation**: Accept size increase as documented in ADR-008. Users who need minimal size should continue using Docker image.

### Risk 3: Checksum Generation Error
**Description**: Checksums could be generated from intermediate files instead of final release artifacts.  
**Impact**: High - Invalid checksums compromise security and user trust.  
**Likelihood**: Low - Workflow will generate checksums immediately after packaging.  
**Mitigation**: Ensure SHA256SUMS is generated in the same job step after packaging, before upload. Include verification step in workflow.

### Risk 4: Incomplete Multi-Platform Support Expectations
**Description**: Users may expect all platforms in Phase 1 despite clear documentation of linux-x64 only.  
**Impact**: Low - User confusion, support requests.  
**Likelihood**: Medium - Common with phased rollouts.  
**Mitigation**: Clearly label Phase 1 releases with "Linux x64 only" in release notes. Provide timeline for Phase 2/3 in ADR-008.

### Risk 5: Workflow Job Dependency Issues
**Description**: If `build-linux-x64-binary` runs before `release` job completes, asset upload may fail.  
**Impact**: Medium - Release would be incomplete (missing binary assets).  
**Likelihood**: Low - Proper job dependency configuration (`needs: release`) prevents this.  
**Mitigation**: Use `needs: release` in job definition to ensure proper sequencing. Test workflow in staging environment.

### Risk 6: Archive Permission Loss
**Description**: Executable permissions may not be preserved when creating tar.gz archive.  
**Impact**: Medium - Users would need to manually `chmod +x` after extraction.  
**Likelihood**: Low - `tar` preserves permissions by default.  
**Mitigation**: Verify in workflow that `tar -czf` preserves execute bit. Include permission verification step after packaging.

## Open Questions

1. **ADR-008 Approval Status**: ADR-008 is currently marked "Proposed". Does it need formal approval before implementation, or can Phase 1 proceed?  
   → **Action**: Maintainer to confirm approval status or approve ADR-008.

2. **Release Notes Template**: Should the release notes template be updated to highlight binary availability, or is this a Technical Writer task?  
   → **Action**: Confirm with Maintainer. If in scope, Requirements Engineer can flag as requirement.

3. **Versionize Interaction**: Does adding binary build to release workflow require any changes to Versionize configuration or CI workflow?  
   → **Action**: Architect/Developer to verify during implementation.

4. **Binary Naming Convention**: Should the binary inside the archive be named `tfplan2md` or `tfplan2md-linux-x64`?  
   → **Decision**: Use `tfplan2md` (no suffix) to match OpenTofu convention and simplify user experience.

## References

- **Issue #461**: [Feature]: Publish pre-built binaries for multiple architectures  
- **ADR-008**: [docs/adr-008-multi-platform-binary-distribution.md](../../adr-008-multi-platform-binary-distribution.md)  
- **Current Release Workflow**: [.github/workflows/release.yml](../../../.github/workflows/release.yml)  
- **OpenTofu Releases**: https://github.com/opentofu/opentofu/releases (reference for naming convention)  
- **.NET Native AOT Documentation**: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/  
- **.NET Runtime Identifier Catalog**: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog

---

## Approval

**Requirements Engineer**: Specification complete and ready for Maintainer review.  
**Maintainer**: _[Pending approval]_  

Once approved, handoff to **Architect** agent for technical design and ADR updates.
