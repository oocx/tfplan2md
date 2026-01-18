# Agent Skill: Create and Update Website Examples

## Purpose

Create and update interactive examples on the tfplan2md website that show both rendered markdown output and source code with toggle functionality.

## When to Use This Skill

- Adding new feature examples to feature detail pages
- Creating examples for the main examples page
- Updating existing examples when output format changes
- Adding before/after comparisons for feature pages

## Example Component Structure

All interactive examples use a consistent HTML structure with shared JavaScript functionality:

### Required HTML Structure

```html
<div class="code-block interactive-example">
    <div class="code-header">
        <span class="code-title">Example Title Here</span>
        <div class="example-controls">
            <div class="view-toggle">
                <button class="toggle-btn active" data-view="rendered">Rendered</button>
                <button class="toggle-btn" data-view="source">Source</button>
            </div>
            <button class="fullscreen-btn" aria-label="Toggle Fullscreen">⛶</button>
        </div>
    </div>
    <div class="example-content">
        <div class="view-pane rendered-view active">
            <!-- RENDERED HTML CONTENT GOES HERE -->
        </div>
        <div class="view-pane source-view">
            <pre><code><!-- MARKDOWN SOURCE GOES HERE --></code></pre>
        </div>
    </div>
</div>
```

### Required Script Tag

Every page using interactive examples must include the shared JavaScript:

```html
<!-- For pages in website/ root -->
<script src="assets/js/interactive-examples.js"></script>

<!-- For pages in website/features/ -->
<script src="../assets/js/interactive-examples.js"></script>
```

The script must be included **after** the theme toggle script and **before** the closing `</body>` tag.

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
6. Extract the HTML (strip any GitHub-specific wrappers like `<markdown-accessiblity-table>`)

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

### For Feature Detail Pages

1. **Identify the source content:**
   - Locate the feature section in `artifacts/comprehensive-demo.md`
   - Find the corresponding HTML in `artifacts/comprehensive-demo.github.html`

2. **Extract rendered HTML:**
   - Copy the HTML from the .github.html file
   - Remove any platform-specific wrappers (e.g., `<markdown-accessiblity-table>`)
   - Keep semantic HTML (headings, tables, code blocks)
   - Ensure emoji and icons are preserved

3. **Extract markdown source:**
   - Copy the markdown from the .md file
   - Escape HTML special characters in the source view:
     - `<` → `&lt;`
     - `>` → `&gt;`
     - `&` → `&amp;`

4. **Insert into page:**
   - Open the feature detail HTML page
   - Find or create a comparison section
   - Add the interactive-example structure
   - Paste rendered HTML into `.rendered-view`
   - Paste escaped markdown into `.source-view` `<code>` block

5. **Set a descriptive title:**
   - Update `.code-title` with a clear, specific title
   - Examples: "Firewall Network Rule Collection Output", "NSG Security Rules Diff"

6. **Verify script tag exists:**
   - Check that `<script src="../assets/js/interactive-examples.js"></script>` is present
   - Should be before closing `</body>` tag

### For Examples Page

Follow the same process, but use `src="assets/js/interactive-examples.js"` (no `../` prefix).

## Step-by-Step: Update an Existing Example

1. **Regenerate artifacts:**
   ```bash
   scripts/generate-demo-artifacts.sh
   ```

2. **Locate the example:**
   - Find the HTML page with the example
   - Identify the `.interactive-example` div

3. **Update rendered view:**
   - Extract new HTML from regenerated artifacts
   - Replace content inside `.rendered-view`

4. **Update source view:**
   - Extract new markdown from regenerated artifacts
   - Escape HTML characters
   - Replace content inside `.source-view` `<code>` block

5. **Test:**
   - Open the page in a browser
   - Toggle between rendered and source views
   - Verify content renders correctly
   - Test fullscreen mode

## Content Guidelines

### Rendered View Content
- Use semantic HTML: `<h3>`, `<table>`, `<code>`, etc.
- Preserve all emoji and icons from tfplan2md output
- Keep inline styles from complex diffs (e.g., firewall rule changes)
- No markdown - this is the final rendered HTML

### Source View Content
- Show the exact markdown that generates the rendered output
- Escape all HTML characters (`<`, `>`, `&`)
- Preserve whitespace and indentation
- Include markdown table syntax, headings, code blocks
- This is what users would put in their PR comments

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
- Check that `interactive-examples.js` script tag is present
- Verify script path is correct (`assets/` vs `../assets/`)
- Check browser console for JavaScript errors

### Rendered view looks wrong
- Verify HTML is valid (not missing closing tags)
- Check that CSS classes are correct
- Ensure styles from `style.css` apply to content

### Source view doesn't highlight
- The JavaScript auto-highlights markdown syntax
- Verify content is inside `.source-view code` elements
- Check that HTML is properly escaped

### Content overflows
- Use fullscreen mode for large examples
- Consider splitting very large examples into multiple smaller ones
- Ensure tables are not too wide (test on mobile)

## Files Modified/Created

When adding examples, these files are typically involved:

- **HTML pages:** `website/examples.html`, `website/features/*.html`
- **Shared JavaScript:** `website/assets/js/interactive-examples.js` (rarely modified)
- **Source content:** `artifacts/*.md` (regenerated via script)
- **Rendered content:** `artifacts/*.html` (regenerated via script)

## Validation Checklist

Before committing changes:

- [ ] Interactive example has correct HTML structure
- [ ] Script tag included with correct path
- [ ] Rendered view shows proper HTML output
- [ ] Source view shows escaped markdown
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

## Examples Repository

Current interactive examples on the website:

1. **examples.html:**
   - 5 firewall/NSG/role examples

2. **Feature detail pages:**
   - `features/firewall-rules.html` - Firewall semantic diff
   - `features/nsg-rules.html` - NSG security rules diff
   - `features/azure-optimizations.html` - 2 examples (Key Vault, Role Assignment)
   - `features/sensitive-masking.html` - Sensitive value masking
   - `features/module-grouping.html` - Module-based grouping
   - `features/large-values.html` - Large value diff formatting

Reference these for consistent patterns and structure.
