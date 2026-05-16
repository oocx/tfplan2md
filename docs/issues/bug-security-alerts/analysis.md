# Issue: GitHub Security Alert Remediation (8 Alerts)

## Problem Description

GitHub's Security tab reports 8 open security/quality alerts for the `oocx/tfplan2md` repository. These span two categories:

- **CodeQL high-severity** findings in HTML template files (alerts #42, #43, #44)
- **OpenSSF Scorecard** findings for repository process and supply-chain hygiene (alerts #48, #99, #103, #104, #113)

This document provides root cause analysis and remediation guidance for all 8 alerts so that a Developer can implement the fixes.

---

## Alert Group 1 — Bad HTML Filtering Regexp (High) — Alerts #42, #43, #44

**CodeQL rule:** `js/bad-tag-filter` (CWE-185)

### Affected Files

| Alert | File | Line |
|-------|------|------|
| #44 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html` | 1394 |
| #43 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html` | 1384 |
| #42 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html` | 1394 |

### Steps to Reproduce

1. Open any of the three template HTML files
2. Navigate to the indicated line number
3. Observe the embedded, minified highlight.js JavaScript code

### Expected Behavior

The HTML templates should not contain regex patterns that CodeQL identifies as incomplete HTML comment or tag filters.

### Actual Behavior

All three files embed the full minified highlight.js 11.9.0 library inline inside `<script>` tags. Within that library (line ~1394), the HTML/XML language grammar definition contains the following JavaScript:

```javascript
},e.COMMENT(/<!--/,/-->/,{relevance:10}),{begin:/<!\[CDATA\[/,end:/\]\]>/,
```

And on lines ~1391–1393:

```javascript
contains:[{className:"meta",begin:/<![a-zA-Z]/,end:/>/,relevance:10,...
```

CodeQL flags two patterns in this code:

1. **`e.COMMENT(/<!--/,/-->/,...)`** — The `/-->/` regex is used to match the end of HTML comments. HTML comments can also end with `--!>` (per the HTML5 specification), which this pattern does not handle. CodeQL treats this as a bypassable HTML comment filter (CWE-185).

2. **`/<![a-zA-Z]/`** — Matches `<!DOCTYPE`, `<!ELEMENT`, etc., but CodeQL may flag the character range as potentially insufficient.

### Root Cause Analysis

#### Affected Components

- File: `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html#L1394`
- File: `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html#L1384`
- File: `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html#L1394`

#### What's Broken

These regex patterns are part of the **highlight.js 11.9.0 HTML/XML syntax highlighter grammar** — they are not application-level HTML sanitizers. Their purpose is to _recognise_ HTML syntax for colorization, not to filter or remove it from untrusted input.

However, CodeQL scans all JavaScript code in HTML `<script>` blocks, including vendored/third-party libraries. It cannot determine from context that these patterns are part of a syntax highlighter rather than a security sanitizer.

The patterns became flaggable because highlight.js 11.9.0 uses `e.COMMENT(/<!--/,/-->/)` where `/-->/` is an incomplete HTML comment terminator regex. Newer versions of highlight.js (≥ 11.10.0) contain upstream fixes to these specific patterns that resolve the CodeQL findings.

#### Why It Happened

The highlight.js library is inlined (embedded verbatim) into the three HTML template files. When the library version was chosen (11.9.0), these CodeQL patterns were not yet known. The fix in highlight.js ≥ 11.10.0 specifically addresses the `/-->/` comment pattern.

### Suggested Fix Approach

**Option A (Preferred): Update embedded highlight.js to latest version**

The cleanest fix is to update the vendored highlight.js bundle from 11.9.0 to the latest version (≥ 11.10.0 — currently 11.11.x). The updated version contains upstream fixes to the specific regex patterns CodeQL flags.

Steps:
1. Download the latest minified highlight.js from the [highlight.js releases](https://github.com/highlightjs/highlight.js/releases) or CDN
2. Download the updated `github-dark.min.css` theme CSS from the same version
3. Replace the inlined JS bundle (`<script id="vendor-hljs-js">`) in all three templates
4. Replace the inlined theme CSS (`<style id="vendor-hljs-github-dark">`) in all three templates
5. Run the test suite to verify syntax highlighting still works

**Option B (Alternative): Extract highlight.js to an external file**

Move the highlight.js content to a standalone `.js` file and reference it via `<script src="...">`. This separates the vendor library from the template and can be handled differently in CodeQL configuration (e.g., path exclusions for vendor files).

**Verification:**
After applying the fix, trigger CodeQL analysis. Alerts #42, #43, and #44 should be resolved. Also check that the related overly-permissive regex range alerts (#39, #40, #41) are resolved — they share the same root cause.

### Related Tests

- End-to-end rendering tests that produce `.github.html` and `.azdo.html` output files
- Visual inspection of syntax highlighting in the rendered HTML output
- Any `HtmlRenderer` integration tests in `tests/`

---

## Alert Group 2 — Code-Review (High) — Alert #103

**Detected by:** OpenSSF Scorecard  
**Category:** Process/Policy  
**Scorecard check:** `Code-Review`

### Problem Description

OpenSSF Scorecard checks whether recent commits to the default branch (main) have been reviewed by at least one other person before merging. Alert #103 indicates that one or more recent commits were merged without a code review, or that branch protection rules do not enforce code review requirements.

### Root Cause Analysis

#### Affected Component

GitHub repository settings — branch protection rules for the `main` branch.

#### What's Broken

Scorecard's Code-Review check inspects:
- Whether branch protection rules require at least 1 approving review
- Whether recent merged PRs show evidence of review (approved review state)
- Whether the "Dismiss stale pull request approvals" setting is enabled

If PRs can be merged without an approved review (either because branch protection doesn't require it, or because the repository owner bypasses protection), Scorecard will flag it.

#### Why It Happened

This is a repository settings gap, not a code defect. The branch protection rules may not require approved reviews before merging, or the maintainer may have bypassed protection in some cases.

### Suggested Fix Approach

**Requires GitHub Repository Settings Change** (cannot be done via code commit alone):

1. Go to **Settings → Branches → Branch protection rules** for `main`
2. Enable **"Require a pull request before merging"**
3. Set **"Required number of approvals"** to at least 1
4. Enable **"Dismiss stale pull request approvals when new commits are pushed"**
5. Consider enabling **"Require review from Code Owners"** if a `CODEOWNERS` file exists

**Note:** This is a repository settings change. A Developer cannot implement this via a code commit. The Maintainer must apply this change in GitHub settings.

---

## Alert Group 3 — Branch-Protection (High) — Alert #48

**Detected by:** OpenSSF Scorecard  
**Category:** Process/Policy  
**Scorecard check:** `Branch-Protection`

### Problem Description

OpenSSF Scorecard Alert #48 indicates that the default branch (`main`) does not have adequate branch protection rules configured. This is a superset of the Code-Review check — it covers multiple branch protection settings beyond just code review.

### Root Cause Analysis

#### Affected Component

GitHub repository settings — branch protection rules for the `main` branch.

#### What's Broken

The Scorecard Branch-Protection check evaluates:
- Require pull request reviews before merging (at least 1)
- Dismiss stale reviews when new commits are pushed
- Require status checks to pass before merging
- Require branches to be up to date before merging
- Include administrators in branch protection
- Restrict who can push to matching branches
- Require signed commits (optional, high-value)

A low or failing score on this check means several of these protections are absent.

#### Why It Happened

Branch protection was either not configured when the repository was set up, or was partially configured without enabling all recommended protections.

### Suggested Fix Approach

**Requires GitHub Repository Settings Change** (cannot be done via code commit alone):

1. Go to **Settings → Branches → Branch protection rules** for `main`
2. Enable all required checks:
   - ✅ Require pull request before merging
   - ✅ Require at least 1 approved review
   - ✅ Dismiss stale pull request approvals when new commits are pushed
   - ✅ Require status checks to pass before merging (select: `build`, `test`, `ci`)
   - ✅ Require branches to be up to date before merging
   - ✅ Do not allow bypassing the above settings (includes admins)
3. Consider enabling **"Require signed commits"** for highest Scorecard score

**Note:** This requires Maintainer action in GitHub Settings.

---

## Alert Group 4 — Fuzzing (Medium) — Alert #104

**Detected by:** OpenSSF Scorecard  
**Category:** Testing  
**Scorecard check:** `Fuzzing`

### Problem Description

OpenSSF Scorecard checks whether the project uses fuzz testing. Alert #104 indicates that no fuzz tests are present in the repository, and the project is not registered with Google's OSS-Fuzz service.

### Root Cause Analysis

#### Affected Component

The `src/` directory — no fuzz test files detected.

#### What's Broken

Scorecard's Fuzzing check looks for:
- Project registration with [OSS-Fuzz](https://google.github.io/oss-fuzz/)
- Fuzz test files matching known patterns (e.g., `FuzzXxx` functions for Go, corpus directories)
- For .NET projects: integration with [SharpFuzz](https://github.com/Metalnem/sharpfuzz) or similar tools

No fuzz tests currently exist in the repository.

#### Why It Happened

Fuzz testing was not part of the initial test strategy. The project's test suite uses NUnit unit tests and integration tests but not property-based or fuzz testing.

### Suggested Fix Approach

**Option A (Minimal effort): Add SharpFuzz-based fuzz tests**

1. Add [SharpFuzz](https://github.com/Metalnem/sharpfuzz) NuGet package to the test project
2. Write a fuzz target that passes arbitrary bytes to the Terraform plan JSON parser
3. Add a `fuzz/` directory with a simple fuzz test:

```csharp
// FuzzTests/FuzzPlanParser.cs
public static class FuzzPlanParser
{
    public static void Fuzz(ReadOnlySpan<byte> input)
    {
        try
        {
            var json = Encoding.UTF8.GetString(input);
            // Call the plan parser with arbitrary input
            TfPlanParser.Parse(json);
        }
        catch (JsonException) { } // Expected for invalid JSON
        catch (FormatException) { } // Expected for invalid plan format
        // Any other exception = potential bug
    }
}
```

4. Scorecard will detect the SharpFuzz dependency and fuzz method naming pattern

**Option B (Higher value): Register with OSS-Fuzz**

1. Create `oss-fuzz/` directory with project configuration
2. Submit a PR to [google/oss-fuzz](https://github.com/google/oss-fuzz) adding the project
3. OSS-Fuzz will run continuous fuzzing and report findings

Scorecard grants full marks for OSS-Fuzz registration.

---

## Alert Group 5 — Pinned-Dependencies in release.yml (Medium) — Alert #113

**Detected by:** OpenSSF Scorecard  
**Category:** Supply chain  
**Scorecard check:** `Pinned-Dependencies`

### Affected File

`.github/workflows/release.yml` — line 614

### Steps to Reproduce

1. Open `.github/workflows/release.yml`
2. Navigate to line 614
3. Observe the SLSA generator workflow reference uses a version tag instead of a commit SHA

### Expected Behavior

All GitHub Actions `uses:` references should be pinned to an immutable commit SHA (not a mutable version tag) to prevent supply-chain attacks where a tag is moved to point to malicious code.

### Actual Behavior

Line 614 of `release.yml` contains:

```yaml
uses: slsa-framework/slsa-github-generator/.github/workflows/generator_generic_slsa3.yml@v2.1.0
```

The `@v2.1.0` is a Git **tag** — a mutable reference. If an attacker compromises the `slsa-framework/slsa-github-generator` repository and moves the `v2.1.0` tag to a different commit, all workflows using this reference would silently start running malicious code.

### Root Cause Analysis

#### Affected Component

- File: `.github/workflows/release.yml#L614`

#### What's Broken

The `slsa-framework/slsa-github-generator` reusable workflow is referenced by version tag `v2.1.0` instead of by its immutable commit SHA. All other action references in `release.yml` are already SHA-pinned (e.g., `actions/checkout@de0fac2e...`), but this one reusable workflow was missed.

#### Why It Happened

Reusable workflow references (`uses: org/repo/.github/workflows/file.yml@ref`) support the same pinning syntax as action references, but this is less commonly known. The SLSA generator's own documentation historically showed tag-based references, which may have led to the tag-based usage here.

### Suggested Fix Approach

Pin the SLSA generator workflow reference to its commit SHA.

The commit SHA for `slsa-framework/slsa-github-generator` at tag `v2.1.0` is:
**`f7dd8c54c2067bafc12ca7a55595d5ee9b75204a`**

(Verified via: `gh api /repos/slsa-framework/slsa-github-generator/git/refs/tags/v2.1.0`)

**Change in `.github/workflows/release.yml` line 614:**

```yaml
# Before:
uses: slsa-framework/slsa-github-generator/.github/workflows/generator_generic_slsa3.yml@v2.1.0

# After:
uses: slsa-framework/slsa-github-generator/.github/workflows/generator_generic_slsa3.yml@f7dd8c54c2067bafc12ca7a55595d5ee9b75204a # v2.1.0
```

**Verification:**
- Run the Scorecard workflow manually after the change
- Verify the SLSA provenance generation step still completes successfully in a release run

### Related Tests

- SLSA provenance job in `release.yml` (`generate-slsa-provenance` job or similar)

---

## Alert Group 6 — Pinned-Dependencies in Dockerfile (Medium) — Alert #99

**Detected by:** OpenSSF Scorecard  
**Category:** Supply chain  
**Scorecard check:** `Pinned-Dependencies`

### Affected File

`src/Dockerfile` — line 9 (apk add command)

### Steps to Reproduce

1. Open `src/Dockerfile`
2. Navigate to line 9
3. Observe that Alpine packages are installed without version pinning

### Expected Behavior

Package installations in Dockerfiles should pin exact package versions to ensure reproducible builds and prevent silent introduction of malicious or breaking changes via package updates.

### Actual Behavior

Line 9 of `src/Dockerfile` contains:

```dockerfile
RUN apk add --no-cache upx clang build-base zlib-dev linux-headers bash lld
```

All packages (`upx`, `clang`, `build-base`, `zlib-dev`, `linux-headers`, `bash`, `lld`) are installed without version pinning. Alpine's `apk` will resolve and install the latest available version at image build time, which changes whenever Alpine updates its package repository.

Note: The base image `FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:828a5235b7df373...` IS correctly pinned to a SHA digest, which pins the Alpine base version. However, the `apk add` packages are resolved from the Alpine package repository at the time of `apk update` (implicit in `apk add`), which may differ from the snapshot baked into the base image.

### Root Cause Analysis

#### Affected Component

- File: `src/Dockerfile#L9`

#### What's Broken

The packages installed via `apk add` do not have explicit version constraints. While the Alpine base is pinned via SHA, the package metadata in the Alpine package repositories can be updated (new packages, updated dependencies). Scorecard's Pinned-Dependencies check flags unpinned `apk add` commands as a supply-chain risk.

#### Why It Happened

Alpine package version pinning is less commonly practiced than Docker image pinning, and Alpine's package ecosystem makes it harder to pin (no lock file equivalent by default). The initial Dockerfile was written for functionality without supply-chain hardening.

### Suggested Fix Approach

**Step 1: Determine current package versions**

In the current base image environment, run:
```bash
docker run --rm mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:828a5235b7df373cc96b5ca74a4823a19f9e1fea654abf01e1cb1dd9c767b718 \
  sh -c "apk add --no-cache upx clang build-base zlib-dev linux-headers bash lld && apk info -v upx clang build-base zlib-dev linux-headers bash lld"
```

**Step 2: Update Dockerfile to pin versions**

Example form (actual versions must be determined from Step 1):
```dockerfile
# Install native toolchain prerequisites for NativeAOT (Alpine uses apk)
# Versions pinned for reproducibility — update by running:
#   docker run --rm mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:<digest> \
#     apk info -v upx clang build-base zlib-dev linux-headers bash lld
RUN apk add --no-cache \
    upx=X.Y.Z-rN \
    clang=X.Y.Z-rN \
    build-base=X.Y.Z-rN \
    zlib-dev=X.Y.Z-rN \
    linux-headers=X.Y.Z-rN \
    bash=X.Y.Z-rN \
    lld=X.Y.Z-rN
```

**Step 3: Test Docker build**

Verify the Docker image builds successfully with the pinned versions:
```bash
docker build -f src/Dockerfile .
```

**Step 4: Document the update process**

Add a comment in the Dockerfile explaining how to update the pinned versions, so future maintainers know how to refresh them when upgrading the base image.

### Related Tests

- Docker image build in `release.yml`
- Any CI/CD steps that build or test the Docker image

---

## Summary and Priority Order

| Alert | Severity | Type | Fix Location | Fix Complexity | Requires Settings? |
|-------|----------|------|--------------|----------------|--------------------|
| #44/#43/#42 — Bad HTML filtering regexp | High | Code change | 3 HTML template files | Medium (update highlight.js bundle) | No |
| #103 — Code-Review | High | Process | GitHub Settings | Low (toggle settings) | **Yes — Maintainer only** |
| #48 — Branch-Protection | High | Process | GitHub Settings | Low (toggle settings) | **Yes — Maintainer only** |
| #113 — Pinned-Dependencies (release.yml) | Medium | Code change | `.github/workflows/release.yml:614` | Low (change one line) | No |
| #104 — Fuzzing | Medium | New tests | New fuzz test project | High (new code) | No |
| #99 — Pinned-Dependencies (Dockerfile) | Medium | Code change | `src/Dockerfile:9` | Medium (version lookup required) | No |

### Recommended Fix Order for Developer

1. **Alert #113** (~5 min) — Pin SLSA generator SHA in `release.yml` line 614. Single line change with known SHA.
2. **Alerts #44/#43/#42** (~1–2 hours) — Update embedded highlight.js from 11.9.0 to latest in all 3 template files. Download new bundle, replace inline script and CSS.
3. **Alert #99** (~30 min) — Pin Alpine package versions in `src/Dockerfile`. Requires running Docker to determine current versions.
4. **Alert #104** (~1 day) — Add fuzz tests (new `FuzzTests` project with SharpFuzz).

### Maintainer Action Required

5. **Alerts #103 and #48** — Enable branch protection rules in GitHub Settings → Branches → main. A developer cannot implement this via code commit. The Maintainer must apply these settings directly in the GitHub repository settings UI.

---

## Additional Context

### Relationship to Existing Security Analysis

An earlier analysis at `docs/issues/fix-security-issues/github-security-analysis.md` covers a broader set of 30 CodeQL alerts (Categories A–E), including these three `Bad HTML filtering regexp` alerts under "Category A". That analysis also identifies related alerts #39, #40, #41 (overly permissive regex range — `[$_A-z]`) which share the same root cause (outdated highlight.js 11.9.0) and will also be resolved by the highlight.js update.

Fixing alerts #42/#43/#44 via highlight.js update will simultaneously resolve alerts #39, #40, #41.

### SLSA Generator SHA Verification

The commit SHA `f7dd8c54c2067bafc12ca7a55595d5ee9b75204a` for `slsa-framework/slsa-github-generator@v2.1.0` was verified via:
```bash
gh api /repos/slsa-framework/slsa-github-generator/git/refs/tags/v2.1.0
# Returns: {"object": {"type": "commit", "sha": "f7dd8c54c2067bafc12ca7a55595d5ee9b75204a", ...}}
```

### Highlight.js Update Resources

- Latest release: https://github.com/highlightjs/highlight.js/releases
- CDN (also provides SRI hashes): https://cdnjs.com/libraries/highlight.js
- The bundled file to replace is the "minified + all languages" build (`highlight.min.js`)
- Also update the theme CSS: `styles/github-dark.min.css`
