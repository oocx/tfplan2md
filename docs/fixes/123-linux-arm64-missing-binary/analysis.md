# Issue: Linux ARM64 Binary Missing from Releases v1.42.1 and v1.43.0

## Problem Description

The `linux-arm64` and `linux-musl-arm64` pre-built binaries are absent from GitHub Release
assets for v1.42.1 and v1.43.0, while v1.42.0 included them correctly. The `Build linux-arm64
Binary` and `Build linux-musl-arm64 Binary` jobs in the Release workflow fail, causing the
overall release run to be marked as failed and the consolidated `SHA256SUMS` file to be skipped.

## Steps to Reproduce

1. Trigger the release workflow for any tag ≥ v1.42.1.
2. Observe the `Build linux-arm64 Binary` and `Build linux-musl-arm64 Binary` jobs fail.
3. Check the GitHub Release assets — `linux-arm64` and `linux-musl-arm64` archives are missing.

## Expected Behavior

All six platform binaries are uploaded to every GitHub Release:
- `tfplan2md_<version>_linux-x64.tar.gz`
- `tfplan2md_<version>_linux-arm64.tar.gz` ✅ expected
- `tfplan2md_<version>_linux-musl-x64.tar.gz`
- `tfplan2md_<version>_linux-musl-arm64.tar.gz` ✅ expected
- `tfplan2md_<version>_macos-arm64.tar.gz`
- `tfplan2md_<version>_windows-x64.zip`

## Actual Behavior

`linux-arm64` and `linux-musl-arm64` binaries are missing from v1.42.1 and v1.43.0.

**Job: `Build linux-arm64 Binary`** — container immediately exits:
```
WARNING: The requested image's platform (linux/amd64) does not match the detected host platform (linux/arm64/v8)
Error response from daemon: container ... is not running
```

**Job: `Build linux-musl-arm64 Binary`** — Docker run fails:
```
WARNING: The requested image's platform (linux/amd64) does not match the detected host platform (linux/arm64/v8)
exec /bin/sh: exec format error
Process completed with exit code 255.
```

## Root Cause Analysis

### Affected Components

- File: `.github/workflows/release.yml` — `build-binaries` matrix job
  - `linux-arm64` matrix entry (`container:` field), line ~258
  - `linux-musl-arm64` matrix entry (`Build Binary (musl via Docker)` step), line ~355–361
- Introducing commit: `ae4e33c` — *"fix(azapi): boolean values render as lowercase true/false"*

### What's Broken

Commit `ae4e33c` — which nominally fixed a rendering bug — also pinned SHA digests to Docker
image references in `release.yml` for supply-chain security hardening. The digests chosen are
**platform-specific AMD64 (linux/amd64) manifest digests**, not **multi-architecture manifest
list digests**:

| Image | Pinned Digest | Actual Architecture |
|-------|---------------|---------------------|
| `mcr.microsoft.com/dotnet/sdk:10.0-noble` | `sha256:6d7f69bc…` | linux/amd64 only |
| `mcr.microsoft.com/dotnet/sdk:10.0-alpine` | `sha256:828a5235…` | linux/amd64 only |
| `mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine` | `sha256:06c12910…` | linux/amd64 only |

Docker images published to registries have two types of digest:
- **Platform manifest digest** — identifies a single-architecture image blob (e.g., amd64 only)
- **Manifest list (index) digest** — identifies a multi-arch index that redirects to the
  correct platform blob at pull time

When a digest pointing to an AMD64-only manifest is used on an ARM64 runner, Docker pulls the
AMD64 image rather than refusing. The image then crashes immediately because the AMD64 binary
cannot be executed on an ARM64 CPU (`exec format error`).

The `linux-x64` and `linux-musl-x64` jobs are unaffected because they run on AMD64 runners
(`ubuntu-latest`), so the AMD64 digest matches the runner.

### Why It Happened

The `ae4e33c` commit mixed a runtime bug fix with a security hardening change. The SHA digests
were likely obtained by running `docker pull ... && docker inspect ...` (or a similar tooling
command) on an AMD64 development machine, which naturally returned AMD64-specific digests.
No ARM64 runner was used during validation.

