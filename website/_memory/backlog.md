# Website backlog

This file is the source of truth for all open website tasks.

## Rules
- Add new work here before starting implementation.
- Keep each task small and unambiguous.
- Update **Status** as work progresses.
- Close tasks by marking them **✅ Done** (do not delete rows).

## Open tasks

### #1 — Update Friendly Resource Names icon

| Field | Value |
|-------|-------|
| Page(s) | `/features/index.html` |
| Effort | Low |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Change the Friendly Resource Names feature icon from 🏷️ to 🆔. Tag Visualization uses 🏷️ (which matches how tags are visualized in the product), so Friendly Resource Names needs a different icon to avoid conflict.

**Definition of Done:**
- [x] Icon changed from 🏷️ to 🆔 in `/features/index.html`
- [x] No duplicate icons exist on the features page

---

### #2 — Create homepage

| Field | Value |
|-------|-------|
| Page(s) | `/index.html` |
| Effort | High |
| Value | High |
| Status | ✅ Done |
| Depends On | #10 |

**Description:** Create the main homepage for tfplan2md. The page should include: badges at top ("Built 100% with GitHub Copilot", "Docker Ready", "Free & Open Source"), hero section with problem statement (raw terraform plan output in build log vs. PR comment with rendered tfplan2md output), feature showcase with screenshots, and clear "Get Started" CTA. Initially use placeholder images; actual screenshots will be generated after page content is defined.

**Definition of Done:**
- [x] Page created at `/index.html`
- [x] Hero section with before/after visual comparison
- [x] Feature showcase section with screenshots
- [x] "Get Started" CTA linking to `/getting-started.html`
- [x] Badges linking to relevant pages (Copilot badge → contributing, Docker → Docker Hub)
- [x] Navigation working correctly
- [x] Responsive on mobile, tablet, desktop
- [x] Content derived from README.md (no marketing fluff)

---

### #3 — Create getting-started page

| Field | Value |
|-------|-------|
| Page(s) | `/getting-started.html` |
| Effort | Medium |
| Value | High |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the installation and first usage guide. Content should include Docker installation, first usage example, and CI/CD integration snippets for GitHub Actions and Azure Pipelines. Code snippets must be copy/paste ready.

**Definition of Done:**
- [x] Page created at `/getting-started.html`
- [x] Docker installation instructions with copy/paste command
- [x] First usage example with sample command
- [x] GitHub Actions integration snippet
- [x] Azure Pipelines integration snippet
- [x] All code snippets are copy/paste ready
- [x] Navigation working correctly
- [x] Responsive design
- [x] Content derived from README.md Quick Start section

---

### #4 — Create docs page

| Field | Value |
|-------|-------|
| Page(s) | `/docs.html` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the documentation page with CLI reference and template customization guide. This is a reference page for users who need detailed usage information.

**Definition of Done:**
- [x] Page created at `/docs.html`
- [x] CLI reference section with all options documented
- [x] Template customization guide section
- [x] Troubleshooting section (if applicable)
- [x] Navigation working correctly
- [x] Responsive design
- [x] Content derived from docs/spec.md and docs/features.md

---

