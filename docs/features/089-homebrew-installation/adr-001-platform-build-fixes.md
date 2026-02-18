# ADR-001: Platform Build Fixes for Multi-Platform Binary Distribution

## Status

Proposed

## Context

Feature 047 (Multi-Platform Binary Distribution) implemented a GitHub Actions matrix build to create Native AOT binaries for multiple platforms. However, the following platforms are currently failing:

- **linux-arm64**: Cross-compilation failure (building ARM64 binary on x64 runner)
- **macos-x64**: Missing Xcode toolchain on macOS-13 Intel runner
- **macos-arm64**: Missing Xcode toolchain on macOS-14 Apple Silicon runner  
- **windows-arm64**: Cross-compilation failure (building ARM64 binary on x64 runner)

Feature 089 (Homebrew Installation) requires **at least macOS platforms** to work correctly. Homebrew is primarily a macOS package manager, and the formula must support both Intel (x64) and Apple Silicon (ARM64) architectures.

### Root Cause Analysis

**1. macOS Platform Failures (macos-x64, macos-arm64)**

The release workflow uses native runners:
- `macos-13` for Intel (x64) 
- `macos-14` for Apple Silicon (ARM64)

**Problem**: The workflow sets `needs_clang: false` for macOS platforms (line 253, 259 in release.yml), assuming that Xcode tools are pre-installed on GitHub-hosted macOS runners.

**Reality**: GitHub Actions macOS runners do **not** have Xcode Command Line Tools pre-installed. .NET 10 NativeAOT requires:
- Xcode clang compiler for native code generation
- macOS SDK headers for system library linking
- Apple linker (ld) for creating the final executable

Without these tools, the `dotnet publish -p:PublishAot=true` command fails during the native compilation phase.

**2. Linux ARM64 Failure (linux-arm64)**

The release workflow uses `ubuntu-latest` (x64 runner) with a container (`mcr.microsoft.com/dotnet/sdk:10.0-noble`) and attempts to build for RID `linux-arm64`.

**Problem**: This is **cross-architecture compilation** (x64 host → ARM64 target). The workflow installs `clang` (line 275) but does not install ARM64-specific cross-compilation toolchain.

**Reality**: .NET NativeAOT requires:
- ARM64 cross-compiler: `gcc-aarch64-linux-gnu`
- ARM64 binutils: `binutils-aarch64-linux-gnu`
- Proper linker configuration via environment variables

Without these, the clang compiler cannot generate ARM64 code or link ARM64 binaries.

**3. Windows ARM64 Failure (windows-arm64)**

The release workflow uses `windows-latest` (x64 runner) and attempts to build for RID `win-arm64`.

**Problem**: This is **cross-architecture compilation** (x64 host → ARM64 target) on Windows. GitHub Actions does not provide native ARM64 Windows runners in the free tier.

**Reality**: Cross-compiling ARM64 on Windows x64 requires:
- Visual Studio ARM64 build tools
- Windows ARM64 SDK
- Complex configuration for .NET NativeAOT ARM64 cross-compilation

This setup is fragile and not well-documented. Microsoft's official guidance is to use native ARM64 hardware or skip the platform.

## Options Considered

### Option 1: Fix macOS Builds with Xcode Installation (RECOMMENDED for Homebrew)

**Description**: Add Xcode Command Line Tools installation step for macOS platforms before building.

**Implementation**:
```yaml
- name: Install Xcode Command Line Tools (macOS)
  if: startsWith(matrix.platform, 'macos-')
  run: |
    # Xcode CLT is required for NativeAOT compilation on macOS
    sudo xcode-select --install || true
    # Wait for installation to complete
    until xcode-select -p &>/dev/null; do sleep 5; done
    xcode-select -p
```

**Pros**:
- ✅ **Simple**: Single command fixes both macOS platforms
- ✅ **Native builds**: No cross-compilation complexity (macos-13 builds x64, macos-14 builds ARM64)
- ✅ **Well-supported**: .NET NativeAOT officially supports macOS with Xcode tools
- ✅ **Unblocks Homebrew**: Homebrew requires macOS binaries as a minimum viable feature
- ✅ **Fast**: Xcode CLT installation adds ~2-3 minutes per macOS job

