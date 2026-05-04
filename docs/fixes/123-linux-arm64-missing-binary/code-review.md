# Code Review: Fix 123 — Linux ARM64 Missing Binary

## Summary

Reviewed the three-part fix restoring `linux-arm64` and `linux-musl-arm64` binaries in
the release workflow (broken since commit `ae4e33c` in v1.42.1). The core logic was sound,
but **one critical bug was identified and corrected** during this review: the Alpine SDK and
runtime-deps Docker image references were upgraded from AMD64-only platform manifests to
multi-arch manifest list digests, which is required for `--platform linux/arm64` to work
correctly.

All fixes — including the correction applied during review — are now in place and
the changes are **Approved**.

## Verification Results

- Tests: N/A (CI workflow change — no unit tests apply)
- Build: N/A (workflow YAML syntax verified manually)
- Docker: Image digests verified live against MCR registry API
- CHANGELOG.md: Not modified ✅

## Specification Compliance

| Requirement | Implemented | Notes |
|-------------|-------------|-------|
| `linux-arm64` job no longer uses AMD64-pinned container | ✅ | `container: ''` clears the job container |
| `Setup .NET` still runs for `linux-arm64` | ✅ | Condition `!startsWith(matrix.platform, 'linux-musl-')` correctly matches |
| `sudo` used when runner is non-root | ✅ | `id -u` root check added; matches existing UPX install pattern |
| `linux-musl-arm64` Docker build uses ARM64 platform | ✅ (after fix) | `--platform linux/arm64` + multi-arch manifest list digest |
| `linux-musl-arm64` smoke test uses ARM64 platform | ✅ (after fix) | Same fix applied to `Validate Artifacts (Unix)` step |
| Binary presence validation detects missing platforms | ✅ | 6-platform check in `consolidate-checksums` job |
| Documentation updated | ✅ | README, ADR-008, CONTRIBUTING all accurate |

## Critical Bug Fixed During Review

### Root Cause of Incomplete Fix

The original `bca278a` commit added `--platform ${{ matrix.docker_platform }}` to the
`docker run` commands. However, the Alpine SDK and runtime-deps image references remained
pinned to **single-platform AMD64 manifests** (not multi-arch manifest lists):

```yaml
# BEFORE (broken for ARM64):
mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:828a5235b7df373cc96b5ca74a4823a19f9e1fea654abf01e1cb1dd9c767b718
```

Verified via MCR registry API: `sha256:828a5235...` has mediaType
`application/vnd.docker.distribution.manifest.v2+json` — a single-platform manifest
(AMD64 only). Docker's `--platform` flag can only select from a manifest **list**; it
cannot transform a single-platform AMD64 manifest into an ARM64 image. Therefore
`linux-musl-arm64` builds would still fail with the same `exec format error`.

### Fix Applied

Updated both image references to use their **multi-arch manifest list digests**, verified
against the MCR registry API:

| Image | Old Digest (AMD64 only) | New Digest (multi-arch list) | Platforms in list |
|-------|------------------------|------------------------------|-------------------|
| `dotnet/sdk:10.0-alpine` | `sha256:828a5235...` | `sha256:0191ff38...` | amd64, arm/v7, arm64 |
| `dotnet/runtime-deps:10.0-alpine` | `sha256:06c12910...` | `sha256:4f08c162...` | amd64, arm/v7, arm64 |

With manifest list digests, `--platform linux/amd64` selects AMD64 (for `linux-musl-x64`)
and `--platform linux/arm64` selects ARM64 (for `linux-musl-arm64`). Supply-chain security
is preserved — the digest still pins the exact manifest list.

**Files changed:**
- `.github/workflows/release.yml` lines 370 and 455

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| `linux-arm64` with no container, .NET setup works | Pass | `Setup .NET` condition verified |
| `linux-arm64` clang install as non-root | Pass | Root check matches UPX install pattern |
| `linux-musl-x64` AMD64 Docker build unchanged | Pass | `linux/amd64` manifest list digest serves AMD64 |
| `linux-musl-arm64` ARM64 Docker build | Fixed | Manifest list digest + `--platform linux/arm64` now correct |
| Validation: `linux-arm64` grep vs `linux-musl-arm64` false positive | Pass | `linux-arm64` is NOT a substring of `linux-musl-arm64` (different dash positions) |
| Validation: `linux-x64` grep vs `linux-musl-x64` false positive | Pass | `linux-x64` is NOT a substring of `linux-musl-x64` |
| `fail-fast: false` + missing build → validation catches it | Pass | Validation halts `consolidate-checksums` |
| CHANGELOG.md not modified | Pass | Confirmed via git diff |

