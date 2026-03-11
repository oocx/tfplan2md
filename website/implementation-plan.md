# website2 Implementation Plan

## 1. Purpose

This document turns the accepted `website2` specification and architecture into an executable implementation and migration plan.

The goal is a 1:1 migration of the current production website into an Eleventy-based source structure under `website2`, without introducing intentional redesign work before parity is established.

## 2. Planning assumptions

1. `website/` remains the active production website until cutover.
2. `website2/` is implementation-in-progress and may be incomplete for a significant period.
3. Eleventy is the selected generator.
4. Markdown-first authoring is the default, but template-first sections and raw HTML are allowed where required for parity.
5. The primary fidelity target is latest Chromium-based browsers, with graceful degradation elsewhere.

## 3. Scope of migration

### 3.1 In scope for the initial 1:1 migration

The following production routes must be migrated to `website2`.

#### Top-level production routes

1. `/index.html`
2. `/getting-started.html`
3. `/docs.html`
4. `/examples.html`
5. `/architecture.html`
6. `/ai-workflow.html`
7. `/contributing.html`

#### Feature routes

1. `/features/index.html`
2. `/features/azdo-variable-groups.html`
3. `/features/azure-optimizations.html`
4. `/features/custom-templates.html`
5. `/features/firewall-rules.html`
6. `/features/inline-diffs.html`
7. `/features/large-values.html`
8. `/features/misc.html`
9. `/features/module-grouping.html`
10. `/features/nsg-rules.html`
11. `/features/semantic-icons.html`
12. `/features/sensitive-masking.html`
13. `/features/static-analysis.html`
14. `/features/value-formatting.html`

#### Provider routes

1. `/providers/index.html`
2. `/providers/azuread.html`
3. `/providers/azuredevops.html`
4. `/providers/azurerm.html`
5. `/providers/msgraph.html`

### 3.2 Out of scope for the initial 1:1 migration

The following content is not part of the initial cutover scope and should not block production-route parity.

1. `website/prototypes/**`
2. `website/assets/icons/*.html` comparison and gallery pages
3. `website/_memory/archived-designs/**`
4. Any experimental or reference-only HTML files not linked from the production navigation

These may be migrated later as archived reference material if they still provide value.

## 4. Definition of 1:1 parity

1:1 parity does not require byte-for-byte identical HTML. It requires equivalent user-visible behavior, information architecture, and styling intent.

### 4.1 Required parity dimensions

Each migrated page must preserve:

1. Route path and page purpose
2. Navigation presence and active-state behavior
3. Page title and core metadata intent
4. Section ordering and information hierarchy
5. All user-visible production content
6. Existing screenshots, examples, and CTAs unless explicitly superseded
7. Theme behavior
8. Mobile navigation behavior
9. Interactive example behavior where present
10. Footer links and shared chrome

### 4.2 Allowed differences before cutover

The following differences are acceptable before cutover if they do not alter user-visible intent:

1. Cleaner generated markup
2. Different internal source structure
3. Different script file organization
4. Improved accessibility markup
5. Reduced-fidelity animations or advanced styling outside the primary browser baseline

### 4.3 Not acceptable before cutover

1. Intentional redesign of page layout or content
2. Missing sections or missing example views
3. Broken navigation, theme toggle, or mobile menu
4. Reduced accessibility versus the current site
5. Removing routes that are currently part of production navigation

## 5. Deliverables

The implementation should produce the following concrete deliverables.

### 5.1 Foundation deliverables

1. `website2/package.json`
2. `website2/.eleventy.js` or Eleventy config equivalent
3. `website2/src/` source tree
4. `website2/dist/` generated output directory
5. Shared data files for navigation, footer, features, providers, and examples
6. Shared layout and component templates
7. Shared CSS and JavaScript modules

### 5.2 Migration deliverables

1. Migrated top-level pages
2. Migrated feature pages
3. Migrated provider pages
4. Centralized example data and fragments
5. Updated verification scripts for `website2`
6. Parallel CI build for `website2`

### 5.3 Cutover deliverables

1. Deployment switched to `website2/dist/`
2. Legacy `website/` removed or archived after parity verification
3. Documentation updated to refer to `website2` as the primary site source

## 6. Phase plan

## Phase 0: Baseline and parity inventory

### Goal

Create the migration baseline before any Eleventy implementation work begins.

### Tasks

1. Inventory all production routes and classify them by complexity.
2. Inventory all shared assets used by production routes.
3. Inventory all inline scripts and identify shared behavior versus page-specific behavior.
4. Identify shared repeated page blocks: navbar, footer, hero patterns, cards, example widgets, lightbox patterns.
5. Create page parity checklists for every in-scope route.

### Outputs

1. Route inventory table
2. Shared component inventory
3. Page parity checklist templates

### Exit criteria

1. Every in-scope route is classified.
2. Every current shared behavior has an intended target in the new architecture.

## Phase 1: Eleventy scaffolding

### Goal

Establish the generator, source layout, and build scripts without migrating page content yet.

### Tasks

1. Initialize `website2/package.json` with Eleventy and minimal supporting packages.
2. Create Eleventy configuration for source, includes, passthrough copy, and output directories.
3. Create the initial `src/` tree.
4. Define passthrough handling for existing assets that can be reused immediately.
5. Create basic build, clean, serve, and lint scripts.
6. Add ignore rules for generated output.

### Outputs

1. Working Eleventy build
2. Empty but valid generated site in `dist/`
3. Initial development scripts

### Exit criteria

1. `website2` builds locally without route content.
2. Source and output separation is working as designed.

## Phase 2: Shared shell and global behavior

### Goal

Recreate the shared site shell before individual page migration.

### Tasks

1. Create the base layout with document shell, metadata hooks, and body classes.
2. Create shared navbar partial and central navigation data.
3. Create shared footer partial and central footer data.
4. Move theme toggle behavior into a shared JavaScript module.
5. Move mobile menu behavior into a shared JavaScript module.
6. Move shared CSS tokens, layers, and global styles into the new asset structure.
7. Recreate the current light/dark theme behavior.

### Outputs

1. `base` layout
2. `nav` partial
3. `footer` partial
4. Shared theme and mobile-nav scripts
5. Shared global CSS

### Exit criteria

1. A placeholder page can render with correct shared chrome.
2. Theme toggle and mobile nav work in the generated site.
3. Shared shell behavior matches the current website.

## Phase 3: Shared content model and components

### Goal

Create the reusable abstractions needed for page migration.

### Tasks

1. Define shared data files for navigation, footer, features, providers, and common CTAs.
2. Create reusable component templates for hero blocks, feature cards, provider cards, callouts, and screenshot blocks.
3. Implement the centralized `example-block` component.
4. Define storage rules for example metadata, rendered HTML fragments, and source Markdown fragments.
5. Move `interactive-examples.js` behavior into modular scripts for tabs and fullscreen.
6. Define conventions for page-local overrides when a component is insufficient.

### Outputs

1. Shared data model
2. Shared component library
3. Centralized example component and supporting scripts

### Exit criteria

1. Example-heavy content can be rendered without inline duplicated control markup.
2. At least one reference page can consume the new components successfully.

## Phase 4: Pilot migration pages

### Goal

Migrate a small representative set of pages to prove the architecture before bulk migration.

### Recommended pilot set

1. `/ai-workflow.html` as a content-heavy page with standard shared chrome
2. `/providers/index.html` as a card-driven listing page
3. `/features/index.html` as a feature-grid page using shared data and cards

### Tasks

1. Migrate each pilot page into Markdown plus templates.
2. Compare generated output with the existing page.
3. Fix gaps in layouts, spacing, cards, and metadata handling.
4. Update component contracts if the pilot exposes missing flexibility.

### Exit criteria

1. Three page types are proven in the new architecture.
2. No major architectural blockers remain for bulk migration.

## Phase 5: Bulk migration batch A

### Goal

Migrate low-to-medium complexity pages with limited interactive content.

### Batch A pages

