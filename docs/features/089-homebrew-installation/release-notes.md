# Homebrew Installation Support

## Summary

tfplan2md can now be installed via Homebrew on macOS and Linux (including WSL), providing a convenient package manager experience with automatic updates.

## What's New

### Homebrew Installation

You can now install tfplan2md using Homebrew:

```bash
brew tap oocx/tfplan2md
brew install tfplan2md
```

This provides:
- **Easy installation** - No need to manually download binaries or manage PATH
- **Automatic updates** - Get notified of new versions with `brew outdated`
- **Simple upgrades** - Update to the latest version with `brew upgrade tfplan2md`
- **Clean uninstallation** - Remove with `brew uninstall tfplan2md`

**Supported Platforms:**
- macOS x64 (Intel)
- macOS ARM64 (Apple Silicon - M1/M2/M3)
- Linux x64 (including WSL)

### Platform Support Improvements

As part of this release, we've made the following platform support changes:

#### ✅ Fixed: macOS Builds
- **macOS x64 (Intel)** - Build failures fixed with Xcode Command Line Tools installation
- **macOS ARM64 (Apple Silicon)** - Now building successfully for M1/M2/M3 Macs
- Both architectures now have reliable release binaries available

#### ❌ Removed: Windows ARM64
- **Windows ARM64** builds have been removed from the release pipeline
- **Reason**: GitHub Actions does not provide native ARM64 Windows runners, and cross-compilation is complex and fragile
- **Impact**: Very low (<1% of Windows market share)
- **Alternatives**: 
  - Windows ARM64 users can use the x64 binary (runs via emulation)
  - Docker image is also available for Windows (WSL2)
- **Note**: Windows ARM64 support may be re-added in the future if GitHub Actions provides native ARM64 Windows runners

## Installation

### Homebrew (macOS and Linux) - NEW!

```bash
brew tap oocx/tfplan2md
brew install tfplan2md
```

### Docker (All Platforms)

```bash
docker pull oocx/tfplan2md:latest
```

### Pre-built Binaries

Download from [GitHub Releases](https://github.com/oocx/tfplan2md/releases):

**Available Platforms:**
- Linux x64 and ARM64
- Windows x64
- macOS x64 (Intel) and ARM64 (Apple Silicon)

See README.md for detailed installation instructions.

## Upgrade Notes

### For Existing Users

**No action required.** All existing installation methods continue to work:
- Docker image
- Direct binary downloads
- Building from source

Homebrew is available as an **additional** installation option for macOS and Linux users.

### For Windows ARM64 Users

If you previously used Windows ARM64 binaries (or were planning to):
- **Option 1**: Use the Windows x64 binary (runs via ARM64 emulation on Windows 11)
- **Option 2**: Use the Docker image with WSL2

## Breaking Changes

**None.** Existing installation methods and functionality remain unchanged.

## Platform Compatibility

| Platform | Architecture | Homebrew | Docker | Binary | Status |
|----------|-------------|----------|--------|--------|--------|
| **macOS** | x64 (Intel) | ✅ | ✅ | ✅ | Supported |
| **macOS** | ARM64 (M1/M2/M3) | ✅ | ✅ | ✅ | Supported |
| **Linux** | x64 | ✅ | ✅ | ✅ | Supported |
| **Linux** | ARM64 | ❌ | ✅ | ✅ | Supported (no Homebrew) |
| **Windows** | x64 | ❌ (WSL only) | ✅ | ✅ | Supported |
| **Windows** | ARM64 | ❌ | ✅ | ❌ | Use x64 binary or Docker |

**Legend:**
- ✅ Supported
- ❌ Not available
- WSL: Windows Subsystem for Linux

## Technical Details

### Homebrew Formula

The Homebrew formula automatically detects your platform (macOS Intel, macOS Apple Silicon, or Linux x64) and downloads the appropriate pre-built binary from GitHub Releases.

**Formula Location**: https://github.com/oocx/homebrew-tfplan2md

**Automated Updates**: The Homebrew formula is automatically updated when new stable releases are published.

### macOS Build Fixes

macOS builds were previously failing because GitHub Actions macOS runners do not have Xcode Command Line Tools pre-installed by default. The release workflow now:
1. Installs Xcode Command Line Tools before building
2. Validates the installation
3. Compiles native binaries using the Xcode toolchain

This adds approximately 2-3 minutes to macOS build times but ensures reliable builds.

## Related Features

- **Feature 047**: Multi-Platform Binary Distribution - Provides the pre-built binaries that Homebrew downloads
- **Feature 089**: Homebrew Installation Support (this feature)

## Feedback

If you encounter any issues with Homebrew installation or have suggestions for improvement:
- Open an issue: https://github.com/oocx/tfplan2md/issues
- For Homebrew-specific issues, check the tap repository: https://github.com/oocx/homebrew-tfplan2md

## Next Steps

After installing via Homebrew, you can:
1. Verify installation: `tfplan2md --version`
2. See usage help: `tfplan2md --help`
3. Convert your first plan: `terraform show -json plan.tfplan | tfplan2md > report.md`

For complete usage documentation, see the [README](https://github.com/oocx/tfplan2md#readme) or visit the [official website](https://oocx.github.io/tfplan2md/).
