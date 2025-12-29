# Architecture: Markdown Quality Validation

## Status

Proposed

## Context

The `tfplan2md` tool generates markdown reports from Terraform plans. Users have reported rendering issues in Azure DevOps and GitHub PR comments, specifically:
- Tables breaking due to missing surrounding newlines or newlines within cells.
- Headings breaking due to missing surrounding newlines.
- Potential issues with unescaped special characters (pipes, asterisks, etc.) in resource names or values.

We need to ensure the generated markdown is valid and renders correctly on both platforms.

## Options Considered

### Option 1: Scriban Helper + Markdig Validation (Recommended)

- **Implementation**:
  - Add a custom Scriban helper function `escape_markdown` (or similar) to `ScribanHelpers.cs`.
  - Update built-in templates to use this filter for all dynamic values.
  - Update built-in templates to ensure correct spacing (newlines) around block elements.
- **Validation**:
  - **Unit Tests**: Use `Markdig` (a robust C# Markdown parser) in tests to parse the generated output and validate the structure (e.g., "contains 1 table", "table has X rows"). This is more reliable than regex.
  - **CI**: Integrate `markdownlint-cli2` into the GitHub Actions workflow to lint the generated output of the comprehensive demo.
- **Pros**:
  - Idiomatic for Scriban (filters).
  - `Markdig` provides a strong validation engine without needing a browser.
  - `markdownlint` is the industry standard for markdown quality.
- **Cons**:
  - Requires manual updates to templates (but we own the built-in ones).

### Option 2: Post-processing / DOM Manipulation

- **Implementation**:
  - Render the template to a string.
  - Parse the string with a Markdown parser, traverse the AST, fix issues, and regenerate.
- **Pros**:
  - Could automatically fix some structural issues.
- **Cons**:
  - "Fixing" markdown is hard and error-prone.
  - Doesn't solve the input escaping problem at the source.
  - Heavy performance cost.

### Option 3: Auto-escaping Model

- **Implementation**:
  - Wrap the `ReportModel` in a proxy that automatically escapes all string properties.
- **Pros**:
  - Safe by default.
- **Cons**:
  - Complex to implement (reflection or dynamic proxies).
  - Might prevent intentional markdown injection (though rare).

## Decision

**Option 1** is selected. It aligns with the existing architecture (Scriban) and provides a clear path for both fixing issues (templates + helpers) and validating them (Markdig + markdownlint).

## Rationale

- **Simplicity**: Adding a helper function is low-complexity.
- **Control**: Template authors (us) retain control over layout while ensuring data safety.
- **Robustness**: Using a real markdown parser for tests ensures we are testing the *structure*, not just string matching.

## Implementation Notes

### 1. Scriban Helper: `escape_markdown`

Add to `ScribanHelpers.cs`:

```csharp
public static string EscapeMarkdown(string? input)
{
    if (string.IsNullOrEmpty(input)) return string.Empty;
    // 1. Escape pipes | (for tables)
    // 2. Escape newlines (replace with <br/> or space, depending on context - usually <br/> for table cells)
    // 3. Escape * _ ` [ ] ( ) etc.
    // Note: Be careful not to double-escape if we use this on already escaped content.
    // Ideally, this is used on raw values.
}
```

### 2. Template Updates

Update `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/*.sbn`:

- Ensure blank lines before/after tables and headings.
- Apply `| escape_markdown` to:
  - Resource names
  - Attribute values
  - Tag keys/values
  - Error messages

### 3. Testing with Markdig

Add `Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownValidationTests.cs`:

```csharp
[Fact]
public void Render_ComprehensiveDemo_IsValidMarkdown()
{
    // Arrange
    var plan = ...;
    var renderer = new MarkdownRenderer();
    
    // Act
    var markdown = renderer.Render(plan);
    
    // Assert
    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    var document = Markdown.Parse(markdown, pipeline);
    
    // Check for valid table structure
    var tables = document.Descendants<Table>().ToList();
    tables.Should().NotBeEmpty();
    
    // Check that no raw HTML blocks exist where they shouldn't (if we want to avoid them)
}
```

### 4. CI Integration

- Add `markdownlint-cli2` configuration `.markdownlint.json` to root.
- Add step to `.github/workflows/ci.yml` to run linting on generated demo output.

## Consequences

### Positive
- **Reliability**: Generated reports will render consistently.
- **Safety**: Malicious or accidental special characters in Terraform plans won't break the report.
- **Quality**: CI will catch regressions in markdown structure.

### Negative
- **Template Complexity**: Templates become slightly more verbose with `| escape_markdown`.
- **Performance**: Slight overhead for escaping and parsing in tests (negligible).
