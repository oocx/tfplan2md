# Architecture: Markdown to HTML Rendering Tool

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

We need a standalone .NET tool that converts `tfplan2md`-generated Markdown reports into HTML in two platform flavors:

- **GitHub PR comments** (approximate GitHub Flavored Markdown rendering and sanitization)
- **Azure DevOps PR comments / Wiki pages** (approximate Azure DevOps Markdown rendering)

This tool is intended for development workflows:

- HTML-based tests (DOM inspection, Playwright)
- website examples
- screenshot generation / visual regression

Key constraints from the spec:

- **Not pixel-perfect** compatibility; “good enough approximation” is acceptable.
- **Static HTML output only** (no JS-based Markdown rendering).
- Must handle `tfplan2md`’s two large-value diff modes:
  - `inline-diff`: contains inline HTML with `style` attributes (ADO keeps; GitHub strips)
  - `simple-diff`: fenced code blocks with `diff` language
- Wrapper templates use a **simple placeholder** `{{content}}`.

## Assumptions

- Input Markdown is primarily produced by `tfplan2md` and generally follows [docs/markdown-specification.md](../../markdown-specification.md).
- The renderer does **not** need to be a general-purpose, security-hardened HTML sanitizer for arbitrary untrusted Markdown.

## Options Considered

### Option 1: Markdig + two pipelines + flavor post-processing ✅ Recommended

- Use **Markdig** to parse Markdown into an AST and render HTML.
- Select a pipeline per flavor (GitHub vs Azure DevOps).
- Apply **minimal, explicit flavor-specific post-processing**, especially for `inline-diff`.

**Pros**
- Aligns with the feature spec’s explicit library choice (Markdig).
- Enables *targeted* platform differences (e.g., stripping `style` attributes for GitHub) without inventing a new Markdown dialect.
- Keeps dependencies small and output deterministic (good for tests).

**Cons**
- Still an approximation; some GitHub/ADO nuances won’t match.
- Requires careful choices for which extensions to enable to avoid “surprising” output.

### Option 2: Markdig + HTML DOM sanitizer (AngleSharp/HtmlAgilityPack) for GitHub

- Same as Option 1, but implement GitHub’s “style stripping” by parsing rendered HTML and removing attributes.

**Pros**
- More robust than string/regex operations on HTML.

**Cons**
- Adds a heavier dependency solely for a narrow requirement.
- Can introduce output changes unrelated to the spec if the sanitizer normalizes HTML.

### Option 3: cmark-gfm / GitHub renderer bindings

- Use GitHub’s reference renderer for GFM.

**Pros**
- Closer to GitHub behavior.

**Cons**
- Adds native dependencies / platform complexity.
- Doesn’t help with Azure DevOps approximation.
- Conflicts with the spec’s “Markdig integrated” success criteria.

### Option 4: Hybrid (Markdig for Azure DevOps, cmark-gfm for GitHub)

- Azure DevOps flavor uses **Markdig** (per spec and likely close to ADO behavior).
- GitHub flavor uses **cmark-gfm** (closer to GitHub's GFM parsing than Markdig's GFM approximation).
- Keep the same wrapper-template mechanism and the same CLI shape.

**Pros**
- Best chance of matching GitHub's markdown-to-HTML *parsing* behavior for edge cases (tables, autolinks, softbreak nuances, weird nesting).
- Lets ADO continue to use Markdig (good fit with the research summary).

**Cons**
- Adds native/runtime complexity (RID-specific assets, packaging, CI matrix considerations).
- Two renderers means duplicated tests and higher maintenance burden.
- Still does not automatically replicate GitHub's full pipeline (HTML sanitization, attribute stripping, allowed-tag policy, heading IDs, etc.) unless we implement those separately.
- Potential mismatch risk: tfplan2md emits inline HTML for `inline-diff`; if GitHub sanitization behavior is emulated elsewhere, we must ensure cmark-gfm output + post-processing still matches expectations.

## Decision

Choose **Option 1: Markdig + two pipelines + flavor post-processing**.

We will **start with Option 1**. If, after using the tool in real workflows, GitHub rendering fidelity is not good enough, we can introduce the hybrid approach (Option 4) later as a **separate feature**.

## High-Level Design

### Project layout

Per spec, introduce a new console project:

- `tools/Oocx.TfPlan2Md.HtmlRenderer/` (new .NET console app)
- `tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`
  - `github-wrapper.html`
  - `azdo-wrapper.html`
- `tests/Oocx.TfPlan2Md.HtmlRenderer.Tests/` (new test project)

This stays separate from the main `tfplan2md` CLI project, preserving the existing “single project for the CLI tool” guidance in [docs/spec.md](../../spec.md).

### Core components (suggested)

- `CliOptions` + `CliParser`
  - Match the existing style from `tfplan2md` (simple, explicit parsing).
- `MarkdownToHtmlRenderer`
  - `RenderFragment(string markdown, HtmlFlavor flavor) -> string`
- `MarkdigPipelineFactory`
  - `Create(HtmlFlavor flavor) -> MarkdownPipeline`
- `FlavorPostProcessor`
  - `PostProcess(MarkdownDocument doc, HtmlFlavor flavor)` for AST-based adjustments
- `WrapperTemplateApplier`
  - `Apply(string templateHtml, string contentHtml) -> string`
  - Strictly requires the placeholder `{{content}}`.

