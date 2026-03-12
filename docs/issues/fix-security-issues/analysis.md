# Issue: Fix Security Issues Detected by GitHub

## Problem Description

GitHub has flagged multiple security issues for this repository. Direct access to GitHub's Security APIs (code scanning, Dependabot, secret scanning alerts) was not available in this analysis environment (HTTP 403), so this analysis combines:

1. Manual review of open Dependabot pull requests (available via the standard REST API)
2. Local `dotnet list package --vulnerable --include-transitive` scan
3. Manual security review of CI/CD workflow files
4. Manual review of C# source code for common security patterns

---

## Security Issues Found

### Issue 1 — Outdated `docker/login-action` (v3 → v4)

**Severity:** Medium  
**Type:** Dependabot dependency update  
**GitHub PR:** [#605 — chore(deps): bump docker/login-action from 3 to 4](https://github.com/oocx/tfplan2md/pull/605)

#### Steps to Reproduce

1. Open `.github/workflows/release.yml`
2. Observe `uses: docker/login-action@v3` at line 594
3. Note that v4 has been available since May 2025

#### Expected Behavior

The workflow should use the latest stable major version of `docker/login-action` to ensure it runs on a supported Node.js runtime and receives security patches.

#### Actual Behavior

The workflow uses `docker/login-action@v3`, which:
- Runs on Node 20 (soon-to-be-deprecated in GitHub Actions)
- Contains older versions of `@actions/core` (pre-3.0.0) which have known security patches in newer versions
- Contains older `@docker/actions-toolkit` dependency versions

#### Root Cause Analysis

**Affected Component:**
- File: `.github/workflows/release.yml#L594`

**What's Broken:**

`docker/login-action` v3 uses `@actions/core` version `1.11.1`. The update to v4 bumps `@actions/core` to `3.0.0`, which contains security improvements. Additionally, the Node.js 20 runtime used by v3 is being deprecated by GitHub in favor of Node 24.

The `docker/login-action` also handles Docker Hub credentials (username + password) at runtime. Running the credential-handling logic on an older, less-maintained dependency chain increases supply-chain risk.

#### Suggested Fix

Update `.github/workflows/release.yml` line 594:
```yaml
# Before:
uses: docker/login-action@v3
# After:
uses: docker/login-action@v4
```

---

### Issue 2 — Outdated `docker/build-push-action` (v6 → v7)

**Severity:** Medium  
**Type:** Dependabot dependency update  
**GitHub PR:** [#606 — chore(deps): bump docker/build-push-action from 6 to 7](https://github.com/oocx/tfplan2md/pull/606)

#### Steps to Reproduce

1. Open `.github/workflows/release.yml`
2. Observe `uses: docker/build-push-action@v6` at line 604
3. Note that v7 has been available since May 2025

#### Expected Behavior

The workflow should use the latest stable major version of `docker/build-push-action`.

#### Actual Behavior

The workflow uses `docker/build-push-action@v6`, which:
- Runs on Node 20 (soon-to-be-deprecated in GitHub Actions)
- Contains vulnerable transitive dependencies:
  - `undici` (pre-5.29.0) — had multiple CVEs including HTTP header injection
  - `lodash` (pre-4.17.23) — had prototype pollution CVEs in older versions
  - `minimatch` (pre-3.1.5) — had a ReDoS vulnerability
- Contains older `@actions/core` (pre-3.0.0)

#### Root Cause Analysis

**Affected Component:**
- File: `.github/workflows/release.yml#L604`

**What's Broken:**

`docker/build-push-action` v6 pins `undici` at `5.28.4`, `lodash` at `4.17.21`, and `minimatch` at `3.1.2`. The v7 release bumps these to patched versions (undici 5.29.0, lodash 4.17.23, minimatch 3.1.5) as well as moving to Node 24 runtime and `@actions/core` 3.0.0.

Supply-chain vulnerabilities in actions that handle build secrets (like Docker Hub tokens) are particularly important to patch.

#### Suggested Fix

Update `.github/workflows/release.yml` line 604:
```yaml
# Before:
uses: docker/build-push-action@v6
# After:
uses: docker/build-push-action@v7
```

---

### Issue 3 — `DOCKERHUB_USERNAME` Stored as a GitHub Secret

**Severity:** Low  
**Type:** Security misconfiguration / supply chain risk

#### Steps to Reproduce

1. Open `.github/workflows/release.yml`, lines 580–597
2. Observe that `${{ secrets.DOCKERHUB_USERNAME }}` is used to construct Docker image tag prefixes (e.g., `oocx/tfplan2md:latest`)
3. Note that the Docker Hub username (`oocx`) is publicly visible in the Docker Hub image URL and README

#### Expected Behavior

Non-sensitive configuration values (like a public Docker Hub namespace) should be stored as GitHub Actions **variables** (not secrets), or hardcoded directly if they are static. Secrets should be reserved for sensitive values like passwords, tokens, and API keys.

#### Actual Behavior

`DOCKERHUB_USERNAME` is stored as a GitHub Secret and used inline in `run:` steps to construct image tags. This means:

1. **Debugging is harder**: The username value is masked in logs, making it difficult to see the actual image tag being constructed.
2. **Undefined secret causes silent failure**: If `DOCKERHUB_USERNAME` is not set, the image tag becomes `/tfplan2md:latest` (with an empty prefix), which is an invalid Docker image reference that may fail silently or publish to an unintended repository.
3. **Public value treated as private**: The Docker Hub username `oocx` is visible in the repository's README, Docker Hub page, and public image tags — it's not a sensitive value and doesn't need masking.
4. **OIDC alternative not used**: Modern Docker Hub publishing can use OIDC token federation instead of stored credentials, eliminating the need to store a long-lived `DOCKERHUB_TOKEN` secret at all.

#### Root Cause Analysis

**Affected Component:**
- File: `.github/workflows/release.yml#L580-L597`

**What's Broken:**

The `DOCKERHUB_USERNAME` secret is used in a `run:` step to construct the image tag string:
```yaml
TAGS="${{ secrets.DOCKERHUB_USERNAME }}/tfplan2md:${{ needs.release.outputs.version }}"
```

This pattern has two problems:
- The GitHub Actions expression `${{ secrets.X }}` in a `run:` block injects the secret value directly into the shell script. GitHub masks this value in logs, but it's still stored in the workflow environment.
- Using a secret to store a public value defeats the purpose of secrets (protecting sensitive data).

**Why It Happened:**

The Docker Hub username was stored alongside the `DOCKERHUB_TOKEN` secret for convenience, treating both as "Docker Hub credentials". However, the username is public configuration while the token is private credentials — they should be handled differently.

#### Suggested Fix

**Option A (Recommended): Use a GitHub Actions Variable**

Convert `DOCKERHUB_USERNAME` from a secret to a repository variable (`vars.DOCKERHUB_USERNAME`) via the repository Settings → Variables page. Then update the workflow to reference it as:
```yaml
TAGS="${{ vars.DOCKERHUB_USERNAME }}/tfplan2md:${{ needs.release.outputs.version }}"
```
And for the login step:
```yaml
username: ${{ vars.DOCKERHUB_USERNAME }}
```

**Option B (Simplest): Hardcode the username**

Since the Docker Hub username is a static value (`oocx`) that is publicly visible:
```yaml
TAGS="oocx/tfplan2md:${{ needs.release.outputs.version }}"
```
And:
```yaml
username: oocx
```

---

### Issue 4 — No CodeQL Security Scanning Workflow

**Severity:** Low  
**Type:** Missing security control

#### Steps to Reproduce

1. Browse `.github/workflows/` directory
2. Observe no `codeql.yml` or security scanning workflow
3. Note that the PR validation workflow does not run CodeQL

#### Expected Behavior

For a public C# repository, GitHub recommends setting up CodeQL code scanning to automatically detect common security vulnerabilities (path traversal, injection, insecure deserialization, etc.) in pull requests.

#### Actual Behavior

No CodeQL workflow is configured. The repository processes untrusted user-provided input (Terraform plan JSON files), reads and writes files from user-specified paths, and includes complex JSON parsing logic — all areas where CodeQL analysis could catch security vulnerabilities.

#### Root Cause Analysis

**Affected Components:**
- File: `src/Oocx.TfPlan2Md/ProgramEntry.cs#L174-L180` (file path from user input)
- File: `src/Oocx.TfPlan2Md/ProgramEntry.cs#L134-L136` (output file write)
- File: `src/Oocx.TfPlan2Md/CodeAnalysis/WildcardExpander.cs#L26-L58` (wildcard path expansion)

**What's Missing:**

The project currently relies only on:
- `dotnet list package --vulnerable` (NuGet package vulnerability scanning)
- Manual code review

GitHub CodeQL for C# would provide:
- CWE-022 (Path Traversal) detection — relevant because user-controlled paths are passed to `File.Exists`, `File.ReadAllTextAsync`, `Directory.EnumerateFiles`
- CWE-078 (OS Command Injection) detection
- CWE-089 (SQL Injection) detection
- CWE-502 (Deserialization) detection

**Why It Happened:**

The project was set up without a CodeQL workflow. Given it's a CLI tool (not a web service), the risk surface is lower, but adding CodeQL is a recommended security practice for public repositories on GitHub.

#### Suggested Fix

Add a CodeQL workflow at `.github/workflows/codeql.yml`:

```yaml
name: CodeQL

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 8 * * 1'  # Weekly on Monday

permissions:
  security-events: write
  contents: read

jobs:
  analyze:
    name: Analyze (C#)
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v6

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          global-json-file: src/global.json

      - name: Build
        run: dotnet build src/tfplan2md.slnx --no-restore --configuration Release

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
```

---

## NuGet Package Vulnerability Scan Results

Running `dotnet list src/tfplan2md.slnx package --vulnerable --include-transitive` confirms:

```
The given project `Oocx.TfPlan2Md` has no vulnerable packages given the current sources.
The given project `Oocx.TfPlan2Md.TUnit` has no vulnerable packages given the current sources.
The given project `JsonEmbedGenerator` has no vulnerable packages given the current sources.
The given project `Oocx.TfPlan2Md.CoverageEnforcer` has no vulnerable packages given the current sources.
The given project `Oocx.TfPlan2Md.HtmlRenderer` has no vulnerable packages given the current sources.
The given project `Oocx.TfPlan2Md.ScreenshotGenerator` has no vulnerable packages given the current sources.
The given project `Oocx.TfPlan2Md.TerraformShowRenderer` has no vulnerable packages given the current sources.
```

✅ **No vulnerable NuGet packages detected.**

---

## Summary Table

| # | Issue | Severity | Type | Fix |
|---|-------|----------|------|-----|
| 1 | `docker/login-action@v3` outdated | Medium | Dependabot | Update to `@v4` in `release.yml:594` |
| 2 | `docker/build-push-action@v6` outdated | Medium | Dependabot | Update to `@v7` in `release.yml:604` |
| 3 | `DOCKERHUB_USERNAME` as secret | Low | Misconfiguration | Use `vars.DOCKERHUB_USERNAME` or hardcode |
| 4 | No CodeQL scanning | Low | Missing control | Add `.github/workflows/codeql.yml` |

---

## Priority Order for Fixing

1. **Issues 1 & 2 (Outdated Docker Actions)** — Fix together in one commit. High ROI: Dependabot PRs already exist (#605, #606), this is a straightforward version bump. Addresses actual transitive dependency CVEs (`undici`, `lodash`).

2. **Issue 4 (Add CodeQL Workflow)** — Add a CodeQL workflow. Low effort, high value for ongoing security. The workflow runs automatically and will flag any future issues.

3. **Issue 3 (DOCKERHUB_USERNAME secret)** — Convert from secret to variable. Requires a repository settings change (not just a code change), and the Developer will need to note that the repository maintainer must create a `DOCKERHUB_USERNAME` variable in GitHub Settings → Variables (or update the workflow to hardcode the value).

---

## Related Tests

After the fix is implemented:
- [ ] Verify the Docker publish workflow succeeds in a release run (integration test)
- [ ] Verify CodeQL workflow runs without errors on the main branch
- [ ] Verify `docker/login-action@v4` authenticates successfully (requires Docker Hub credentials in CI)
- [ ] Verify `docker/build-push-action@v7` builds and pushes the image successfully

## Additional Context

- Open Dependabot PRs: [#605](https://github.com/oocx/tfplan2md/pull/605), [#606](https://github.com/oocx/tfplan2md/pull/606)
- GitHub Issue: [#610 — Fix security issues detected by GitHub](https://github.com/oocx/tfplan2md/issues/610)
- Local vulnerability scan confirmed no vulnerable NuGet packages (run date: 2025-07-10)
