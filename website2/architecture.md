# website2 Architecture

## 1. Scope

This document describes the proposed architecture for `website2`, the next-generation static website for tfplan2md.

It focuses on source organization, content flow, reusable components, static generation, progressive enhancement, and migration coexistence with the current `website/` folder.

## 2. Architectural drivers

The architecture is driven by the following repository realities:

1. The current site is static and content-heavy, not application-heavy.
2. Many pages duplicate shared chrome and interactive example markup.
3. The repository already has strong documentation and artifact sources that should be reused.
4. AI agents must be able to make small, safe changes without understanding a large frontend framework.
5. The implementation should favor semantic HTML and modern CSS over framework abstractions.

## 3. System context

### 3.1 Inputs

`website2` consumes:

1. Page content authored in Markdown.
2. Shared metadata and site configuration stored in data files.
3. Reusable templates and components.
4. Existing project documentation from `README.md`, `docs/`, `examples/`, and `artifacts/`.
5. Static assets such as screenshots, icons, and logos.

### 3.2 Outputs

`website2` produces:

1. Static HTML pages
2. Static CSS and JavaScript assets
3. Static media assets
4. Optional generated metadata such as sitemaps or feeds, if later required

## 4. Technology baseline

The selected generator is Eleventy. See [adrs/adr-001-static-site-generator-selection.md](adrs/adr-001-static-site-generator-selection.md).

### 4.1 Recommended stack

| Concern | Recommendation |
| --- | --- |
| Static site generator | Eleventy |
| Primary page authoring | Markdown |
| Layout/component templates | Nunjucks includes and macros |
| Shared data | JSON, YAML, or JS data files |
| Styling | Native CSS with custom properties, layers, and modern layout features |
| Client-side behavior | Small vanilla JavaScript modules |
| Syntax highlighting | Optional lightweight library or pre-rendered markup, decided later |

## 5. High-level building blocks

### 5.1 Content layer

Markdown files define page content, front matter, page metadata, and component usage.

Responsibilities:

1. Hold page-specific copy.
2. Reference shared components instead of repeating HTML scaffolding.
3. Remain readable in raw form.
4. Support 1:1 migration of the current site, with template escape hatches where Markdown alone is not sufficient.

### 5.2 Layout layer

Layouts wrap page content with shared HTML document structure.

Responsibilities:

1. `<html>`, `<head>`, metadata, canonical links, and main document shell.
2. Shared navbar and footer inclusion.
3. Page-level body classes and theme hooks.

### 5.3 Component layer

Components render reusable content blocks.

Responsibilities:

1. Render consistent HTML for shared UI blocks.
2. Accept simple inputs from Markdown or front matter.
3. Keep rendered markup semantic and predictable.

Key components:

1. `nav`
2. `footer`
3. `hero`
4. `feature-card`
5. `provider-card`
6. `example-block`
7. `lightbox-image`
8. `callout`

### 5.4 Data layer

Shared data files hold structured content reused across pages.

Responsibilities:

1. Navigation items
2. Footer links
3. Feature metadata
4. Provider metadata
5. Example definitions and asset references

### 5.5 Asset layer

Static assets are copied through the build without runtime processing unless optimization is explicitly added later.

Responsibilities:

1. Images
2. SVG icons
3. Screenshots
4. Fonts, if ever needed
5. Static JavaScript modules
6. Global CSS

### 5.6 Progressive enhancement layer

Small JavaScript modules enhance already-usable HTML.

Responsibilities:

1. Theme toggle
2. Mobile navigation toggle
3. Example tabs
4. Fullscreen behavior for examples
5. Optional lightbox handling

Rule: page content must remain understandable without JavaScript where practical.

## 6. Proposed directory layout

```text
website2/
  README.md
  specification.md
  architecture.md
  adrs/
    adr-001-static-site-generator-selection.md
    adr-002-authoring-model.md
    adr-003-example-component-and-content-model.md
    adr-004-build-output-and-migration-layout.md
    adr-005-browser-baseline-and-styling.md
  src/
    _data/
      site.json
      navigation.json
      footer.json
      features.json
      providers.json
      examples/
    _includes/
      layouts/
      partials/
      components/
    assets/
      css/
      js/
      images/
      screenshots/
    pages/
      index.md
      examples.md
      docs.md
      architecture.md
      ai-workflow.md
      contributing.md
      getting-started.md
      features/
      providers/
  dist/
```

Notes:

1. `src/` is authored source.
2. `dist/` is generated output.
3. `website/` remains untouched as the active legacy site until cutover.

## 7. Content rendering flow

### 7.1 Static build flow

1. Generator loads shared data.
2. Generator reads Markdown pages and front matter.
3. Layouts and components are applied.
4. Component calls pull data from page front matter and shared example definitions.
5. Final HTML is written to `dist/`.
6. Static assets are copied to `dist/`.

