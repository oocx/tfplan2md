# Plan: Improve Web Designer agent with website memory docs + screenshot inventory

**Date:** 2026-01-03

## Verbatim requirements (Maintainer)

> I want to improve the web-designer agent.
>
> 1) in 025, we created a table of all features and classified how we want to represent them on the web site (value, group). Copy this table so somewhere in the website directory. I want to create a list of files like this that must be used by the web design agent as memory for decisions on the website content. Update the agent to use these files.
>
> 2) as part of these files, I also want to store the planned web site structure. The document must include the hierarchical structure of the web site. For each individual site, the document must describe the title, purpose, content summary, target audience, and list of links to which other sites this site ilnks to. It must also be used as log for each site to note decisions we made on content, design or structure of each page. Update the agents instructions on how to use this document.
>
> 3) we created new html export and screnshot tools. The html export can generate html from markdown reports and supports github and azure devops profiles. the screenshot tool can generate a screenshot based on that html export and also supports azdo and gh profiles. The web-design agent must use these tools to generate all screenthos and html exports it needs as examples or to showcase features on the web site. It must keep a list of screenshots it uses as markdown document. This document must contain the screenshot file name, explain how to generate the screenshot (exact tool usage), and describe the indended purpose of the screenshot.

## Current context (what already exists)

- Feature categorization table exists in: `docs/features/025-github-pages-website/feature-definitions.md`.
- Website content currently lives in: `website/` (today it contains `style.css` and `features/index.html`).
- Web Designer agent exists in: `.github/agents/web-designer.agent.md`.
- Dev tools exist and have documented CLI usage:
  - HTML renderer: `dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- ...` (supports `--flavor github|azdo`).
  - Screenshot generator: `dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- ...` (captures from the HTML output; supports targets).

## Design goals for the change

- Put “website content decisions” into explicit, versioned docs under `website/` so the Web Designer agent can treat them as source-of-truth memory.
- Keep the memory docs:
  - Small, navigable, and easy to update in PRs.
  - Explicit about “what we decided” vs “what we might do later”.
- Make screenshot + HTML example generation reproducible via exact commands.
- Ensure non-functional requirements (especially accessibility) are explicit and enforced.
- Ensure design/style decisions are recorded and consistently applied.

## Proposed new website memory files (source of truth)

Create a dedicated directory under `website/`:

- `website/_memory/` (new)
  - Purpose: source-of-truth “memory” docs for website content, structure, and examples.
  - Rationale: keeps the memory next to the site, avoids scattering decisions across `docs/`.

### File 1: Feature representation table (copy from 025)

- `website/_memory/feature-definitions.md`
  - Content: copy the “Complete Feature Table” (and relevant headings) from `docs/features/025-github-pages-website/feature-definitions.md`.
  - Required table columns:
    - `Feature`
    - `Description`
    - `Group`
    - `Value`
    - `Icon/Image` (new)
      - Must reference the icon/image used to represent the feature on the website (e.g., an asset filename/path).
      - Uniqueness rule: different features may not use the same icon/image.
  - Maintenance rule: when features are reclassified (Value/Group) or their icon/image changes, update this file and use it to drive which pages exist, what gets highlighted, and which visuals are used.

### File 2: Website structure + per-page decision log

- `website/_memory/site-structure.md`
  - Must include:
    - A hierarchical site map.
    - For each page ("individual site"):
      - Title
      - Purpose
      - Content summary
      - Target audience
      - Links to other pages (outbound links)
      - Decision log (dated entries) for content/design/structure decisions.

Initial structure rule (important):

- The initial site map and page list must be derived from the pages that exist in `website/` at the time of creation.
- Planned/future pages may be captured separately (e.g., a “Planned additions” section), but must be clearly marked as planned and must not be mixed into the “current site map”.

Recommended format (so it stays consistent):

- Top section: a tree like:
  - `/` (index)
  - `/features/` (index)
  - `/features/<feature>.html`
  - …

- Then one page spec block per page:
  - `## /features/index.html`
    - **Title:** ...
    - **Purpose:** ...
    - **Content summary:** ...
    - **Target audience:** ...
    - **Links to:**
      - ...
    - **Decision log:**
      - 2026-01-03: ...

Note: The decision log should record outcomes only (not long discussions), so it stays usable as memory.

### File 3: Screenshot inventory (with exact tool usage)

- `website/_memory/screenshots.md`
  - Must contain, per screenshot:
    - Screenshot file name (and where it lives in `website/`)
    - Exact commands to generate it (including both HTML export and screenshot capture)
    - Intended purpose (where it’s used / what feature it demonstrates)

Recommended conventions:

- Store images under `website/assets/screenshots/` (new directory).
- Use deterministic, descriptive filenames (e.g., `semantic-diff-azdo.png`, `nsg-rules-table-azdo.png`).
- Keep the inventory as the definitive list of “approved” screenshots used in the website.

## Plan: skills vs Web Designer agent instructions

### Skills to add

1) Visual asset generation workflow (HTML export + screenshots)

- Skill: `.github/skills/website-visual-assets/`

