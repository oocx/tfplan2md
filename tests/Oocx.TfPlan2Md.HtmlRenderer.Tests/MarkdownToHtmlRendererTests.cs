using System.Text.RegularExpressions;
using Oocx.TfPlan2Md.HtmlRenderer;
using Oocx.TfPlan2Md.HtmlRenderer.Rendering;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Validates the core markdown-to-HTML rendering behavior.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public sealed class MarkdownToHtmlRendererTests
{
    /// <summary>
    /// Ensures tables and details/summary blocks are rendered into the expected HTML elements.
    /// Related acceptance: Task 3 (tables, details support).
    /// </summary>
    [Fact]
    public void RenderFragment_TablesAndDetails_RendersExpectedElements()
    {
        const string markdown = """
# Heading

| Col1 | Col2 |
| --- | ---: |
| a | b |

<details><summary>More</summary>Body</details>
""";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<details>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<summary>More</summary>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<th", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures GitHub flavor wraps tables with the platform accessibility element and role attribute.
    /// Related acceptance: Feature 027 GitHub fidelity.
    /// </summary>
    [Fact]
    public void RenderFragment_GitHub_WrapsTablesForAccessibility()
    {
        const string markdown = """
| A | B |
| - | - |
| 1 | 2 |
""";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.Contains("<markdown-accessiblity-table", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("role=\"table\"", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures fenced code blocks emit language classes compatible with syntax highlighting libraries.
    /// Related acceptance: Task 3 (code blocks with language class) and TC-08.
    /// </summary>
    [Fact]
    public void RenderFragment_CodeBlock_IncludesLanguageClass()
    {
        const string markdown = """
```diff
- old
+ new
```
""";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("<code class=\"language-diff\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<pre>", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures GitHub flavor strips style attributes from inline HTML to match sanitization.
    /// Related acceptance: TC-05.
    /// </summary>
    [Fact]
    public void RenderFragment_GitHub_StripsStyleAttributes()
    {
        const string markdown = "<span style=\"color:red\">text</span>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.DoesNotContain("style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<span", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<span >", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Azure DevOps flavor preserves style attributes used by inline diff rendering.
    /// Related acceptance: TC-06.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_PreservesStyleAttributes()
    {
        const string markdown = "<span style=\"color:red\">text</span>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("style=\"color:red\"", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Azure DevOps flavor applies block and inline-block styling to inline diff code/spans for fidelity.
    /// Related acceptance: Feature 027 Azure DevOps cosmetics.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_AddsDiffDisplayStyles()
    {
        const string markdown = "<code style=\"color:red\"><span style=\"background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; padding-left: 8px; margin-left: 0\">- old</span></code>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("display:block", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("display:inline-block", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures display directives are not duplicated when already present with whitespace.
    /// Related acceptance: Feature 027 Azure DevOps cosmetics.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_DoesNotDuplicateDisplayDirectives()
    {
        const string markdown = "<code style=\"display: block; padding:0\"><span style=\"display: inline-block; color:red\">value</span></code>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        var match = Regex.Match(html, "<span[^>]*style=\\\"([^\\\"]*)\\\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success);
        var occurrences = Regex.Matches(match.Groups[1].Value, "display:\\s*inline-block", RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, occurrences);
    }

    /// <summary>
    /// Ensures trailing semicolons in style attributes are trimmed during normalization.
    /// Related acceptance: Feature 027 Azure DevOps cosmetics.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_TrimsTrailingSemicolons()
    {
        const string markdown = "<code style=\"padding:0; margin:0; \" aria-label=\"removed\">value</code>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        var match = Regex.Match(html, "style=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success);
        Assert.False(match.Groups[1].Value.Trim().EndsWith(';'));
    }

    /// <summary>
    /// Ensures Azure DevOps flavor keeps single newlines as soft breaks while honoring hard breaks (two spaces).
    /// Related acceptance: TC-07.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_LineBreaksRespectTwoSpaces()
    {
        const string markdown = """
Line 1
Line 2
Line 3  
Line 4
""";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.DoesNotContain("Line 1<br", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Line 2<br", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Line 3", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Line 4", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<br", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures inline HTML such as &lt;br/&gt; is preserved in the output, aligning with tfplan2md reports.
    /// Related acceptance: Task 3 (inline HTML support).
    /// </summary>
    [Fact]
    public void RenderFragment_PreservesInlineHtml()
    {
        const string markdown = "Value<br/>Next";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.Contains("<br/>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Value", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures emoji and backtick content in headings produce percent-encoded slugs matching platform patterns.
    /// Related acceptance: Feature 027 heading id fidelity.
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_EmojiHeadingSlugMatchesPattern()
    {
        const string markdown = "### 📦 Module: `module.network`";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("id=\"user-content-%F0%9F%93%A6-module%3A-%60module.network%60\"", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Azure DevOps flavor preserves style attributes on details elements for resource borders.
    /// Related acceptance: TC-03 (Feature 029).
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_PreservesDetailsStyleAttributes()
    {
        const string markdown = "<details style=\"margin-bottom:12px; border:1px solid #f0f0f0; padding:12px;\"><summary>Resource</summary>Content</details>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        Assert.Contains("<details", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("style=\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("border:1px solid #f0f0f0", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("margin-bottom:12px", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("padding:12px", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures GitHub flavor strips style attributes from details elements matching platform sanitization.
    /// Related acceptance: TC-04 (Feature 029).
    /// </summary>
    [Fact]
    public void RenderFragment_GitHub_StripsDetailsStyleAttributes()
    {
        const string markdown = "<details style=\"margin-bottom:12px; border:1px solid #f0f0f0; padding:12px;\"><summary>Resource</summary>Content</details>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.GitHub);

        Assert.Contains("<details", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<summary>Resource</summary>", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Azure DevOps flavor normalizes details tags with style attributes (trims trailing semicolons).
    /// Related acceptance: TC-03 (Feature 029).
    /// </summary>
    [Fact]
    public void RenderFragment_AzDo_NormalizesDetailsStyleAttributes()
    {
        const string markdown = "<details style=\"border:1px solid #f0f0f0; padding:12px; \"><summary>Resource</summary>Content</details>";

        var renderer = CreateRenderer();
        var html = renderer.RenderFragment(markdown, HtmlFlavor.AzureDevOps);

        var match = Regex.Match(html, "<details[^>]*style=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Expected style attribute on details tag");
        var styleValue = match.Groups[1].Value.Trim();
        Assert.False(styleValue.EndsWith(';'), "Trailing semicolon should be trimmed from style attribute");
        Assert.Contains("border:1px solid #f0f0f0", styleValue, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("padding:12px", styleValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates the renderer with a default pipeline factory for tests.
    /// </summary>
    /// <returns>An initialized <see cref="MarkdownToHtmlRenderer"/> instance.</returns>
    private static MarkdownToHtmlRenderer CreateRenderer()
    {
        var factory = new MarkdigPipelineFactory();
        return new MarkdownToHtmlRenderer(factory);
    }
}
