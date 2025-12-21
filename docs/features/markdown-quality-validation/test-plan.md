# Test Plan: Markdown Quality Validation

## Overview

This test plan validates the "Markdown Quality Validation" feature, which ensures that the generated markdown is valid, well-formed, and renders correctly on GitHub and Azure DevOps. The plan covers unit tests for the new escaping helper, integration tests using `Markdig` to validate markdown structure, and CI integration for linting.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Escape pipes `\|` in values | TC-01 | Unit |
| Escape newlines in values | TC-02 | Unit |
| Escape other special chars (`*`, `_`, etc.) | TC-03 | Unit |
| Tables have correct structure (Markdig validation) | TC-04 | Integration |
| Headings have correct structure (Markdig validation) | TC-05 | Integration |
| Built-in templates produce valid markdown | TC-06 | Integration |
| Comprehensive demo renders correctly | TC-06 | Integration |
| CI fails on invalid markdown | TC-07 | CI/Linting |

## Test Cases

### TC-01: EscapeMarkdown_Pipes_Escaped

**Type:** Unit

**Description:**
Verifies that the `EscapeMarkdown` helper correctly escapes pipe characters `|` which would otherwise break table cells.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.EscapeMarkdown("value | with | pipes")`.
2. Assert result is `value \| with \| pipes`.

**Expected Result:**
Pipes are escaped with backslashes.

---

### TC-02: EscapeMarkdown_Newlines_Replaced

**Type:** Unit

**Description:**
Verifies that the `EscapeMarkdown` helper replaces newlines with `<br/>` to prevent breaking table rows.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.EscapeMarkdown("line1\nline2")`.
2. Assert result contains `line1<br/>line2` (or similar safe replacement).

**Expected Result:**
Newlines are replaced by HTML line breaks.

---

### TC-03: EscapeMarkdown_SpecialChars_Escaped

**Type:** Unit

**Description:**
Verifies that other markdown special characters are escaped to prevent unintended formatting.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.EscapeMarkdown` with string containing `*`, `_`, `[`, `]`, `` ` ``.
2. Assert result has all characters escaped (e.g., `\*`, `\_`).

**Expected Result:**
Special characters are escaped.

---

### TC-04: Render_BreakingPlan_ValidatesTableStructure

**Type:** Integration

**Description:**
Verifies that a plan containing "breaking" characters (pipes, newlines) renders a valid markdown table structure when processed with the updated templates.

**Preconditions:**
- `TestData/markdown-breaking-plan.json` exists (contains pipes, newlines in values).

**Test Steps:**
1. Parse `markdown-breaking-plan.json`.
2. Render using `MarkdownRenderer`.
3. Parse the output using `Markdig`.
4. Assert that `Markdig` identifies the expected number of `Table` objects.
5. Assert that table cells contain the expected (escaped) text.

**Expected Result:**
Markdig successfully parses the tables, meaning the markdown structure is valid.

---

### TC-05: Render_Headings_ValidatesStructure

**Type:** Integration

**Description:**
Verifies that headings are correctly recognized by a markdown parser (implying correct spacing around them).

**Preconditions:**
- Standard test plan.

**Test Steps:**
1. Render a plan.
2. Parse output with `Markdig`.
3. Assert that `HeadingBlock` objects are found for expected sections ("Summary", "Resource Changes").

**Expected Result:**
Headings are correctly parsed.

---

### TC-06: Render_ComprehensiveDemo_ValidatesFullReport

**Type:** Integration

**Description:**
Runs the full comprehensive demo plan through the renderer and validates the entire document structure.

**Preconditions:**
- `examples/comprehensive-demo/plan.json` exists.

**Test Steps:**
1. Render the comprehensive demo plan.
2. Parse with `Markdig`.
3. Validate presence of key sections (Summary table, Module headers).
4. Validate no broken HTML blocks or unparsed content.

**Expected Result:**
Full report is valid markdown.

---

### TC-07: CI_MarkdownLint_Check

**Type:** CI/Linting

**Description:**
Verifies that `markdownlint-cli2` runs in the CI pipeline and checks the generated output of the comprehensive demo.

**Preconditions:**
- `.markdownlint.json` exists.
- CI workflow updated.
- Comprehensive demo output is generated during build.

**Test Steps:**
1. (Manual/CI) Push a change that generates invalid markdown in the comprehensive demo (e.g., remove a newline before a table in a template).
2. Verify CI fails.
3. Revert change.
4. Verify CI passes.
5. **Developer Requirement**: Developers must run the linter locally on the generated demo output before creating a PR.

**Expected Result:**
CI enforces markdown quality on the comprehensive demo output.

## Test Data Requirements

- `TestData/markdown-breaking-plan.json`: A Terraform plan JSON specifically crafted to include:
    - Resource names with pipes and asterisks.
    - Tag values with newlines.
    - Attribute values with brackets and backticks.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Null input to escape helper | Returns empty string | TC-01 (variation) |
| Empty string input | Returns empty string | TC-01 (variation) |
| String with only special chars | All escaped | TC-03 |
| Very long string with special chars | Escaped correctly, no truncation | TC-03 |

## Open Questions

- None. We will use `<br/>` for line breaks as it is safer for broader compatibility.

## Process Requirements

- **Linting**: Developers/Agents must run `markdownlint-cli2` locally before creating a PR. This is enforced via CI, but local execution is required to avoid CI churn.