2) Chrome DevTools-based troubleshooting / rendering analysis

- Skill: `.github/skills/website-devtools/`

3) Website quality checks (local verification workflow)

- Skill: `.github/skills/website-quality-check/`
  - Scope: lightweight, repeatable checks the Web Designer should run while working on the site.
  - Examples of what it covers (keep minimal):
    - Local preview workflow
    - Link/navigation sanity checks
    - Accessibility spot checks
    - Check that the site follows `website/_memory/style-guide.md`
    - How to use DevTools to validate rendering and capture findings

### Keep as Web Designer agent instructions + website memory docs

1) Memory docs policy and content requirements

- `.github/agents/web-designer.agent.md` must require reading and updating `website/_memory/*`.

2) Style guide and NFRs

- Memory docs:
  - `website/_memory/style-guide.md`
  - `website/_memory/non-functional-requirements.md`
- Enforcement stays in `.github/agents/web-designer.agent.md`.

3) Code examples inventory

- Memory doc: `website/_memory/code-examples.md`
- Enforcement stays in `.github/agents/web-designer.agent.md`.

4) Feature icon/image consistency

- Memory doc: `website/_memory/feature-definitions.md` (via the required `Icon/Image` column)
- Enforcement stays in `.github/agents/web-designer.agent.md` (must keep icons consistent and unique across features).

### File 4: Website style guide (design decisions)

- `website/_memory/style-guide.md`
  - Purpose: capture and enforce important design and style decisions.
  - Must include (at minimum):
    - Typography rules (headings/body sizing, spacing conventions)
    - Layout rules (page width constraints, grid patterns, spacing rhythm)
    - Component patterns used in the site (e.g., “feature cards”, “callout blocks”)
    - Image/screenshot usage rules (sizes/aspect ratios, captions, alt text style)
    - Decision log (dated) for changes to the style guide
  - Rule: the Web Designer must follow this guide when working on the site, and update it when decisions change.

### File 5: Non-functional requirements (NFRs)

- `website/_memory/non-functional-requirements.md`
  - Purpose: make NFRs explicit, testable, and easy to reference.
  - Must include (at minimum):
    - Accessibility: the website must be fully accessible (keep the accessibility requirements here as the canonical list).
    - Browser support: support only the latest version of Edge and Firefox.
    - Standards: usage of modern standards and features (HTML/CSS/JS) is encouraged, assuming they are supported by the supported browsers.
    - Performance / maintainability constraints (keep scope minimal unless explicitly expanded).

### File 6: Code examples inventory (with generation instructions)

- `website/_memory/code-examples.md`
  - Purpose: the definitive list of code examples/snippets shown on the website.
  - Must contain, per code example:
    - A stable identifier or name
    - Where it is used on the website (page + section)
    - Source-of-truth location in the repo (file path)
    - How to (re)generate or refresh it
      - If generated by tooling: exact command(s)
      - If maintained manually: an explicit procedure (and the canonical source to copy from)
    - Intended purpose (what user problem it addresses)

## Required Web Designer agent updates

Update `.github/agents/web-designer.agent.md` so the agent:

### 1) Treats website memory docs as mandatory context

- Add `website/_memory/feature-definitions.md` to “Context to Read”.
- Add `website/_memory/site-structure.md` to “Context to Read”.
- Add `website/_memory/screenshots.md` to “Context to Read”.
- Add `website/_memory/style-guide.md` to “Context to Read”.
- Add `website/_memory/non-functional-requirements.md` to “Context to Read”.
- Add `website/_memory/code-examples.md` to “Context to Read”.

Add an explicit instruction block like:

- When making website decisions (content, layout, page list), the agent must:
  1. Read the memory docs.
  2. Apply changes consistent with them.
  3. Update the memory docs in the same PR when decisions change.

### 2) Define how to use the site structure doc

Add instructions that the agent must:

- Use `website/_memory/site-structure.md` as the single source of truth for:
  - Which pages exist.
  - Page purpose and content boundaries.
  - Navigation/outbound links.
- Record decisions in the per-page “Decision log” section whenever:
  - A page title/purpose changes.
  - A page is added/removed/renamed.
  - Major content is added/removed.

### 3) Require HTML renderer + screenshot generator usage for visuals

Add instructions that the agent must:

- Generate all showcased HTML exports using the HTML renderer tool (no hand-written HTML “mock” screenshots).
- Generate all showcased screenshots using the screenshot generator tool (Playwright), driven by the exported HTML.
- Maintain `website/_memory/screenshots.md` as the authoritative inventory of screenshots used.

Include the exact commands (documented in repo) in the agent as canonical patterns.

### 4) Require a style guide and NFR compliance

Add instructions that the agent must:

- Treat `website/_memory/style-guide.md` as the canonical source for visual/design decisions.
- Treat `website/_memory/non-functional-requirements.md` as the canonical source for quality constraints.
- Keep both documents updated when decisions change (dated decision log entries).

### 5) Require code examples inventory

Add instructions that the agent must:

- Keep `website/_memory/code-examples.md` up to date with every code snippet added/removed/changed on the website.
- Prefer examples sourced from existing documentation (README.md, docs/) unless explicitly instructed otherwise.
- When an example is generated (e.g., report output), record the exact command(s) that produce it.

### 6) Require Chrome DevTools MCP usage during website work

Add instructions that the agent must:

- Be configured with the Chrome DevTools MCP tool set: `io.github.chromedevtools/chrome-devtools-mcp/*`.
- Use the Chrome DevTools tools while working on the website to:
  - Analyze how pages render (DOM/CSS/layout) and confirm expected behavior.
  - Troubleshoot issues together with the Maintainer (e.g., reproduce and inspect a rendering issue).

Note: This plan does not enumerate specific DevTools MCP commands/APIs; the agent should use whatever capabilities are available via `io.github.chromedevtools/chrome-devtools-mcp/*` in the current environment.

Update to incorporate skills:

- The Web Designer agent should explicitly mention and use the skills:
  - `website-visual-assets` (for HTML exports + screenshots + inventory updates)
  - `website-devtools` (for Chrome DevTools MCP usage)
  - `website-quality-check` (for local verification and repeatable quality checks)

#### Canonical commands to embed into agent guidance

HTML export (GitHub):
```bash
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github
# Output: artifacts/comprehensive-demo.github.html
```

HTML export (Azure DevOps wrapper):
```bash
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html \
  --output artifacts/comprehensive-demo.azdo.html
```

Screenshot generation examples:

- Full page:
```bash
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/full-report-azdo.png \
  --full-page
```

- Target specific Terraform resource:
```bash
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/firewall-resource-azdo.png \
  --target-terraform-resource-id "azurerm_firewall.example"
```

- Target selector:
```bash
dotnet run --project tools/Oocx.TfPlan2Md.ScreenshotGenerator -- \
  --input artifacts/comprehensive-demo.azdo.html \
  --output website/assets/screenshots/firewall-details-azdo.png \
  --target-selector "details:has(summary:has-text('azurerm_firewall'))"
```

## Implementation steps (what will change)

1. Add website memory directory and files
   - Create `website/_memory/`.
  - Add `website/_memory/feature-definitions.md` by copying the table from the 025 doc and adding the required `Icon/Image` column.
    - Ensure every feature has an assigned icon/image.
    - Ensure icon/image assignments are unique across features.
  - Add `website/_memory/site-structure.md` with the initial site map and initial page specs.
  - Add `website/_memory/screenshots.md` with an initial empty inventory + template entries.
  - Add `website/_memory/style-guide.md` with initial style rules + decision log scaffold.
  - Add `website/_memory/non-functional-requirements.md` with the initial NFR list.
  - Add `website/_memory/code-examples.md` with an initial empty inventory + template entries.

2. Seed the initial site structure doc
   - Derive the initial site map from the pages that exist in `website/` at the time of implementation.
     - Current known pages (today): `/website/features/index.html` and `/website/style.css`.
   - Capture planned/future pages separately (clearly marked as planned), to avoid implying they already exist.

3. Update the Web Designer agent instructions
   - Add the new memory docs to “Context to Read”.
   - Add explicit “must update memory docs when decisions change” rules.
   - Add a “Visual assets workflow” section requiring HTML renderer + screenshot generator usage.
  - Add a “DevTools workflow” section describing how/when to use Chrome DevTools MCP tools while developing and troubleshooting.

4. Add new skills
  - Create `.github/skills/website-visual-assets/`.
  - Create `.github/skills/website-devtools/`.
  - Create `.github/skills/website-quality-check/`.
  - Reference these skills from the Web Designer agent instructions.

5. Add/adjust website asset directories as needed
   - Create `website/assets/screenshots/` (empty at first).

6. (Optional follow-up) Add a website assets directory for code examples (only if needed)
  - Prefer embedding code blocks directly in HTML pages.
  - If the site needs separate downloadable example files, add `website/assets/examples/` and track them via `website/_memory/code-examples.md`.

7. (Optional follow-up) Validate agent/tooling instructions
   - Ensure the documented commands match `README.md` and `docs/features.md`.
   - Ensure paths are relative to repo root and reproducible.

## Out of scope (for this change)

- Creating new website pages beyond what’s required to host the memory docs.
- Adding new features, filters, carousels, or new UX beyond the existing spec.
- Changing the Web Designer agent’s model assignment (unless separately requested).

## Acceptance criteria

- There is a clear “memory” directory under `website/` containing:
  - Feature representation table (copied from 025).
  - Site structure + per-page decision log document.
  - Screenshot inventory document with required fields.
- Style guide document with design decisions.
- Non-functional requirements document.
- Code examples inventory document with generation instructions.
- Web Designer agent explicitly reads these docs and updates them when decisions change.
- Screenshot inventory requires exact, reproducible commands using the existing HTML renderer + screenshot generator tools.
- Web Designer agent is configured to use `io.github.chromedevtools/chrome-devtools-mcp/*` and uses it during website work for inspection/troubleshooting.
