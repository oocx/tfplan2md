# Website backlog

This file is the source of truth for all open website tasks.

## Rules
- Add new work here before starting implementation.
- Keep each task small and unambiguous.
- Update **Status** as work progresses.
- Close tasks by marking them **✅ Done** (do not delete rows).
- Tasks are sorted by ID in descending order (newest first).

## Open tasks

### #25 — Create tool to approximate "terraform show" output from JSON plans

| Field | Value |
|-------|-------|
| Page(s) | N/A (tooling) |
| Effort | High |
| Value | High |
| Status | 🔲 Not Started |
| Depends On | — |

**Description:** Create a tool that produces a close approximation of "terraform show" output based on our JSON plan files. This is needed because feature pages require "without tfplan2md" sections showing raw Terraform output, but we only have JSON format plans (not binary plans that `terraform show` requires).

**Requirements:**
- Analyze how `terraform show` displays plans
- Create tool that reads JSON plan format and produces similar text output
- Output should match Terraform's formatting conventions (indentation, symbols, colors if applicable)
- Support for all resource types used in comprehensive-demo

**Definition of Done:**
- [ ] Tool created that converts JSON plans to Terraform show-style output
- [ ] Output format closely matches real `terraform show` output
- [ ] Tool documented in code-examples.md with usage instructions
- [ ] Tool tested with comprehensive-demo plan

---

### #24 — Add "without tfplan2md" sections to all feature detail pages

| Field | Value |
|-------|-------|
| Page(s) | `/features/*.html` |
| Effort | High |
| Value | High |
| Status | 🔲 Blocked |
| Depends On | #25 |

**Description:** All feature detail pages must have a "without tfplan2md" section showing the output of "terraform show" for the same Terraform plan. This provides clear before/after comparison and demonstrates the value of tfplan2md.

**Pages to update:**
1. features/firewall-rules.html
2. features/nsg-rules.html
3. features/module-grouping.html
4. features/sensitive-masking.html
5. features/azure-optimizations.html
6. features/large-values.html

**Approach:**
1. Wait for #25 (tool to generate terraform show output from JSON)
2. Generate "terraform show" output for relevant sections from comprehensive-demo
3. Add comparison sections to each feature page showing raw Terraform output vs tfplan2md output
4. Use two-column layout or before/after format for clarity

**Definition of Done:**
- [ ] All 6 feature pages have "without tfplan2md" sections
- [ ] Raw Terraform output generated using tool from #25
- [ ] Visual comparison clearly demonstrates value of tfplan2md
- [ ] Examples use same source data (comprehensive-demo) for authenticity

---

### #23 — Replace homepage example with screenshot

| Field | Value |
|-------|-------|
| Page(s) | `/index.html` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Replace the interactive example on the homepage with a static screenshot showing comprehensive-demo output.

**Completed:** 2026-01-04
- Generated full-page comprehensive-demo screenshot (1920×3896px)
- Cropped 580×400px firewall rules section for homepage display
- Cropped 1200×800px firewall rules section for lightbox
- Firewall section shows semantic diffs with inline before/after changes
- Crops trim left whitespace (start at x=200) to show actual report content
- Replaced interactive example with firewall screenshot
- Implemented lightbox modal showing larger firewall view on click
- Lightbox supports ESC key, click outside, and close button
- Updated screenshots.md with generation and crop commands
- Removed complex interactive example JavaScript code
- Added lightbox CSS styles to style.css

---

|-------|-------|
| Page(s) | `/index.html`, `/features/*.html` |
| Effort | High |
| Value | High |
| Status | ✅ Done |
| Depends On | — |

**Description:** Task #18 replaced examples only on `/examples.html`. Other pages still have hand-crafted examples that should be replaced with real tfplan2md generated output for authenticity.

**Pages with examples to replace:**
1. **index.html (homepage)** - Hero section has simplified Network Security Rules example - should use real comprehensive-demo output
2. **features/firewall-rules.html** - "With tfplan2md" comparison example shows hand-crafted firewall table
3. **features/nsg-rules.html** - Has code-block example
4. **features/module-grouping.html** - Has example structure code-block
5. **features/sensitive-masking.html** - Has before/after comparison code-blocks
6. **features/azure-optimizations.html** - Has role assignment examples
7. **features/value-formatting.html** - Has formatting examples
8. **features/large-values.html** - Has large value example
9. **features/custom-templates.html** - Has template example
10. **features/misc.html** - Has various feature examples