**Timeline:**
- v1.42.0 (commit `1786f1f`): Uses bare image tags (`mcr.microsoft.com/dotnet/sdk:10.0-noble`)
  → Docker resolves to arch-appropriate image on each runner → ARM64 works ✅
- commit `ae4e33c` (post v1.42.0): Pins AMD64-only digest → ARM64 runners get AMD64 image → fail ❌
- v1.42.1, v1.43.0: Both released after `ae4e33c` → ARM64 binaries missing ❌

## Suggested Fix Approach

There are two independent problems to fix:

### Fix 1 — `linux-arm64` container job

**Problem:** The `container:` key in the matrix entry pins the SDK image to an AMD64-only digest.

**Recommended fix:** Remove the `container` field from the `linux-arm64` matrix entry and rely
on `actions/setup-dotnet` instead (which is already used by the macOS, Windows, and all non-musl
jobs). The `ubuntu-24.04-arm` runner itself is Ubuntu 24.04 Noble (glibc 2.39), identical in
glibc version to the `10.0-noble` SDK container, so binary compatibility is preserved.

```yaml
# Before
- platform: linux-arm64
  os: ubuntu-24.04-arm
  rid: linux-arm64
  archive_ext: tar.gz
  binary_name: tfplan2md
  container: mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:6d7f69bc7bc9d4510ca255977b1f53ce52a79307e048a91450b2aecd63627cc3
  needs_clang: true
  compress_with_upx: true

# After
- platform: linux-arm64
  os: ubuntu-24.04-arm
  rid: linux-arm64
  archive_ext: tar.gz
  binary_name: tfplan2md
  container: ''
  needs_clang: true
  compress_with_upx: true
```

The `setup-dotnet` step already has an `if: "!startsWith(matrix.platform, 'linux-musl-')"` condition
that will correctly run for this job once the container is removed.

### Fix 2 — `linux-musl-arm64` Docker build step

**Problem:** The `Build Binary (musl via Docker)` step runs `docker run` with an Alpine SDK
image pinned to an AMD64-only digest. Running this on the `ubuntu-24.04-arm` ARM64 runner
fails with `exec format error`.

**Recommended fix:** Add `--platform linux/arm64` to the `docker run` invocation so Docker
explicitly requests the ARM64 manifest even when the digest is otherwise ambiguous, OR update
the digest to point to the ARM64 platform manifest for the Alpine SDK.

The simpler and more robust fix is the `--platform` flag:

```bash
# Before
docker run --rm \
  -v "$(pwd):/work" \
  -w /work \
  -e RID="${{ matrix.rid }}" \
  -e PLATFORM="${{ matrix.platform }}" \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:828a5235b7df373cc96b5ca74a4823a19f9e1fea654abf01e1cb1dd9c767b718 \
  sh -c '...'

# After
docker run --rm \
  --platform linux/arm64 \
  -v "$(pwd):/work" \
  -w /work \
  -e RID="${{ matrix.rid }}" \
  -e PLATFORM="${{ matrix.platform }}" \
  mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:828a5235b7df373cc96b5ca74a4823a19f9e1fea654abf01e1cb1dd9c767b718 \
  sh -c '...'
```

Similarly, the `Validate Artifacts (Unix)` step also runs `docker run` for musl binary smoke
testing — the same `--platform` flag should be added there for the ARM64 case.

### Files to Change

| File | Change |
|------|--------|
| `.github/workflows/release.yml` | Clear `container:` for `linux-arm64` matrix entry |
| `.github/workflows/release.yml` | Add `--platform linux/arm64` to `docker run` in `Build Binary (musl via Docker)` step |
| `.github/workflows/release.yml` | Add `--platform linux/arm64` to `docker run` in `Validate Artifacts (Unix)` step (musl smoke test) |

## Proposed Automated Detection Mechanism

To prevent future releases with missing platform binaries, add a **post-build release verification
job** to the release workflow.

### Option A — Verify expected checksums in `consolidate-checksums` (lightweight)

In the `Consolidate Checksums` job, after assembling `SHA256SUMS`, assert that the file contains
exactly one line per expected platform:

