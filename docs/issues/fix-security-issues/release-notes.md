# Release Notes: Security Hardening — CodeQL Alerts

## Type
🔒 Security Fix

## Summary
Resolved all 30 open CodeQL security alerts from the GitHub Security tab across five categories: missing workflow permissions, unsafe URL validation, untrusted CDN inclusions, and bad regex patterns in embedded highlight.js.

## Problem
GitHub Advanced Security reported 30 open CodeQL alerts against this repository:

- **Workflows missing `permissions`** — two workflow files had no `permissions` block, giving them default write access.
- **Incomplete URL substring sanitization** — `scripts/update-azure-api-mappings.py` used `'learn.microsoft.com' in url`, which could be bypassed with a URL like `evil.com/learn.microsoft.com/...`.
- **Inclusion from untrusted source** — several artifact HTML files committed to the repository referenced external CDN URLs for CSS/JS, meaning the page content depended on third-party servers.
- **Overly permissive regex range** (`[$_A-z]`) — the character class `A-z` in embedded highlight.js spans non-letter ASCII characters such as `[\]^`` (codepoints 91–96).
- **Bad HTML filtering regexp** (`/<![a-z]/`) — the lowercase-only range missed uppercase SGML constructs, allowing certain HTML to bypass the filter.

## Changes Made

### Workflow Permissions (#1, #2)
- `uat-validate.yml`: added `permissions: contents: read`
- `copilot-setup-steps.yml`: added `permissions: contents: read`

### URL Sanitization (#3)
- `scripts/update-azure-api-mappings.py`: replaced `'learn.microsoft.com' in url` with `urlparse(url).hostname == 'learn.microsoft.com'`

### CDN Inclusions (#4–#15, #30–#35, #38)
- Removed committed artifact HTML files that loaded external CSS/JS from CDNs; these files are regenerated on demand and should not be tracked in git
- Extended `.gitignore` to cover `*.azdo-dark.html`, `*.github-dark.html`, and `*.github-wrapped.html` variant patterns
- `examples/firewall-rules-demo/firewall-rules.azdo.html`: replaced all CDN `<link>` and `<script>` references with inlined CSS and JS from the current template

### Regex Fixes in Embedded highlight.js (#39–#44)
Patched both patterns in all three wrapper templates (`src/Oocx.TfPlan2Md/RenderTargets/*/WrapperTemplate.cs`):
- `[$_A-z]` → `[$_A-Za-z]`
- `/<![a-z]/` → `/<![a-zA-Z]/`

### CI Hardening
- `docker/login-action` v3 → v4
- `docker/build-push-action` v6 → v7
- Hardcoded `DOCKERHUB_USERNAME` (`oocx`) instead of masking a public value as a secret

## Impact

### Security
- ✅ All 30 CodeQL alerts resolved
- ✅ Workflow files now follow the principle of least privilege
- ✅ URL validation is no longer bypassable via path-prefix injection
- ✅ Committed HTML pages are fully self-contained (no external CDN dependencies)
- ✅ highlight.js regex patterns in all render-target templates are corrected

### Compatibility
- **Non-breaking**: No changes to Terraform plan parsing or markdown output
- Artifact HTML files previously committed under `docs/` have been untracked; they are still regenerated on demand by the demo generation scripts

## Verification
- ✅ GitHub-managed CodeQL scan (`dynamic/github-code-scanning/codeql`) passes on this PR for all languages (csharp, python, javascript-typescript, actions)
- ✅ All unit tests pass