**Cons**:
- ⚠️ **Additional build time**: ~2-3 minutes for Xcode installation
- ⚠️ **Runner dependency**: Relies on `xcode-select --install` being available

**Trade-offs**: Minor build time increase is acceptable given that it enables Homebrew support (primary feature goal).

---

### Option 2: Fix Linux ARM64 with Cross-Compilation Toolchain

**Description**: Install ARM64 cross-compilation toolchain on Linux x64 runner.

**Implementation**:
```yaml
- name: Install NativeAOT ARM64 cross-compilation prerequisites (Linux)
  if: matrix.platform == 'linux-arm64'
  run: |
    apt-get update
    apt-get install -y --no-install-recommends \
      clang \
      gcc-aarch64-linux-gnu \
      binutils-aarch64-linux-gnu
    # Set linker for .NET NativeAOT
    echo "CXX=clang" >> $GITHUB_ENV
    echo "CC=clang" >> $GITHUB_ENV
```

**Pros**:
- ✅ **Enables linux-arm64**: Supports ARM-based cloud instances (AWS Graviton, etc.)
- ✅ **No new runners**: Uses existing ubuntu-latest + container setup
- ✅ **Documented pattern**: Cross-compilation toolchain is well-known

**Cons**:
- ⚠️ **Complexity**: Requires environment variable configuration
- ⚠️ **Testing difficulty**: Cannot easily test ARM64 binary on x64 runner (need emulation or separate testing)
- ⚠️ **Not critical for Homebrew**: Linux ARM64 is not widely used with Homebrew

**Trade-offs**: Valuable for completeness but not required for Homebrew feature (Phase 2 priority).

---

### Option 3: Skip Windows ARM64 (RECOMMENDED)

**Description**: Remove `windows-arm64` from the build matrix.

**Rationale**:
- Windows ARM64 represents <1% of Windows market share
- GitHub Actions does not provide native ARM64 Windows runners
- Cross-compilation is complex and fragile
- No user demand for Windows ARM64 binaries

**Implementation**:
```yaml
# Remove this matrix entry:
# - platform: windows-arm64
#   os: windows-latest
#   rid: win-arm64
#   ...
```

**Pros**:
- ✅ **Simplifies workflow**: Removes fragile cross-compilation setup
- ✅ **No impact on users**: Negligible user base for Windows ARM64
- ✅ **Faster releases**: One less build job to wait for

**Cons**:
- ❌ **Incomplete platform coverage**: Does not support Windows ARM64 devices

**Trade-offs**: Acceptable trade-off given low usage and high complexity. Can be re-added later if native runners become available.

---

### Option 4: Use Native ARM64 Runners for All ARM64 Builds

**Description**: Use native ARM64 runners for linux-arm64 and windows-arm64.

**Pros**:
- ✅ **No cross-compilation**: Native builds are simpler and more reliable
- ✅ **Better testing**: Can run built binaries on the same runner

**Cons**:
- ❌ **Not available**: GitHub Actions does not provide ARM64 runners for Linux or Windows in the free tier
- ❌ **Self-hosted required**: Requires setting up and maintaining self-hosted ARM64 runners
- ❌ **Operational burden**: Adds infrastructure complexity

**Decision**: Rejected for now. Can be reconsidered if GitHub adds ARM64 runners or if demand justifies self-hosted setup.

---

## Decision

**Immediate Implementation (Required for Homebrew Feature 089):**

1. **Fix macOS builds**: Implement Option 1 (Install Xcode Command Line Tools) for both `macos-x64` and `macos-arm64` platforms
2. **Skip Windows ARM64**: Implement Option 3 (Remove from matrix) to eliminate fragile cross-compilation

**Phase 2 (Optional Enhancement):**

3. **Fix Linux ARM64**: Implement Option 2 (ARM64 cross-compilation toolchain) as a follow-up improvement

## Rationale

**Why prioritize macOS over Linux ARM64?**

- **Homebrew requirement**: Feature 089 specifically requires macOS binaries. Homebrew is primarily a macOS package manager (though it supports Linux).
- **User demand**: macOS users (both Intel and Apple Silicon) represent a larger portion of the developer community than Linux ARM64 users.
- **Simplicity**: macOS fix is straightforward (install Xcode tools). Linux ARM64 requires cross-compilation setup.

**Why skip Windows ARM64?**