### 7.2 Example block rendering flow

1. A page references an example by identifier or inline component arguments.
2. The `example-block` component looks up shared data and content fragments.
3. The component renders semantic HTML for header, controls, views, and body.
4. A small enhancement script binds tab switching and fullscreen behavior.

This removes the repeated inline markup currently duplicated across many pages.

## 8. Recommended component contract

### 8.1 Example block inputs

The example block should accept the following inputs:

| Input | Purpose |
| --- | --- |
| `id` | Stable reference to shared example data |
| `title` | Optional override for display title |
| `badge` | Optional label such as template name or release tag |
| `description` | Optional supporting text |
| `renderedHtml` | Rendered HTML fragment for the rendered tab |
| `sourceMarkdown` | Markdown source for the source tab |
| `variant` | Optional style variant |
| `cta` | Optional link metadata |

### 8.2 Example block rules

1. The component owns its wrapper markup.
2. Page authors should not manually recreate tab controls.
3. Shared behavior must be driven by stable data attributes rather than page-specific scripts.
4. If the same logical example appears on multiple pages, the source data should live in one place.

## 9. CSS architecture

The CSS architecture should keep site styling separate from embedded rendered examples.

Recommended structure:

1. Token layer for colors, spacing, typography, and radii.
2. Base layer for resets and low-level elements.
3. Website component layer for navigation, cards, sections, and page chrome.
4. Example layer for embedded rendered report approximations.
5. Utility layer only if genuinely needed and kept very small.

This continues the intent of the current CSS layer separation while making it easier to reason about in a generated site.

The primary fidelity target is latest Chromium-based browsers. Outside that baseline, reduced fidelity for visuals and animations is acceptable if core content and interactions remain usable.

## 10. JavaScript architecture

JavaScript should be split into small focused modules rather than page-local inline scripts.

Recommended modules:

1. `theme-toggle.js`
2. `mobile-nav.js`
3. `example-tabs.js`
4. `example-fullscreen.js`
5. `lightbox.js` if still needed

Rules:

1. No global page-specific script blobs unless there is no cleaner alternative.
2. Event binding should use stable selectors or data attributes.
3. Enhancements should fail safely without breaking the base content.

## 11. Deployment architecture

### 11.1 Local preview

Local preview should build `website2/dist/` and serve only the generated output, not the source templates directly.

### 11.2 GitHub Pages

Recommended deployment path:

1. CI builds the static site from `website2/src/`.
2. CI uploads or deploys `website2/dist/`.
3. The production site remains on the current `website/` pipeline until migration is complete.
4. At cutover, the deployment source switches from the legacy website build to the `website2` build.

## 12. Verification architecture

`website2` should support a repeatable verification flow:

1. Lint templates, Markdown, CSS, and JS.
2. Build generated output.
3. Run link and structural validation on generated HTML.
4. Preview generated pages locally.
5. Use DevTools to verify console status and responsive layout.
6. Capture screenshots for changed pages in light and dark themes.

## 13. Migration strategy

### 13.1 Phase 1: Documentation and scaffolding

1. Define specification and architecture.
2. Confirm ADRs.
3. Scaffold the generator and source structure.

### 13.2 Phase 2: Shared shell and utilities

1. Migrate navbar, footer, theme toggle, and page layout.
2. Move page-local scripts into shared modules.
3. Define page parity criteria for each migrated page.

### 13.3 Phase 3: Example system

1. Implement `example-block` component.
2. Centralize example data and fragments.
3. Migrate pages that currently duplicate rendered/source markup.

### 13.4 Phase 4: Page migration

1. Migrate simple pages first.
2. Migrate example-heavy and feature-heavy pages next.
3. Compare output and fix styling regressions.
4. Do not introduce intentional redesign changes until parity is established.

### 13.5 Phase 5: Cutover

1. Switch deployment to `website2` output.
2. Remove legacy `website/` source only after parity is confirmed.

## 14. Risks

1. Introducing too many template syntaxes would reduce maintainability.
2. Overusing client-side behavior would undercut the static-first approach.
3. Treating all example fragments as inline page content would recreate current duplication problems.
4. Diverging from existing content strategy would make the site less trustworthy.
5. Failing to define what 1:1 parity means per page could create migration ambiguity.

## 15. ADR index

1. [adrs/adr-001-static-site-generator-selection.md](adrs/adr-001-static-site-generator-selection.md)
2. [adrs/adr-002-authoring-model.md](adrs/adr-002-authoring-model.md)
3. [adrs/adr-003-example-component-and-content-model.md](adrs/adr-003-example-component-and-content-model.md)
4. [adrs/adr-004-build-output-and-migration-layout.md](adrs/adr-004-build-output-and-migration-layout.md)
5. [adrs/adr-005-browser-baseline-and-styling.md](adrs/adr-005-browser-baseline-and-styling.md)
