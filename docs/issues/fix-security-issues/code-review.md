# Code Review: Fix Security Issues Detected by GitHub

## Summary

Reviewed the security fixes applied in commit `39aa7d3`: updating Docker action versions, hardcoding the Docker Hub username, and adding a new CodeQL scanning workflow. The Docker action version bumps and username hardcoding are correct and complete. However, the new `codeql.yml` contains two significant defects that will cause the workflow to fail at runtime, and the work protocol is missing required agent log entries.

## Verification Results

- **Tests:** N/A — changes are GitHub Actions workflow files only; no C# code changed
- **Build:** N/A — no source code changes
- **Docker:** N/A — no Dockerfile changes
- **Workflow lint:** Not run (no local act/actionlint tooling); issues identified by manual review

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| `docker/login-action` updated to v4 | ✅ | N/A | Correctly applied at line 594 |
| `docker/build-push-action` updated to v7 | ✅ | N/A | Correctly applied at line 604 |
| `DOCKERHUB_USERNAME` hardcoded as `oocx` | ✅ | N/A | Applied in all 5 occurrences (tags + login username) |
| CodeQL workflow added | ✅ (partial) | ❌ | File added but will fail at build step (see Issues) |
| CodeQL workflow uses correct C# language | ✅ | N/A | `languages: csharp` is correct |
| CodeQL workflow runs on push, PR, and schedule | ✅ | N/A | Triggers are correctly configured |
| CodeQL workflow has appropriate permissions | ✅ | N/A | `security-events: write` + `contents: read` is correct |

**Spec Deviations Found:**

- The `codeql.yml` build step uses `--no-restore` without a prior `dotnet restore` step — the workflow **will fail** on a fresh runner
- `actions/checkout` and `actions/setup-dotnet` versions in `codeql.yml` are inconsistent with the rest of the repository

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| CodeQL build on fresh runner (no package cache) | ❌ Fail | `dotnet build --no-restore` will fail without `dotnet restore` |
| Docker tags with pre-release version | Pass | `oocx` hardcoded correctly in all 4 TAGS lines |
| Docker login with hardcoded username | Pass | `username: oocx` is correct and matches Docker Hub namespace |
| CodeQL workflow without `DOCKERHUB_TOKEN` | Pass | CodeQL workflow has no Docker dependency |
| Schedule trigger fires weekly | Pass | `0 8 * * 1` = Monday 08:00 UTC, correct |
| Empty `DOCKERHUB_USERNAME` secret (removed) | Pass | Hardcoded value eliminates the silent-failure risk |

## Review Decision

**Status:** ❌ Changes Requested

## Snapshot Changes

- Snapshot files changed: No
- N/A

## Issues Found

### Blockers

#### B1 — `codeql.yml`: Missing `dotnet restore` step — workflow will fail

**File:** `.github/workflows/codeql.yml`, step "Build" (line 34)

The build command uses `--no-restore`:
```yaml
run: dotnet build src/tfplan2md.slnx --no-restore --configuration Release
```

On a fresh GitHub Actions runner, NuGet packages have not been downloaded. Without `dotnet restore`, this step will fail with errors like:
```
error: Unable to find package X. No packages exist with this id in source(s).
```

Every other workflow in the repository that uses `--no-restore` has a prior restore step. For example:

- `pr-validation.yml` lines 136 + 152:
  ```yaml
  - name: Restore dependencies
    run: dotnet restore src/tfplan2md.slnx
  - name: Build
    run: dotnet build src/tfplan2md.slnx --no-restore --configuration Release
  ```
- `coverage-data.yml` lines 32 + 38: same pattern

**Fix required:** Add a `dotnet restore` step between "Setup .NET" and "Build":
```yaml
      - name: Restore
        run: dotnet restore src/tfplan2md.slnx

      - name: Build
        run: dotnet build src/tfplan2md.slnx --no-restore --configuration Release
```

---

#### B2 — Work Protocol: Developer agent has not logged work

**File:** `docs/issues/fix-security-issues/work-protocol.md`

The Bug Fix workflow requires both **Issue Analyst** and **Developer** to log entries in the Work Protocol. The Developer committed the fixes (`39aa7d3`) but did not append their log entry to `work-protocol.md`. Only the Issue Analyst entry is present.

Per the review guidelines: *"Missing agent entries are a Blocker issue."*

**Fix required:** The Developer agent must append their work log entry to `work-protocol.md` before this review can be approved.

---

