# Work Protocol — Fix 123: Linux ARM64 Missing Binary

## Developer Agent Log

**Date:** 2025-07-11
**Branch:** fix/123-linux-arm64-missing-binary

### Summary

Implemented all three fixes specified in the analysis document to restore linux-arm64 and
linux-musl-arm64 binary builds in the release workflow.

### Root Cause Recap

Commit `ae4e33c` pinned Docker image digests to AMD64-only manifest digests. When the
`ubuntu-24.04-arm` (ARM64) runners tried to pull these images, they got AMD64 images that
fail with `exec format error` on ARM64 hardware.

### Changes Made

**File:** `.github/workflows/release.yml`

1. **Fix 1 — `linux-arm64` container removed:**
   - Changed `container:` for the `linux-arm64` matrix entry from the AMD64-pinned
     `mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:6d7f69bc…` to `''` (empty).
   - The `ubuntu-24.04-arm` runner is already Ubuntu 24.04 Noble with the same glibc as the
     container image, so binary compatibility is preserved.
   - The existing `Setup .NET` step (gated with `!startsWith(matrix.platform, 'linux-musl-')`)
     already handles .NET installation for this job.
   - Also updated `Install NativeAOT linker prerequisite (Linux)` step to use `sudo` when
     not running as root (mirrors the existing UPX install pattern), since the ARM64 runner
     is non-root unlike the container.

2. **Fix 2 — `--platform` flags for musl Docker builds:**
   - Added `docker_platform: linux/amd64` to the `linux-musl-x64` matrix entry.
   - Added `docker_platform: linux/arm64` to the `linux-musl-arm64` matrix entry.
   - Added `--platform ${{ matrix.docker_platform }}` to the `docker run` command in the
     `Build Binary (musl via Docker)` step.
   - Added `--platform ${{ matrix.docker_platform }}` to the `docker run` command in the
     `Validate Artifacts (Unix)` smoke-test step.

3. **Fix 3 — Automated detection (binary presence validation):**
   - Added a new `Verify all platform binaries are present` step in the
     `consolidate-checksums` job, inserted after `Consolidate SHA256 checksums` and before
     `Upload consolidated checksums`.
   - The step asserts that all six expected platforms (`linux-x64`, `linux-arm64`,
     `linux-musl-x64`, `linux-musl-arm64`, `macos-arm64`, `windows-x64`) appear in the
     `SHA256SUMS` file, failing the workflow with an actionable error message if any are
     absent.

### Artifacts Produced

- `.github/workflows/release.yml` — updated

### Problems Encountered

None. The analysis document was thorough and the fixes were straightforward.

### Status

Done. Ready for Code Reviewer.
