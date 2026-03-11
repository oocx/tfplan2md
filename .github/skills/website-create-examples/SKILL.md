---
name: website-create-examples
description: Create and update interactive examples for the Eleventy website using page entrypoints and src/examples fragments.
---

# Skill Instructions

## Purpose

Create and update interactive examples on the tfplan2md website using the Eleventy authoring model.

## When to Use This Skill

- Adding new feature examples to feature detail pages
- Creating examples for the main examples page
- Updating existing examples when output format changes
- Adding before/after comparisons for feature pages

## Current Authoring Model

Interactive examples are not hand-authored inline as large HTML blocks on page files.

- Page entrypoints live under `website/src/pages/`
- Interactive example source-of-truth lives under `website/src/examples/<example-id>/`
- Each example directory contains:
  - `meta.json` for the example title and metadata
  - `rendered.html` for the rendered output pane
  - `source.html` for the source pane markup
- Pages render examples through the Eleventy `exampleBlock` shortcode defined in `website/.eleventy.js`
- Shared interactive behavior is initialized globally from `website/src/site-assets/js/site.js`, so page-local script tags are not needed

## Hard Rules

### Must
- [ ] Edit page entrypoints in `website/src/pages/` and example fragments in `website/src/examples/`; do not hand-edit generated output in `website/dist/`.
- [ ] Keep example IDs stable and descriptive, following the existing `<page-or-section>--example-<n>` pattern.
- [ ] Source example content from real repository artifacts such as `artifacts/`, `examples/`, generated demo output, or other repository-backed material.
- [ ] Run `scripts/website-verify.sh --all` after adding or changing examples.

### Must Not
- [ ] Do not reintroduce page-local JavaScript blobs or per-page script tags for interactive examples.
- [ ] Do not invent rendered output or source content that is not grounded in the repository.

## Where to Get Example Content

### Source 1: artifacts/ Directory (Primary)

The `artifacts/` directory contains pre-generated markdown and HTML examples from the comprehensive demo:

**Markdown Source:**
- `artifacts/comprehensive-demo.md` - Full markdown output
- `artifacts/comprehensive-demo-simple-diff.md` - Simplified diff format

**Rendered HTML:**
- `artifacts/comprehensive-demo.github.html` - GitHub-rendered HTML
- `artifacts/comprehensive-demo.azdo.html` - Azure DevOps-rendered HTML

**How to Extract:**
1. Open the markdown file
2. Search for the resource type or feature (e.g., `azurerm_firewall_network_rule_collection`)
3. Copy the relevant section for the source view
4. Open the corresponding `.github.html` file
5. Find the matching section for the rendered view
6. Extract the HTML that should appear inside the rendered pane

### Source 2: Generate New Examples

If the exact example doesn't exist in artifacts:

1. **Run the demo generation script:**
   ```bash
   scripts/generate-demo-artifacts.sh
   ```
   This regenerates all artifacts from the current codebase.

2. **Create custom examples:**
   ```bash
   # Create a custom Terraform plan
   cd examples/
   # Modify existing demo or create new scenario
   terraform plan -out=plan.tfplan
   terraform show -json plan.tfplan > plan.json
   
   # Generate markdown
   docker run -v $(pwd):/data oocx/tfplan2md:latest plan.json > output.md
   ```

3. **Render to HTML:**
   Use the HtmlRenderer tool:
   ```bash
   dotnet run --project src/tools/Oocx.TfPlan2Md.HtmlRenderer \
     --input artifacts/comprehensive-demo.md \
     --output artifacts/custom-example.github.html \
     --platform github
   ```

## Step-by-Step: Add a New Example

### 1. Choose the page entrypoint

- Open the relevant page under `website/src/pages/`
- Existing examples page entrypoint: `website/src/pages/examples.njk`
- Existing feature detail pages live under `website/src/pages/features/`

### 2. Create or update the example directory

- Create or update `website/src/examples/<example-id>/`
- Add or update `meta.json`
- Add or update `rendered.html`
- Add or update `source.html`

### 3. Keep the example fragment responsibilities clear

- `meta.json` holds the example title and metadata used by the renderer
- `rendered.html` contains the rendered example pane content
- `source.html` contains the source-pane markup, typically a `<pre><code>...</code></pre>` block
- Keep the title specific, for example `Firewall Network Rule Collection Output`

### 4. Reference the example from the page

- Use the `exampleBlock` shortcode from the relevant `.njk` page:

```njk
{{ exampleBlock("features--firewall-rules--example-1") | safe }}
```

- Place it inside the existing page structure and surrounding content blocks
- Reuse existing macros and section patterns from `website/src/_includes/components/`

### 5. Verify the rendered result

- Run `scripts/website-verify.sh --all`
- Preview the relevant page in `website/dist/`
- Use browser tools to confirm toggle behavior, fullscreen behavior, and responsive layout

## Step-by-Step: Update an Existing Example

1. **Regenerate artifacts:**
   ```bash
   scripts/generate-demo-artifacts.sh
   ```

2. **Locate the example:**
   - Find the `exampleBlock("...")` call in `website/src/pages/`
   - Open the matching directory under `website/src/examples/`

3. **Update rendered view:**
   - Extract new HTML from regenerated artifacts
   - Replace content in `rendered.html`

4. **Update source view:**
   - Extract new markdown from regenerated artifacts
   - Escape HTML characters when needed
   - Replace content in `source.html`

5. **Test:**
   - Open the page in a browser preview
   - Toggle between rendered and source views
   - Verify content renders correctly
   - Test fullscreen mode

## Content Guidelines

### Rendered View Content
- Use semantic HTML: headings, tables, code blocks, details/summary, lists, and other markup that matches the actual rendered output
- Preserve emoji and icons from real tfplan2md output when present
- Keep inline styles only when they are part of the real rendered output
- Do not replace the shared wrapper generated by the shortcode with custom page markup

### Source View Content
- Show the exact source content that explains or generates the rendered output
- Escape HTML characters when source is rendered inside `<code>`
- Preserve whitespace and indentation
- Include markdown, HTML, or mixed source only when that matches the real source material
- This is what users would copy or inspect alongside the rendered result

### Title Guidelines
- Be specific: "Firewall Network Rule Collection Output" not "Example Output"
- Describe the resource type or feature shown
- Keep under 60 characters for readability

## Common Patterns

### Table-Based Examples
Most features show tables. Rendered view has `<table>`, source view has pipe-separated markdown.

### Diff Examples (Firewall/NSG Rules)
Use inline styles for before/after highlighting. Keep all styles in rendered view; show escaped HTML tags in source view.

### Module Grouping
Show hierarchical structure with `<details>` in rendered view; show markdown `<details>` tags in source view.

### Large Values
Show collapsed sections in rendered view; show the markdown collapsible syntax in source view.

## Troubleshooting

### Toggle buttons don't work
- Check `website/src/site-assets/js/site.js` and the interactive examples module wiring
- Check browser console for JavaScript errors
- Confirm the page is using the shared base layout and not bypassing the normal site bootstrap

### Rendered view looks wrong
- Verify `rendered.html` is valid and complete
- Check that the page still uses the shared example wrapper emitted by the shortcode
- Ensure shared styles and client-side assets are loading in the rendered page

### Source view doesn't highlight
- Verify `source.html` contains the expected code block structure
- Check that HTML is properly escaped
- Check shared syntax-highlighting behavior in the previewed page

### Content overflows
- Use fullscreen mode for large examples
- Consider splitting very large examples into multiple smaller ones
- Ensure tables are not too wide (test on mobile)

## Files Modified/Created

When adding examples, these files are typically involved:

- **Page entrypoints:** `website/src/pages/*.njk`, `website/src/pages/features/*.njk`
- **Example fragments:** `website/src/examples/<example-id>/meta.json`, `rendered.html`, `source.html`
- **Shared JavaScript:** `website/src/site-assets/js/` (rarely modified)
- **Source content:** `artifacts/*.md` (regenerated via script)
- **Rendered content:** `artifacts/*.html` (regenerated via script)

## Validation Checklist

Before committing changes:

- [ ] Interactive example has correct HTML structure
- [ ] Example is referenced from the correct `.njk` page using `exampleBlock`
- [ ] Rendered view shows proper HTML output
- [ ] Source view shows the correct escaped source when needed
- [ ] Title is descriptive and specific
- [ ] Toggle between views works
- [ ] Fullscreen mode works
- [ ] Content renders correctly in both light and dark themes
- [ ] Mobile responsive (test narrow viewport)
- [ ] No JavaScript console errors

## Related Skills

- **website-visual-assets** - Generate screenshots and HTML exports
- **generate-demo-artifacts** - Regenerate the comprehensive demo
- **website-quality-check** - Verify overall website quality
