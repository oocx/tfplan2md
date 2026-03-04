# ADR-011: UPX Binary Compression for Standalone Releases

## Status

Accepted

## Context

tfplan2md distributes pre-built NativeAOT standalone binaries as GitHub Release assets for multiple platforms. These binaries are produced by the release workflow and are also consumed by the Homebrew tap for macOS and Linux users.

NativeAOT binaries are self-contained (they include the .NET runtime) and are typically 15–30 MB per platform. Applying UPX compression can reduce binary sizes by approximately 50%, improving download speeds and reducing storage requirements.

UPX (Ultimate Packer for eXecutables) is a portable, open-source executable compressor that works by adding a decompressor stub to the binary. At runtime, the binary decompresses itself into memory before executing. This adds a small startup overhead (~50–100 ms), which is negligible for a CLI tool.

The question is which platforms can safely use UPX.

## Platform Compatibility Assessment

| Platform | Format | UPX Support | Applied | Reason |
|---|---|---|---|---|
| `linux-x64` | ELF x86_64 | ✅ Full | ✅ Yes | UPX 4.x has first-class ELF x86_64 support |
| `linux-arm64` | ELF ARM64 | ✅ Full | ✅ Yes | UPX 4.x supports ELF AArch64 |
| `linux-musl-x64` | ELF x86_64 (musl) | ✅ Full | ✅ Yes | Same ELF format; UPX is libc-agnostic |
| `linux-musl-arm64` | ELF ARM64 (musl) | ✅ Full | ✅ Yes | Same ELF format; UPX is libc-agnostic |
| `windows-x64` | PE64 (.exe) | ✅ Full | ✅ Yes | UPX has long-standing PE64 support |
| `macos-arm64` | Mach-O ARM64 | ❌ Excluded | ❌ No | See macOS section below |

### macOS Exclusion

UPX is explicitly **excluded** for macOS (`macos-arm64`) for the following reasons:

1. **Code signing invalidation**: UPX restructures the Mach-O binary, which invalidates any existing code signature. Modern macOS (10.15+) requires binaries from the internet to be signed.
2. **Gatekeeper quarantine**: Unsigned or tampered binaries downloaded from the internet are blocked by Gatekeeper. Users would see an "Apple cannot check it for malicious software" error or similar.
3. **Homebrew implications**: The Homebrew formula downloads and installs the macOS binary. A broken binary would silently fail or show Gatekeeper errors to all Homebrew users.
4. **Re-signing not feasible**: Resolving these issues requires an Apple Developer account and notarization, which is not currently part of the release pipeline.

The macOS binary is distributed without UPX compression. Its size (~20–30 MB) is acceptable for a one-time Homebrew install.

## Decision

Apply UPX `--best` compression to all platforms **except** `macos-arm64`.

- UPX is installed at release time via package manager (`apt-get install upx-ucl` on Linux, `choco install upx` on Windows)
- Compression runs after the NativeAOT build, before packaging and checksum generation
- The existing smoke test (`--help`) in the Validate Artifacts step validates compressed binaries

## Consequences

### Positive

- **Smaller downloads**: ~50% reduction in binary size for Linux and Windows platforms
- **Faster CI**: Artifact upload/download in GitHub Actions is faster
- **Better user experience**: Faster `curl` downloads from GitHub Releases
- **No runtime dependencies**: UPX decompressor is embedded in the binary; no UPX required on the user's machine

### Negative

- **Startup overhead**: ~50–100 ms additional startup time due to in-memory decompression (negligible for a CLI tool)
- **macOS excluded**: macOS users receive a larger uncompressed binary (unavoidable without code signing infrastructure)
- **Future signing**: If code signing is added for macOS in the future, UPX must not be applied to macOS even if UPX gains full Mach-O support, unless signing happens after UPX compression

### Neutral

- **No user-visible behavioral changes**: The compressed binary behaves identically to the uncompressed version
- **Checksums reflect compressed binary**: The SHA256SUMS file contains checksums of the compressed archives

## Alternatives Considered

### Alternative 1: Apply UPX to All Platforms Including macOS

**Rejected**: macOS Gatekeeper blocks unsigned/modified binaries, which would break the Homebrew formula and all direct downloads for macOS users. This is a critical distribution failure.

### Alternative 2: No Compression

**Rejected**: Binary sizes are 15–30 MB per platform. Compression to ~7–15 MB provides meaningful improvement in download time, especially for users in regions with slower internet connections or corporate proxies with bandwidth limits.

### Alternative 3: Use a Different Compression Tool

Not evaluated. UPX is the de facto standard for native binary compression, is widely trusted in the open-source ecosystem, and supports all our target ELF and PE64 formats.

## References

- UPX homepage: https://upx.github.io/
- UPX supported formats: https://upx.github.io/#features
- macOS code signing requirements: https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution
