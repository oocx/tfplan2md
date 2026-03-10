# Website2 Markdown Migration Plan

## 1. Purpose

This document compares each production route in `website/` with its current implementation in `website2/` and defines the remaining work required to migrate page bodies from imported legacy HTML to native Eleventy Markdown and reusable components.

The objective is to preserve the current layout, styling, interaction model, and information hierarchy while removing the `legacyContent` dependency page by page.

## 2. Current implementation baseline

The current Website2 state is:

1. All production routes exist in `website2/src/pages/` and build successfully to `website2/dist/`.
2. Shared shell concerns have already been migrated: base layout, navigation, footer, theme toggle, mobile navigation, lightbox, carousel, docs TOC, copy buttons, and interactive example behavior.
3. All 26 production routes now render from native Markdown sources and shared components instead of the `legacyContent` shortcode, including `/architecture.html`, `/ai-workflow.html`, `/contributing.html`, `/docs.html`, `/examples.html`, `/getting-started.html`, `/index.html`, `/features/index.html`, `/features/firewall-rules.html`, `/features/custom-templates.html`, `/features/inline-diffs.html`, `/features/misc.html`, `/features/semantic-icons.html`, `/features/value-formatting.html`, `/features/nsg-rules.html`, `/features/module-grouping.html`, `/features/large-values.html`, `/features/sensitive-masking.html`, `/features/static-analysis.html`, `/features/azdo-variable-groups.html`, `/features/azure-optimizations.html`, `/providers/index.html`, `/providers/azuread.html`, `/providers/azuredevops.html`, `/providers/azurerm.html`, and `/providers/msgraph.html`.
4. No production routes still render their body content through the `legacyContent` shortcode.
5. Shared migration components are now active in real page usage: `hero-block`, `section-header`, `feature-detail`, `comparison-block`, `related-docs`, `code-window`, `feature-card`, `provider-card`, and `example-block`.
6. Imported page-body artifacts have been removed from the active Website2 source tree.
7. Interactive example fragments now live in the normal source tree under `website2/src/examples/`.

This means route parity remains preserved while the migration has fully removed imported legacy page bodies from the active production routes.

## 3. Page-by-page comparison

