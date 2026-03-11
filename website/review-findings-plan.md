# website2 Review Findings — Implementation Plan

<!-- markdownlint-disable MD029 MD031 MD032 -->

## 1. Purpose

This document turns the findings from the comprehensive website2 review into an actionable implementation plan with concrete tasks, acceptance criteria, and a recommended execution order.

All migration phases from `implementation-plan.md` are complete. This plan focuses on post-migration quality improvements discovered during the review.

## 2. Finding inventory

Each finding has a severity (High / Medium / Low), an identifier, and a short title.

| ID | Severity | Title |
|----|----------|-------|
| F-01 | High | Missing `<main>` landmark on 25 of 26 pages |
| F-02 | High | No favicon |
| F-03 | High | No canonical URLs or Open Graph metadata |
| F-04 | High | Dead JavaScript asset shipped to output |
| F-05 | Medium | Monolithic CSS file (4,161 lines) |
| F-06 | Medium | Pervasive inline styles in templates |
| F-07 | Medium | Oversized page templates |
| F-08 | Medium | Duplicate `escapeHtml` functions |
| F-09 | Medium | CI/CD code duplication across pages |
| F-10 | Medium | Inconsistent hero pattern |
| F-11 | Low | Lightbox theme-change image source bug |
| F-12 | Low | Carousel hardcoded gap value |
| F-13 | Low | No clipboard error handling |
| F-14 | Low | Deprecated `window.pageYOffset` API |
| F-15 | Low | Inconsistent section background alternation |
| F-16 | Low | `docsPage.supportedTools` raw string escaping |
| F-17 | Low | Inconsistent code style in docs-toc.js |

## 3. Phase plan

Work is organized into four phases. Each phase is independently deliverable and verifiable. Phases are ordered by impact and dependency — earlier phases unblock later ones.

---

### Phase 1: Quick wins and correctness fixes

**Goal:** Fix accessibility, dead code, and browser-error issues with minimal risk.

**Estimated scope:** 5 targeted file edits.

#### Task 1.1: Add `<main>` landmark to base layout (F-01)

**File:** `src/_includes/layouts/base.njk`

**Change:** Wrap `{{ content | safe }}` in a `<main id="main-content">` element.

**Before:**
```html
{% include "partials/nav.njk" %}
{{ content | safe }}
{% include "partials/footer.njk" %}
```

**After:**
```html
{% include "partials/nav.njk" %}
<main id="main-content">
  {{ content | safe }}
</main>
{% include "partials/footer.njk" %}
```

**Side effect check:** `docs.njk` already has its own `<main class="docs-content">`. This will create a nested `<main>`, which is invalid HTML. Two options:

- **Option A (recommended):** Add `<main>` to the base layout and remove the `<main>` from `docs.njk`, using a `<div class="docs-content">` instead.
- **Option B:** Use a front-matter flag (`noMainWrapper: true`) to conditionally skip the base-level `<main>` for docs.

**Acceptance criteria:**
1. Every generated HTML page contains exactly one `<main>` element.
2. HTMLHint passes on all 26 pages.
3. `website2-verify.sh --all` passes.

#### Task 1.2: Remove dead interactive-examples.js (F-04)

**File to delete:** `src/assets/js/interactive-examples.js`

**Rationale:** This 147-line IIFE is copied to `dist/assets/js/` via passthrough but never referenced by any HTML page. The site loads `site.js` which imports the ES module version from `src/site-assets/js/modules/interactive-examples.js`.

**Acceptance criteria:**
1. File `src/assets/js/interactive-examples.js` no longer exists.
2. `dist/assets/js/interactive-examples.js` no longer appears after build.
3. All interactive example tabs and fullscreen still work.
4. `website2-verify.sh --all` passes.

#### Task 1.3: Add favicon (F-02)

**Files:**
- Add a favicon file (SVG preferred for scalability) under `src/media-root/favicon.svg` (passthrough-copied to site root).
- Update `src/_includes/layouts/base.njk` to include `<link rel="icon">`.