**Approach:**
1. Extract relevant sections from `artifacts/comprehensive-demo.md` or generate new demo artifacts if needed
2. For homepage: use a compelling real example (firewall or NSG with clear before/after)
3. For feature pages: ensure examples accurately demonstrate the specific feature
4. Use HtmlRenderer tool with azdo flavor where rendered view is needed
5. Update code-examples.md with all extraction commands

**Definition of Done:**
- [ ] Homepage hero example replaced with real output
- [ ] All 9 feature detail pages reviewed and examples replaced where appropriate
- [ ] Examples accurately demonstrate their respective features
- [ ] All use authentic tfplan2md formatting from real artifacts
- [ ] code-examples.md updated with new extraction commands
- [ ] Visual consistency across all pages (Azure DevOps styling already applied via CSS)

---

### #21 — Update branch naming rules for website work

| Field | Value |
|-------|-------|
| Page(s) | N/A |
| Effort | Low |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Update documentation to specify that website work should use `website/<name>` branch naming instead of `feature/website-<name>`. This aligns with existing conventions (workflow/ for workflow changes) and makes website branches easily identifiable.

**Files to update:**
- `/docs/agents.md` — Branch naming conventions table
- `/.github/agents/web-designer.agent.md` — Example branch creation commands
- `/website/contributing.html` — Branch prefixes table (if present)
- `/CONTRIBUTING.md` — Branch prefixes documentation

**Definition of Done:**
- [x] `docs/agents.md` branch naming table updated with `website/` prefix
- [x] Web Designer agent examples use `website/<name>` format
- [x] All documentation consistent with new naming convention
- [x] CONTRIBUTING.md updated with website/ prefix
- [x] website/contributing.html updated with website/ prefix
- [x] Backlog updated to mark task as done

---

### #25 — Create tool to approximate "terraform show" output from JSON plans

| Field | Value |
|-------|-------|
| Page(s) | N/A (tooling) |
| Effort | High |
| Value | High |
| Status | 🔲 Not Started |
| Depends On | — |

**Description:** Create a tool that produces a close approximation of "terraform show" output based on our JSON plan files. This is needed because feature pages require "without tfplan2md" sections showing raw Terraform output, but we only have JSON format plans (not binary plans that `terraform show` requires).

**Requirements:**
- Analyze how `terraform show` displays plans
- Create tool that reads JSON plan format and produces similar text output
- Output should match Terraform's formatting conventions (indentation, symbols, colors if applicable)
- Support for all resource types used in comprehensive-demo

**Definition of Done:**
- [ ] Tool created that converts JSON plans to Terraform show-style output
- [ ] Output format closely matches real `terraform show` output
- [ ] Tool documented in code-examples.md with usage instructions
- [ ] Tool tested with comprehensive-demo plan

---

### #24 — Add "without tfplan2md" sections to all feature detail pages

| Field | Value |
|-------|-------|
| Page(s) | `/features/*.html` |
| Effort | High |
| Value | High |
| Status | 🔲 Blocked |
| Depends On | #25 |

**Description:** All feature detail pages must have a "without tfplan2md" section showing the output of "terraform show" for the same Terraform plan. This provides clear before/after comparison and demonstrates the value of tfplan2md.

**Pages to update:**
1. features/firewall-rules.html
2. features/nsg-rules.html
3. features/module-grouping.html
4. features/sensitive-masking.html
5. features/azure-optimizations.html
6. features/large-values.html

**Approach:**
1. Wait for #25 (tool to generate terraform show output from JSON)
2. Generate "terraform show" output for relevant sections from comprehensive-demo
3. Add comparison sections to each feature page showing raw Terraform output vs tfplan2md output
4. Use two-column layout or before/after format for clarity

