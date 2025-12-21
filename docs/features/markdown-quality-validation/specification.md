# Feature: Markdown Quality Validation

## Overview

Improve markdown generation quality to prevent rendering errors in GitHub and Azure DevOps Services. This feature ensures that tfplan2md generates valid, well-formed markdown that renders correctly on both platforms, with comprehensive validation at build time and runtime.

## User Goals

- **DevOps Engineers** need markdown reports that render correctly in PR comments on GitHub and Azure DevOps Services without manual fixes
- **Project Maintainers** need confidence that releases don't contain markdown rendering bugs
- **Template Authors** need clear guidelines on which markdown features are safe to use

## Problem Statement

Recent releases have shipped with markdown rendering errors discovered when pasting generated output into Azure DevOps PR comments:
- Tables not rendering due to extra line breaks inside tables or missing line breaks around tables
- Headings not rendering due to missing line breaks before/after headings

These issues indicate:
1. Insufficient test coverage for markdown structure
2. No documented markdown subset specification
3. Lack of automated validation tools in the build pipeline
4. Potential escaping issues with special characters from Terraform plan data

## Scope

### In Scope

1. **Markdown Format Specification**
   - Document the subset of markdown that works on both GitHub and Azure DevOps Services
   - Include examples of supported features (headings, tables, lists, code blocks, etc.)
   - Explicitly call out features that differ between platforms
   - Provide escaping rules for special characters

2. **Input Escaping**
   - Ensure all Terraform plan values are properly escaped when rendered
   - Values include: resource names, attribute values, tag names/values, module names, etc.
   - Escape markdown special characters: `|`, `*`, `_`, `[`, `]`, `(`, `)`, `#`, `` ` ``, `\`, `<`, `>`, `&`
   - Handle edge cases: newlines, empty values, null values

3. **Fix Existing Issues**
   - Add comprehensive tests first to discover existing markdown rendering bugs
   - Fix any bugs revealed by the new tests (e.g., line breaks, escaping)
   - Fix issues in built-in templates to make tests pass
   - Ensure comprehensive demo example renders correctly
   - **Critical**: All tests must pass before PR can be created

4. **Test Improvements**
   - Add unit tests that validate markdown structure
   - Test line breaks around tables (before and after)
   - Test line breaks around headings
   - Test table cell content doesn't contain unescaped newlines
   - Test special characters in Terraform values are escaped
   - Test common error cases: empty tables, tables with long content, nested lists
   - Add tests using realistic edge cases from real Terraform plans
   - Validate built-in templates on every build

5. **External Validation Tools**
   - Integrate markdown linter (e.g., markdownlint, markdown-cli) into CI pipeline
   - Configure linter rules for GitHub/Azure DevOps compatibility
   - Fail builds on markdown validation errors
   - Run linter on all generated markdown from built-in templates

6. **Visual Rendering Tests**
   - Create tests that render markdown to HTML using the same engines as GitHub/Azure DevOps
   - Compare rendered output to expected structure
   - Catch rendering issues that text-based tests miss (e.g., malformed HTML)
   - Consider snapshot testing for comprehensive demo example

### Out of Scope

- Support for markdown features not available on both GitHub and Azure DevOps Services
- Migration or validation of user-provided custom templates (users are responsible for their own templates)
- Advanced markdown features (e.g., GitHub-specific extensions, mermaid diagrams, math blocks)
- Markdown beautification or style improvements beyond correctness

## User Experience

### For DevOps Engineers (Tool Users)

**Current Behavior:**
1. Run `tfplan2md plan.json` to generate report
2. Copy markdown output and paste into PR comment
3. Discover that tables or headings don't render correctly
4. Manually fix markdown or regenerate with different options
5. Report issue for future release

**New Behavior:**
1. Run `tfplan2md plan.json` to generate report
2. Copy markdown output and paste into PR comment
3. Markdown renders correctly on first try
4. No manual fixes needed

### For Template Authors

**Current Behavior:**
- No clear documentation on which markdown features work reliably
- Trial and error to discover compatibility issues
- Risk of creating templates that break on certain platforms

**New Behavior:**
- Refer to `docs/markdown-specification.md` for supported features
- Use documented escaping helpers in Scriban templates
- Confidence that following guidelines ensures compatibility

### For Project Maintainers

**Current Behavior:**
- Markdown bugs discovered after release
- Manual testing by pasting output into PR comments
- No automated validation of markdown quality

**New Behavior:**
- CI pipeline validates markdown quality automatically
- Built-in templates validated on every build
- Markdown bugs caught before release
- Clear test failures indicate markdown structure issues

## Success Criteria

- [ ] Markdown specification document exists at `docs/markdown-specification.md`
- [ ] Specification includes examples of all supported features (tables, headings, lists, code blocks, etc.)
- [ ] Specification documents escaping rules for special characters
- [ ] All existing markdown rendering bugs in built-in templates are fixed
- [ ] All Terraform plan values are escaped in built-in templates
- [ ] Unit tests validate line breaks around tables (minimum 10 test cases)
- [ ] Unit tests validate line breaks around headings (minimum 5 test cases)
- [ ] Unit tests validate special characters are escaped (minimum 8 test cases covering all special characters)
- [ ] Built-in templates validated on every build (CI must pass)
- [ ] Markdown linter integrated into CI pipeline
- [ ] Linter configuration file exists (e.g., `.markdownlint.json`)
- [ ] CI fails if built-in templates generate invalid markdown
- [ ] Visual rendering tests exist for comprehensive demo example
- [ ] All tests pass on both simulated GitHub and Azure DevOps markdown engines
- [ ] Comprehensive demo example renders correctly when pasted into Azure DevOps PR comment (manual verification)

## Implementation Notes

### Markdown Engines to Research

- **GitHub**: GitHub Flavored Markdown (GFM) - based on CommonMark
- **Azure DevOps Services**: Custom markdown flavor - based on markdown-it

### Special Character Escaping Priority

High priority (observed issues):
- Newlines in table cells
- Missing line breaks around tables and headings

Medium priority (potential issues):
- Pipe symbols `|` in table cells
- Backticks `` ` `` in regular text
- Asterisks `*` and underscores `_` that could trigger emphasis

Low priority (edge cases):
- Angle brackets `<`, `>` in text
- Ampersands `&` in text
- Square brackets `[`, `]` and parentheses `(`, `)` that could trigger links

### Testing Strategy

1. **Tests First** - Add comprehensive tests to discover existing issues (test-driven approach)
2. **Bug Fixes** - Fix issues revealed by the tests
3. **Unit Tests** - Fast, isolated tests for escaping and structure
4. **Integration Tests** - End-to-end rendering with realistic plans
5. **Linting** - Static analysis of generated markdown
6. **Visual Tests** - Render to HTML and validate structure
7. **Manual Verification** - Paste comprehensive demo into Azure DevOps PR comment

### Linter Tool Options

- **markdownlint-cli2** - Modern, fast, configurable
- **remark-lint** - Extensible, JavaScript-based
- **markdown-cli** - Simple, Docker-compatible

Recommendation: markdownlint-cli2 (most popular, good VS Code integration)

### Visual Rendering Options

- **markdown-it** - Used by Azure DevOps, available as npm package
- **marked** - Fast, GitHub-compatible renderer
- **unified/remark** - Parse markdown to AST for structural validation

## Open Questions

None - ready for architectural design.

## Related Documentation

- [Testing Strategy](../../testing-strategy.md)
- [Built-in Templates Feature](../built-in-templates/specification.md)
- Scriban documentation: https://github.com/scriban/scriban/blob/master/doc/language.md
