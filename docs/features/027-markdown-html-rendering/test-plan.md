# Test Plan: Markdown to HTML Rendering Tool

## Overview

This test plan covers the `Oocx.TfPlan2Md.HtmlRenderer` tool, which converts `tfplan2md` markdown reports into HTML fragments or documents, approximating the rendering of GitHub and Azure DevOps.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| CLI accepts all specified options and validates inputs | TC-01, TC-02, TC-03 | Integration |
| Output filename correctly derived when omitted | TC-04 | Integration |
| GitHub flavor strips `style` attributes from `inline-diff` | TC-05 | Unit |
| Azure DevOps flavor preserves `style` attributes | TC-06 | Unit |
| Azure DevOps flavor handles line breaks correctly | TC-07 | Unit |
| Both flavors render `simple-diff` as code blocks | TC-08 | Unit |
| Markdown features (tables, details, etc.) render correctly | TC-09 | Unit |
| Wrapper template mode generates complete HTML | TC-10 | Unit |
| Missing `{{content}}` in template causes error | TC-11 | Unit |
| Tool can process all existing demo artifacts | TC-12 | Integration |
| Error messages for missing files or invalid flavors | TC-13 | Integration |
| Output matches Gold Standard renderings | TC-17, TC-18 | Integration |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: GitHub Flavor Rendering

**User Goal**: Verify that a `tfplan2md` report renders correctly in a GitHub-like environment, specifically checking that `inline-diff` styles are stripped but content remains readable.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.

**Expected Output**:
- A complete HTML document using the GitHub wrapper template.
- `inline-diff` sections should NOT have background colors (as GitHub strips them), but the text should be present.
- Tables, code blocks, and collapsible sections (`<details>`) should look similar to GitHub's UI.

**Success Criteria**:
- [ ] Output renders correctly in a browser.
- [ ] `style` attributes are absent in the HTML fragment for GitHub flavor.
- [ ] Collapsible sections work.

---

### Scenario 2: Azure DevOps Flavor Rendering

**User Goal**: Verify that a `tfplan2md` report renders correctly in an Azure DevOps-like environment, specifically checking that `inline-diff` styles are preserved and line breaks follow ADO rules.

**Test PR Context**:
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- A complete HTML document using the Azure DevOps wrapper template.
- `inline-diff` sections SHOULD have background colors (red/green) as ADO preserves inline styles.
- Line breaks within paragraphs should only occur if there are two trailing spaces.

**Success Criteria**:
- [ ] Output renders correctly in a browser.
- [ ] `style` attributes are present in the HTML fragment for Azure DevOps flavor.
- [ ] Line breaks match ADO expectations.

## Test Cases

### TC-01: CLI_ValidOptions_Success

**Type:** Integration

**Description:**
Verify that the CLI accepts all required options and executes successfully.

**Preconditions:**
- `tfplan2md` report exists at `tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/comprehensive-demo.md`

**Test Steps:**
1. Run `dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- --input <path> --flavor github`
2. Check exit code.

**Expected Result:**
- Exit code 0.
- Output file created at `<path>.github.html`.

---

### TC-02: CLI_MissingRequiredOptions_Error

**Type:** Integration

**Description:**
Verify that the CLI fails when required options are missing.

**Test Steps:**
1. Run `dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- --input <path>` (missing flavor)
2. Check exit code and error message.

**Expected Result:**
- Exit code 1.
- Error message indicates `--flavor` is required.

---

### TC-03: CLI_InvalidFlavor_Error

**Type:** Integration

**Description:**
Verify that the CLI fails when an invalid flavor is provided.

**Test Steps:**
1. Run `dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- --input <path> --flavor invalid`
2. Check exit code and error message.

**Expected Result:**
- Exit code 1.
- Error message lists valid flavors (`github`, `azdo`).

---

### TC-04: CLI_OutputFilenameDerivation_CorrectPath

**Type:** Integration

**Description:**
Verify that the output filename is correctly derived when `--output` is omitted.

**Test Steps:**
1. Run `dotnet run --project tools/Oocx.TfPlan2Md.HtmlRenderer -- --input reports/my-plan.md --flavor github`
2. Verify existence of `reports/my-plan.github.html`.

**Expected Result:**
- File `reports/my-plan.github.html` exists.

---

### TC-05: RenderFragment_GitHubFlavor_StripsInlineStyles

**Type:** Unit

**Description:**
Verify that the GitHub flavor strips `style` attributes from HTML tags.

**Test Data:**
Markdown: `<span style="color: red;">deleted</span>`

**Expected Result:**
HTML: `<span>deleted</span>` (or similar, without `style`)

---

### TC-06: RenderFragment_AzDoFlavor_PreservesInlineStyles

**Type:** Unit

**Description:**
Verify that the Azure DevOps flavor preserves `style` attributes.

**Test Data:**
Markdown: `<span style="color: red;">deleted</span>`

**Expected Result:**
HTML: `<span style="color: red;">deleted</span>`

---

### TC-07: RenderFragment_AzDoFlavor_LineBreaks

**Type:** Unit