**Definition of Done:**
- [ ] All 6 feature pages have "without tfplan2md" sections
- [ ] Raw Terraform output generated using tool from #25
- [ ] Visual comparison clearly demonstrates value of tfplan2md
- [ ] Examples use same source data (comprehensive-demo) for authenticity

---

### #23 — Replace homepage example with screenshot

| Field | Value |
|-------|-------|
| Page(s) | `/index.html` |
| Effort | Medium |
| Value | Medium |
| Status | 🔲 Not Started |
| Depends On | — |

**Description:** Replace the interactive example on the homepage with a static screenshot showing an NSG example from comprehensive-demo. The current interactive viewer is too complex for the two-column hero layout and unnecessary detail for homepage.

**Requirements:**
- Screenshot cropped to approximately 580×400px
- Shows NSG example from comprehensive-demo rendered in Azure DevOps style
- Clicking the screenshot opens a modal/lightbox showing full 1920×1080 comprehensive-demo screenshot
- Screenshot must be optimized for web (compressed, appropriate format)

**Approach:**
1. Generate full 1920×1080 screenshot of comprehensive-demo rendered output
2. Crop a compelling NSG section to 580×400px for homepage display
3. Implement click-to-enlarge functionality (lightbox/modal)
4. Replace interactive-example component with image + modal
5. Update screenshots.md with generation commands

**Definition of Done:**
- [ ] 580×400px cropped screenshot created and optimized
- [ ] 1920×1080px full screenshot created
- [ ] Homepage uses cropped screenshot instead of interactive example
- [ ] Click opens full-size screenshot in modal/lightbox
- [ ] Modal has close button and ESC key support
- [ ] Screenshots documented in screenshots.md with generation commands
- [ ] Both images stored in website/assets/ directory

---

### #22 — Replace hand-crafted examples with real output on all pages

| Field | Value |
|-------|-------|
| Page(s) | `/index.html`, `/features/*.html` |
| Effort | High |
| Value | High |
| Status | ✅ Done |
| Depends On | — |

**Description:** Task #18 replaced examples only on `/examples.html`. Other pages still have hand-crafted examples that should be replaced with real tfplan2md generated output for authenticity.

**Pages with examples to replace:**
1. **index.html (homepage)** - Hero section has simplified Network Security Rules example - should use real comprehensive-demo output
2. **features/firewall-rules.html** - "With tfplan2md" comparison example shows hand-crafted firewall table
3. **features/nsg-rules.html** - Has code-block example
4. **features/module-grouping.html** - Has example structure code-block
5. **features/sensitive-masking.html** - Has before/after comparison code-blocks
6. **features/azure-optimizations.html** - Has role assignment examples
7. **features/large-values.html** - Has large value example

**Approach:**
1. Extract relevant sections from `artifacts/comprehensive-demo.md` or generate new demo artifacts if needed
2. For homepage: use a compelling real example (NSG rules with clear presentation)
3. For feature pages: ensure examples accurately demonstrate the specific feature
4. Use HtmlRenderer tool with azdo flavor where rendered view is needed
5. Update code-examples.md with all extraction commands

**Definition of Done:**
- [x] Homepage hero example replaced with real output
- [x] All 7 feature detail pages reviewed and examples replaced where appropriate
- [x] Examples accurately demonstrate their respective features
- [x] All use authentic tfplan2md formatting from real artifacts
- [x] code-examples.md updated with new extraction commands
- [x] Visual consistency across all pages (Azure DevOps styling already applied via CSS)
- [x] All feature pages now use interactive viewer component with rendered/source toggle

---

### #20 — Skip PR validation for website-only changes

| Field | Value |
|-------|-------|
| Page(s) | N/A |
| Effort | Low |
| Value | High |
| Status | ✅ Done |
| Depends On | — |

**Description:** Update PR validation workflow to skip expensive validation steps (build, test, formatting) when only website content has changed. This will speed up PRs for website changes and reduce CI/CD load.

**Approach:**
1. Update the filter step in `.github/workflows/pr-validation.yml`
2. Add `website/` to the list of ignored paths (similar to `docs/` and `.github/agents/`)
3. Ensure the filter correctly skips validation when only website files change

