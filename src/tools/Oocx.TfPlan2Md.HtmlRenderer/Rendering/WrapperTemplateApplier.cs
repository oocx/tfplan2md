using System.Globalization;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Applies wrapper templates by inserting rendered HTML fragments into a <c>{{content}}</c> placeholder.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal sealed class WrapperTemplateApplier
{
    /// <summary>
    /// Token that must exist in wrapper templates to indicate insertion point for rendered content.
    /// </summary>
    private const string ContentPlaceholder = "{{content}}";

    /// <summary>
    /// Injects rendered HTML into a wrapper template at the required placeholder location.
    /// </summary>
    /// <param name="templateHtml">Wrapper template HTML containing the <c>{{content}}</c> placeholder.</param>
    /// <param name="contentHtml">Rendered HTML fragment to embed.</param>
    /// <returns>Composed HTML document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when template or content is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the template does not contain <c>{{content}}</c>.</exception>
    public string Apply(string templateHtml, string contentHtml)
    {
        ArgumentNullException.ThrowIfNull(templateHtml);
        ArgumentNullException.ThrowIfNull(contentHtml);

        if (!templateHtml.Contains(ContentPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(FormattableString.Invariant($"Template must contain placeholder {ContentPlaceholder} for content insertion."));
        }

        return templateHtml.Replace(ContentPlaceholder, contentHtml, StringComparison.Ordinal);
    }
}