**Description:**
Verify that Azure DevOps flavor handles line breaks correctly (requires two spaces for soft break).

**Test Data:**
Markdown:
```
Line 1
Line 2
Line 3  
Line 4
```

**Expected Result:**
- Line 1 and Line 2 are combined in the same paragraph without `<br>`.
- Line 3 and Line 4 have a line break between them (e.g., `<br>` or separate paragraphs depending on Markdig config).

---

### TC-08: RenderFragment_SimpleDiff_RendersAsCodeBlock

**Type:** Unit

**Description:**
Verify that `simple-diff` (fenced code blocks with `diff`) renders correctly in both flavors.

**Test Data:**
Markdown:
```diff
- old
+ new
```

**Expected Result:**
HTML: `<pre><code class="language-diff">...</code></pre>`

---

### TC-09: RenderFragment_MarkdownFeatures_CorrectHtml

**Type:** Unit

**Description:**
Verify that standard markdown features used by `tfplan2md` render correctly.

**Test Data:**
- Tables with alignment
- `<details>` and `<summary>`
- Headings
- Bold/Italic

**Expected Result:**
- Valid HTML structure for each feature.

---

### TC-10: ApplyTemplate_ValidTemplate_CompleteHtml

**Type:** Unit

**Description:**
Verify that the wrapper template correctly embeds the fragment.

**Test Data:**
Template: `<html><body>{{content}}</body></html>`
Fragment: `<h1>Hello</h1>`

**Expected Result:**
`<html><body><h1>Hello</h1></body></html>`

---

### TC-11: ApplyTemplate_MissingPlaceholder_Error

**Type:** Unit

**Description:**
Verify that a template without `{{content}}` causes an error.

**Expected Result:**
- Exception or error message indicating missing placeholder.

---

### TC-12: Integration_ProcessDemoArtifacts_Success

**Type:** Integration

**Description:**
Verify that the tool can process all existing demo artifacts in the `artifacts/` directory.

**Test Steps:**
1. For each `.md` file in `artifacts/`:
   - Run the tool for both `github` and `azdo` flavors.
2. Verify exit code 0 for all.

---

### TC-13: CLI_MissingInputFile_Error

**Type:** Integration

**Description:**
Verify that the tool fails gracefully if the input file does not exist.

**Test Steps:**
1. Run tool with `--input non-existent.md`
2. Check exit code 1 and error message.

### TC-17: GoldStandard_GitHub_Match

**Type:** Integration

**Description:**
Verify that the generated HTML for GitHub flavor is "close enough" to the actual GitHub rendering.

**Preconditions:**
- `comprehensive-demo-simple-diff.actual-gh-rendering.html` exists.

**Test Steps:**
1. Run tool with `comprehensive-demo-simple-diff.md` and `--flavor github`.
2. Compare output with `comprehensive-demo-simple-diff.actual-gh-rendering.html`.

**Expected Result:**
- The core HTML structure (tables, details, code blocks) matches.
- Minor differences in attributes like `dir="auto"` or `class="notranslate"` are acceptable if they don't affect the primary goal of the tool.

---

### TC-18: GoldStandard_AzDo_Match

**Type:** Integration

**Description:**
Verify that the generated HTML for Azure DevOps flavor is "close enough" to the actual ADO rendering.

**Preconditions:**
- `comprehensive-demo.actual-azdo-rendering.html` exists.

**Test Steps:**
1. Run tool with `comprehensive-demo.md` and `--flavor azdo`.
2. Compare output with `comprehensive-demo.actual-azdo-rendering.html`.

**Expected Result:**
- The core HTML structure matches.
- `style` attributes on `<span>` elements are preserved.
- `id` attributes on headings match the `user-content-` prefix pattern.

## Test Data Requirements

- `tests/Oocx.TfPlan2Md.HtmlRenderer.Tests/TestData/`
  - `inline-diff.md` - Snippet with `<span>` and `style`.
  - `simple-diff.md` - Snippet with ` ```diff `.
  - `line-breaks.md` - Snippet testing ADO line break rules.
  - `comprehensive.md` - A full report for integration testing.
- `docs/features/027-markdown-html-rendering/`
  - `comprehensive-demo.actual-azdo-rendering.html` - Gold standard for ADO.
  - `comprehensive-demo-simple-diff.actual-gh-rendering.html` - Gold standard for GitHub.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty input file | Warning, empty output, exit 0 | TC-14 |
| Input file with only whitespace | Empty output, exit 0 | TC-14 |
| Template with multiple `{{content}}` | Replace all or first (define behavior) | TC-15 |
| Markdown with nested HTML | Preserve safe tags, strip styles if GitHub | TC-16 |

## Non-Functional Tests

- **Performance**: Tool should process a large (1MB+) markdown file in under 2 seconds.
- **Compatibility**: Output HTML should be viewable in modern browsers (Chrome, Firefox, Safari, Edge).

## Open Questions

- Should we support multiple `{{content}}` placeholders in a template? (Architecture says "strictly requires the placeholder", implying at least one).
- Do we need to handle emoji shortcodes (e.g., `:white_check_mark:`) or only Unicode? (Spec says "Unicode emoji characters").
