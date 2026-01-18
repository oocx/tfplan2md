# Tasks: Markdown Quality Validation

## Overview

Improve markdown generation quality to prevent rendering errors in GitHub and Azure DevOps Services. This feature ensures that `tfplan2md` generates valid, well-formed markdown that renders correctly on both platforms, with comprehensive validation at build time and runtime.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)

## Tasks

### Task 1: Document Markdown Subset Specification

**Priority:** High

**Description:**
Create a document that describes the subset of markdown features supported by `tfplan2md` that are compatible with both GitHub and Azure DevOps Services.

**Acceptance Criteria:**
- [ ] File `docs/markdown-specification.md` created.
- [ ] Includes examples of supported features (headings, tables, lists, code blocks).
- [ ] Documents escaping rules for special characters.
- [ ] Explicitly mentions `<br/>` for line breaks in tables.

**Dependencies:** None

---

### Task 2: Setup Test Infrastructure and Data

**Priority:** High

**Description:**
Add the `Markdig` library to the test project and create a test plan JSON that contains characters known to break markdown.

**Acceptance Criteria:**
- [ ] `Markdig` package added to `Oocx.TfPlan2Md.Tests.csproj`.
- [ ] `src/tests/Oocx.TfPlan2Md.Tests/TestData/markdown-breaking-plan.json` created with:
    - Resource names containing pipes `|` and asterisks `*`.
    - Tag values containing newlines `\n`.
    - Attribute values containing brackets `[` `]` and backticks `` ` ``.

**Dependencies:** None

---

### Task 3: Implement Structural Validation Tests (Discovery)

**Priority:** High

**Description:**
Implement tests that use `Markdig` to validate the structure of the generated markdown. These tests should fail initially, revealing the current bugs.

**Acceptance Criteria:**
- [ ] `src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs` created.
- [ ] `Render_BreakingPlan_ValidatesTableStructure` implemented (TC-04).
- [ ] `Render_Headings_ValidatesStructure` implemented (TC-05).
- [ ] `Render_ComprehensiveDemo_ValidatesFullReport` implemented (TC-06).
- [ ] Tests fail as expected when run against current templates.

**Dependencies:** Task 2

---

### Task 4: Implement Markdown Escaping Helper

**Priority:** High

**Description:**
Add a custom Scriban helper `escape_markdown` to handle special characters and newlines.

**Acceptance Criteria:**
- [ ] `EscapeMarkdown` method added to `ScribanHelpers.cs`.
- [ ] `escape_markdown` registered in `RegisterHelpers`.
- [ ] Pipes `|` are escaped as `\|`.
- [ ] Newlines `\n` are replaced with `<br/>`.
- [ ] Special characters `*`, `_`, `[`, `]`, `` ` `` are escaped.
- [ ] Unit tests `TC-01`, `TC-02`, `TC-03` implemented and passing.

**Dependencies:** Task 3

---

### Task 5: Update Built-in Templates

**Priority:** High

**Description:**
Update all built-in templates to use the `escape_markdown` filter and ensure correct spacing around block elements (headings and tables).

**Acceptance Criteria:**
- [ ] All dynamic values in `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/*.sbn` use `| escape_markdown`.
- [ ] Blank lines added before and after all headings (`#`, `##`, etc.).
- [ ] Blank lines added before and after all tables.
- [ ] Structural validation tests from Task 3 now pass.

**Dependencies:** Task 4

---

### Task 6: Integrate Markdown Linting in CI

**Priority:** Medium

**Description:**
Add `markdownlint-cli2` to the CI pipeline to validate the generated markdown from the comprehensive demo.

**Acceptance Criteria:**
- [ ] `.markdownlint.json` created in the root directory with rules compatible with GFM and Azure DevOps.
- [ ] `.github/workflows/ci.yml` updated to:
    - Generate the comprehensive demo report during the build.
    - Run `markdownlint-cli2` against the generated report.
- [ ] CI fails if the generated report has linting errors.

**Dependencies:** Task 5

---

### Task 7: Update Agent Instructions for Local Linting

**Priority:** Medium

**Description:**
Update agent instructions to ensure that agents run the markdown linter locally before creating a PR.

**Acceptance Criteria:**
- [ ] `.github/agents/developer.agent.md` updated to include a step for running the markdown linter on the comprehensive demo output.
- [ ] `.github/agents/code-reviewer.agent.md` updated to verify that linting was performed.

**Dependencies:** Task 6

---

### Task 8: Visual Rendering Tests (Optional/Bonus)

**Priority:** Low

**Description:**
Implement a test that renders the markdown to HTML to catch any remaining rendering issues.

**Acceptance Criteria:**
- [ ] Test case added that renders the comprehensive demo to HTML using `Markdig`.
- [ ] Validates that no "raw" markdown remains in the HTML (e.g. unparsed tables).

**Dependencies:** Task 5

## Implementation Order

1. **Task 1** - Define the target format.
2. **Task 2** - Prepare the tools and data.
3. **Task 3** - Confirm the bugs with failing tests.
4. **Task 4** - Build the fix (helper).
5. **Task 5** - Apply the fix (templates).
6. **Task 6** - Automate quality check in CI.
7. **Task 7** - Update process for agents.
8. **Task 8** - Final safety check.

## Open Questions

- None.
