# GitHub CodeQL Security Analysis — All 30 Alerts

Generated from the 30 CodeQL alerts at https://github.com/oocx/tfplan2md/security

---

## Category A: Bad HTML Filtering Regexp (High) — 3 Alerts
**CodeQL rule:** `js/bad-tag-filter`
**Alerts:** #44, #43, #42

### Affected Files
| Alert | File | Line |
|-------|------|------|
| #44 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html` | 1394 |
| #43 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html` | 1384 |
| #42 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html` | 1394 |

### Problematic Code

All three files contain identical minified `highlight.js` code. The flagged line contains:

```javascript
},e.COMMENT(/<!--/,/-->/,{relevance:10}),{begin:/<!\[CDATA\[/,end:/\]\]>/,
```

The `e.COMMENT(/<!--/,/-->/)` call defines a highlight.js syntax comment mode using `/-->/` as the HTML comment end pattern. CodeQL flags this because HTML comments can also end with `--!>` which this regex does not match — making it a potentially bypassable HTML tag filter.

Additionally, lines 1391–1393 use:
```javascript
contains:[{className:"meta",begin:/<![a-z]/,end:/>/,relevance:10,...
```
`/<![a-z]/` only matches lowercase letters after `<!`. With `case_insensitive:!0` (case-sensitive mode), this misses `<!DOCTYPE` (uppercase D).

### Root Cause

These regexes are part of an **embedded, minified copy of highlight.js 11.9.0** — a third-party syntax highlighting library. They are not application security filters; they are purely UI pattern matchers for syntax highlighting. CodeQL cannot distinguish context and flags them as HTML sanitizers.

### Fix

Update the embedded highlight.js bundle to the latest version (≥ 11.10.0), which contains upstream fixes for these patterns. The bundle is embedded verbatim in:

- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html`

Alternatively, remove the inline bundle and load highlight.js via CDN with SRI (which also resolves Category D).

---

## Category B: Incomplete URL Substring Sanitization (High) — 1 Alert
**CodeQL rule:** `py/incomplete-url-substring-sanitization`
**Alert:** #3

### Affected File
`scripts/update-azure-api-mappings.py` — line 66

### Problematic Code

```python
# Line 66:
if 'learn.microsoft.com' in self.current_href and '/rest/api/' in self.current_href:
    # Ensure the URL doesn't have version parameters
    clean_url = self.current_href.split('?')[0]
    self.mappings[resource_type] = clean_url
```

### Root Cause

Using `'domain' in url` to validate a URL hostname is bypassable. A malicious URL can embed the trusted domain as a substring:

```
https://evil.com/learn.microsoft.com/rest/api/...
https://learn.microsoft.com.evil.com/rest/api/...
```

Both pass the `'learn.microsoft.com' in self.current_href` check.

### Fix

Replace substring `in` check with proper URL parse + hostname check:

```python
from urllib.parse import urlparse

# Instead of:
if 'learn.microsoft.com' in self.current_href and '/rest/api/' in self.current_href:

# Use:
parsed = urlparse(self.current_href)
if parsed.hostname == 'learn.microsoft.com' and parsed.path.startswith('/rest/api/'):
```

**Impact:** Low risk in practice (script runs locally/in CI to build a mapping file), but the fix is trivial and correct.

---

## Category C: Overly Permissive Regular Expression Range (Medium) — 3 Alerts
**CodeQL rule:** `js/overly-permissive-character-class`
**Alerts:** #41, #40, #39

### Affected Files
| Alert | File | Line |
|-------|------|------|
| #41 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html` | 600 |
| #40 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html` | 600 |
| #39 | `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html` | 590 |

### Problematic Code

Line 600 in the minified highlight.js block:

```javascript
PARAMS_CONTAINS:h,CLASS_REFERENCE:y},illegal:/#(?![$_A-z])/,
```

The character class `[$_A-z]` uses the range `A-z` (ASCII 65–122). This range **unintentionally includes** non-letter characters between `Z` (90) and `a` (97): `[`, `\`, `]`, `^`, `_`, `` ` `` (ASCII 91–96). This is overly permissive — the `illegal` check for JavaScript `#` sigil can be bypassed using these characters.

### Fix

Same root cause as Category A — this is embedded **highlight.js 11.9.0**. Fix by updating the highlight.js bundle.

If patching directly in the template, change:
```javascript
illegal:/#(?![$_A-z])/,
```
to:
```javascript
illegal:/#(?![$_A-Za-z])/,
```

---

## Category D: Inclusion of Functionality from Untrusted Source (Medium) — 21 Alerts
**CodeQL rule:** `js/functionality-from-untrusted-source`
**Alerts:** Multiple alerts in `artifacts/` and `examples/` HTML files

### Affected Files