**Definition of Done:**
- [x] `website/` added to ignored paths in pr-validation.yml filter step
- [x] PRs with only website changes skip build/test/formatting steps
- [x] PRs with mixed changes (website + code) still run full validation
- [x] Filter logic correctly handles edge cases (no files changed, etc.)
- [x] Skip message updated to mention website changes

---

### #19 — Replace emoji icons with SVG icons on features page

| Field | Value |
|-------|-------|
| Page(s) | `/features/index.html` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | #10 |

**Description:** Replace all emoji icons on the features index page with the corresponding SVG icons from `/assets/icons/`. The SVG icons were created in task #10 but are not yet being used on the features page. This will provide a more professional appearance and consistent styling.

**Approach:**
1. Review feature-definitions.md to map each feature to its SVG icon file
2. Replace `<div class="feature-icon">🔍</div>` style markup with `<img src="../assets/icons/semantic-diff.svg" alt="Semantic Diffs" class="feature-icon">`
3. Update CSS if needed to ensure SVG icons display correctly at the right size
4. Verify icons work in both light and dark modes (SVGs use "White Halo" technique)

**Definition of Done:**
- [x] All emoji icons replaced with SVG icons from `/assets/icons/`
- [x] Icon-to-feature mapping follows feature-definitions.md
- [x] Icons display correctly at intended size (64x64 for regular, 32x32 for small)
- [x] Icons work in both light and dark mode (White Halo technique)
- [x] No visual regressions (spacing, alignment maintained)
- [x] All icon files referenced actually exist
- [x] Added CSS for compact feature cards
- [x] Updated hero padding to match other pages (60px)

---

### #18 — Replace hand-crafted examples with real generated output

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | High |
| Value | High |
| Status | ✅ Done |
| Depends On | — |

**Description:** Replace remaining hand-crafted examples on examples page with real tfplan2md output generated from actual artifacts. The firewall rules example was already replaced (task completed outside backlog). Remaining examples to replace: Module Grouping, Role Assignment Display, Sensitive Value Masking.

**Approach:**
1. Extract relevant sections from `artifacts/comprehensive-demo.md`
2. Generate HTML using HtmlRenderer tool (azdo flavor)
3. Replace hand-crafted HTML/markdown in examples.html
4. Update code-examples.md with source artifacts and extraction commands

**Definition of Done:**
- [x] Module Grouping example replaced with real comprehensive-demo output
- [x] Role Assignment Display example replaced with real comprehensive-demo output
- [x] Sensitive Value Masking example replaced with real comprehensive-demo output
- [x] All examples use authentic tfplan2md formatting (not simplified versions)
- [x] code-examples.md updated with extraction commands for each example
- [x] screenshots.md updated to reflect all examples now use real output

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

### #16 — Move CI/CD to Built-In Capabilities

| Field | Value |
|-------|-------|
| Page(s) | `/features/index.html` |
| Effort | Low |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Move CI/CD Integration feature from "What Sets Us Apart" (High priority section) to "Built-In Capabilities" (Medium priority section). Per feature-definitions.md, CI/CD Integration is Medium value.

**Definition of Done:**
- [x] CI/CD Integration card removed from "What Sets Us Apart" section
- [x] CI/CD Integration card added to "Built-In Capabilities" section
- [x] Card content and icon preserved

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

### #14 — Implement example viewer + Style Isolation (CSS Layers)

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | High |
| Value | High |
| Status | ✅ Done |
| Depends On | #5, #13 |

**Description:** Implement an interactive example viewer component that shows examples in both rendered and source code views. Users can toggle between views. Source view has markdown syntax highlighting. Rendered view approximates Azure DevOps Services PR style. Both views have a full screen button. **Critical:** Implement CSS Layers for style isolation to prevent website styles from interfering with rendered examples (see ADR-004).

**Definition of Done:**
- [x] Toggle to switch between "Rendered" and "Source" views
- [x] Rendered view styled to approximate Azure DevOps PR rendering
- [x] Source view shows raw markdown with syntax highlighting
- [x] Full screen button for rendered view
- [x] Full screen button for source view
- [x] Works on mobile, tablet, desktop
- [x] Keyboard accessible (toggle, full screen buttons)
- [x] CSS Layers implemented for style isolation (see ADR-004)
- [x] Website styles wrapped in `@layer website { ... }`
- [x] Example styles in separate `@layer examples { ... }`
- [x] No interference between website and example styles verified

