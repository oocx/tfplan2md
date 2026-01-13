# ADR-002: Use .NET Chiseled (Distroless) Docker Image

## Status

Superseded by Feature 037 (AOT-Compiled Trimmed Docker Image)

As of Feature 037, the Docker image uses FROM scratch with minimal musl libraries instead of the chiseled runtime image, achieving 14.7MB (89.6% reduction from baseline) with superior security posture.

## Context

tfplan2md is distributed as a Docker image for use in CI/CD pipelines. The choice of base image affects:

- Image size (download time, storage costs)
- Security (attack surface, CVE exposure)
- Compatibility (shell access, debugging capabilities)

Options considered:

1. **Full runtime image** (`mcr.microsoft.com/dotnet/runtime:10.0`) - ~200MB, includes shell
2. **Alpine-based image** (`mcr.microsoft.com/dotnet/runtime:10.0-alpine`) - ~100MB, musl libc
3. **Chiseled image** (`mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled`) - ~50MB, no shell

## Decision

Use the .NET Chiseled (distroless) image as the runtime base image.

## Rationale

- **Minimal attack surface**: No shell, no package manager, no unnecessary utilities
- **Smallest size**: Approximately 50MB, fastest to pull in CI/CD pipelines
- **Security-first**: Runs as non-root by default, fewer CVEs to patch
- **Microsoft-supported**: Official Microsoft image with regular security updates
- **Sufficient for CLI**: tfplan2md doesn't need shell access or debugging tools

## Consequences

### Positive

- Smallest possible image size for faster CI/CD pipelines
- Minimal security vulnerabilities from OS components
- Follows security best practices for containerized applications

### Negative

- Cannot shell into container for debugging (use multi-stage builds with debug target if needed)
- Some diagnostic tools unavailable at runtime
- Must ensure all dependencies are statically linked or included