**Change in base.njk `<head>`:**
```html
<link rel="icon" href="{{ rootPrefix }}favicon.svg" type="image/svg+xml">
```

**Source:** Extract from the existing `assets/images/logo-full.svg` or create a simplified variant.

**Acceptance criteria:**
1. Browser dev tools show no 404 for favicon.
2. Tab icon renders in Chromium.
3. `website2-verify.sh --all` passes.

#### Task 1.4: Add canonical URLs and Open Graph metadata (F-03)

**Files:**
- `src/_data/site.js` — add `baseUrl` property.
- `src/_includes/layouts/base.njk` — add meta tags in `<head>`.

**Data change (`site.js`):**
```js
baseUrl: "https://oocx.github.io/tfplan2md"
```

**Template change (`base.njk` `<head>`):**
```html
<link rel="canonical" href="{{ site.baseUrl }}/{{ permalink | replace('index.html', '') }}">
<meta property="og:title" content="{{ title }}">
<meta property="og:description" content="{{ description }}">
<meta property="og:type" content="website">
<meta property="og:url" content="{{ site.baseUrl }}/{{ permalink | replace('index.html', '') }}">
```

**Acceptance criteria:**
1. Every generated page contains a `<link rel="canonical">` tag.
2. Every generated page contains `og:title` and `og:description`.
3. `website2-verify.sh --all` passes.

#### Task 1.5: Fix deprecated API and missing error handling (F-13, F-14)

**Files:**
- `src/site-assets/js/modules/docs-toc.js` — replace `window.pageYOffset` with `window.scrollY`.
- `src/site-assets/js/modules/copy-buttons.js` — add `try/catch` around `navigator.clipboard.writeText()`.

**Acceptance criteria:**
1. No deprecated API warnings in browser console.
2. Copy button does not throw unhandled rejection on failure.
3. JS lint passes.

### Phase 1 exit criteria

1. All 26 pages have exactly one `<main>` element.
2. Dead JS file removed from source and output.
3. Favicon loads without 404.
4. Canonical and OG tags present on all pages.
5. No deprecated API usage.
6. `website2-verify.sh --all` passes.

---

### Phase 2: JavaScript quality improvements

**Goal:** Fix bugs, deduplicate code, and harden client-side modules.

**Estimated scope:** 5 file edits, 1 new file.

#### Task 2.1: Extract shared `escapeHtml` utility (F-08)

**New file:** `src/site-assets/js/modules/utils.js`

```js
export function escapeHtml(text) {
  return text
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}
```

**Updated files:**
- `src/site-assets/js/modules/interactive-examples.js` — import `escapeHtml` from `./utils.js`, remove local copy.
- `src/site-assets/js/modules/code-tabs.js` — import `escapeHtml` from `./utils.js`, remove local copy.

**Acceptance criteria:**
1. Only one `escapeHtml` definition exists in the codebase.
2. Both modules import from the shared utility.
3. Syntax highlighting in rendered/source tabs and code tabs still works.
4. JS lint passes.

#### Task 2.2: Fix lightbox theme-change bug (F-11)

**File:** `src/site-assets/js/modules/lightbox.js`

**Change:** Store a reference to the trigger element when opening the lightbox. On `tfplan2md:themechange`, use the stored reference to find the correct image source instead of querying the DOM for the first matching element.

**Acceptance criteria:**
1. When multiple screenshot elements exist, changing theme while lightbox is open shows the correct image.
2. Lightbox still works for single images.

#### Task 2.3: Read carousel gap from CSS (F-12)

**File:** `src/site-assets/js/modules/carousel.js`

**Change:** Replace hardcoded `+ 24` with a computed value:
```js
const gap = parseInt(getComputedStyle(track).gap, 10) || 24;
const slideWidth = slides[0].offsetWidth + gap;
```

