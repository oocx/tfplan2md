# UAT Test Plan: Markdown to HTML Rendering Tool

## Goal
Verify that the `Oocx.TfPlan2Md.HtmlRenderer` tool generates HTML that accurately approximates how GitHub and Azure DevOps render `tfplan2md` reports.

## Artifacts
**Artifact to use:** `artifacts/comprehensive-demo.md`

**Creation Instructions:**
1. Build the tool: `dotnet build tools/Oocx.TfPlan2Md.HtmlRenderer`
2. Generate GitHub version:
   ```bash
   dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
     --input artifacts/comprehensive-demo.md \
     --flavor github \
     --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/github-wrapper.html \
     --output artifacts/uat-027-github.html
   ```
3. Generate Azure DevOps version:
   ```bash
   dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- \
     --input artifacts/comprehensive-demo.md \
     --flavor azdo \
     --template tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html \
     --output artifacts/uat-027-azdo.html
   ```

## Test Steps
1. Open `artifacts/uat-027-github.html` in a web browser.
2. Open `artifacts/uat-027-azdo.html` in a web browser.
3. Compare the rendering with actual PR comments on GitHub and Azure DevOps (if available) or against the expected outcomes below.
4. Compare the generated HTML files with the "Gold Standard" renderings:
   - `docs/features/027-markdown-html-rendering/comprehensive-demo-simple-diff.actual-gh-rendering.html`
   - `docs/features/027-markdown-html-rendering/comprehensive-demo.actual-azdo-rendering.html`

## Validation Instructions (Test Description)

### GitHub Flavor (`uat-027-github.html`)
**Specific Resources/Sections:**
- Any resource with a large attribute change (e.g., `azurerm_key_vault_secret`).
- The "Summary" table.

**Exact Attributes:**
- Look for `inline-diff` sections (character-level diffs).

**Expected Outcome:**
- The `inline-diff` sections should NOT have background colors (red/green). The text should be plain but readable. This matches GitHub's behavior of stripping `style` attributes.
- The overall look and feel (fonts, table borders) should resemble GitHub.
- Syntax highlighting should be present in code blocks (if Highlight.js is working in the template).

**Before/After Context:**
- Previously, we didn't have a tool to preview this. This tool allows us to see exactly what GitHub will do to our `inline-diff` HTML.

### Azure DevOps Flavor (`uat-027-azdo.html`)
**Specific Resources/Sections:**
- Any resource with a large attribute change.
- Paragraphs with single line breaks.

**Exact Attributes:**
- Look for `inline-diff` sections.
- Look for line breaks in the "Description" or "Notes" sections.

**Expected Outcome:**
- The `inline-diff` sections SHOULD have background colors (red/green) for additions and deletions.
- Line breaks should only occur where there are two trailing spaces in the markdown. Single newlines should be collapsed into a single line of text.
- The overall look and feel should resemble Azure DevOps.

**Before/After Context:**
- Azure DevOps preserves inline styles, so the `inline-diff` should be much more readable than on GitHub. This tool verifies that we are preserving those styles for the `azdo` flavor.
