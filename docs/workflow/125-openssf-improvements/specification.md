# OpenSSF Scorecard Improvements

## Problem Statement

Improve the repository's OpenSSF Scorecard score by implementing the top 5 highest-impact security practices.

## Top 5 Changes

### 1. Pin All GitHub Actions to Full SHA Hashes (Pinned-Dependencies, weight 10)

**Impact:** Highest — moves `Pinned-Dependencies` score from 0 to near-perfect.

All `uses:` references in GitHub Actions workflows were changed from mutable version tags
(e.g., `@v6`) to immutable SHA-pinned references (e.g., `@de0fac2e...  # v6.0.2`).
This prevents supply-chain attacks where a malicious actor takes over a tag or publishes
a new version with malicious code.

**Files changed:** All 8 workflow files in `.github/workflows/`.

### 2. Add CodeQL Analysis Workflow (SAST, weight 10)

**Impact:** High — adds C# static analysis security scanning, satisfying the `SAST` check.

A new `.github/workflows/codeql.yml` workflow was added that runs CodeQL analysis
on C# source code on every push to `main`, every pull request, and on a weekly schedule.
This catches security vulnerabilities in the source code before they can be exploited.

### 3. Add SLSA Build Provenance Attestation (Signed-Releases, weight 10)

**Impact:** High — adds cryptographic provenance for release artifacts, satisfying the
`Signed-Releases` check at SLSA Level 2.

`actions/attest-build-provenance` was added to the `build-binaries` job in `release.yml`
to generate verifiable SLSA provenance attestations for each binary artifact published
to GitHub Releases. Users can verify the provenance using `gh attestation verify`.

### 4. Tighten Workflow Token Permissions (Token-Permissions, weight 10)

**Impact:** Medium — improves the `Token-Permissions` score by applying least-privilege.

Changes made:
- **`ci.yml`**: Removed `checks: write` and `pull-requests: read` — the versioning job
  only needs `contents: write` to push the versionize commit and tag.
- **`coverage-data.yml`**: Changed from workflow-level `contents: write` to
  `permissions: contents: read` at workflow level with `contents: write` at job level only.
- **`release.yml`**: Changed from workflow-level `contents: write` to
  `permissions: contents: read` at workflow level with per-job permissions scoped to
  only what each job actually needs (`contents: write`, `attestations: write`,
  `id-token: write` only where required).

### 5. Add `.github/CODEOWNERS` File (Branch-Protection, weight 5)

**Impact:** Low-Medium — adds code ownership metadata, contributing to the
`Branch-Protection` check which requires code owners to review pull requests.

A `CODEOWNERS` file was added that assigns `@oocx` as the owner of all files,
ensuring that the maintainer is automatically requested for review on every PR.

## Expected Score Impact

| Change | Check | Weight | Before | After (est.) |
|--------|-------|--------|--------|--------------|
| SHA-pin all actions | Pinned-Dependencies | 10 | 0/10 | 9–10/10 |
| CodeQL workflow | SAST | 10 | 0/10 | 9–10/10 |
| SLSA provenance | Signed-Releases | 10 | 0/10 | 5–8/10 |
| Tighten permissions | Token-Permissions | 10 | 6/10 | 8–9/10 |
| CODEOWNERS | Branch-Protection | 5 | partial | improved |

Estimated overall score improvement: from ~5–6/10 to ~8–9/10.