1. `/architecture.html`
2. `/contributing.html`
3. `/getting-started.html`
4. `/docs.html`
5. `/providers/azuread.html`
6. `/providers/azuredevops.html`
7. `/providers/azurerm.html`
8. `/providers/msgraph.html`

### Focus

1. Validate long-form Markdown authoring.
2. Validate provider metadata patterns.
3. Validate page-specific script handling where needed.

### Exit criteria

1. All Batch A routes pass parity review.
2. Shared shell and data model hold up under long-form content pages.

## Phase 6: Bulk migration batch B

### Goal

Migrate the homepage and non-example-heavy feature pages.

### Batch B pages

1. `/index.html`
2. `/features/misc.html`
3. `/features/semantic-icons.html`
4. `/features/value-formatting.html`
5. `/features/custom-templates.html`
6. `/features/inline-diffs.html`

### Focus

1. Homepage parity for hero, screenshots, CTA, and problem/solution sections.
2. Validate section-level component reuse for feature detail pages.
3. Preserve current visual hierarchy without redesign.

### Exit criteria

1. Homepage parity is verified.
2. Feature page section patterns are stable enough for more complex routes.

## Phase 7: Bulk migration batch C

### Goal

Migrate example-heavy and interaction-heavy routes.

### Batch C pages

1. `/examples.html`
2. `/features/firewall-rules.html`
3. `/features/nsg-rules.html`
4. `/features/module-grouping.html`
5. `/features/sensitive-masking.html`
6. `/features/azure-optimizations.html`
7. `/features/large-values.html`
8. `/features/static-analysis.html`
9. `/features/azdo-variable-groups.html`

### Focus

1. Centralized example-block fidelity.
2. Source/rendered tab parity.
3. Fullscreen parity.
4. Example fragment reuse across multiple pages.

### Exit criteria

1. Example interactions behave consistently across all migrated pages.
2. No page duplicates old inline example scaffolding.

## Phase 8: Verification hardening and CI

### Goal

Make `website2` verifiable and safe to run in parallel with the legacy site.

### Tasks

1. Add `website2` build verification script.
2. Add linting for Markdown, templates, CSS, and JS.
3. Add link checking for generated output.
4. Add preview workflow targeting generated output.
5. Add CI job that builds `website2` in parallel without affecting production deployment.
6. Add optional screenshot verification workflow for changed pages.

### Exit criteria

1. `website2` can be built and validated in CI.
2. Verification is strong enough to catch migration regressions before cutover.

## Phase 9: Cutover readiness review

### Goal

Explicitly confirm that `website2` is ready to replace `website/`.

### Tasks

1. Complete the parity checklist for every in-scope production page.
2. Validate shared navigation, theme behavior, examples, and footer links across the full site.
3. Confirm no production route exists only in the legacy site.
4. Confirm documentation and scripts reference `website2` as the new source of truth.
5. Prepare the cutover change set separately from the migration work.

### Exit criteria

1. All in-scope pages have signed-off parity.
2. CI and verification are green.
3. Cutover is a mechanical switch rather than a redesign step.

## Phase 10: Cutover and cleanup

### Goal

Promote `website2` to the production website source.

### Tasks

1. Switch deployment to `website2/dist/`.
2. Remove or archive the legacy `website/` source.
3. Update contributing and website maintenance documentation.
4. Remove migration-only compatibility code that is no longer needed.

### Exit criteria

1. Production deployment uses `website2` only.
2. Legacy source is no longer the active website path.

## 7. Page complexity classification

### 7.1 Low complexity

1. `/architecture.html`
2. `/providers/index.html`
3. `/providers/azuread.html`
4. `/providers/azuredevops.html`
5. `/providers/azurerm.html`
6. `/providers/msgraph.html`

### 7.2 Medium complexity

1. `/ai-workflow.html`
2. `/contributing.html`
3. `/getting-started.html`
4. `/docs.html`
5. `/features/index.html`
6. `/features/misc.html`
7. `/features/semantic-icons.html`
8. `/features/value-formatting.html`
9. `/features/custom-templates.html`
10. `/features/inline-diffs.html`
11. `/index.html`

