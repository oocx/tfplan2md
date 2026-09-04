# Security Fixes: CodeQL Bad HTML Filtering Regexp and Supply-Chain Pinning

This patch resolves five GitHub security alerts — three CodeQL high-severity findings and two OpenSSF Scorecard supply-chain findings — with no change to any user-facing behaviour.

## 🔒 Security fixes

### Fixed incomplete HTML comment regex in embedded highlight.js (Alerts #42, #43, #44)

**Problem:** CodeQL rule `js/bad-html-filtering-regexp` (CWE-185) flagged two regex patterns inside the minified highlight.js 11.9.0 bundle that is embedded inline in all three HTML wrapper templates:

- `e.COMMENT(/<!--/, /-->/)` — the end pattern `/-->/` does not match the alternative HTML5 comment terminator `--!>`, making it an incomplete HTML comment filter.
- `/<![a-zA-Z]/` — flagged as a potential overly-broad tag filter.

**Fix:** Patched both patterns directly in all three templates (a minimal inline fix rather than a full library upgrade):

- `/-->/` → `/-->|--!>/` — now handles both valid HTML5 comment terminators.
- `/<![a-zA-Z]/` → `/<![A-Z]/` — XML/SGML declarations (`<!DOCTYPE`, `<!ELEMENT`, etc.) always use uppercase; this change is semantically equivalent given that highlight.js compiles all patterns with the `i` (case-insensitive) flag for HTML/XML mode.

**Templates updated:**
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html`

### Pinned SLSA generator reusable workflow to immutable SHA (Alert #113)

**Problem:** The SLSA provenance generation step in `.github/workflows/release.yml` referenced the `slsa-framework/slsa-github-generator` reusable workflow by the mutable tag `@v2.1.0`. If the tag were moved to a different commit (intentionally or through a supply-chain attack), the release pipeline would silently run different code.

**Fix:** Replaced the mutable tag reference with the immutable commit SHA for `v2.1.0`:

```yaml
# Before
uses: slsa-framework/slsa-github-generator/...@v2.1.0

# After
uses: slsa-framework/slsa-github-generator/...@f7dd8c54c2067bafc12ca7a55595d5ee9b75204a # v2.1.0
```

### Pinned Alpine package versions in Dockerfile (Alert #99)

**Problem:** `src/Dockerfile` installed seven Alpine packages (`upx`, `clang`, `build-base`, `zlib-dev`, `linux-headers`, `bash`, `lld`) without version constraints. While the base image was already digest-pinned, the packages themselves were resolved from the Alpine package repository at build time, introducing a supply-chain risk.

**Fix:** All seven packages are now pinned to the exact versions present in the Alpine 3.23 package index that corresponds to the already-pinned base image digest:

```dockerfile
RUN apk add --no-cache \
    upx=5.0.2-r0 \
    clang=21.1.2-r2 \
    build-base=0.5-r3 \
    zlib-dev=1.3.2-r0 \
    linux-headers=6.16.12-r0 \
    bash=5.3.3-r1 \
    lld=21.1.2-r1
```

## ℹ️ Maintainer action required (not fixed by code)

Two Scorecard alerts require GitHub repository settings changes that cannot be applied via a code commit:

| Alert | Required action |
|-------|----------------|
| **#103 Code-Review (High)** | Settings → Branches → main → enable "Require a pull request before merging" (≥ 1 approval) |
| **#48 Branch-Protection (High)** | Settings → Branches → main → enable full branch protection (status checks, dismiss stale reviews, include admins) |

## 🔗 Commits

- [`afb11db`](https://github.com/oocx/tfplan2md/commit/afb11db) fix: pin SLSA generator to SHA digest (alert #113)
- [`010a79a`](https://github.com/oocx/tfplan2md/commit/010a79a) fix: update highlight.js HTML comment regex to prevent CodeQL alerts (#42 #43 #44)
- [`5a220b4`](https://github.com/oocx/tfplan2md/commit/5a220b4) fix: pin Alpine package versions in Dockerfile (alert #99)