**Acceptance criteria:**
1. Carousel alignment matches CSS gap regardless of the value.
2. Carousel still works with the current 24px gap.

#### Task 2.4: Standardize code style in docs-toc.js (F-17)

**File:** `src/site-assets/js/modules/docs-toc.js`

**Change:** Re-indent from 4-space to 2-space to match all other modules.

**Acceptance criteria:**
1. File uses 2-space indentation throughout.
2. JS lint passes.
3. Docs TOC scroll-spy still works.

### Phase 2 exit criteria

1. No duplicate utility functions across JS modules.
2. Lightbox theme-change uses stored element reference.
3. Carousel reads CSS gap dynamically.
4. Consistent code style across all JS modules.
5. `website2-verify.sh --all` passes.

---

### Phase 3: Template and CSS architecture improvements

**Goal:** Reduce inline styles, improve consistency, and split the monolithic CSS.

**Estimated scope:** Multiple file edits across templates, components, and CSS.

#### Task 3.1: Standardize section background alternation (F-15)

**File:** `src/style.css`

**Change:** Ensure `.section-alt` class is properly defined (it should already exist). Audit all pages and replace inline `style="background: var(--color-surface);"` on `<section>` elements with `class="section-alt"`.

**Files to update (pages using inline section backgrounds):**
- `src/pages/examples.njk`
- `src/pages/getting-started.njk`
- `src/pages/architecture.njk`
- `src/pages/docs.njk`
- `src/pages/index.njk`
- `src/pages/providers/index.njk`
- Feature detail pages as needed

**Acceptance criteria:**
1. No `<section>` element uses `style="background: var(--color-surface);"`.
2. Alternating section backgrounds look identical to current output.
3. Visual parity preserved.

#### Task 3.2: Move provider-card inline styles to CSS (F-06 partial)

**Files:**
- `src/_includes/components/provider-card.njk` — replace 10 inline styles with CSS classes.
- `src/style.css` — add corresponding CSS rules.

**New CSS classes needed:**
```css
.provider-card-header { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.provider-card-icon { margin: 0; font-size: 32px; }
.provider-card-title { margin: 0; }
.provider-status-badge { display: inline-block; padding: 4px 12px; color: white; border-radius: 12px; font-size: 12px; font-weight: 600; margin-top: 4px; }
.provider-card-section-title { margin-top: 24px; margin-bottom: 12px; font-size: 16px; }
.provider-card-list { margin-left: 20px; font-size: 14px; }
.provider-card-list li { margin-bottom: 8px; }
.provider-card-note { margin-top: 24px; padding: 12px; background: var(--color-surface); font-size: 14px; }
.provider-card-actions { display: flex; gap: 12px; margin-top: 24px; }
```

**Acceptance criteria:**
1. `provider-card.njk` has zero inline `style=""` attributes.
2. Provider pages render identically to current output.
3. CSS lint passes.

#### Task 3.3: Move docs-blocks inline styles to CSS (F-06 partial)

**Files:**
- `src/_includes/components/docs-blocks.njk` — replace 12 inline styles with CSS classes.
- `src/style.css` — add corresponding CSS rules.

**Acceptance criteria:**
1. `docs-blocks.njk` has zero inline `style=""` attributes.
2. Docs page renders identically to current output.
3. CSS lint passes.

#### Task 3.4: Standardize hero usage (F-10)

**Files:**
- `src/pages/ai-workflow.njk` — replace raw HTML hero with `heroBlock` macro call.
- Verify `src/pages/index.njk` hero. The index page hero has gradient backgrounds and badges that go beyond `heroBlock`'s current capabilities. Two options:
  - **Option A (recommended):** Extend the `heroBlock` macro to accept optional `badges` and `backgroundStyle` arguments to support the index hero's needs.
  - **Option B:** Accept that the homepage hero is intentionally custom and document this as a deliberate exception.

