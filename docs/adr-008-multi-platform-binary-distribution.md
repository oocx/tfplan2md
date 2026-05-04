# ADR-008: Multi-Platform Binary Distribution

## Status

Accepted (All phases implemented)

## Implementation Status

**All Phases Completed.**

All six platforms are built and published as release assets on every GitHub Release:

| Platform | Archive | Notes |
|----------|---------|-------|
| linux-x64 | `tfplan2md_<version>_linux-x64.tar.gz` | Ubuntu 24.04+, glibc 2.39 |
| linux-arm64 | `tfplan2md_<version>_linux-arm64.tar.gz` | Ubuntu 24.04+ ARM64, glibc 2.39 |
| linux-musl-x64 | `tfplan2md_<version>_linux-musl-x64.tar.gz` | Alpine Linux x64 (musl) |
| linux-musl-arm64 | `tfplan2md_<version>_linux-musl-arm64.tar.gz` | Alpine Linux ARM64 (musl) |
| macos-arm64 | `tfplan2md_<version>_macos-arm64.tar.gz` | macOS 11+ Apple Silicon |
| windows-x64 | `tfplan2md_<version>_windows-x64.zip` | Windows 10+ x64 |

A `SHA256SUMS` file is also included for checksum verification.

> **Note:** Releases v1.42.1 and v1.43.0 are affected by a regression (fix #123) where
> AMD64-only Docker image digest pins caused the `linux-arm64` and `linux-musl-arm64`
> builds to fail. The fix removes the container override for the `linux-arm64` job and
> adds `--platform linux/arm64` to the musl Docker build steps. Releases from v1.44.0
> onward include all six platform binaries.

## Context

Currently, tfplan2md is distributed exclusively as a Docker container image (14.7MB Alpine-based Native AOT binary). While this works well for containerized environments, it presents challenges for:

1. **Closed/air-gapped systems**: Organizations with high compliance requirements cannot pull images from public registries
2. **Non-containerized environments**: Systems without Docker/container runtime require alternative distribution
3. **Local development**: Developers need native binaries for quick testing without Docker overhead
4. **CI/CD flexibility**: Not all CI/CD systems have container runtime available

A feature request (#461) specifically asked for OpenTofu-style pre-built binaries across multiple platforms and architectures.

## Decision

We will provide pre-built native binaries for multiple platforms alongside the existing Docker image, using .NET 10 Native AOT compilation and GitHub Actions matrix builds.

### Supported Platforms (Phased Approach)

**Phase 1: Essential Linux Support**
- linux-x64 (glibc) - Standard Linux distributions
- linux-arm64 (glibc) - ARM-based cloud instances

**Phase 2: Developer Platforms**
- darwin-arm64 - macOS Apple Silicon
- darwin-x64 - macOS Intel
- win-x64 - Windows 10/11/Server

**Phase 3: Complete Coverage**
- linux-musl-x64 - Alpine standalone
- linux-musl-arm64 - ARM Alpine
- win-arm64 - Windows ARM

### Distribution Model

1. **Primary**: Docker image (unchanged, remains recommended distribution method)
2. **Supplementary**: Pre-built binaries as GitHub Release assets
3. **Naming Convention**: Follow OpenTofu pattern: `tfplan2md_<version>_<os>_<arch>.<ext>`
4. **Checksums**: Include SHA256SUMS file for verification

### Implementation Strategy

1. **GitHub Actions Matrix Build**
   - Parallel builds on native runners (ubuntu, macos, windows)
   - Each runner builds for its native OS with platform-specific toolchain
   - No cross-OS compilation required (only cross-arch within same OS)

2. **Build Process**
   ```bash
   dotnet publish src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj \
     -c Release \
     -r <runtime-identifier> \
     --self-contained true \
     -p:PublishAot=true \
     -o artifacts/<rid>
   ```

3. **Packaging**
   - Linux/macOS: tar.gz archives
   - Windows: zip archives
   - Include single executable binary (no runtime dependencies)

4. **Workflow Integration**
   - Modify `.github/workflows/release.yml`
   - Add matrix build job running parallel to Docker build
   - Upload all artifacts to GitHub Release
   - Generate and attach SHA256SUMS

## Consequences

### Positive

1. **Broader Accessibility**: Enables tfplan2md usage in restricted environments
2. **Simplified Local Development**: Direct binary execution without Docker
3. **CI/CD Flexibility**: Works in any environment, containerized or not
4. **Compliance Friendly**: Easier to audit and approve single binaries vs container images
5. **Low Maintenance**: Native AOT already configured, workflow-only changes
6. **Parallel Execution**: Matrix builds don't significantly increase release time

### Negative

1. **Increased Build Time**: ~5-10 minutes per platform (mitigated by parallel execution)
2. **Larger Binary Sizes**: glibc binaries ~15-30MB vs 14.7MB Docker image (acceptable for standalone use)
3. **Additional Release Assets**: More files to manage per release
4. **Platform-Specific Testing**: Each platform should ideally be tested (though Native AOT provides high confidence)

### Neutral

1. **Documentation Updates**: Need to document binary download and usage
2. **Multiple Distribution Methods**: Users must choose between Docker and direct binary
3. **Checksum Verification**: Users responsible for verifying checksums (best practice anyway)

## Alternatives Considered

### Alternative 1: Docker-only with Volume Mounting
**Decision**: Rejected - Doesn't address core issue of environments without container runtime

### Alternative 2: NuGet Package Distribution
**Decision**: Rejected - Requires .NET runtime on target system, defeats purpose of Native AOT

### Alternative 3: Cross-compiled from Single Runner
**Decision**: Rejected - .NET Native AOT doesn't officially support cross-OS compilation

### Alternative 4: Release All Platforms Immediately
**Decision**: Rejected - Phased approach allows validation and user feedback

## Notes

- .NET 10 Native AOT provides excellent cross-platform support with native runners
- OpenTofu's naming convention is well-established in Terraform ecosystem
- GitHub Actions provides free native runners for all target platforms
- Binary size increase is acceptable given use case (no container runtime available)
- Docker remains primary/recommended distribution method
- Implementation can be done incrementally (Phase 1 → Phase 2 → Phase 3)

## References

- Issue #461: [Feature]: Publish pre-built binaries for multiple architectures
- OpenTofu releases: https://github.com/opentofu/opentofu/releases
- .NET Native AOT documentation: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- .NET Runtime Identifier Catalog: https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
- Investigation document: `.tmp/multi-platform-binary-investigation.md`
