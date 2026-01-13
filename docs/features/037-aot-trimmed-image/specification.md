# Feature: AOT-Compiled Trimmed Docker Image

## Overview

Replace the current standard .NET runtime build with an AOT (Ahead-of-Time) compiled, trimmed version to reduce Docker image size and improve deployment performance and security posture. This optimization targets CI/CD scenarios where image download time and minimal attack surface are critical.

## User Goals

- **Faster Deployments**: Reduce Docker image download time in CI/CD pipelines through smaller image size
- **Improved Security**: Minimize attack surface by using smaller base images and eliminating unused code through trimming
- **Transparent Migration**: All existing features continue working without any changes to user-facing behavior or command-line interface

## Scope

### In Scope

- Enable .NET AOT compilation for the tfplan2md application
- Enable assembly trimming to remove unused code
- Evaluate and select minimal base Docker images:
  - `runtime-deps:10.0-noble-chiseled`
  - `scratch`
  - Other suitable minimal images
- Ensure all reflection-based features remain functional:
  - Embedded template resource loading
  - Assembly metadata extraction (version, commit hash)
  - Custom template loading from filesystem
- Measure and document performance metrics:
  - Build time comparison (before/after)
  - Final Docker image size comparison (before/after)
  - Document tradeoffs explicitly
- Ensure all existing tests pass with the AOT version
- Update Dockerfile to use AOT publishing

### Out of Scope

- Creating a separate AOT build alongside the standard build (full replacement only)
- Changes to command-line interface or user-facing behavior
- Performance optimization beyond what AOT naturally provides
- New features or capabilities

## User Experience

### Behavior

From the user's perspective, tfplan2md should work **identically** to the current version:
- Same commands and options
- Same output format and quality
- Same error handling and messages
- Same template system (built-in and custom)
- Same metadata in generated reports

### Deployment Impact

- Docker image pulls complete faster due to reduced size
- Container startup time may improve due to AOT compilation
- Build times in CI/CD increase due to AOT compilation overhead (acceptable tradeoff)

## Success Criteria

- [x] All existing unit tests pass with the AOT-compiled version
- [x] All existing integration tests pass with the AOT-compiled version
- [x] All features documented in `docs/features.md` continue working:
  - Custom templates (via `--template` flag)
  - Principal mapping (via `--principal-mapping` flag)
  - Large value handling options
  - Sensitive value detection
  - Resource grouping
  - Report metadata (version, commit hash, timestamp)
  - All Azure-specific features
- [x] Docker image size is measurably reduced: **14.7MB (89.6% reduction)**
- [x] Build time impact is documented: ~2x increase (45s → 90s), acceptable
- [x] Base image is minimal and secure: FROM scratch with 3 musl libraries
- [x] No reflection-related runtime errors: AotScriptObjectMapper handles all reflection
- [x] Documentation updated with new build/deployment characteristics

## Constraints

- Must maintain backward compatibility with all existing features
- Must work with .NET 10 and C# 13
- Must handle all reflection patterns used by:
  - Scriban templating engine
  - Assembly resource loading
  - Assembly attribute reading
- Longer build times are acceptable given the deployment benefits

## Performance Metrics to Capture

### Baseline (Current Version)
- Docker image size: **141MB** (standard .NET runtime)
- CI/CD build time: ~45 seconds
- Runtime: JIT compilation

### AOT Version (Final)
- Docker image size: **14.7MB** (musl-based, FROM scratch)
  - **89.6% reduction** from baseline
  - **70.6% below 50MB target**
- CI/CD build time: ~90 seconds (2x baseline, acceptable for deployment benefits)
- Runtime: Native binary, no JIT overhead
- Base image: FROM scratch with minimal musl libraries (3 files)

### Size Optimization Journey
1. Initial AOT (glibc): 46.3MB (-67% from baseline)
2. Remove debug symbols: 29.8MB (-36% from 46.3MB)
3. Remove SSL/crypto libs: 21.5MB (-28% from 29.8MB)
4. Minimal glibc libs: 18.3MB (-15% from 21.5MB)
5. **Switch to musl: 14.7MB (-20% from 18.3MB)**

### Technical Implementation
- Runtime: linux-musl-x64 (Alpine-based SDK)
- Trimming: TrimMode=full with aggressive optimizations
- Libraries: Only 3 essential .so files (ld-musl, libgcc_s, libstdc++)
- Reflection handling: Explicit AotScriptObjectMapper replaces ScriptObject.Import
- Security: Non-root user (UID 1654), no shell, minimal attack surface

## Open Questions

**For Architect:** (All resolved)
1. ✅ Scriban uses reflection for template parsing - preserved via TrimmerRootDescriptor.xml
2. ✅ Created AotScriptObjectMapper for explicit mapping (replaces reflection-based ScriptObject.Import)
3. ✅ Embedded resources work correctly with TrimMode=full
4. ✅ Using PublishAot=true with TrimMode=full
5. ✅ TrimMode=full provides maximum size reduction
6. ✅ Scriban requires preservation in TrimmerRootDescriptor.xml
7. ✅ FROM scratch with minimal musl libraries provides best size/security
8. ✅ InvariantGlobalization=true eliminates culture-specific assemblies
