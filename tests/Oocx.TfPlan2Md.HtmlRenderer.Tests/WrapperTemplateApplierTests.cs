using Oocx.TfPlan2Md.HtmlRenderer.Rendering;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Validates behavior of the wrapper template applier.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public sealed class WrapperTemplateApplierTests
{
    /// <summary>
    /// Ensures the applier replaces the content placeholder with rendered HTML.
    /// </summary>
    [Fact]
    public void Apply_ReplacesPlaceholder_WithContent()
    {
        const string template = "<html><body>{{content}}</body></html>";
        const string content = "<h1>Title</h1>";
        var applier = new WrapperTemplateApplier();

        var result = applier.Apply(template, content);

        Assert.Contains(content, result, StringComparison.Ordinal);
        Assert.DoesNotContain("{{content}}", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ensures an error is raised when the placeholder is missing.
    /// </summary>
    [Fact]
    public void Apply_MissingPlaceholder_Throws()
    {
        const string template = "<html><body>No slot</body></html>";
        var applier = new WrapperTemplateApplier();

        var ex = Assert.Throws<InvalidOperationException>(() => applier.Apply(template, "content"));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
