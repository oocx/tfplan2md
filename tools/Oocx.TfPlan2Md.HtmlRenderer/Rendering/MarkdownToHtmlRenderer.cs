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

        return PostProcess(html, flavor);
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

    /// <summary>
    /// Applies flavor-specific post-processing including sanitization and attribute alignment.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md
    /// </summary>
    /// <param name="html">Rendered HTML fragment.</param>
    /// <param name="flavor">Target flavor.</param>
    /// <returns>Adjusted HTML fragment.</returns>
    private static string PostProcess(string html, HtmlFlavor flavor)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var withDirection = AddDirAuto(html);
        var withNbsp = ReplaceNonBreakingSpaces(withDirection);

        if (flavor == HtmlFlavor.GitHub)
        {
            var withoutIds = RemoveHeadingIds(withNbsp);
            return RemoveStyleAttributes(withoutIds);
        }

        var withPrefixedIds = PrefixHeadingIds(withNbsp, "user-content-");
        return withPrefixedIds;
    }

    /// <summary>
    /// Adds dir="auto" to headings and paragraphs when missing to mirror platform output.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with dir attributes on headings and paragraphs.</returns>
    private static string AddDirAuto(string html)
    {
        return Regex.Replace(html, "<(h[1-6]|p)(?=[\\s>])([^>]*)>", match =>
        {
            var tagName = match.Groups[1].Value;
            var attributes = match.Groups[2].Value;
            if (Regex.IsMatch(attributes, "\\sdir=", RegexOptions.IgnoreCase))
            {
                return match.Value;
            }

            return $"<{tagName} dir=\"auto\"{attributes}>";
        }, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Removes heading id attributes to align with GitHub rendering where ids are absent.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML without heading ids.</returns>
    private static string RemoveHeadingIds(string html)
    {
        return Regex.Replace(html, "<(h[1-6])(.*?)\\sid=\".*?\"(.*?)>", "<$1$2$3>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Prefixes heading id attributes to approximate Azure DevOps / GitHub user-content ids.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <param name="prefix">Prefix to apply to existing ids.</param>
    /// <returns>HTML with prefixed heading ids.</returns>
    private static string PrefixHeadingIds(string html, string prefix)
    {
        return Regex.Replace(html, "<(h[1-6])(.*?)\\sid=\"(.*?)\"(.*?)>", match =>
        {
            var tag = match.Groups[1].Value;
            var beforeId = match.Groups[2].Value;
            var id = match.Groups[3].Value;
            var afterId = match.Groups[4].Value;
            if (id.StartsWith(prefix, StringComparison.Ordinal))
            {
                return match.Value;
            }

            return $"<{tag}{beforeId} id=\"{prefix}{id}\"{afterId}>";
        }, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Replaces non-breaking space characters with HTML entities for clarity and alignment with references.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with explicit &amp;nbsp; entities.</returns>
    private static string ReplaceNonBreakingSpaces(string html)
    {
        return html.Replace("\u00A0", "&nbsp;", StringComparison.Ordinal);
    }
}
