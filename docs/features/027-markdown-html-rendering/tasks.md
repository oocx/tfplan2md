# Tasks: Markdown to HTML Rendering Tool

## Overview

This feature implements a standalone .NET tool that converts `tfplan2md` markdown reports into HTML, approximating how GitHub and Azure DevOps render markdown. This tool supports development workflows like automated testing, website examples, and screenshot generation.

Reference: [specification.md](specification.md), [architecture.md](architecture.md), [test-plan.md](test-plan.md)

## Tasks

### Task 1: Project Setup and Infrastructure

**Priority:** High

**Description:**
Create the new console application project and its corresponding unit test project.

**Acceptance Criteria:**
- [x] New project `Oocx.TfPlan2Md.HtmlRenderer` created in `tools/` directory.
- [x] New test project `Oocx.TfPlan2Md.HtmlRenderer.Tests` created in `tests/` directory.
- [x] Both projects added to the solution `tfplan2md.slnx`.
- [x] `Markdig` NuGet package added to the renderer project.
- [x] Basic "Hello World" console app runs and tests pass.

**Dependencies:** None

---

### Task 2: CLI Implementation and Option Parsing

**Priority:** High

**Description:**
Implement the command-line interface with the specified options and validation logic.

**Acceptance Criteria:**
- [x] CLI supports `--input` (required), `--output` (optional), `--flavor` (required), and `--template` (optional).
- [x] Input file existence is validated.
- [x] Flavor is validated to be either `github` or `azdo`.
- [x] Output filename is correctly derived if `--output` is omitted (e.g., `report.md` -> `report.github.html`).
- [x] Output directory is created if it doesn't exist.
- [x] Tool exits with code 1 and clear error message on validation failure.
- [x] Tool exits with code 0 and warning on empty input file.

**Dependencies:** Task 1

---

### Task 3: Core Markdown Rendering Logic

**Priority:** High

**Description:**
Implement the core rendering logic using Markdig to convert Markdown to an HTML fragment.

**Acceptance Criteria:**
- [x] `MarkdownToHtmlRenderer` class implemented.
- [x] Supports standard Markdown features: tables, headings, lists, bold/italic, links.
- [x] Supports `tfplan2md` specific features: `<details>`, `<summary>`, `<br/>`.
- [x] Fenced code blocks include `class="language-*"` for syntax highlighting compatibility.
- [x] `simple-diff` format (code blocks with `diff` language) renders correctly.

**Dependencies:** Task 1

---

### Task 4: Flavor-Specific Rendering (GitHub & Azure DevOps)

**Priority:** High

**Description:**
Implement the platform-specific rendering rules for GitHub and Azure DevOps flavors.

**Acceptance Criteria:**
- [x] **GitHub Flavor**:
    - [x] Strips all `style` attributes from raw HTML nodes (e.g., `<span>` in `inline-diff`).
    - [x] Uses GFM-like extensions (tables, strikethrough, autolinks).
- [x] **Azure DevOps Flavor**:
    - [x] Preserves all `style` attributes in raw HTML nodes.
    - [x] Handles line breaks correctly (soft breaks do NOT become `<br/>` unless two trailing spaces are present).
- [x] Unit tests verify these differences using `inline-diff` and line break samples.

**Dependencies:** Task 3

---

### Task 5: Wrapper Template Application

**Priority:** Medium

**Description:**
Implement the logic to wrap the rendered HTML fragment in a complete HTML document using a template.

**Acceptance Criteria:**
- [x] `WrapperTemplateApplier` class implemented.
- [x] Replaces `{{content}}` placeholder with the HTML fragment.
- [x] Validates that the placeholder exists in the template; fails with exit code 1 if missing.
- [x] Supports reading template from a file path.

**Dependencies:** Task 3

---

### Task 6: Default Wrapper Templates and CSS

**Priority:** Medium

**Description:**
Create default wrapper templates for GitHub and Azure DevOps with approximating CSS and syntax highlighting.

**Acceptance Criteria:**
- [x] `github-wrapper.html` created in `tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`.
- [x] `azdo-wrapper.html` created in `tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`.
- [x] Templates include basic HTML5 structure.
- [x] Templates include CSS that approximates the look and feel of the respective platforms.
- [x] Templates include Highlight.js (via CDN) for syntax highlighting in code blocks.

**Dependencies:** Task 5

---

### Task 7: Integration Testing with Demo Artifacts

**Priority:** Medium

**Description:**
Verify the tool against all existing demo artifacts and "Gold Standard" renderings.

**Acceptance Criteria:**
- [x] Integration tests process all `.md` files in `artifacts/` for both flavors.
- [x] Generated HTML for `comprehensive-demo-simple-diff.md` (GitHub flavor) matches `comprehensive-demo-simple-diff.actual-gh-rendering.html` (core structure).
- [x] Generated HTML for `comprehensive-demo.md` (Azure DevOps flavor) matches `comprehensive-demo.actual-azdo-rendering.html` (core structure).

**Dependencies:** Task 4, Task 6

---

### Task 8: Documentation and Final Polish

**Priority:** Low

**Description:**
Update project documentation to include the new tool and its usage.

**Acceptance Criteria:**
- [x] `README.md` updated with instructions on how to run the HTML renderer.
- [x] `docs/features.md` (if applicable) updated to list the tool.
- [x] Code follows project style guidelines (C# conventions, immutable structures where appropriate).

**Dependencies:** Task 7

## Implementation Order

1. **Task 1 (Setup)**: Foundation for all other tasks.
2. **Task 3 (Core Rendering)**: Basic functionality.
3. **Task 4 (Flavors)**: Essential for the "approximation" goal.
4. **Task 2 (CLI)**: Makes the tool usable from the command line.
5. **Task 5 & 6 (Templates)**: Enables full document generation.
6. **Task 7 (Integration)**: Final validation.
7. **Task 8 (Docs)**: Completion.

## Open Questions

- Should the tool be distributed as a .NET Tool (`dotnet tool install`)? (Spec says "standalone .NET tool", but doesn't explicitly mention NuGet distribution yet).
- Are there any specific versions of Highlight.js or Prism.js preferred? (Spec mentions Highlight.js as recommended).
