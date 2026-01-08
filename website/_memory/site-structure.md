# Website Structure (Source of Truth)

This document is the **source of truth** for the current website structure and for per-page intent.

## Current site map (derived from `website/`)

### Implemented Pages
- `/features/index.html` ✅
- `/ai-workflow.html` ✅

### Planned Pages (not yet created)
- `/index.html`
- `/getting-started.html`
- `/docs.html`
- `/examples.html`
- `/providers/index.html`
- `/architecture.html`
- `/contributing.html`
- `/features/firewall-rules.html`
- `/features/nsg-rules.html`
- `/features/azure-optimizations.html`
- `/features/large-values.html`
- `/features/misc.html`
- `/features/module-grouping.html`
- `/features/semantic-icons.html`
- `/features/custom-templates.html`

## Navigation Structure

The navbar (present in all pages) links to:

| Nav Item | Target | Status |
|----------|--------|--------|
| Features | `features/index.html` | ✅ Exists |
| Install | `getting-started.html` | ❌ Missing |
| Docs | `docs.html` | ❌ Missing |
| Examples | `examples.html` | ❌ Missing |
| Providers | `providers/index.html` | ❌ Missing |
| Architecture | `architecture.html` | ❌ Missing |
| AI Workflow | `ai-workflow.html` | ✅ Exists |
| Contributing | `contributing.html` | ❌ Missing |
| GitHub (CTA) | `https://github.com/oocx/tfplan2md` | ✅ External |

## Page specifications

### /features/index.html

- **Title:** Features - tfplan2md
- **Purpose:** Present the feature overview and link to deeper feature pages.
- **Content summary:** Feature categories ("What Sets Us Apart", "Built-In Capabilities", "Also Included") with brief descriptions and links.
- **Target audience:** Evaluators, Users
- **Sections:**
  1. Hero with title and subtitle
  2. "What Sets Us Apart" - 8 featured cards (Semantic Diffs, Firewall Rules, NSG Rules, Role Assignment, Large Values, CI/CD, PR Rendering, Friendly Names)
  3. "Built-In Capabilities" - 8 cards (Plan Summary, Module Grouping, Collapsible Details, Tag Visualization, Smart Iconography, Custom Templates, Provider Agnostic, Local Names)
  4. "Also Included" - 4 compact cards (Sensitive Masking, Minimal Container, Container Support, Dark/Light Mode)
  5. Footer with Product/Resources/Community links
- **Feature links (internal, all missing):**
  - `firewall-rules.html`
  - `nsg-rules.html`
  - `azure-optimizations.html#principal-mapping`
  - `large-values.html`
  - `misc.html#friendly-names`
  - `misc.html#plan-summary`
  - `module-grouping.html`
  - `misc.html#collapsible-details`
  - `semantic-icons.html#tags`
  - `semantic-icons.html`
  - `custom-templates.html`
  - `misc.html#local-names`
- **External links:**
  - `https://github.com/oocx/tfplan2md`
  - `https://hub.docker.com/r/oocx/tfplan2md`
  - `https://github.com/oocx/tfplan2md/issues`
  - `https://github.com/oocx/tfplan2md/blob/main/LICENSE`
- **Accessibility features:**
  - Theme toggle with aria-label
  - Mobile menu button with aria-label
  - Semantic heading hierarchy (h1 → h2 → h3)
- **Decision log:**
  - 2026-01-03: Seeded from currently present HTML pages in `website/`.
  - 2026-01-03: Documented all internal/external links and navigation structure.
  - 2026-01-07: Added Dark/Light Mode feature card to "Also Included" section (now 4 cards total).

### /index.html (Planned)

- **Title:** tfplan2md - Human-readable Terraform plan reports
- **Purpose:** Homepage with hero, problem/solution, and CTAs
- **Target audience:** All visitors
- **Content sources:** README.md

### /getting-started.html (Planned)

- **Title:** Get Started - tfplan2md
- **Purpose:** Installation and first usage guide
- **Target audience:** Users
- **Content sources:** README.md (Quick Start section)

### /docs.html (Planned)

- **Title:** Documentation - tfplan2md
- **Purpose:** CLI reference and template customization
- **Target audience:** Users, Power Users
- **Content sources:** docs/spec.md, docs/features.md

### /examples.html (Planned)

- **Title:** Examples - tfplan2md
- **Purpose:** Interactive examples and before/after visuals
- **Target audience:** Evaluators, Users
- **Content sources:** examples/comprehensive-demo/, artifacts/
- **Requirements (from chat):**
  - Show examples in both **rendered** and **source code (markdown)** views
  - Ability to **switch between views**
  - **Markdown syntax highlighting** in source view
  - Rendered view must use style that **approximates Azure DevOps Services PR** rendering
  - **Full screen button** for both views
  - **Must use real tfplan2md reports, not mockups**

### /providers/index.html (Planned)

- **Title:** Providers - tfplan2md
- **Purpose:** Provider-specific template documentation
- **Target audience:** Power Users
- **Content sources:** docs/features.md (provider sections)

### /architecture.html (Planned)

- **Title:** Architecture - tfplan2md
- **Purpose:** High-level system overview (arc42)
- **Target audience:** Contributors
- **Content sources:** docs/architecture.md

### /contributing.html (Planned)

- **Title:** Contributing - tfplan2md
- **Purpose:** How to contribute, AI workflow
- **Target audience:** Contributors
- **Content sources:** CONTRIBUTING.md, docs/agents.md
- **Requirements (from chat):**
  - Include information about how this project uses AI
  - Describe the multi-agent workflow (from agents.md)
  - "Built 100% with GitHub Copilot" badge should link to ai-workflow.html

### /ai-workflow.html

- **Title:** AI Development Workflow - tfplan2md
- **Purpose:** Describe the agentic development workflow used in this project
- **Content summary:** High-level overview of the multi-agent AI workflow, workflow diagram, key agents description, execution modes
- **Target audience:** Contributors, anyone interested in AI-powered development
- **Sections:**
  1. Hero with title and subtitle
  2. Overview - description of agent-based workflow and benefits (4 benefit cards)
  3. Workflow Diagram - Mermaid diagram showing complete agent flow
  4. Key Agents - grid of 12 agent cards with emoji, title, and description
  5. How It Works - 5-step process explanation
  6. Dual-Mode Execution - Local (VS Code) vs Cloud (GitHub) modes
  7. Learn More - CTAs to full agents.md documentation and contributing guide
  8. Footer with Product/Resources/Community links
- **External links:**
  - `https://github.com/oocx/tfplan2md/blob/main/docs/agents.md` (full documentation)
  - `https://github.com/oocx/tfplan2md` (GitHub repo)
  - Other standard footer links
- **Accessibility features:**
  - Theme toggle with aria-label
  - Mobile menu button with aria-label
  - Semantic heading hierarchy (h1 → h2 → h3)
  - Mermaid diagram with light/dark theme support
- **Decision log:**
  - 2026-01-07: Created page based on GitHub issue requirement
  - 2026-01-07: Added to navbar on all pages
  - 2026-01-07: Adapted Mermaid diagram from agents.md with theme support