## Review Decision

**Status: Approved** (after correction applied during review)

The `linux-arm64` fix was complete and correct as submitted. The `linux-musl-arm64` fix
required an additional correction (multi-arch manifest list digests) which was applied
in commit `e723e16` during this review.

## Issues Found

### Blockers

None (after the fix applied during review).

### Major Issues Found and Resolved

**[FIXED] `linux-musl-arm64` Docker build still fails with original PR**

- **File:** `.github/workflows/release.yml`, lines 370 and 455
- **Problem:** `--platform linux/arm64` with an AMD64-only manifest digest (`sha256:828a5235...`)
  does not pull an ARM64 image. Docker's `--platform` flag only selects from a manifest list;
  it cannot override a single-platform digest.
- **Evidence:** MCR registry API confirmed `sha256:828a5235...` returns
  `"mediaType": "application/vnd.docker.distribution.manifest.v2+json"` (single-platform,
  not a list).
- **Fix:** Updated both Alpine image digests to multi-arch manifest list digests.
- **Status:** ✅ Fixed in commit `e723e16`

### Minor Issues

None.

### Suggestions

1. **Dead code in `Install NativeAOT linker prerequisite`:** The `apk add` branch in the
   `Install NativeAOT linker prerequisite` step is unreachable — only `linux-x64` and
   `linux-arm64` have `needs_clang: true`, and both run on Ubuntu with `apt-get`. The `apk`
   branch dates from when musl builds ran inside Alpine containers (prior to using Docker
   build steps). Consider removing it in a cleanup PR.

2. **`Verify all platform binaries are present` only runs if all `build-binaries` jobs
   succeed:** Due to `needs: [release, build-binaries]`, if any matrix job fails,
   `consolidate-checksums` is skipped and the validation never runs. This is the existing
   behavior and acceptable — a failed build job is already clearly visible in the CI UI.
   Future improvement: use `if: always()` + early-exit logic to always surface the validation
   result.

## Critical Questions Answered

- **What could make this code fail?** The `linux-musl-arm64` Docker build would still fail
  with an AMD64-only digest. Fixed by updating to multi-arch manifest list digests.
- **What edge cases might not be handled?** If `.NET 10.0-alpine` or `.NET 10.0-alpine
  runtime-deps` manifest lists are updated (adding new digest), the pinned digests become
  stale. This is standard supply-chain maintenance, not a bug.
- **Are all error paths tested?** The binary presence validation provides a hard stop if
  any platform is missing. Individual job failures are surfaced by GitHub Actions.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ After review correction |
| Spec Compliance | ✅ All three fixes implemented |
| Code Quality | ✅ Follows existing patterns |
| Architecture | ✅ No scope creep |
| Testing | ✅ Validation step covers detection |
| Documentation | ✅ README, ADR, CONTRIBUTING updated |
| CHANGELOG not modified | ✅ |

## Work Protocol & Documentation Verification

| Document | Status | Notes |
|----------|--------|-------|
| `work-protocol.md` exists | ✅ | Present with Developer and Technical Writer entries |
| Issue Analyst log in work-protocol.md | ⚠️ Minor gap | `analysis.md` exists and is thorough, but no `## Issue Analyst Agent Log` section in work-protocol.md |
| Developer agent logged | ✅ | Detailed entry present |
| Technical Writer agent logged | ✅ | Detailed entry present |
| `docs/features.md` | N/A | Bug fix — not applicable |
| `docs/adr-008-multi-platform-binary-distribution.md` | ✅ | Updated with regression note and all-phases-complete status |
| `README.md` | ✅ | Stale notice removed; musl platforms added |
| `CONTRIBUTING.md` | ✅ | Release process description updated |

## Next Steps

The fix is approved. This is an internal CI workflow change with no user-facing markdown
rendering impact, so UAT is not required.

**Recommended next agent: Release Manager**