### 7.3 High complexity

1. `/examples.html`
2. `/features/firewall-rules.html`
3. `/features/nsg-rules.html`
4. `/features/module-grouping.html`
5. `/features/sensitive-masking.html`
6. `/features/azure-optimizations.html`
7. `/features/large-values.html`
8. `/features/static-analysis.html`
9. `/features/azdo-variable-groups.html`

## 8. Shared component migration order

Shared components should be implemented in this order:

1. Base layout
2. Navigation
3. Footer
4. Theme toggle
5. Mobile navigation
6. Hero block
7. Feature and provider card components
8. Screenshot and lightbox wrapper
9. Example-block component
10. Page-specific helper components, if still needed

This order intentionally delivers the page shell before the complex example system.

## 9. Data model implementation order

1. `site` metadata
2. Navigation data
3. Footer data
4. Feature metadata
5. Provider metadata
6. Example metadata
7. Shared CTA and badge data if reuse justifies it

The rule is simple: only introduce a shared data file when at least two pages benefit from the same structured data.

## 10. Verification gates

## Gate A: Foundation gate

Required before migrating pilot pages:

1. Eleventy build works locally.
2. Shared shell renders.
3. Theme toggle works.
4. Mobile navigation works.

## Gate B: Pilot gate

Required before bulk page migration:

1. One content-heavy page passes parity.
2. One listing page passes parity.
3. One feature-grid page passes parity.

## Gate C: Example gate

Required before migrating example-heavy pages:

1. `example-block` supports rendered/source tabs.
2. Fullscreen behavior works.
3. Example fragments are centralized and reusable.

## Gate D: Cutover gate

Required before replacing the legacy site:

1. All in-scope production pages migrated.
2. All parity checklists completed.
3. No new problems introduced in validation tools.
4. DevTools checks clean on representative pages.
5. Screenshot checks completed for changed page groups.

## 11. Parity checklist template

Each route should be reviewed against the same checklist.

1. Route exists at the correct output path.
2. Title and key metadata match intent.
3. Navigation links and active state are correct.
4. Footer links are correct.
5. Theme toggle works.
6. Mobile navigation works.
7. Section order matches the legacy page.
8. Screenshots and assets load correctly.
9. Internal and external links are correct.
10. Interactive examples behave correctly, if present.
11. Content is complete and not simplified accidentally.
12. Desktop and mobile layouts are acceptable.

## 12. CI and script plan

### 12.1 New scripts to add

1. `website2` build script
2. `website2` clean script
3. `website2` preview script
4. `website2` lint script
5. `website2` verify script

### 12.2 CI plan

1. Add a non-deploying CI job for `website2` early in the migration.
2. Keep the legacy `website/` deployment untouched until cutover.
3. Switch deployment only after Gate D is complete.

## 13. Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Markdown alone cannot reproduce a page exactly | Parity delays | Allow page-level template sections and raw HTML escapes |
| Example system becomes too abstract | Slower migration | Start with one concrete example-block contract and expand only when needed |
| Shared data model grows too early | Complexity creep | Only centralize data reused by multiple pages |
| Bulk migration hides regressions | Harder review | Use phased batches with parity gates |
| Legacy and website2 drift during long migration | Rework | Prefer short migration batches and reconcile page changes quickly |

## 14. Recommended execution order

If implementation begins immediately, the recommended order is:

1. Phase 1: Eleventy scaffolding
2. Phase 2: Shared shell and global behavior
3. Phase 3: Shared content model and components
4. Phase 4: Pilot migration pages
5. Phase 5: Bulk migration batch A
6. Phase 6: Bulk migration batch B
7. Phase 7: Bulk migration batch C
8. Phase 8: Verification hardening and CI
9. Phase 9: Cutover readiness review
10. Phase 10: Cutover and cleanup

## 15. Definition of done for this plan

This plan is complete when it provides:

1. A route-complete migration scope
2. A phased implementation order
3. A component and data migration sequence
4. Explicit parity and verification gates
5. A clear separation between initial migration scope and later archival/reference work
