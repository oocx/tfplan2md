# Work Protocol: Multi-Platform Docker Image Build

**Work Item:** `docs/features/657-multi-platform-image-build/`
**Branch:** `oocx/feature-multi-platform-image-build`
**Workflow Type:** Feature
**Created:** 2026-05-19

## Agent Work Log

### Developer
- **Date:** 2026-05-19
- **Summary:** Added QEMU setup step and `platforms: linux/amd64,linux/arm64` to the Docker build-push step in `.github/workflows/release.yml`. Updated `src/Dockerfile` to use a manifest list digest and an arch-aware `dotnet publish` RID via `ARG TARGETARCH` so both platforms build correctly with NativeAOT.
- **Artifacts Produced:**
  - `.github/workflows/release.yml` (added `docker/setup-qemu-action`, added `platforms` to build-push step)
  - `src/Dockerfile` (updated base image digest to manifest list; arch-aware `dotnet publish` RID)
- **Problems Encountered:** None.