```bash
# After generating SHA256SUMS, verify all expected platforms are present
EXPECTED_PLATFORMS=(
  "linux-x64"
  "linux-arm64"
  "linux-musl-x64"
  "linux-musl-arm64"
  "macos-arm64"
  "windows-x64"
)

missing=()
for platform in "${EXPECTED_PLATFORMS[@]}"; do
  if ! grep -q "$platform" SHA256SUMS; then
    missing+=("$platform")
  fi
done

if [ "${#missing[@]}" -gt 0 ]; then
  echo "::error::Missing binaries for platforms: ${missing[*]}"
  echo "Release is incomplete. Check the Build * Binary jobs for failures."
  exit 1
fi

echo "All expected platforms are present in SHA256SUMS."
```

This check is self-documenting (the expected platform list acts as a manifest), runs in CI
without external API calls, and causes the `consolidate-checksums` job to fail loudly — which
would block Homebrew formula updates and surface the problem before users notice.

### Option B — Dedicated `verify-release-assets` job (thorough)

Add a new job after `consolidate-checksums` that uses the GitHub API to verify all expected
assets are present on the GitHub Release:

```yaml
verify-release-assets:
  name: Verify Release Assets
  runs-on: ubuntu-latest
  needs: [release, consolidate-checksums]
  if: needs.release.outputs.version != ''
  permissions:
    contents: read
  steps:
    - name: Verify all platform binaries are present
      env:
        GH_TOKEN: ${{ github.token }}
        VERSION: ${{ needs.release.outputs.version }}
      run: |
        EXPECTED=(
          "tfplan2md_${VERSION}_linux-x64.tar.gz"
          "tfplan2md_${VERSION}_linux-arm64.tar.gz"
          "tfplan2md_${VERSION}_linux-musl-x64.tar.gz"
          "tfplan2md_${VERSION}_linux-musl-arm64.tar.gz"
          "tfplan2md_${VERSION}_macos-arm64.tar.gz"
          "tfplan2md_${VERSION}_windows-x64.zip"
          "SHA256SUMS"
        )
        ASSETS=$(gh release view "v${VERSION}" --json assets --jq '[.assets[].name]' | tr -d '[]"' | tr ',' '\n')
        missing=()
        for asset in "${EXPECTED[@]}"; do
          if ! echo "$ASSETS" | grep -qF "$asset"; then
            missing+=("$asset")
          fi
        done
        if [ "${#missing[@]}" -gt 0 ]; then
          echo "::error::Release v${VERSION} is missing assets: ${missing[*]}"
          exit 1
        fi
        echo "All expected assets are present on release v${VERSION}."
```

**Recommendation:** Implement Option A immediately (it's a small addition to an existing job) and
consider Option B as a follow-up for end-to-end verification.

## Related Tests

After the fix is applied:
- Trigger the Release workflow via `workflow_dispatch` for an existing tag (e.g., v1.43.0).
- Verify that `Build linux-arm64 Binary` completes successfully.
- Verify that `Build linux-musl-arm64 Binary` completes successfully.
- Verify that `tfplan2md_<version>_linux-arm64.tar.gz` and `tfplan2md_<version>_linux-musl-arm64.tar.gz` appear as assets on the GitHub Release.
- Verify that `SHA256SUMS` is generated and uploaded.

## Additional Context

- Affected releases: v1.42.1 (run [25079249616](https://github.com/oocx/tfplan2md/actions/runs/25079249616)),
  v1.43.0 (run [25192580223](https://github.com/oocx/tfplan2md/actions/runs/25192580223))
- Last working release: v1.42.0 (run [24940253234](https://github.com/oocx/tfplan2md/actions/runs/24940253234))
- Introducing commit: `ae4e33c` (mixed fix/hardening commit)
- Related ADR: [docs/adr-008-multi-platform-binary-distribution.md](../adr-008-multi-platform-binary-distribution.md)
- Related prior issue: [docs/issues/108-binary-builds-failed](../issues/108-binary-builds-failed/analysis.md) (different root cause — NETSDK1207)