| Route | Legacy page role | Current Website2 state | Native Markdown target | Main migration needs |
| --- | --- | --- | --- | --- |
| `/index.html` | Marketing-style homepage with hero, problem/solution split, carousel, code tabs, install CTA | Migrated to Markdown with homepage sections and existing carousel/lightbox hooks preserved | Markdown page with front matter plus hero, split-section, carousel, code-tabs, CTA components | Reuse the homepage blocks if future landing sections are added |
| `/getting-started.html` | Installation and usage guide with cards and command blocks | Migrated to Markdown with install cards, quick-start steps, CI/CD tabs, and security sections preserved | Markdown page with hero, step sections, command blocks, related-links cards | Reuse the CI/CD tabs and command block structure on other guide pages |
| `/docs.html` | Long-form reference page with sticky TOC and docs sidebar | Migrated to Markdown with docs sidebar, TOC hooks, and reference sections preserved | Markdown page plus generated TOC/sidebar data and docs section layout | Consider later extracting docs-section helpers only if authoring repetition becomes a real maintenance cost |
| `/examples.html` | Example gallery with 8 example blocks, badges, CTA sections, real-world cards | Migrated to Markdown with centralized `exampleBlock` usage across all examples | Markdown page using centralized example-block components and listing cards | The gallery is now the reference implementation for example-block composition |
| `/architecture.html` | Long-form explanation page | Migrated to Markdown with shared hero wrapper and preserved section markup | Mostly direct Markdown with minimal helper components | Continue normalizing repeated section wrappers as more long-form pages migrate |
| `/ai-workflow.html` | Long-form workflow explanation page | Migrated to Markdown with preserved workflow-specific layout blocks | Mostly direct Markdown with minimal helper components | Reuse the same layout primitives for other process/documentation pages if needed |
| `/contributing.html` | Contributor guide with rich long-form sections | Migrated to Markdown with preserved docs tables and code blocks | Mostly direct Markdown with callouts/code blocks | Keep refining shared documentation wrappers if more docs-heavy pages move over |
| `/features/index.html` | Feature directory page with large card grids | Migrated to Markdown and shared feature-card data/model | Markdown page driven mainly by shared feature data | Expand the same data model to related listing pages |
| `/features/azdo-variable-groups.html` | Feature detail page with 5 example blocks and comparison sections | Migrated to Markdown with comparison, example-block, highlight grid, and CTA sections | Markdown page with feature-detail, comparison, example-block sections | Continue reusing the same comparison/example contracts on the remaining gallery-style pages |
| `/features/azure-optimizations.html` | Multi-section feature page with 5 example blocks and multiple comparisons | Migrated to Markdown with repeated comparison sections, stacked examples, and related docs | Markdown page with repeated comparison sections and stacked examples | Reuse the multi-section Azure feature pattern where future provider pages need it |
| `/features/custom-templates.html` | Feature detail page with code examples and related docs | Migrated to Markdown with feature-detail, code-window, and related-docs components | Mostly direct Markdown with feature-detail wrappers | Reuse the same pattern for similar docs-oriented feature pages |
| `/features/firewall-rules.html` | Feature detail page with comparison and 1 example block | Migrated to Markdown using shared comparison, feature-detail, related-docs, code-window, and example-block components | Markdown page with comparison component and related-docs block | Reuse the same comparison/example contract on the remaining example-heavy feature pages |
| `/features/inline-diffs.html` | Feature explanation page with static content | Migrated to Markdown with preserved screenshot/example layout | Mostly direct Markdown with small helper components | Reuse the same screenshot/card structure for other visual feature pages |
| `/features/large-values.html` | Feature detail page with comparison and 1 example block | Migrated to Markdown with comparison, configuration, and related docs sections | Markdown page with comparison component and example-block | Reuse the comparison plus configuration pattern for other CLI-driven feature pages |
| `/features/misc.html` | Small multi-section feature page | Migrated to Markdown with feature-detail, code-window, and related-docs components | Mostly direct Markdown with feature-detail wrappers | Use the same light-weight pattern for short multi-section feature pages |
| `/features/module-grouping.html` | Feature detail page with comparison and 1 example block | Migrated to Markdown with comparison and related docs sections | Markdown page with comparison component and example-block | Reuse the same feature-detail shell on remaining docs-heavy pages |
| `/features/nsg-rules.html` | Feature detail page with comparison and 1 example block | Migrated to Markdown with comparison and related docs sections | Markdown page with comparison component and example-block | Reuse the same comparison shell for additional resource-specific templates |
| `/features/semantic-icons.html` | Feature explanation page with static content | Migrated to Markdown with feature-detail and related-docs components | Mostly direct Markdown with small helper components | Good template for other mostly-static feature pages |
| `/features/sensitive-masking.html` | Feature detail page with comparison-heavy content and 1 example block | Migrated to Markdown with repeated comparison blocks and CLI guidance | Markdown page with repeated comparison component and example-block | Reuse the same two-column comparison contract for security-related features |
| `/features/static-analysis.html` | Feature page with 2 example blocks, cards, table, callout | Migrated to Markdown with feature-detail, tool cards, table, and example-block sections | Markdown page with feature-detail, card grid, table, example-block sections | Reuse the CLI option table and tool-card layout on other integration pages |
| `/features/value-formatting.html` | Feature explanation page with static content | Migrated to Markdown with feature-detail wrapper | Mostly direct Markdown with small helper components | Reuse the same structure for concise rules/pattern pages |
| `/providers/index.html` | Provider listing page with large provider cards and template docs section | Migrated to Markdown and shared provider-card data/model | Markdown page driven by provider data plus shared provider-card component | Reuse the provider-card contract if more provider families are added |
| `/providers/azuread.html` | Provider detail page | Migrated to Markdown with shared feature-detail wrapper | Mostly direct Markdown with provider-detail wrapper | Use this pattern for remaining long-form provider/foundation pages |
| `/providers/azuredevops.html` | Provider detail page | Migrated to Markdown with shared feature-detail wrapper | Mostly direct Markdown with provider-detail wrapper | Reuse the same status and roadmap presentation pattern where applicable |
| `/providers/azurerm.html` | Provider detail page | Migrated to Markdown with shared feature-detail wrapper and code-window example | Mostly direct Markdown with provider-detail wrapper | Keep extracting shared provider-detail patterns instead of duplicating inline styles |
| `/providers/msgraph.html` | Provider detail page | Migrated to Markdown with shared feature-detail wrapper | Mostly direct Markdown with provider-detail wrapper | Minimal remaining work unless design parity changes |