- **Low usage**: Windows ARM64 devices are rare in the developer community (<1% market share).
- **No native runners**: GitHub Actions does not provide Windows ARM64 runners, forcing complex cross-compilation.
- **Not critical**: Windows users primarily use x64 (supported). Windows ARM64 can be added later if demand emerges.

## Consequences

### Positive

- ✅ **Unblocks Homebrew feature**: macOS binaries will be available for Formula distribution
- ✅ **Improved reliability**: Native builds (with proper tooling) are more reliable than cross-compilation
- ✅ **Faster releases**: Removing windows-arm64 reduces build matrix complexity
- ✅ **Better user experience**: macOS users get working binaries via Homebrew
- ✅ **Incremental approach**: Linux ARM64 can be added later without blocking current feature

### Negative

- ⚠️ **Slightly longer macOS builds**: ~2-3 minutes added for Xcode installation (acceptable)
- ⚠️ **Incomplete ARM64 support**: linux-arm64 and windows-arm64 remain unsupported initially (acceptable trade-off)

### Neutral

- 📝 **Documentation updates**: Need to document supported platforms clearly
- 📝 **Testing requirements**: Should test macOS binaries on both Intel and Apple Silicon before release

## Implementation Notes

### For Developer Agent

**Changes required in `.github/workflows/release.yml`:**

1. **Add Xcode installation step** (insert after line 265, before "Setup .NET"):

```yaml
- name: Install Xcode Command Line Tools (macOS)
  if: startsWith(matrix.platform, 'macos-')
  run: |
    # Xcode CLT is required for .NET NativeAOT compilation on macOS
    # xcode-select --install starts GUI installer, but it may already be installed
    sudo xcode-select --install 2>&1 || true
    # Verify installation
    if ! xcode-select -p &>/dev/null; then
      echo "Error: Xcode Command Line Tools not found after installation"
      exit 1
    fi
    echo "Xcode Command Line Tools installed at: $(xcode-select -p)"
```

2. **Remove windows-arm64 matrix entry** (delete lines 239-245):

```yaml
# DELETE THIS BLOCK:
# - platform: windows-arm64
#   os: windows-latest
#   rid: win-arm64
#   archive_ext: zip
#   binary_name: tfplan2md.exe
#   container: ''
#   needs_clang: false
```

3. **Update platform list documentation** in ADR-008 and README.md to reflect:
   - ✅ Supported: linux-x64, windows-x64, macos-x64, macos-arm64
   - 🚧 Planned: linux-arm64 (Phase 2)
   - ❌ Not supported: windows-arm64 (low demand, no native runners)

### For Quality Engineer

**Testing requirements:**

1. **macOS x64**: Test on Intel Mac or GitHub Actions macos-13 runner
   - Verify binary runs: `./tfplan2md --version`
   - Test functionality: `./tfplan2md examples/azure_cdn.json`
   
2. **macOS ARM64**: Test on Apple Silicon Mac or GitHub Actions macos-14 runner
   - Verify binary runs: `./tfplan2md --version`
   - Test functionality: `./tfplan2md examples/azure_cdn.json`
   
3. **Cross-platform verification**: Ensure binaries are NOT cross-compatible (x64 binary should fail on ARM64 Mac and vice versa)

### Phase 2: Linux ARM64 Cross-Compilation (Optional)

If linux-arm64 support is desired later, modify the workflow:

```yaml
- name: Install NativeAOT linker prerequisite (Linux)
  if: matrix.needs_clang
  run: |
    apt-get update
    if [ "${{ matrix.platform }}" == "linux-arm64" ]; then
      # ARM64 cross-compilation toolchain
      apt-get install -y --no-install-recommends \
        clang \
        gcc-aarch64-linux-gnu \
        binutils-aarch64-linux-gnu
    else
      # x64 native compilation
      apt-get install -y --no-install-recommends clang
    fi
```

## References

- **.NET NativeAOT Documentation**: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- **GitHub Actions macOS Runners**: https://github.com/actions/runner-images/tree/main/images/macos
- **Xcode Command Line Tools**: https://developer.apple.com/xcode/resources/
- **ADR-008**: Multi-Platform Binary Distribution
- **Feature 047**: Multi-Platform Binary Distribution Implementation
- **Feature 089**: Homebrew Installation Support (requires macOS binaries)
