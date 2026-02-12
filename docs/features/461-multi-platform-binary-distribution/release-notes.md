# Pre-built Linux x64 Binary Distribution

Phase 1 implementation of ADR-008: Multi-Platform Binary Distribution. tfplan2md releases now include a pre-built, self-contained Linux x64 binary alongside the Docker image, enabling usage in air-gapped systems, CI/CD pipelines without container runtime, and local development environments.

## ✨ Features

### Pre-built Native Binary for Linux x64 (glibc)

Starting with the next release, tfplan2md will provide a downloadable, self-contained Linux x64 binary as a GitHub Release asset. This binary:

- **Requires no .NET runtime** - Built with .NET Native AOT for zero runtime dependencies
- **Single executable** - No installation required, just download and run
- **Verified integrity** - Includes SHA256 checksums for verification
- **glibc-based Linux** - Works on Ubuntu, Debian, RHEL, CentOS, Fedora, etc. (Note: Alpine/musl not supported in Phase 1)

**Use cases:**
- **Air-gapped/closed systems**: Organizations that cannot pull Docker images from public registries
- **CI/CD pipelines**: Build agents without container runtime or where Docker adds overhead
- **Local development**: Quick testing without Docker setup
- **Restricted environments**: Systems with strict security policies prohibiting containers

**Distribution format:**
- Archive: `tfplan2md_<version>_linux_x64.tar.gz` (flat structure, single binary)
- Checksums: `SHA256SUMS` (standard format compatible with `sha256sum -c`)

## 📚 Documentation

### Updated README.md Installation Section

The README now includes three installation options:

1. **Docker Image (Recommended)** - Primary distribution method, unchanged
2. **Pre-built Binary (Linux x64)** - NEW - Step-by-step download, verification, and usage instructions
3. **Build from Source** - Existing option, renamed for consistency

The new Pre-built Binary section includes:
- Download commands with version placeholders
- Checksum verification using `sha256sum -c`
- Archive extraction and execution examples
- System requirements (glibc-based Linux, no .NET runtime)
- Use cases and compatibility notes (Alpine/musl limitation)

### Updated ADR-008 Status

`docs/adr-008-multi-platform-binary-distribution.md` status changed from "Proposed" to "Accepted (Phase 1: Linux x64 implemented)" with implementation status tracking:

- **Phase 1 (Complete)**: Linux x64 (glibc) - Available starting next release
- **Phase 2 (Planned)**: linux-arm64, darwin-x64, darwin-arm64, win-x64
- **Phase 3 (Future)**: musl-based Linux (Alpine), additional Windows versions

### Updated Features Documentation

`docs/features.md` now includes a "Pre-built Binaries" section documenting:
- Binary distribution format and naming convention
- Release workflow integration
- Checksum generation and verification
- Validation steps performed before upload
- Phase 1 scope and future platforms

## 🔗 Commits

User-facing implementation commits:

- [`bd0fbf4`](https://github.com/oocx/tfplan2md/commit/bd0fbf44) feat: implement Linux x64 binary build and distribution workflow
- [`b63b79f`](https://github.com/oocx/tfplan2md/commit/b63b79f0) docs: document Linux x64 binary distribution feature

Internal testing and validation commits (not user-facing):

- [`624b0c5`](https://github.com/oocx/tfplan2md/commit/624b0c55) docs: mark tasks T001-T007 as complete
- [`c870d90`](https://github.com/oocx/tfplan2md/commit/c870d909) docs: complete Phase 2 testing documentation (T008-T013)
- [`7d4e143`](https://github.com/oocx/tfplan2md/commit/7d4e143f) docs: code review completed - ADR-008 Phase 1 approved for UAT

## ▶️ Getting started

Starting with the next release (v1.x.x+), download and use the pre-built binary:

```bash
# Set version (replace with actual release version)
VERSION="1.x.x"

# Download binary and checksums
wget https://github.com/oocx/tfplan2md/releases/download/v${VERSION}/tfplan2md_${VERSION}_linux_x64.tar.gz
wget https://github.com/oocx/tfplan2md/releases/download/v${VERSION}/SHA256SUMS

# Verify integrity
sha256sum -c SHA256SUMS --ignore-missing

# Extract
tar -xzf tfplan2md_${VERSION}_linux_x64.tar.gz

# Run (no installation required)
./tfplan2md examples/azure_cdn.json > plan.md
```

**System requirements:**
- Linux x64 with glibc (Ubuntu 22.04+, Debian 11+, RHEL 8+, etc.)
- No .NET runtime required
- Note: Alpine Linux (musl) not supported in Phase 1

**Docker remains the recommended distribution method** for containerized environments. Use the pre-built binary when containers are not available or practical.

## 📋 Implementation Details

**Technical approach:**
- GitHub Actions `release.yml` workflow extended with new `build-linux-x64-binary` job
- Job runs in parallel with Docker build after release creation (`needs: release`)
- Uses `dotnet publish` with Native AOT targeting linux-x64
- Flat tar.gz structure (single binary, no nested directories) following OpenTofu convention
- Standard SHA256SUMS format compatible with `sha256sum -c`
- Four validation steps before upload: executable check, smoke test, archive integrity, checksum verification
- Assets uploaded to GitHub Release using `softprops/action-gh-release@v2`

**Performance:**
- Binary build completes in ~1.8 minutes
- No impact on Docker build (parallel execution)
- Total workflow time: ~7 minutes (well under 10-minute target)

**Future phases:**
- **Phase 2**: Additional platforms (linux-arm64, darwin-x64, darwin-arm64, win-x64) with matrix-based builds
- **Phase 3**: musl-based Linux (Alpine), additional Windows runtime identifiers

**No breaking changes** - Docker image distribution unchanged, binary distribution is additive.
