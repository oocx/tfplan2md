# website

This folder contains the Eleventy-based production website for tfplan2md.

The legacy hand-authored website is archived in `../website.old/` as historical reference. The deployable website source now lives here and builds to `website/dist/`.

## Documents

- `specification.md` - Product and delivery specification for the new site
- `architecture.md` - Technical architecture for source structure, build pipeline, components, and deployment
- `implementation-plan.md` - Detailed phased migration and delivery plan
- `markdown-migration-plan.md` - Page-by-page comparison and plan for replacing imported legacy HTML with native Markdown sources
- `adrs/` - Open architectural decisions with alternatives, pros/cons, and recommendations
- `route-inventory.md` - In-scope route inventory used for migration tracking
- `shared-component-inventory.md` - Shared shell/component inventory for the migrated site
- `parity-checklists.md` - Parity checklist template expanded for each production route

## Status

This folder now contains:

- The Eleventy source tree in `src/`
- Shared layouts, data files, and JavaScript modules
- Source-managed interactive example fragments in `src/examples/`
- Shared migration components now used by real routes (`hero-block`, `section-header`, `feature-detail`, `comparison-block`, `related-docs`, `code-window`, `feature-card`, `provider-card`, `example-block`)
- Generated parity inventories and checklists
- The built site output in `dist/` after `npm run build`

Current state:

- The Eleventy migration is complete
- All production routes are authored from the Eleventy source tree in `src/`
- Legacy imported page-body artifacts have been removed from `src/_generated/`
- `scripts/website-verify.sh --all` is the canonical local verification command

Useful commands:

- `npm run build`
- `npm run serve`
- `scripts/website-build.sh --all`
- `scripts/website-verify.sh --all`