**Acceptance criteria:**
1. `ai-workflow.njk` uses `heroBlock` macro.
2. The decision on the homepage hero is documented (either extended macro or documented exception).
3. Visual parity preserved.

#### Task 3.5: Fix docsPage.supportedTools raw string escaping (F-16)

**File:** `src/_data/docsPage.js`

**Change:** Fix the `String.raw` template literal for the Checkov and TfLint command examples. The current mixed `\` and `\\` escaping produces incorrect backslash sequences. Use consistent escaping.

**Acceptance criteria:**
1. Rendered command examples show correct shell syntax.
2. No double-backslash artifacts in output HTML.

#### Task 3.6: Split monolithic CSS file (F-05)

**Current state:** Single 4,161-line `src/style.css` with `@layer` structure.

**Proposed split:**

| New file | Content | Approximate lines |
|----------|---------|-------------------|
| `src/styles/tokens.css` | CSS custom properties (light/dark themes, shared variables) | ~80 |
| `src/styles/base.css` | Reset, body, and foundational element styles | ~30 |
| `src/styles/layout.css` | Navbar, footer, sections, containers, responsive grid | ~500 |
| `src/styles/components.css` | Cards, buttons, badges, code blocks, tables, callouts | ~1,200 |
| `src/styles/pages.css` | Page-specific styles (docs sidebar, architecture, workflow) | ~800 |
| `src/styles/interactive.css` | Theme toggle, mobile nav, example tabs, fullscreen, lightbox, carousel | ~500 |
| `src/styles/examples.css` | `@layer examples` — rendered view (Azure DevOps approximation) | ~140 |
| `src/style.css` | Layer declaration + `@import` statements only | ~15 |

**Implementation approach:**
1. Create `src/styles/` directory.
2. Extract each section into its own file.
3. Update `src/style.css` to use `@import` statements (or alternatively, create an Eleventy build step to concatenate).
4. Verify `@layer` semantics are preserved.
5. Update passthrough copy if directory structure changes.

**Note:** CSS `@import` has a small performance cost (sequential loading). If this matters:
- **Option A:** Use CSS `@import` — simple, native, good enough for a documentation site.
- **Option B:** Add a build-time concatenation step in Eleventy config — optimal performance, slightly more complex.

**Acceptance criteria:**
1. No single CSS file exceeds ~1,200 lines.
2. `@layer` order is preserved: base, website, examples.
3. All pages render identically to current output.
4. CSS lint passes.
5. `website2-verify.sh --all` passes.

### Phase 3 exit criteria

1. Zero inline `style=""` in component macros (provider-card, docs-blocks).
2. No inline section background styles on page templates.
3. All pages use `heroBlock` macro (or documented exception for homepage).
4. CSS split into maintainable files.
5. Supported tools commands render correctly.
6. Visual parity preserved across all 26 pages.

---

### Phase 4: Data-driven refactoring

**Goal:** Reduce template size and improve maintainability by extracting hardcoded content into data files, following the proven `features/index.njk` pattern.

**Estimated scope:** 3-4 new data files, major rewrites of 3-4 page templates.

#### Task 4.1: Extract CI/CD code examples into shared data (F-09)

**New file:** `src/_data/cicdExamples.js`

**Content:** GitHub Actions YAML, Azure Pipelines YAML, GitLab CI YAML, and Security Tools shell commands currently duplicated between `index.njk` and `getting-started.njk`.

**Files to update:**
- `src/pages/index.njk` — replace inline CI/CD code blocks with data-driven rendering.
- `src/pages/getting-started.njk` — replace inline CI/CD code blocks with data-driven rendering.

**Acceptance criteria:**
1. CI/CD examples defined once in data.
2. Both pages render the same content from the shared source.
3. No content regression.

#### Task 4.2: Data-drive architecture.njk (F-07)

**Current state:** 403 lines of hardcoded HTML.

**New data files:**
- `src/_data/architecturePage.js` — quality goals, core components, technology stack, architectural patterns, and ADRs as structured data.

**Updated template:** `src/pages/architecture.njk` — iterate data arrays with macros.

**Target:** Reduce from ~403 lines to ~80-120 lines.

**Acceptance criteria:**
1. Page renders identically to current output.
2. Template is under 150 lines.
3. Adding a new quality goal or component requires only a data file edit.

#### Task 4.3: Data-drive ai-workflow.njk (F-07)

**Current state:** 152 lines with 12 hardcoded agent cards and 5 hardcoded process steps.

**New data file:** `src/_data/aiWorkflowPage.js` — agents, process steps, execution modes.

**Updated template:** `src/pages/ai-workflow.njk` — iterate data arrays.

**Target:** Reduce from ~152 lines to ~60-80 lines.

**Acceptance criteria:**
1. Page renders identically.
2. Adding a new agent requires only a data file edit.

#### Task 4.4: Reduce getting-started.njk size (F-07)

**Current state:** 380 lines.

**Approach:** After Task 4.1 extracts CI/CD examples, this page should shrink significantly. Evaluate remaining hardcoded content and extract where beneficial:
- Installation methods → data or partial
- Quick start steps → data
- Security integration options → data

**Target:** Reduce to under 200 lines.

**Acceptance criteria:**
1. Page renders identically.
2. Template under 200 lines.

#### Task 4.5: Reduce contributing.njk size (F-07)

**Current state:** 295 lines.

**Approach:** Extract branch prefix tables, commit type tables, and coding standards into data or partials.

**Target:** Reduce to under 200 lines.

**Acceptance criteria:**
1. Page renders identically.
2. Template under 200 lines.

### Phase 4 exit criteria

1. No page template exceeds 200 lines (except docs.njk which has a complex sidebar layout).
2. CI/CD examples defined once and reused.
3. All data-driven pages render identically to current hardcoded versions.
4. `website2-verify.sh --all` passes.

---

## 4. Recommended execution order

```
Phase 1 ──► Phase 2 ──► Phase 3 ──► Phase 4
(quick wins)  (JS quality)  (CSS/templates)  (data-driven)
```

Phases 1 and 2 are independent and could be parallelized if desired. Phase 3 should follow Phase 2 (inline style work may touch the same files). Phase 4 depends on Phase 3 task 3.1 (section background standardization).

## 5. Verification strategy

After each phase:

1. Run `npm run build` and verify 26 pages generated.
2. Run `scripts/website2-verify.sh --all` (HTML, CSS, JS, markdown lint + link/asset check).
3. Spot-check 3-5 representative pages in browser (light + dark mode).
4. Verify no new console errors.

After Phase 3 and Phase 4 (visual changes possible):

5. Full visual comparison of all 26 pages against current output.
6. Test mobile responsive layout on representative pages.

## 6. Risk assessment

| Risk | Mitigation |
|------|------------|
| CSS split breaks cascade order | Verify `@layer` declaration order is preserved. Test all pages after split. |
| Data-driven refactoring introduces content regression | Compare generated HTML before and after using diff. Keep existing templates as reference until verified. |
| `<main>` addition breaks docs.njk layout | Test Option A (replace docs `<main>` with `<div>`) first; fall back to Option B (front-matter flag) if layout breaks. |
| OG meta tags expose incorrect URLs | Validate canonical URLs by grepping all generated HTML after build. |
| Removing dead JS breaks something we missed | Search entire dist/ for references before deletion. Verify interactive examples on 3+ pages. |

## 7. Out of scope

The following items were noted during review but are intentionally excluded from this plan:

1. **Touch/swipe support for carousel** — UX enhancement, not a defect.
2. **CSS minification or bundling** — optimization, not required for a documentation site.
3. **Sitemap generation** — useful but not a review finding.
4. **Eleventy upgrade or dependency updates** — separate maintenance concern.
5. **Page content updates** — this plan addresses structural and code quality issues, not content accuracy.
