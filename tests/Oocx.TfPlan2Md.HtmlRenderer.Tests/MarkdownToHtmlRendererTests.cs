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

        Assert.Contains("<table>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<details>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<summary>More</summary>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<th", html, StringComparison.OrdinalIgnoreCase);
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
    /// Creates the renderer with a default pipeline factory for tests.
    /// </summary>
    /// <returns>An initialized <see cref="MarkdownToHtmlRenderer"/> instance.</returns>
    private static MarkdownToHtmlRenderer CreateRenderer()
    {
        var factory = new MarkdigPipelineFactory();
        return new MarkdownToHtmlRenderer(factory);
    }
}
