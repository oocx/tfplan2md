# Website backlog

This file is the source of truth for all open website tasks.

## Rules
- Add new work here before starting implementation.
- Keep each task small and unambiguous.
- Update **Status** as work progresses.
- Close tasks by marking them **✅ Done** (do not delete rows).

## Open tasks

| ID | Title | Page(s) | Effort | Value | Status | Notes |
|---:|---|---|---|---|---|---|
| 1 | Update Friendly Resource Names icon | `/features/` | Low | Medium | ⬜ Not started | Change icon from 🏷️ to 🆔 to avoid conflict with Tag Visualization which uses 🏷️ (matches how tags are visualized in product). |
| 2 | Create homepage | `/index.html` | High | High | ⬜ Not started | Currently missing. features/index.html links to `../index.html` which doesn't exist. |
| 3 | Create getting-started page | `/getting-started.html` | Medium | High | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 4 | Create docs page | `/docs.html` | Medium | Medium | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 5 | Create examples page | `/examples.html` | Medium | Medium | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 6 | Create providers index | `/providers/index.html` | Medium | Medium | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 7 | Create architecture page | `/architecture.html` | Medium | Low | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 8 | Create contributing page | `/contributing.html` | Low | Low | ⬜ Not started | Currently missing. Linked from nav but doesn't exist. |
| 9 | Create feature detail pages | `/features/*.html` | High | Medium | ⬜ Not started | 8 feature pages linked but not created: firewall-rules, nsg-rules, azure-optimizations, large-values, misc, module-grouping, semantic-icons, custom-templates. |
| 10 | Add SVG icons to assets | `/assets/icons/` | Medium | Medium | ⬜ Not started | feature-definitions.md references SVG icons (e.g., `assets/icons/firewall-rule-interpretation.svg`) but folder is empty. |
| 11 | Generate screenshots | `/assets/screenshots/` | Medium | High | ⬜ Not started | No screenshots exist yet. Need to generate from comprehensive-demo artifacts. |
| 12 | Redesign logo | All pages | High | Medium | ⬜ Not started | Current logo not suitable for small sizes. Needs complete redesign (not based on old one). Must work at navbar scale. |
| 13 | Replace mockup examples | `/examples.html` | Medium | High | ⬜ Not started | Examples page must use real tfplan2md reports, not mockups. |
| 14 | Implement example viewer | `/examples.html` | High | High | ⬜ Not started | Show examples in rendered + source views with toggle. Markdown syntax highlighting. Full screen button. Azure DevOps PR style for rendered view. |
| 15 | Add Dark/Light Mode feature | `/features/` | Low | Low | ⬜ Not started | Add Dark/Light Mode to feature list on features page. |
| 16 | Move CI/CD to Built-In Capabilities | `/features/` | Low | Medium | ⬜ Not started | CI/CD Integration should be in "Built-In Capabilities" (Medium), not "What Sets Us Apart" (High). Website currently shows it in wrong section. |
| 17 | Preserve design variations | N/A | Low | Low | ⬜ Not started | All design variations (1-6 plus refinements) should be preserved for future reference per maintainer request. |

## Effort Scale
- **Low**: < 15 minutes
- **Medium**: 15-45 minutes  
- **High**: > 45 minutes

## Value Scale
- **Low**: Nice-to-have
- **Medium**: Improves UX/maintainability
- **High**: Essential for site functionality or fixes known issues
