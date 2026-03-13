# website Specification

## 1. Purpose

`website` is the production website for tfplan2md.

It is implemented as an Eleventy-based static site that is intended to remain easy for both humans and AI agents to maintain. The legacy hand-authored site is retained under `website.old/` as historical reference only.

## 2. Goals

### 2.1 Primary goals

1. Preserve the current content strategy: technical, example-driven, and grounded in real tfplan2md output.
2. Remove duplicated page chrome and repeated example markup.
3. Centralize repeated content blocks and reusable UI components.
4. Prefer semantic HTML and native CSS features over frameworks.
5. Use a markdown-friendly authoring model that remains readable in code review.
6. Make the site easier to evolve incrementally without large-scale rewrites.

### 2.2 Secondary goals

1. Improve consistency of navigation, metadata, footer content, and page scaffolding.
2. Standardize how rendered/source examples are authored and regenerated.
3. Improve maintainability for AI-driven editing by reducing markup repetition and template sprawl.
4. Keep the build and deployment pipeline simple enough to understand without specialized frontend knowledge.

## 3. Non-goals

1. Rebranding tfplan2md or changing its content strategy.
2. Introducing a CSS framework or component library.
3. Building a client-heavy web application.
4. Rewriting tfplan2md product documentation outside website concerns.
5. Reintroducing the archived hand-authored website as the primary source.

## 4. Stakeholders

| Stakeholder | Need |
| --- | --- |
| Evaluators | Understand the value of tfplan2md through concrete examples |
| Users | Find installation, usage, examples, and provider coverage quickly |
| Power Users | Find template and provider-specific details without wading through marketing copy |
| Contributors | Understand website structure, authoring flow, and how to extend shared components |
| AI Agents | Modify content and structure safely with minimal ambiguity |

## 5. Source inputs and constraints

### 5.1 Authoritative content sources

The new site must derive content from the current project sources instead of inventing copy:

- `README.md`
- `docs/`
- `examples/`
- `artifacts/`
- `website.old/_memory/*.md` when historical reference is needed

### 5.2 Existing website constraints to preserve

1. Technical, example-driven writing style.
2. Real examples and screenshots only.
3. WCAG 2.1 AA-oriented accessibility baseline.
4. Minimal dependencies and simple maintenance.
5. Responsive layouts for mobile and desktop.

### 5.3 Additional constraints for website

1. No CSS framework.
2. Prefer semantic HTML5.
3. Prefer native CSS and browser features over abstraction layers.
4. Reusable content blocks must be centrally defined.
5. Authored source must remain under `website/src/` and generated output under `website/dist/`.

## 6. Browser support target

The primary fidelity target for `website` is the latest Chromium-based browsers.

Outside that baseline, including older Chromium versions and other evergreen browsers, reduced fidelity for visuals, animations, and advanced styling is acceptable as long as the site remains usable for reading, navigation, and core interactions.

This decision is documented in [adrs/adr-005-browser-baseline-and-styling.md](adrs/adr-005-browser-baseline-and-styling.md).

## 7. Information architecture

`website` should preserve the intent of the current top-level pages while simplifying authoring and reuse.

### 7.1 Required top-level routes

1. Home
2. Features index
3. Feature detail pages
4. Getting started
5. Docs
6. Examples
7. Providers index
8. Provider detail pages
9. Architecture
10. AI workflow
11. Contributing

### 7.2 Page intent rules

1. Home explains the problem and shows value with visual evidence.
2. Features pages explain capabilities with concise copy and real examples.
3. Examples pages prioritize rendered/source switching and fullscreen viewing.
4. Docs and architecture pages link to authoritative project documentation instead of duplicating it unnecessarily.
5. Contributing and AI workflow pages explain how the repository works and how to participate.

## 8. Authoring model requirements

1. Pages should be authored in Markdown by default.
2. Layouts, navigation, footer, metadata, and reusable chunks should be authored once and reused across pages.
3. Complex components may use template files, but the default path should remain content-first.
4. Authors must be able to embed reusable example widgets inside Markdown pages without copying large HTML blocks.
5. Raw HTML must remain available as an escape hatch for exceptional cases.
6. The authoring model must be capable of recreating the current website layout with 1:1 fidelity where required during migration.

