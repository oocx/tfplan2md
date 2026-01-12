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

- [ ] All existing unit tests pass with the AOT-compiled version
- [ ] All existing integration tests pass with the AOT-compiled version
- [ ] All features documented in `docs/features.md` continue working:
  - Custom templates (via `--template` flag)
  - Principal mapping (via `--principal-mapping` flag)
  - Large value handling options
  - Sensitive value detection
  - Resource grouping
  - Report metadata (version, commit hash, timestamp)
  - All Azure-specific features
- [ ] Docker image size is measurably reduced (document exact measurements)
- [ ] Build time impact is documented and acceptable (longer builds expected)
- [ ] Base image is minimal and secure (chiseled/distroless or scratch)
- [ ] No reflection-related runtime errors in production scenarios
- [ ] Documentation updated with new build/deployment characteristics

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
- Docker image size
- CI/CD build time
- Image pull time (representative network conditions)

### AOT Version
- Docker image size
- CI/CD build time
- Image pull time (representative network conditions)
- Any runtime performance differences (if measurable)

## Open Questions

**For Architect:**
1. What AOT compatibility issues exist with Scriban templating engine?
2. What trimming annotations or hints are needed for reflection-heavy code?
3. How should embedded resources be handled in AOT builds?
4. Should we use `PublishAot=true` or `PublishTrimmed=true` or both?
5. What `TrimMode` should be used (full, partial)?
6. Are there NuGet packages that need special trimming configuration?
7. Which minimal base image provides the best size/compatibility tradeoff?
8. Do we need `InvariantGlobalization` or are globalization libraries required?