#### B3 — Work Protocol: Technical Writer agent has not logged work

**File:** `docs/issues/fix-security-issues/work-protocol.md`

The Bug Fix workflow also requires a **Technical Writer** entry. No Technical Writer log entry is present. Even if no documentation changes are needed, the agent must log that assessment explicitly.

**Fix required:** The Technical Writer agent must append their work log entry (confirming whether any documentation changes were needed and why) before this review can be approved.

---

### Major Issues

#### M1 — `codeql.yml`: `actions/checkout` uses outdated `@v4` (should be `@v6`)

**File:** `.github/workflows/codeql.yml`, line 21

```yaml
uses: actions/checkout@v4
```

Every other workflow in the repository uses `actions/checkout@v6`:
- `release.yml`: `actions/checkout@v6` (lines 47, 304, 530, 539, 575)
- `ci.yml`: `actions/checkout@v6`
- `pr-validation.yml`: `actions/checkout@v6`
- `copilot-setup-steps.yml`: `actions/checkout@v6`
- `coverage-data.yml`: `actions/checkout@v6`
- `deploy-website.yml`: `actions/checkout@v6`

Using `@v4` in one workflow while all others use `@v6` introduces inconsistency and means the CodeQL workflow runs on an older action version than the rest of the CI pipeline.

**Fix required:** Change to `actions/checkout@v6`.

---

#### M2 — `codeql.yml`: `actions/setup-dotnet` uses outdated `@v4` (should be `@v5`)

**File:** `.github/workflows/codeql.yml`, line 29

```yaml
uses: actions/setup-dotnet@v4
```

Every other workflow in the repository uses `actions/setup-dotnet@v5`:
- `ci.yml`: `actions/setup-dotnet@v5`
- `pr-validation.yml`: `actions/setup-dotnet@v5`
- `coverage-data.yml`: `actions/setup-dotnet@v5`
- `copilot-setup-steps.yml`: `actions/setup-dotnet@v5`
- `release.yml`: `actions/setup-dotnet@v5`

**Fix required:** Change to `actions/setup-dotnet@v5`.

---

### Minor Issues

None.

### Suggestions

#### S1 — Consider a `dotnet restore` `.NET tools` step for completeness

`pr-validation.yml` also restores `.NET tools` (`dotnet tool restore`) before building. For the CodeQL workflow this is less critical (no tool-dependent steps), but it ensures the environment matches the standard CI setup if tools are ever added to the analysis workflow later.

## Critical Questions Answered

- **What could make this code fail?** The `codeql.yml` will fail immediately on the "Build" step because `dotnet restore` is never called but `--no-restore` is used. This is a guaranteed failure on every run.
- **What edge cases might not be handled?** The Docker tag hardcoding to `oocx` is correct for the current repository but would need updating if the Docker Hub namespace ever changes. This is acceptable given the analysis recommendation (Option B).
- **Are all error paths tested?** GitHub Actions workflows are not unit-testable locally, but the issue with missing restore is certain based on the `--no-restore` flag semantics and comparison with working workflows.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness (release.yml changes) | ✅ |
| Correctness (codeql.yml) | ❌ Missing restore step |
| Spec Compliance | ❌ codeql.yml will fail at build |
| Code Quality | ❌ Inconsistent action versions |
| Architecture | ✅ |
| Testing | N/A |
| Documentation / Work Protocol | ❌ Missing Developer and Technical Writer log entries |

## Work Protocol & Documentation Verification

| Check | Status |
|-------|--------|
| `work-protocol.md` exists | ✅ |
| Issue Analyst logged | ✅ |
| Developer logged | ❌ Blocker — entry missing |
| Technical Writer logged | ❌ Blocker — entry missing |
| `docs/features.md` update needed | N/A — bug fix, no new features |
| `docs/architecture.md` update needed | Not required — CI/CD workflow change |
| `README.md` update needed | Not required — no user-facing change |

## Next Steps

The following items must be addressed before this review can be approved:

**Developer agent** must:
1. Add a `dotnet restore src/tfplan2md.slnx` step in `codeql.yml` before the `dotnet build --no-restore` step
2. Update `actions/checkout@v4` → `@v6` in `codeql.yml`
3. Update `actions/setup-dotnet@v4` → `@v5` in `codeql.yml`
4. Append their work log entry to `work-protocol.md`

**Technical Writer agent** must:
5. Review whether any documentation needs updating and log their assessment in `work-protocol.md`

After these fixes, return to the **Code Reviewer** for re-approval.