## 4. Migration groups

The routes fall into five practical groups.

### Group A: Long-form content pages

These pages can move to Markdown first with minimal new abstraction.

1. `/architecture.html`
2. `/ai-workflow.html`
3. `/contributing.html`
4. `/features/custom-templates.html`
5. `/features/inline-diffs.html`
6. `/features/misc.html`
7. `/features/semantic-icons.html`
8. `/features/value-formatting.html`
9. `/providers/azuread.html`
10. `/providers/azuredevops.html`
11. `/providers/azurerm.html`
12. `/providers/msgraph.html`

Status: Completed for `/architecture.html`, `/ai-workflow.html`, `/contributing.html`, `/features/custom-templates.html`, `/features/inline-diffs.html`, `/features/misc.html`, `/features/semantic-icons.html`, `/features/value-formatting.html`, `/providers/azuread.html`, `/providers/azuredevops.html`, `/providers/azurerm.html`, and `/providers/msgraph.html`.

### Group B: Listing and card-driven pages

These pages should become data-driven once the shared card components are promoted from scaffolded macros to actual page usage.

1. `/features/index.html`
2. `/providers/index.html`
3. `/getting-started.html`

Status: Completed for `/features/index.html`, `/providers/index.html`, and `/getting-started.html`.

### Group C: Shared layout special cases

These pages require dedicated layout contracts before Markdown conversion.

1. `/docs.html`
2. `/index.html`

Status: Completed for `/docs.html` and `/index.html`.

### Group D: Example-heavy feature pages

These pages depend on a reusable comparison component and a stable example-block API.

1. `/features/firewall-rules.html`
2. `/features/nsg-rules.html`
3. `/features/module-grouping.html`
4. `/features/large-values.html`
5. `/features/sensitive-masking.html`
6. `/features/static-analysis.html`
7. `/features/azdo-variable-groups.html`
8. `/features/azure-optimizations.html`

Status: Completed for `/features/firewall-rules.html`, `/features/nsg-rules.html`, `/features/module-grouping.html`, `/features/large-values.html`, `/features/sensitive-masking.html`, `/features/static-analysis.html`, `/features/azdo-variable-groups.html`, and `/features/azure-optimizations.html`.

### Group E: Example gallery page

This page should migrate after the example-block contract is proven on feature detail pages.

1. `/examples.html`

Status: Completed for `/examples.html`.

## 5. Design-preserving migration rules

To keep the original layout and design intact during Markdown migration:

1. Keep existing CSS class names and DOM hooks unless a deliberate replacement component preserves the same styling contract.
2. Preserve section order exactly as it exists in the legacy pages.
3. Keep the current `style.css` as the rendering baseline until all body migrations are complete.
4. Replace body markup incrementally, not the visual system.
5. Move repeated inline styles into shared utility classes or component wrappers only when doing so is layout-neutral.
6. Keep all existing image assets, example fragments, CTA wording, and link targets unless there is a correctness issue.
7. Keep current JavaScript selectors and data attributes stable while converting markup.
8. Validate each migrated page against the parity checklist before deleting its generated legacy body source.

## 6. Required component work before page conversion

The remaining page migration depends on a small set of reusable building blocks.

### 6.1 Content wrappers

1. `hero-block`
2. `section-header`
3. `feature-detail`
4. `related-docs`
5. `callout`
6. `command-block`
7. `code-window`

### 6.2 Listing blocks

1. `feature-card`
2. `provider-card`
3. `realworld-card`
4. `small-card-grid`

### 6.3 Comparison and examples

1. `comparison-block`
2. `example-block`
3. `example-stack`
4. `screenshot-frame`

### 6.4 Page-specific layout helpers

1. `docs-layout`
2. `homepage-carousel`
3. `code-tabs`

## 7. Recommended execution order

### Phase 1: Freeze current parity contracts

Goal: convert safely without visual drift.

Tasks:

1. Treat the original `website/` implementation as the visual baseline.
2. Capture section outlines for every page before replacing the imported body.
3. Convert the unchecked parity checklist into route-specific acceptance checks.
4. Add a simple rule: a page is only considered migrated when it no longer uses `legacyContent`.

### Phase 2: Promote reusable components before page rewrites

Goal: avoid re-copying legacy HTML into Markdown.

Tasks:

1. Implement `hero-block`, `section-header`, `feature-detail`, `related-docs`, `comparison-block`, and `command-block` macros.
2. Replace existing ad hoc card markup by wiring `feature-card` and `provider-card` into one real page each.
3. Expand feature/provider data files so page bodies can reference structured metadata instead of hard-coded card HTML.
4. Define front matter conventions for badges, related docs, examples, and section backgrounds.

Exit criteria:

1. At least one listing page and one feature detail page render without `legacyContent`.
2. Shared class names remain compatible with existing CSS and JS.

### Phase 3: Convert Group A pages first

Goal: remove the easiest legacy imports and establish the authoring workflow.

Recommended order:

1. `/architecture.html`
2. `/ai-workflow.html`
3. `/contributing.html`
4. `/features/custom-templates.html`
5. `/features/misc.html`
6. `/features/inline-diffs.html`
7. `/features/semantic-icons.html`
8. `/features/value-formatting.html`
9. Provider detail pages

Why first:

1. These pages mostly need content transcription, section wrappers, code blocks, and related-doc links.
2. They prove the Markdown authoring model without requiring the example system.

### Phase 4: Convert data-driven listing pages

Goal: replace large repeated card markup with structured data and stable components.

Recommended order:

1. `/features/index.html`
2. `/providers/index.html`
3. `/getting-started.html`

Tasks:

1. Expand shared data for titles, descriptions, icons, statuses, badges, and CTA labels.
2. Render card grids from data rather than imported HTML.
3. Keep the exact current card layout, grid sizing, and icon usage.

### Phase 5: Convert special-layout pages

Goal: stabilize the two pages that define the rest of the authoring model.

Recommended order:

1. `/docs.html`
2. `/index.html`

Tasks:

1. Build a docs page model where navigation is generated from section metadata or explicit front matter.
2. Convert homepage sections into structured content blocks while preserving carousel behavior and screenshot lightbox behavior.
3. Keep the current homepage visual hierarchy exactly as-is until after complete parity.

### Phase 6: Convert example-heavy feature pages

Goal: replace the most complex legacy markup using a proven example system.

Recommended order:

1. `/features/firewall-rules.html`
2. `/features/nsg-rules.html`
3. `/features/module-grouping.html`
4. `/features/large-values.html`
5. `/features/sensitive-masking.html`
6. `/features/static-analysis.html`
7. `/features/azdo-variable-groups.html`
8. `/features/azure-optimizations.html`

Tasks:

1. Standardize `comparison-block` inputs for left title, left content, right example id, and notes.
2. Standardize `example-block` data shape for title, rendered HTML, source HTML, badges, and optional CTA.
3. Move repeated example section scaffolding into macros.
4. Keep generated example fragments as the content source until there is a deliberate follow-up plan to author examples directly.

### Phase 7: Convert `/examples.html` last

Goal: migrate the example gallery after the example system has already been proven elsewhere.

Tasks:

1. Replace inline example-section markup with section macros that read from centralized example metadata.
2. Keep the current grouping: report templates, feature examples, real-world examples, and CTA.
3. Preserve badge styling, description copy, and all existing example ordering.

## 8. Route completion definition

A route is fully migrated when all of the following are true:

1. Its page template no longer calls `legacyContent`.
2. Its main body content lives in Markdown or page-local Nunjucks partials under `website2/src/`.
3. Any examples used by the page are rendered through the shared `example-block` contract.
4. Navigation, theme toggle, mobile nav, code tabs, carousel, lightbox, and docs TOC still behave correctly where applicable.
5. The parity checklist for that route is checked off.

## 9. Immediate next steps

The planned route-by-route migration work is complete.

Follow-up work, if needed, should focus on optional cleanup rather than route conversion:

1. Extract repeated homepage, docs, and guide structures into additional shared macros only where it reduces clear maintenance burden.
2. Revisit whether generated example fragments should eventually become authored content rather than imported artifacts.
3. Continue using parity and verification checks as guardrails for future design or content changes.