### #5 — Create examples page

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | Medium |
| Value | Medium |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Create the examples page structure with placeholder examples. This page will host interactive examples showing tfplan2md output. Initially use mockup content to define what examples are needed; real examples will replace placeholders later (#13). The example viewer functionality is a separate task (#14).

**Definition of Done:**
- [ ] Page created at `/examples.html`
- [ ] Page structure ready to host example viewer component
- [ ] Navigation working correctly
- [ ] Responsive design
- [ ] Links to example sources in repo (examples/comprehensive-demo/)

---

### #6 — Create providers index

| Field | Value |
|-------|-------|
| Page(s) | `/providers/index.html` |
| Effort | Medium |
| Value | Medium |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Create the providers index page listing provider-specific template documentation. Should cover azurerm, azuredevops, azuread, and msgraph providers with links to detailed pages (if created) or inline documentation.

**Definition of Done:**
- [ ] Page created at `/providers/index.html`
- [ ] Lists all supported providers: azurerm, azuredevops, azuread, msgraph
- [ ] Brief description of provider-specific features for each
- [ ] Navigation working correctly
- [ ] Responsive design
- [ ] Content derived from docs/features.md provider sections

---

### #7 — Create architecture page

| Field | Value |
|-------|-------|
| Page(s) | `/architecture.html` |
| Effort | Medium |
| Value | Low |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Create the architecture page with high-level system overview based on arc42 documentation. Target audience is contributors who want to understand the codebase.

**Definition of Done:**
- [ ] Page created at `/architecture.html`
- [ ] High-level system overview diagram or description
- [ ] Key architectural decisions explained
- [ ] Navigation working correctly
- [ ] Responsive design
- [ ] Content derived from docs/architecture.md

---

### #8 — Create contributing page

| Field | Value |
|-------|-------|
| Page(s) | `/contributing.html` |
| Effort | Low |
| Value | Low |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Create the contributing page explaining how to contribute, development setup, and the AI workflow. The "Built 100% with GitHub Copilot" badge on homepage should link here.

**Definition of Done:**
- [ ] Page created at `/contributing.html`
- [ ] How to contribute section
- [ ] Development setup instructions
- [ ] AI workflow explanation (multi-agent approach from agents.md)
- [ ] Link to GitHub repository
- [ ] Navigation working correctly
- [ ] Responsive design
- [ ] Content derived from CONTRIBUTING.md and docs/agents.md

---

### #9 — Create feature detail pages

| Field | Value |
|-------|-------|
| Page(s) | `/features/*.html` |
| Effort | High |
| Value | Medium |
| Status | ⬜ Not started |
| Depends On | #10 |

**Description:** Create 8 feature detail pages linked from the features index: firewall-rules, nsg-rules, azure-optimizations, large-values, misc, module-grouping, semantic-icons, custom-templates. Each page should follow a common structure: hero section, problem explanation, solution explanation, visual examples, and usage information. Reference relevant docs/features/ specifications and architecture docs. Initially use placeholder images; actual screenshots will be generated after page content is defined.

**Definition of Done (per page):**
- [ ] Hero section with feature title and brief description
- [ ] Problem statement: what problem does this feature solve?
- [ ] Solution explanation: how does the feature solve it?
- [ ] Visual examples (screenshots showing the feature in action)
- [ ] Usage information / configuration help (where applicable)
- [ ] Links to relevant specification docs (docs/features/*.md)
- [ ] Links to relevant architecture docs (if applicable)
- [ ] Navigation working correctly
- [ ] Responsive design

**Pages to create:**
1. `/features/firewall-rules.html` — Firewall rule semantic diffing
2. `/features/nsg-rules.html` — Network security group rule diffing
3. `/features/azure-optimizations.html` — Azure role assignments with principal mapping
4. `/features/large-values.html` — Large attribute value formatting
5. `/features/misc.html` — Miscellaneous features (plan summary, collapsible details, friendly names, local names)
6. `/features/module-grouping.html` — Module hierarchy grouping
7. `/features/semantic-icons.html` — Smart iconography and tag visualization
8. `/features/custom-templates.html` — Custom Scriban templates

---

### #10 — Add SVG icons to assets

| Field | Value |
|-------|-------|
| Page(s) | `/assets/icons/` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create SVG icons for features as referenced in feature-definitions.md. Icons should work at small sizes and be consistent in style.

**Definition of Done:**
- [x] Icons created for all features that need them (per feature-definitions.md)
- [x] Icons placed in `/assets/icons/` directory
- [x] Icons work at navbar scale (small sizes)
- [x] Consistent visual style across all icons
- [x] Icons referenced correctly in feature pages

---

### #11 — Generate screenshots

| Field | Value |
|-------|-------|
| Page(s) | `/assets/screenshots/` |
| Effort | Medium |
| Value | High |
| Status | ⬜ Not started |
| Depends On | #2, #5, #9 |

**Description:** Generate screenshots from comprehensive-demo artifacts showing tfplan2md output. Screenshots should demonstrate key features and before/after comparisons. This task should be done after pages are created with placeholders, so we know exactly which screenshots are needed. Update the inventory in screenshots.md as pages are defined.

**Definition of Done:**
- [ ] Screenshots generated per inventory in `website/_memory/screenshots.md`
- [ ] Screenshots placed in `/assets/screenshots/` directory
- [ ] Before/after comparison screenshots for homepage hero
- [ ] Feature-specific screenshots for feature pages
- [ ] Screenshots optimized for web (appropriate file size)
- [ ] Alt text documented for each screenshot

---

### #12 — Redesign logo

| Field | Value |
|-------|-------|
| Page(s) | All pages |
| Effort | High |
| Value | Medium |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Create a new logo for tfplan2md. Current logo is not suitable for small sizes. The new logo must work at navbar scale and should not be based on the old design. Multiple design options will be explored with the Maintainer before final selection.

**Definition of Done:**
- [ ] Multiple logo options presented to Maintainer
- [ ] Selected logo works at navbar scale (small sizes)
- [ ] Logo works in both light and dark themes
- [ ] Logo placed in `/assets/` directory
- [ ] Logo integrated into navbar across all pages
- [ ] Archived rejected options in `website/_memory/archived-designs/`

---

### #13 — Replace mockup examples

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | Medium |
| Value | High |
| Status | ⬜ Not started |
| Depends On | #5, #11 |

**Description:** Ensure examples page uses real tfplan2md reports from the comprehensive-demo, not mockups. Examples should show actual output the tool produces.

**Definition of Done:**
- [ ] All examples on page are real tfplan2md output (not mockups)
- [ ] Examples sourced from `examples/comprehensive-demo/` or `artifacts/`
- [ ] Examples cover different feature demonstrations
- [ ] Source markdown files available for example viewer (#14)

---

### #14 — Implement example viewer

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | High |
| Value | High |
| Status | ⬜ Not started |
| Depends On | #5, #13 |

**Description:** Implement an interactive example viewer component that shows examples in both rendered and source code views. Users can toggle between views. Source view has markdown syntax highlighting. Rendered view approximates Azure DevOps Services PR style. Both views have a full screen button.

**Definition of Done:**
- [ ] Toggle to switch between "Rendered" and "Source" views
- [ ] Rendered view styled to approximate Azure DevOps PR rendering
- [ ] Source view shows raw markdown with syntax highlighting
- [ ] Full screen button for rendered view
- [ ] Full screen button for source view
- [ ] Works on mobile, tablet, desktop
- [ ] Keyboard accessible (toggle, full screen buttons)

---

### #15 — Add Dark/Light Mode feature

| Field | Value |
|-------|-------|
| Page(s) | `/features/index.html` |
| Effort | Low |
| Value | Low |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Add Dark/Light Mode to the feature list on the features index page under "Also Included" section. The site already has theme toggle functionality; this task adds it as a documented feature.

**Definition of Done:**
- [ ] Dark/Light Mode feature card added to "Also Included" section
- [ ] Appropriate icon assigned (per feature-definitions.md)
- [ ] Brief description of the feature

---

### #16 — Move CI/CD to Built-In Capabilities

| Field | Value |
|-------|-------|
| Page(s) | `/features/index.html` |
| Effort | Low |
| Value | Medium |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Move CI/CD Integration feature from "What Sets Us Apart" (High priority section) to "Built-In Capabilities" (Medium priority section). Per feature-definitions.md, CI/CD Integration is Medium value.

**Definition of Done:**
- [ ] CI/CD Integration card removed from "What Sets Us Apart" section
- [ ] CI/CD Integration card added to "Built-In Capabilities" section
- [ ] Card content and icon preserved

---

### #17 — Preserve design variations

| Field | Value |
|-------|-------|
| Page(s) | N/A |
| Effort | Low |
| Value | Low |
| Status | ⬜ Not started |
| Depends On | — |

**Description:** Archive all design variations (options 1-6 plus refinements) that were created during the design exploration phase. These should be preserved for future reference.

**Definition of Done:**
- [ ] All design variation files copied to `website/_memory/archived-designs/`
- [ ] Each variation clearly named/numbered
- [ ] Brief description file explaining each variation (if not self-evident)

---

## Effort Scale
- **Low**: < 15 minutes
- **Medium**: 15-45 minutes  
- **High**: > 45 minutes

## Value Scale
- **Low**: Nice-to-have
- **Medium**: Improves UX/maintainability
- **High**: Essential for site functionality or fixes known issues
