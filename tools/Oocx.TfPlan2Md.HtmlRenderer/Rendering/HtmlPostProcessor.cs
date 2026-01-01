using System.Diagnostics.CodeAnalysis;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Coordinates flavor-specific HTML post-processing for rendered markdown.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HtmlPostProcessor
{
    /// <summary>
    /// Applies common normalization then flavor-specific adjustments.
    /// </summary>
    /// <param name="html">Rendered HTML fragment.</param>
    /// <param name="flavor">Target rendering flavor.</param>
    /// <returns>Normalized HTML fragment.</returns>
    public static string PostProcess(string html, HtmlFlavor flavor)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var normalized = CommonHtmlNormalization.Normalize(html);

        return flavor == HtmlFlavor.GitHub
            ? GitHubHtmlPostProcessor.Process(normalized)
            : AzureDevOpsHtmlPostProcessor.Process(normalized);
    }
}
