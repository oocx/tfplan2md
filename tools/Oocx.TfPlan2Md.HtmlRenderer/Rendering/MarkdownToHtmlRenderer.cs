using System.Text.RegularExpressions;
using Markdig;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Converts Markdown content into HTML fragments using Markdig.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
internal sealed class MarkdownToHtmlRenderer
{
    /// <summary>
    /// Matches inline style attributes so GitHub flavor output mirrors platform sanitization rules.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md
    /// </summary>
    private static readonly Regex StyleRegex = new("(\\s)style\\s*=\\s*\"[^\"]*\"|(\\s)style\\s*=\\s*'[^']*'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Provides the Markdig pipeline instances for rendering.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md
    /// </summary>
    private readonly MarkdigPipelineFactory _pipelineFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownToHtmlRenderer"/> class.
    /// </summary>
    /// <param name="pipelineFactory">Factory that builds Markdig pipelines per flavor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipelineFactory"/> is null.</exception>
    public MarkdownToHtmlRenderer(MarkdigPipelineFactory pipelineFactory)
    {
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
    }

    /// <summary>
    /// Renders Markdown into an HTML fragment appropriate for the selected flavor.
    /// </summary>
    /// <param name="markdown">Markdown input to render.</param>
    /// <param name="flavor">Target rendering flavor.</param>
    /// <returns>HTML fragment output.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="markdown"/> is null.</exception>
    public string RenderFragment(string markdown, HtmlFlavor flavor)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var pipeline = _pipelineFactory.Create(flavor);
        var document = Markdown.Parse(markdown, pipeline);
        var html = Markdown.ToHtml(document, pipeline);

        return flavor == HtmlFlavor.GitHub
            ? RemoveStyleAttributes(html)
            : html;
    }

    /// <summary>
    /// Strips style attributes from rendered HTML to mimic GitHub sanitization.
    /// </summary>
    /// <param name="html">HTML fragment to sanitize.</param>
    /// <returns>Sanitized HTML without inline style attributes.</returns>
    private static string RemoveStyleAttributes(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        return StyleRegex.Replace(html, match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
    }
}