### Rendering pipeline

1. Read Markdown from `--input`.
2. If empty:
   - Emit empty output (spec says warning + exit 0).
3. Build Markdig pipeline per `--flavor`.
4. Parse Markdown → `MarkdownDocument`.
5. Apply flavor post-processing to the AST.
6. Render HTML fragment.
7. If `--template` supplied:
   - Load template, replace `{{content}}` with fragment, output full HTML document.
   - If missing placeholder: fail (exit 1).
8. Write output:
   - If `--output` omitted, derive `{inputBase}.{flavor}.html`.
   - Ensure output directory exists.

## Flavor-Specific Behavior

### Common behavior (both flavors)

- Enable CommonMark-compatible parsing.
- Enable table support and fenced code blocks.
- Preserve inline HTML blocks/inlines (required for `<details>`, `<summary>`, `<br/>`, and `inline-diff`).
- Ensure fenced code blocks render with `class="language-*"` for compatibility with Highlight.js/Prism in wrapper templates.

### GitHub flavor

**Goal:** Approximate GitHub PR comment rendering.

- Use Markdig with a “GFM-like” set of extensions (tables, strikethrough, autolinks).
- **`inline-diff`: strip all inline `style` attributes** from raw HTML nodes to match GitHub sanitization behavior.

Implementation approach recommendation:

- Perform style stripping at the **Markdig AST level**:
  - Visit `HtmlInline` and `HtmlBlock` nodes.
  - Remove `style="..."` / `style='...'` attributes from tag text.
  - Do not touch code blocks/spans because they are distinct node types.

This is narrow in scope (only touches raw HTML nodes) and avoids broad regex operations across the entire Markdown.

### Azure DevOps flavor

**Goal:** Approximate ADO PR comment/wiki rendering.

- Use a Markdig pipeline close to GitHub’s (tables, fenced code).
- Preserve raw HTML *as-is*, including `style` attributes.

Line breaks:

- Ensure the pipeline does **not** convert soft breaks into hard breaks (i.e., don’t enable “softbreak as `<br/>`” behavior). This matches ADO’s requirement that hard breaks require two trailing spaces or explicit `<br/>`.

## Wrapper Templates

### Placeholder semantics

- The wrapper template is plain HTML containing a single insertion token: `{{content}}`.
- Replace token with the rendered HTML fragment without escaping (the fragment is already HTML).
- If token is missing, treat as a user error (exit 1).

### Default wrapper templates (examples)

Provide two example templates in-repo (per spec):

- Include GitHub/ADO-ish CSS (approximation).
- Include syntax highlighting via **Highlight.js** (recommended by the spec) or Prism via CDN.
- Keep templates dependency-free besides those CDN links.

Note: the tool should not silently auto-select a default template; wrapper mode is only used when `--template` is explicitly provided.

## Testing Strategy (Renderer Project)

Focus on deterministic unit tests that validate behavior, not pixel-perfect rendering.

Recommended test groups:

- **Fragment rendering**
  - Tables produce `<table>` with expected structure.
  - Fenced code blocks emit `language-*` classes.
  - `<details>` / `<summary>` are preserved.
- **Diff formats**
  - GitHub flavor: `inline-diff` raw HTML loses `style` attributes.
  - Azure DevOps flavor: `inline-diff` preserves `style` attributes.
  - `simple-diff`: both flavors render a code block with `language-diff`.
- **Wrapper application**
  - `{{content}}` replaced exactly once (or define/verify the rule if multiple occurrences are allowed).
  - Missing placeholder fails.
- **Well-formed HTML validation**
  - Parse the output with an HTML parser in tests to ensure it’s structurally valid.

## Documentation Updates (Implementation Guidance)

When implementing the feature, update docs to describe:

- how to run the tool and where templates live
- typical workflows (generate HTML for demo artifacts, use for website/screenshots)

Candidate locations:

- `README.md` (developer-facing usage)
- `docs/features.md` (if this should be listed as a tool/feature)
- `docs/features/027-markdown-html-rendering/specification.md` (link to templates path)

## Risks and Mitigations

- **Risk:** Markdig output differs from GitHub/ADO in subtle cases.
  - Mitigation: limit scope to `tfplan2md`-produced Markdown; test against demo artifacts.
- **Risk:** Style stripping misses edge cases in raw HTML.
  - Mitigation: keep stripping logic minimal and add focused tests on representative `inline-diff` snippets.
- **Risk:** Wrapper templates evolve into a “theme system”.
  - Mitigation: keep wrapper templates examples only; users can supply their own templates.

## Notes on GitHub Fidelity

Using cmark-gfm (Option 3) can produce **closer GitHub results** *for markdown parsing*, because it's the core parser GitHub's GFM spec is built around.

However, in practice, whether it produces **better results for this project** depends on what “better” means:

- If the primary gaps you see are **GFM parsing edge-cases**, cmark-gfm is likely to help.
- If the primary gaps are **GitHub's HTML sanitization and rendering policies** (e.g., style stripping, allowed tags/attributes), then cmark-gfm alone won’t solve that; we'd still need explicit post-processing/sanitization logic.

Given the spec’s goal (“good enough approximation” for tfplan2md outputs) and the added complexity of native dependencies, Option 1 remains the recommended baseline unless we have a concrete set of GitHub mismatches that Markdig cannot reasonably match.
