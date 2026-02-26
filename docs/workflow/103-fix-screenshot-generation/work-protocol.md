# Work Protocol: Fix Screenshot Generation

## Overview

This workflow item fixes screenshot generation failures that were discovered post-release
of feature 102 (Known-After-Apply Rendering). Screenshot generation was skipped during
the release because it consistently failed with timeouts.

## Root Causes Identified

Three distinct bugs were found and fixed:

1. **CDN timeout during page navigation** (`WaitUntilState.NetworkIdle` waited up to 90s for
   `cdnjs.cloudflare.com` CSS/JS to load). Fixed by inlining all CDN assets directly into the
   three HTML rendering templates.

2. **Chromium compositor blocked without virtual display** — Playwright's new headless mode
   requires a compositor context. On developer machines where `DISPLAY=:0` exists but is not
   accessible from non-interactive contexts (e.g., VS Code terminals), `ScreenshotAsync` hangs
   indefinitely waiting for the renderer. Fixed by wrapping the ScreenshotGenerator invocation
   with `xvfb-run` in both screenshot generation scripts.

3. **Clip rectangle outside rendered image for elements below fold** — When targeting a
   resource element that appears below the initial viewport, `FullPage` was `false`, so
   Playwright only rendered the viewport region. The element's clip coordinates were valid
   for the full document but fell outside the viewport-only render, causing
   "Clipped area is either empty or outside the resulting image". Fixed by enabling
   `FullPage = true` whenever a clip target is specified.

## Changes Made

### `src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`
All three templates (`github-wrapper-light.html`, `github-wrapper.html`, `azdo-wrapper.html`)
had their CDN `<link>` and `<script src>` references replaced with inline `<style>` and
`<script>` blocks containing the actual CSS/JS content. Assets inlined:
- `github-markdown.min.css` v5.2.0 (light and dark variants)
- `highlight.js` v11.9.0 CSS (github, github-dark, vs themes)
- `highlight.min.js` v11.9.0

### `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/Capturing/HtmlScreenshotCapturer.cs`
- Added `--disable-gpu` and `--no-sandbox` to `ChromiumLaunchArgs`
- Fixed `BuildScreenshotOptions`: `FullPage = clip is not null || settings.FullPage`
  (was `clip is null && settings.FullPage`)

### `scripts/generate-release-screenshots.sh` and `scripts/generate-screenshot.sh`
Added `run_screenshotter()` helper function that wraps `dotnet run` with
`xvfb-run --auto-servernum --server-args="-screen 0 1920x1080x24"` when xvfb-run
is available, falling back to plain `dotnet run` otherwise.

### `docs/features/102-known-after-apply-rendering/`
- Added three release screenshots: `feature-102-storage-sensitive.png`,
  `feature-102-group-member-ref.png`, `feature-102-all-unknown.png`
- Updated `release-notes.md` to include a Screenshots section referencing them

## Agent Work Log

### Release Manager — 2026-02-26

**Work performed:**
- Investigated screenshot timeout root cause
- Identified three distinct bugs (CDN timeout, xvfb/compositor, FullPage clip)
- Downloaded and inlined all CDN assets into HTML templates
- Added xvfb-run wrapper to both screenshot scripts
- Fixed FullPage clip logic in HtmlScreenshotCapturer.cs
- Generated 3 release screenshots for feature 102
- Updated release-notes.md for feature 102 with screenshot references

**Artifacts produced:**
- `docs/features/102-known-after-apply-rendering/feature-102-storage-sensitive.png`
- `docs/features/102-known-after-apply-rendering/feature-102-group-member-ref.png`
- `docs/features/102-known-after-apply-rendering/feature-102-all-unknown.png`

**Problems encountered:**
- CDN timeout was not the root cause — even after inlining CSS, screenshots failed
- Root cause was missing virtual display context for Playwright's new headless compositor
- Additionally discovered a pre-existing FullPage clip bug for elements below viewport fold
