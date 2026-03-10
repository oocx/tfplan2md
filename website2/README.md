# website2

This folder contains the implemented Eleventy-based replacement website for tfplan2md.

The legacy `website/` folder remains in the repository as historical reference during the transition, but the deployable website source now lives in `website2/` and builds to `website2/dist/`.

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
- Shared migration components now used by real routes (`hero-block`, `section-header`, `feature-detail`, `comparison-block`, `related-docs`, `code-window`, `feature-card`, `provider-card`, `example-block`)
- Generated parity inventories and checklists
- The built site output in `dist/` after `npm run build`

Current migration progress:

- 26 routes no longer use `legacyContent`
- 0 routes still need native Markdown migration
- `scripts/website2-verify.sh --all` passes with the migrated routes in place

Useful commands:

- `npm run build`
- `npm run serve`
- `npm run import:legacy`
- `scripts/website2-verify.sh --all`