---

### #13 — Replace mockup examples

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | Medium |
| Value | High |
| Status | ✅ Done |
| Depends On | #5, #11 |

**Description:** Ensure examples page uses real tfplan2md reports from the comprehensive-demo, not mockups. Examples should show actual output the tool produces.

**Definition of Done:**
- [x] All examples on page are real tfplan2md output (not mockups)
- [x] Examples sourced from `examples/comprehensive-demo/` or `artifacts/`
- [x] Examples cover different feature demonstrations
- [x] Source markdown files available for example viewer (#14)
- [x] Generation commands documented in `website/_memory/code-examples.md`

---

### #12 — Redesign logo

| Field | Value |
|-------|-------|
| Page(s) | All pages |
| Effort | High |
| Value | Medium |
| Status | 🏗️ In progress |
| Depends On | — |

**Description:** Create a new logo for tfplan2md. Current logo is not suitable for small sizes. The new logo must work at navbar scale and should not be based on the old design. Multiple design options will be explored with the Maintainer before final selection.

**Definition of Done:**
- [x] Multiple logo options presented to Maintainer
- [ ] Selected logo works at navbar scale (small sizes)
- [ ] Logo works in both light and dark themes
- [ ] Logo placed in `/assets/` directory
- [ ] Logo integrated into navbar across all pages
- [ ] Archived rejected options in `website/_memory/archived-designs/`

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

### #8 — Create contributing page

| Field | Value |
|-------|-------|
| Page(s) | `/contributing.html` |
| Effort | Low |
| Value | Low |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the contributing page explaining how to contribute, development setup, and the AI workflow. The "Built 100% with GitHub Copilot" badge on homepage should link here.

**Definition of Done:**
- [x] Page created at `/contributing.html`
- [x] How to contribute section
- [x] Development setup instructions
- [x] AI workflow explanation (multi-agent approach from agents.md)
- [x] Link to GitHub repository
- [x] Navigation working correctly
- [x] Responsive design
- [x] Content derived from CONTRIBUTING.md and docs/agents.md

---

### #7 — Create architecture page

| Field | Value |
|-------|-------|
| Page(s) | `/architecture.html` |
| Effort | Medium |
| Value | Low |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the architecture page with high-level system overview based on arc42 documentation. Target audience is contributors who want to understand the codebase.

**Definition of Done:**
- [x] Page created at `/architecture.html`
- [x] High-level system overview diagram or description
- [x] Key architectural decisions explained
- [x] Navigation working correctly
- [x] Responsive design
- [x] Content derived from docs/architecture.md

---

### #6 — Create providers index

| Field | Value |
|-------|-------|
| Page(s) | `/providers/index.html` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the providers index page listing provider-specific template documentation. Should cover azurerm, azuredevops, azuread, and msgraph providers with links to detailed pages (if created) or inline documentation.

**Definition of Done:**
- [x] Page created at `/providers/index.html`
- [x] Lists all supported providers: azurerm, azuredevops, azuread, msgraph
- [x] Brief description of provider-specific features for each
- [x] Navigation working correctly
- [x] Responsive design
- [x] Content derived from docs/features.md provider sections

---

### #5 — Create examples page

| Field | Value |
|-------|-------|
| Page(s) | `/examples.html` |
| Effort | Medium |
| Value | Medium |
| Status | ✅ Done |
| Depends On | — |

**Description:** Create the examples page structure with placeholder examples. This page will host interactive examples showing tfplan2md output. Initially use mockup content to define what examples are needed; real examples will replace placeholders later (#13). The example viewer functionality is a separate task (#14).

**Definition of Done:**
- [x] Page created at `/examples.html`
- [x] Page structure ready to host example viewer component
- [x] Navigation working correctly
- [x] Responsive design
- [x] Links to example sources in repo (examples/comprehensive-demo/)

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

### #1 — Update Friendly Resource Names icon

