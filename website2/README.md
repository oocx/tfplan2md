# website2

This folder contains the implemented Eleventy-based replacement website for tfplan2md.

The legacy `website/` folder remains in the repository as historical reference during the transition, but the deployable website source now lives in `website2/` and builds to `website2/dist/`.

## Documents

- `specification.md` - Product and delivery specification for the new site
- `architecture.md` - Technical architecture for source structure, build pipeline, components, and deployment
- `implementation-plan.md` - Detailed phased migration and delivery plan
- `adrs/` - Open architectural decisions with alternatives, pros/cons, and recommendations
- `route-inventory.md` - In-scope route inventory used for migration tracking
- `shared-component-inventory.md` - Shared shell/component inventory for the migrated site
- `parity-checklists.md` - Parity checklist template expanded for each production route

## Status

This folder now contains:

- The Eleventy source tree in `src/`
- Shared layouts, data files, and JavaScript modules
- Generated parity inventories and checklists
- The built site output in `dist/` after `npm run build`

Useful commands:

- `npm run build`
- `npm run serve`
- `npm run import:legacy`
- `scripts/website2-verify.sh --all`