**In `artifacts/` (tracked HTML output files):**
- `artifacts/azapi-complex-demo.github.html`
- `artifacts/azapi-create-demo.github.html`
- `artifacts/azapi-mapped-resources-demo.github.html`
- `artifacts/azapi-update-demo.github.html`
- `artifacts/azapi-update-resource-update-plan.github.html`
- `artifacts/azure-display-enhancements-demo.github.html`
- `artifacts/azure-display-enhancements-demo.github-dark.html`
- Additional `.azdo.html` files (check `git ls-files artifacts/*.html`)

**In `examples/` (committed demo output files):**
- `examples/api-management-policy-demo/output-azdo.html`
- `examples/api-management-policy-demo/output-github.html`
- `examples/firewall-rules-demo/firewall-rules.azdo.html`

### Problematic Code

All affected files load scripts from `cdnjs.cloudflare.com` **without Subresource Integrity (SRI) hashes**:

```html
<!-- Example from artifacts/azapi-complex-demo.github.html -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github.min.css">
<script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"
        onerror="console.warn('highlight.js failed to load from CDN, syntax highlighting disabled');"></script>
```

Without `integrity="sha384-..."` and `crossorigin="anonymous"` attributes, a CDN compromise could inject malicious scripts executed in users' browsers.

### Root Cause: Two-Tier Problem

**Tier 1 — Generated artifact files (`artifacts/*.html`):**
These files are already listed in `.gitignore`:
```
artifacts/*.github.html
artifacts/*.azdo.html
```
However, they were committed to git **before** those `.gitignore` entries were added and remain **tracked** (confirmed via `git ls-files artifacts/`). They need to be removed from git tracking.

**Tier 2 — Source templates and example files:**
The source templates (`src/tools/.../templates/*.html`) and example output files (`examples/*/output-*.html`) also load CDN scripts without SRI. These are committed intentionally and need SRI hashes added.

### Fix — Two-Part Approach

**Part 1: Untrack artifact files from git (resolves ~15 artifact alerts):**
```bash
git ls-files artifacts/*.html | xargs git rm --cached
```

**Part 2: Add SRI hashes to source templates and example files:**

Fetch actual SRI hashes via cdnjs API:
```bash
curl -s "https://api.cdnjs.com/libraries/highlight.js/11.9.0?fields=sri"
curl -s "https://api.cdnjs.com/libraries/github-markdown-css/5.2.0?fields=sri"
```

Then update all CDN references in source templates to include `integrity` and `crossorigin`:
```html
<script
  src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"
  integrity="sha384-<HASH-FROM-CDNJS-API>"
  crossorigin="anonymous"></script>
```

**Files requiring SRI updates:**
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html` (lines 7, 219)
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper-light.html`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html`
- `examples/api-management-policy-demo/output-azdo.html`
- `examples/api-management-policy-demo/output-github.html`
- `examples/firewall-rules-demo/firewall-rules.azdo.html`

After updating templates, regenerate examples to propagate SRI hashes to example output files.

---

## Category E: Workflow Does Not Contain Permissions (Medium) — 2 Alerts
**CodeQL rule:** `actions/missing-workflow-permissions`
**Alerts:** #2, #1

### Affected Files
| Alert | File | Line |
|-------|------|------|
| #2 | `.github/workflows/uat-validate.yml` | 9 |
| #1 | `.github/workflows/copilot-setup-steps.yml` | 18 |

### Current Code

Both workflows have **no `permissions:` block** at workflow or job level, relying on the repository default (often `write-all` or `read-all`).

**`uat-validate.yml`** — reads code and runs shell tests. Needs only `contents: read`.

**`copilot-setup-steps.yml`** — checkouts repo, installs tools, and authenticates CLI tools. Needs only `contents: read`.

### Fix

Add a `permissions:` block to each job:

**`uat-validate.yml`** — add after `runs-on: ubuntu-latest` (job level, line ~11):
```yaml
    permissions:
      contents: read
```

**`copilot-setup-steps.yml`** — add after `environment: copilot` (job level, line ~22):
```yaml
    permissions:
      contents: read
```

---

## Summary: Fix Priorities

| Category | Severity | Alerts | Fix Location | Complexity |
|----------|----------|--------|--------------|------------|
| A — Bad HTML filtering regexp | High | 3 | Update highlight.js in 3 templates | Medium |
| B — Incomplete URL sanitization | High | 1 | `scripts/update-azure-api-mappings.py:66` | Low |
| C — Overly permissive regex range | Medium | 3 | Same highlight.js update as Category A | Low |
| D — Untrusted CDN inclusion | Medium | 21 | `git rm --cached` artifacts + add SRI to templates | Medium |
| E — Missing workflow permissions | Medium | 2 | Add `permissions: contents: read` to 2 workflows | Low |

### Recommended Fix Order for Developer
1. **Category E** (~5 min) — Add `permissions: contents: read` to 2 workflow files
2. **Category B** (~10 min) — Fix Python URL check with `urlparse`
3. **Category D Part 1** (~10 min) — `git rm --cached` all tracked artifact HTML files
4. **Categories A, C, D Part 2** (~1–2 hours) — Update embedded highlight.js bundle in 3 templates + add SRI hashes to CDN references in templates and examples
