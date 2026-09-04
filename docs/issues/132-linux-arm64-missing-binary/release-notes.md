# Restore Linux ARM64 Binary Downloads

Bug fix release restoring `linux-arm64` and `linux-musl-arm64` pre-built binaries that
went missing from releases v1.42.1 and v1.43.0.

## 🐛 Bug fixes

- **`linux-arm64` binary missing from releases (v1.42.1+):** Commit `ae4e33c` pinned
  the .NET SDK Docker image to an AMD64-only manifest digest. When the `ubuntu-24.04-arm`
  ARM64 runner pulled this image it received an AMD64 binary that immediately failed with
  `exec format error`, silently dropping the platform from the release archive. Fixed by
  removing the container override entirely — the `ubuntu-24.04-arm` runner already has
  Ubuntu 24.04 Noble with glibc installed, so the binary compiles and runs natively.

- **`linux-musl-arm64` binary missing from releases (v1.42.1+):** The Alpine-based musl
  Docker builds were given `--platform linux/arm64` flags but the image digests were still
  pinned to single-platform AMD64 manifests. Docker's `--platform` flag can only select
  from a multi-arch manifest list, not transform a single-arch digest. Fixed by updating
  both `dotnet/sdk:10.0-alpine` and `dotnet/runtime-deps:10.0-alpine` references to their
  multi-arch manifest list digests (`sha256:0191ff38...` and `sha256:4f08c162...`
  respectively), which include amd64, arm/v7, and arm64 images.

- **Release workflow now validates binary presence before publishing:** A new
  `Verify all platform binaries are present` step checks that all six expected platform
  binaries (`linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`,
  `macos-arm64`, `windows-x64`) appear in `SHA256SUMS` before the release is published,
  preventing silent platform omissions in future releases.

## 📚 Documentation

- `README.md`: Removed stale "Available starting with the next release" notice; added
  `linux-musl-x64` and `linux-musl-arm64` to the Available Platforms table (they were
  being built but not listed); updated the PLATFORM example comment to show all six
  choices; corrected Linux requirements note to mention the dedicated musl binaries.

## 🔗 Commits

- [`bca278a`](https://github.com/oocx/tfplan2md/commit/bca278a31479dfa937927fd2c3928556469a9b0c) fix: restore linux arm64 binary builds by fixing Docker platform pinning
- [`e723e16`](https://github.com/oocx/tfplan2md/commit/e723e16feaa2ec13eed6e604bae9150c3e8eded6) fix: use multi-arch manifest list digests for musl Alpine Docker images
- [`7a6f998`](https://github.com/oocx/tfplan2md/commit/7a6f9988c18d73120acb02d3723e2b13b6cbb746) docs: update documentation for linux arm64 build fix
