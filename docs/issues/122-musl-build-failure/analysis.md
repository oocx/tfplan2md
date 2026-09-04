# Issue: musl Builds Fail to Build

**GitHub Actions Run:** [23117834110](https://github.com/oocx/tfplan2md/actions/runs/23117834110)  
**Branch:** `copilot/fix-musl-build-failures`  
**Affected Jobs:**
- Build linux-musl-x64 Binary (Job ID: 67188202111) — ❌ failure
- Build linux-musl-arm64 Binary (Job ID: 67188202122) — ❌ failure

---

## Problem Description

The musl (Alpine/musl libc) binary builds fail during the release workflow. Both
`linux-musl-x64` and `linux-musl-arm64` builds fail after the actual compilation
succeeds, during the UPX binary compression step.

The error message is:

```
E: Could not open lock file /var/lib/apt/lists/lock - open (13: Permission denied)
E: Unable to lock directory /var/lib/apt/lists/
##[error]Process completed with exit code 100.
```

## Steps to Reproduce

1. Trigger a release build via git tag push or workflow_run event from CI.
2. Observe the `Build linux-musl-x64 Binary` and `Build linux-musl-arm64 Binary` jobs.
3. Both fail after the Docker-based compilation completes, during the "Install UPX (Linux)" step.

## Expected Behavior

All six binary build jobs complete successfully and upload their artifacts to the
GitHub release. The musl binaries should be built, compressed with UPX, and attached
to the release along with the other platform binaries.

## Actual Behavior

Both musl builds fail with an `apt-get` permission error (exit code 100) during
the "Install UPX (Linux)" step. The binary compilation itself **succeeds** — only
the post-build UPX installation step fails.

```
##[group]Run apt-get update -qq
apt-get update -qq
apt-get install -y --no-install-recommends upx-ucl
##[endgroup]
E: Could not open lock file /var/lib/apt/lists/lock - open (13: Permission denied)
E: Unable to lock directory /var/lib/apt/lists/
##[error]Process completed with exit code 100.
```

## Root Cause Analysis

### Affected Components

- File: `.github/workflows/release.yml#L358-L362` — "Install UPX (Linux)" step
- Matrix entries: `linux-musl-x64` and `linux-musl-arm64`

### What's Broken

The "Install UPX (Linux)" step in `.github/workflows/release.yml` (lines 358–362)
runs `apt-get` without `sudo`:

```yaml
- name: Install UPX (Linux)
  if: startsWith(matrix.platform, 'linux-') && matrix.compress_with_upx
  run: |
    apt-get update -qq
    apt-get install -y --no-install-recommends upx-ucl
```

This step runs for **all** `linux-*` platforms that have `compress_with_upx: true`,
which includes the musl builds.

### Why It Works for `linux-x64` and `linux-arm64` (Container Builds)

The non-musl Linux builds (`linux-x64`, `linux-arm64`) use a **job-level container**:

```yaml
- platform: linux-x64
  container: mcr.microsoft.com/dotnet/sdk:10.0-noble  # ← root user inside container
  compress_with_upx: true
```

When GitHub Actions runs a job inside a Docker container, the process runs as `root`
by default. Therefore, `apt-get` works without `sudo`.

### Why It Fails for `linux-musl-x64` and `linux-musl-arm64` (No Container)

The musl builds do NOT use a job-level container:

```yaml
- platform: linux-musl-x64
  container: ''   # ← NO container; runs directly on the runner host
  compress_with_upx: true
```

Because Alpine containers have limitations (no bash by default, JavaScript Actions
only supported on x64), the musl builds run the compilation step inside an
**ephemeral Docker container** launched via a `docker run` shell command:

```yaml
- name: Build Binary (musl via Docker)
  if: startsWith(matrix.platform, 'linux-musl-')
  run: |
    docker run --rm \
      -v "$(pwd):/work" \
      -w /work \
      -e RID="linux-musl-x64" \
      -e PLATFORM="linux-musl-x64" \
      mcr.microsoft.com/dotnet/sdk:10.0-alpine \
      sh -c 'apk add --no-cache clang build-base zlib-dev linux-headers && ...'
```

After this Docker build step completes, the **subsequent steps run on the runner
host** — which is the GitHub-hosted `ubuntu-latest` runner. The runner user is the
non-privileged `runner` user (not root), so `apt-get` fails with permission denied.

### Summary Table

| Platform        | Job Container | UPX Install User | Works? |
|-----------------|---------------|------------------|--------|
| `linux-x64`     | .NET SDK Noble | `root` (in container) | ✅ Yes |
| `linux-arm64`   | .NET SDK Noble | `root` (in container) | ✅ Yes |
| `linux-musl-x64`  | None (host runner) | `runner` (non-root) | ❌ No  |
| `linux-musl-arm64` | None (host runner) | `runner` (non-root) | ❌ No  |

### Why It Happened

The `Install UPX (Linux)` step was written to work for the container-based builds
where root is available. When the musl builds were added with `container: ''` (to
avoid Alpine container limitations), the UPX installation step wasn't updated to
account for the non-root runner environment.

There is a similar pattern in the "Install NativeAOT linker prerequisite (Linux)"
step, but that step uses `if: matrix.needs_clang` which is `false` for all musl
builds — so it only runs inside containers (as root) and never hits this issue.

## Suggested Fix Approach

Add root-detection logic to the "Install UPX (Linux)" step so it uses `sudo` when
not already running as root:

```yaml
- name: Install UPX (Linux)
  if: startsWith(matrix.platform, 'linux-') && matrix.compress_with_upx
  run: |
    if [ "$(id -u)" = "0" ]; then
      apt-get update -qq
      apt-get install -y --no-install-recommends upx-ucl
    else
      sudo apt-get update -qq
      sudo apt-get install -y --no-install-recommends upx-ucl
    fi
```

**Why this approach:**
- When running in a container (linux-x64, linux-arm64): `id -u` returns `0`, no sudo needed.
  The .NET SDK Noble containers don't have `sudo` installed, so we can't unconditionally use it.
- When running on the runner host (linux-musl-x64, linux-musl-arm64): `id -u` returns non-zero,
  so `sudo` is invoked. GitHub-hosted runners always have `sudo` available.

**Alternative approaches:**

1. **Simpler `sudo` check**: Use `command -v sudo` instead of `id -u` check — same behavior.
2. **Install UPX inside the Alpine Docker container** (`apk add upx`) and compress
   the binary inside the container. This avoids the host apt-get entirely but requires
   restructuring the musl build step.
3. **Download UPX binary directly** via `curl`/`wget` from GitHub releases, bypassing
   `apt-get` entirely — more complex but fully root-agnostic.

The `id -u` approach (option 1 above) is the simplest one-line change with minimal risk.

## Related Tests

No automated tests exist for the release workflow itself. After the fix:
- [ ] Trigger a release workflow run (or dry-run) and verify both musl jobs succeed.
- [ ] Verify the built `tfplan2md` binary is produced and compressed for `linux-musl-x64`.
- [ ] Verify the built `tfplan2md` binary is produced and compressed for `linux-musl-arm64`.

## Additional Context

- GitHub Actions Run: https://github.com/oocx/tfplan2md/actions/runs/23117834110
- Failed job (x64): https://github.com/oocx/tfplan2md/actions/runs/23117834110/job/67188202111
- Failed job (arm64): https://github.com/oocx/tfplan2md/actions/runs/23117834110/job/67188202122
- ADR-008 (multi-platform binary distribution): `docs/adr-008-multi-platform-binary-distribution.md`
- ADR-011 (UPX binary compression): `docs/adr-011-upx-binary-compression.md`
- The binary compilation itself **succeeds** in both failed jobs — only the post-build
  UPX installation fails. This confirms the fix is isolated to the UPX install step.