The recommended authoring model is documented in [adrs/adr-002-authoring-model.md](adrs/adr-002-authoring-model.md).

## 9. Reusable component requirements

The new site must support centrally defined reusable components, including but not limited to:

1. Navbar
2. Footer
3. Hero blocks
4. Callout blocks
5. Feature cards
6. Provider cards
7. Example blocks with rendered/source tabs
8. Optional screenshot/lightbox wrappers

### 9.1 Example component requirements

The rendered/source example component must support:

1. Title
2. Optional badge or label
3. Description
4. Rendered HTML view
5. Source Markdown view
6. Toggle between views
7. Fullscreen mode
8. Consistent styling across all pages

The recommended example content model is documented in [adrs/adr-003-example-component-and-content-model.md](adrs/adr-003-example-component-and-content-model.md).

## 10. Styling requirements

1. Use semantic HTML and native CSS features.
2. Use design tokens through CSS custom properties.
3. Keep cascade control explicit using techniques such as cascade layers.
4. Use modern layout features such as grid, flexbox, container queries where appropriate, and logical properties when helpful.
5. Avoid JavaScript layout systems when CSS can solve the problem.
6. Preserve a technical visual language rather than a generic marketing style.
7. Treat latest Chromium as the primary fidelity baseline, with graceful degradation elsewhere.

The recommended styling and browser baseline strategy is documented in [adrs/adr-005-browser-baseline-and-styling.md](adrs/adr-005-browser-baseline-and-styling.md).

## 11. Content and data model requirements

1. Shared site data such as navigation items, footer links, CTAs, and feature metadata must be defined centrally.
2. Example content must not be duplicated across pages when the same logical example is reused.
3. Generated examples should trace back to real artifacts or source files.
4. The site should make it clear whether an example is sourced from GitHub rendering, Azure DevOps rendering, raw Markdown, or screenshot assets.

## 12. Build and deployment requirements

1. The site must be statically generated.
2. Local authoring must support deterministic builds.
3. Generated output should be separable from authored source.
4. GitHub Pages deployment must remain straightforward.
5. `website.old/` may remain in the repository, but it must not be treated as deployable source.

The recommended output and migration layout is documented in [adrs/adr-004-build-output-and-migration-layout.md](adrs/adr-004-build-output-and-migration-layout.md).

## 13. Verification requirements

The implementation of `website` must support the following verification workflow:

1. Static generation succeeds locally and in CI.
2. HTML, CSS, and JS linting can run on generated or source files as appropriate.
3. Link validation covers generated pages.
4. DevTools validation covers console cleanliness and responsive layout sanity.
5. Screenshot-based verification remains possible for changed pages.

## 14. Archive and maintenance requirements

### 14.1 Production source requirement

1. `website/` is the only deployable website source.
2. Authored content, layouts, data, styles, and enhancement scripts live under `website/src/` and shared config files under `website/`.
3. Generated output in `website/dist/` must be reproducible from source and verification scripts.

### 14.2 Legacy archive requirement

1. `website.old/` is retained only for historical comparison or migration traceability.
2. Changes to `website.old/` should be exceptional and explicitly requested.
3. Deployment, preview, and verification must target `website/`, not `website.old/`.

## 15. Success criteria

`website` is successful when:

1. Shared structures are defined once and reused everywhere.
2. Page authors mainly edit Markdown and small data files rather than repeated HTML scaffolding.
3. The rendered/source example component is centrally implemented and reused across all applicable pages.
4. The site remains easy to reason about for both humans and AI agents.
5. The migration can happen safely without destabilizing the current website.

## 16. Accepted architectural decisions

Accepted decisions are tracked in ADRs:

1. [adrs/adr-001-static-site-generator-selection.md](adrs/adr-001-static-site-generator-selection.md)
2. [adrs/adr-002-authoring-model.md](adrs/adr-002-authoring-model.md)
3. [adrs/adr-003-example-component-and-content-model.md](adrs/adr-003-example-component-and-content-model.md)
4. [adrs/adr-004-build-output-and-migration-layout.md](adrs/adr-004-build-output-and-migration-layout.md)
5. [adrs/adr-005-browser-baseline-and-styling.md](adrs/adr-005-browser-baseline-and-styling.md)
