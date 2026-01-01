# Feature: Markdown to HTML Rendering Tool

## Overview

A standalone .NET tool that converts tfplan2md markdown reports to HTML, approximating how Azure DevOps Services and GitHub render markdown in pull request comments. This tool will support development workflows including automated testing, website examples, and screenshot generation.

## User Goals

- **Development teams** want to generate HTML from markdown reports for use in automated tests (e.g., Playwright browser tests, HTML inspection)
- **Documentation maintainers** want to create website examples showing how reports render in different platforms
- **QA engineers** want to generate screenshots of reports for visual regression testing and documentation
- **Project maintainers** want to validate that markdown renders correctly on target platforms before publishing

## Research Summary

### GitHub Flavored Markdown (GFM) Rendering

Based on the [GitHub Flavored Markdown Spec](https://github.github.com/gfm/), GitHub's rendering includes:

- **Base**: CommonMark specification with extensions
- **Tables**: GFM table extension with column alignment support (`|---|`, `|:---|`, `|---:|`, `|:---:|`)
- **Task lists**: `- [ ]` and `- [x]` rendered as checkboxes
- **Strikethrough**: `~~text~~` rendered as `<del>text</del>`
- **Autolinks**: Automatic linking of URLs and email addresses
- **Code blocks**: Syntax highlighting with `language-*` CSS classes
- **HTML support**: `<details>`, `<summary>`, `<code>`, `<b>`, `<em>`, `<strong>`, and other safe HTML tags
- **Emoji**: `:emoji:` syntax converted to Unicode characters or emoji images
- **Line breaks**: Soft breaks combine lines; hard breaks require two trailing spaces or backslash

### Azure DevOps Markdown Rendering

Based on [Azure DevOps Markdown Guidance](https://learn.microsoft.com/en-us/azure/devops/project/wiki/markdown-guidance), Azure DevOps rendering includes:

- **Base**: CommonMark-compatible with some variations
- **Context-specific support**: Different markdown features available in different contexts:
  - **Full support**: Wiki pages and Pull Request comments
  - **Limited support**: Dashboard Markdown widget (excludes extended features for performance)
  - **HTML-based**: Some work item fields (markdown support added later, may vary)
- **Tables**: Similar to GFM tables with column alignment
- **HTML support**: Allows `<details>`, `<summary>`, and most HTML tags (except JavaScript, iframes)
- **Code blocks**: Syntax highlighting support in wikis/PRs; plain preformatted text in dashboard widget
- **Emoji**: `:emoji:` syntax support in pull requests and wikis (not in dashboard widget)
- **Line breaks**: **Critical difference** - Azure DevOps handles line breaks differently than most markdown implementations:
  - Requires **two trailing spaces** for soft breaks (line breaks within a paragraph)
  - Single newline without trailing spaces combines lines into a single paragraph
  - This differs from some markdown parsers that treat single newlines as line breaks
- **Task lists**: `- [ ]` and `- [x]` syntax in PRs and wikis (not in dashboard widget)
- **Table of Contents**: `[[_TOC_]]` syntax (case-sensitive) for auto-generated TOC
- **Mermaid diagrams**: Supported in wikis only (not relevant for tfplan2md reports)
- **Mathematical notation**: KaTeX support in wikis and PRs (not in dashboard widget)
- **Collapsible sections**: `<details>`/`<summary>` widely used
- **Slugification**: Lowercase conversion, spaces to hyphens, RFC 3986 escaping for heading anchors

### Key Differences

1. **Line break handling**: **Most important difference** - Azure DevOps is stricter about trailing spaces for line breaks
   - Standard markdown parsers often convert single newlines to `<br/>` or combine into paragraphs differently
   - ADO requires explicit two trailing spaces for soft breaks
   - May affect how tfplan2md reports with `<br/>` tags render
2. **Inline HTML style handling**: **Critical for diff format rendering**
   - **GitHub**: Strips all CSS style attributes from HTML elements for security (e.g., `<span style="...">` becomes `<span>`)
   - **Azure DevOps**: Preserves inline HTML styles (allows `style` attributes on most HTML tags)
   - **Impact on tfplan2md**: This is why tfplan2md provides two diff formats:
     - `inline-diff` (default): Uses inline HTML with CSS styles for character-level diff highlighting - works perfectly in Azure DevOps but GitHub strips the styles (content remains readable)
     - `simple-diff`: Uses traditional markdown code blocks with `diff` language and `+`/`-` markers - fully portable across both platforms
   - **HTML renderer requirement**: The HTML renderer must handle both formats appropriately:
     - When rendering `inline-diff` for GitHub flavor: Either strip style attributes or convert to GitHub-compatible alternatives
     - When rendering `inline-diff` for Azure DevOps flavor: Preserve inline styles as-is
     - When rendering `simple-diff`: Both flavors can render identically using standard `<pre><code class="language-diff">` blocks
3. **Context-specific feature support**: 
   - Dashboard widget excludes extended features (code highlighting, emojis, checklists, HTML, math) for performance
   - Target context: **Pull Request comments and wikis** (full markdown support)
4. **HTML tag support**: Both support similar safe HTML subsets, but Azure DevOps explicitly blocks JavaScript/iframes
5. **Emoji rendering**: Minor differences in how emojis appear visually (Unicode vs images)
6. **Code highlighting**: Different CSS class naming conventions possible; dashboard widget renders code as plain text
7. **TOC generation**: Uses `[[_TOC_]]` syntax vs GitHub's automatic TOC in some contexts

## Scope

### In Scope

**Core Functionality:**
- Standalone .NET 10 console application as a separate project in the solution
- Command-line interface with options for:
  - Input markdown file path (required)
  - Output HTML file path (optional, derived from input if not provided)
  - HTML flavor selection: `github` or `azdo` (required)
  - Optional wrapper template file path
- Markdown to HTML fragment conversion using a markdown parsing library
- Platform-specific rendering adjustments for GitHub and Azure DevOps flavors

**Diff Format Handling:**

tfplan2md generates large attribute value diffs in two formats: `inline-diff` (default) and `simple-diff`. The HTML renderer must handle both appropriately:

**`inline-diff` format:**
- Uses inline HTML `<span>` elements with CSS `style` attributes for character-level diff highlighting
- Example: `<span style="background-color: #ffeef0; color: #24292e;">old value</span>`
- **Azure DevOps rendering**: Preserves all inline styles - render as-is
- **GitHub rendering**: Strips all `style` attributes - the HTML renderer should:
  - Option A: Strip style attributes to match GitHub behavior (content remains but loses highlighting)
  - Option B: Convert inline styles to CSS classes (requires additional CSS in wrapper template)
  - Recommendation: Strip styles for GitHub flavor (simpler, matches actual GitHub behavior)

**`simple-diff` format:**
- Uses markdown code blocks with `diff` language identifier and `+`/`-` line prefixes
- Example: ` ```diff\n- old line\n+ new line\n``` `
- **Both platforms**: Render identically as `<pre><code class="language-diff">...</code></pre>`
- No platform-specific adjustments needed

This design mirrors how tfplan2md handles the fundamental limitation: **GitHub sanitizes inline HTML styles for security, while Azure DevOps allows them**. The dual format approach ensures both platforms have a good user experience.

**Output Formats:**
- HTML fragment output (rendered markdown content without document structure)
- Optional: Complete HTML document when wrapper template is provided
- Wrapper template with placeholder syntax for fragment insertion (e.g., `{{content}}` or similar)

**Rendering Features:**
- Tables with alignment
- Code blocks with syntax highlighting classes
- HTML elements: `<details>`, `<summary>`, `<code>`, `<b>`, `<em>`, `<strong>`, inline HTML used in tfplan2md reports
- Headings (H1-H6)
- Lists (ordered and unordered)
- Emphasis (bold, italic)
- Links and images
- Line breaks (`<br/>` tags where appropriate)
- Escaping and encoding per CommonMark spec

**Testing:**
- Unit test project for the HTML renderer
- Tests covering both GitHub and Azure DevOps flavors
- Tests for wrapper template functionality
- Validation that output is well-formed HTML

**Default Wrapper Templates:**
- Two default wrapper template files (one for GitHub flavor, one for Azure DevOps flavor)
- Templates include basic HTML structure (`<html>`, `<head>`, `<body>`)
- Templates include CSS styles approximating platform rendering
- Templates stored alongside the renderer project (e.g., `tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`)

### Out of Scope

- **Perfect pixel-by-pixel rendering compatibility** with GitHub or Azure DevOps (approximation is acceptable)
- **Integration into tfplan2md CLI** (this is a separate development tool)
- **Syntax validation** of input markdown (assumes valid markdown from tfplan2md)
- **Advanced markdown features** not used in tfplan2md reports (e.g., footnotes, definition lists, Mermaid diagrams, mathematical notation with KaTeX)
- **JavaScript-based rendering** (static HTML only)
- **CSS frameworks** beyond basic styling (users can customize wrapper templates)
- **Screenshot generation** (tool outputs HTML only; screenshot generation is a separate concern)
- **Emoji image assets** (use Unicode emoji characters or expect users to provide CSS for emoji display)
- **Dashboard widget rendering** for Azure DevOps (tool targets full-featured PR/wiki contexts)

## User Experience

### Command-Line Interface

**Basic Usage - HTML Fragment:**
```bash
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github
```
Output: `artifacts/comprehensive-demo.github.html` (fragment)

```bash
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor azdo \
  --output artifacts/comprehensive-demo-azdo.html
```
Output: `artifacts/comprehensive-demo-azdo.html` (fragment)

**With Wrapper Template:**
```bash
dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
  --input artifacts/comprehensive-demo.md \
  --flavor github \
  --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html \
  --output artifacts/comprehensive-demo-complete.html
```
Output: `artifacts/comprehensive-demo-complete.html` (complete HTML document)

### CLI Options

- `--input` or `-i`: Path to input markdown file (required)
- `--output` or `-o`: Path to output HTML file (optional, derived from input if not provided)
- `--flavor` or `-f`: HTML flavor (`github` or `azdo`) (required)
- `--template` or `-t`: Path to wrapper template file (optional; if omitted, generates HTML fragment only)
- `--help` or `-h`: Display help information
- `--version` or `-v`: Display version information

### Output Filename Derivation

When `--output` is not specified:
- Strip `.md` extension from input filename
- Append `.{flavor}.html` (e.g., `comprehensive-demo.md` → `comprehensive-demo.github.html`)
- Place in same directory as input file

### Wrapper Template Syntax

Templates use a simple placeholder syntax:
- `{{content}}` - Replaced with rendered HTML fragment
- Template files are plain HTML with the placeholder at the desired insertion point

**Example Template:**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Terraform Plan Report</title>
    <style>
        /* GitHub-style CSS */
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif; }
        table { border-collapse: collapse; }
        th, td { border: 1px solid #d0d7de; padding: 6px 13px; }
        code { background-color: #f6f8fa; padding: 0.2em 0.4em; border-radius: 3px; }
        /* ... more styles ... */
    </style>
</head>
<body>
    <div class="markdown-body">
        {{content}}
    </div>
</body>
</html>
```

### Error Handling

**Invalid Input:**
- If input file doesn't exist: Display error message with file path, exit with code 1
- If input file is empty: Display warning, generate empty output, exit with code 0
- If markdown cannot be parsed: Display detailed error with line/column information, exit with code 1

**Invalid Flavor:**
- If `--flavor` is not `github` or `azdo`: Display error message listing valid options, exit with code 1

**Template Issues:**
- If template file doesn't exist: Display error message, exit with code 1
- If template doesn't contain `{{content}}` placeholder: Display error message, exit with code 1

**Output Issues:**
- If output directory doesn't exist: Create directory automatically
- If output file cannot be written: Display error with reason (permissions, disk space, etc.), exit with code 1

## Success Criteria

- [ ] Standalone .NET 10 console application created in `tools/Oocx.TfPlan2Md.HtmlRenderer/`
- [ ] Markdig library integrated for markdown parsing
- [ ] Unit test project created for the HTML renderer
- [ ] CLI accepts all specified options and validates inputs correctly
- [ ] GitHub flavor generates HTML that approximates GitHub PR comment rendering
- [ ] Azure DevOps flavor generates HTML that approximates Azure DevOps PR comment and wiki page rendering
  - Note: Targets full-featured contexts (PRs/wikis), not performance-limited dashboard widget
  - Handles line breaks according to Azure DevOps conventions (two trailing spaces for soft breaks)
- [ ] HTML fragment mode generates valid HTML content without document structure
- [ ] Wrapper template mode generates complete HTML documents with fragment embedded
- [ ] Example wrapper templates provided for both GitHub and Azure DevOps flavors in `tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`
- [ ] Wrapper templates include syntax highlighting via Highlight.js or Prism.js (referenced via CDN)
- [ ] Wrapper templates include reasonable default CSS styles approximating platform appearance
- [ ] Code blocks generate `class="language-*"` attributes for syntax highlighting library compatibility
- [ ] Output filename is correctly derived when `--output` is not specified
- [ ] All markdown features used in tfplan2md reports render correctly:
  - [ ] Tables with alignment
  - [ ] Code blocks with syntax highlighting classes
  - [ ] `<details>` and `<summary>` tags
  - [ ] Bold (`<b>`, `<strong>`) and italic (`<em>`) formatting
  - [ ] Inline `<code>` elements
  - [ ] Headings (H1-H6)
  - [ ] Lists (ordered and unordered)
  - [ ] Links and inline HTML
  - [ ] Line breaks (`<br/>`)
  - [ ] Emoji rendering (as Unicode characters)
- [ ] Diff format handling works correctly for both flavors:
  - [ ] `inline-diff` format: Azure DevOps flavor preserves inline styles
  - [ ] `inline-diff` format: GitHub flavor strips inline styles (matching GitHub behavior)
  - [ ] `simple-diff` format: Both flavors render identically with `<pre><code class="language-diff">`
- [ ] Error messages are clear and actionable
- [ ] Tool can process all existing demo artifacts without errors
- [ ] Unit tests provide good coverage of rendering logic and edge cases
- [ ] Documentation updated to describe the new tool

## Design Decisions

### Library Selection

**Decision:** Use Markdig for markdown parsing.

**Rationale:** 
- Azure DevOps likely uses Markdig internally, making it the best choice for approximating Azure DevOps rendering behavior
- Highly extensible with GFM (GitHub Flavored Markdown) extensions support
- Actively maintained with good CommonMark compliance
- Supports both GitHub and Azure DevOps markdown features

### Emoji Handling

**Decision:** Use Unicode emoji characters.

**Rationale:**
- Simple implementation without requiring emoji image assets
- Consistent with modern platform support for Unicode emoji
- Users can customize emoji rendering via CSS in custom wrapper templates if needed

### Syntax Highlighting

**Decision:** Include basic syntax highlighting in default wrapper templates using a 3rd party library.

**Rationale:**
- Provide out-of-the-box syntax highlighting for better approximation of platform rendering
- Use existing, well-tested libraries rather than implementing highlighting from scratch
- Generate `class="language-*"` attributes on code blocks for library compatibility

**Research Findings:**
- **GitHub**: Uses Linguist (Ruby-based) with TextMate grammar files for syntax highlighting - not directly portable to client-side JavaScript
- **Azure DevOps**: Likely uses server-side rendering (documentation doesn't specify client-side library)
- Both platforms' exact highlighting implementations aren't practical to replicate in a standalone HTML renderer

**Selected Approach:**
Use industry-standard JavaScript syntax highlighting libraries that provide good approximations:
- **Primary choice: Highlight.js** - Widely adopted, extensive language support (190+ languages), battle-tested, easy CDN integration
- **Alternative: Prism.js** - Lightweight, extensible, modern syntax highlighting
- Both libraries generate similar visual output to what users see on GitHub and Azure DevOps

### Template Usage

**Decision:** Templates must always be explicitly specified when using wrapper template mode.

**Rationale:**
- No default template fallback - clearer user intent and fewer surprises
- Default wrapper template files provided in repository as examples (`tools/Oocx.TfPlan2Md.HtmlRenderer/templates/`)
- Users must explicitly pass template file path via `--template` argument
- Removes ambiguity about which template is being used

**CLI Behavior:**
- Without `--template`: Generate HTML fragment only
- With `--template <path>`: Generate complete HTML document using specified template
- No `--template default` option - users must provide explicit paths
