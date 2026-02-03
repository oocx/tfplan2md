using Markdig;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Builds Markdig pipelines tailored for HTML rendering flavors.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal sealed class MarkdigPipelineFactory
{
    /// <summary>
    /// Creates a Markdown pipeline for the requested flavor.
    /// </summary>
    /// <param name="flavor">Target HTML flavor.</param>
    /// <returns>A configured <see cref="MarkdownPipeline"/>.</returns>
    public MarkdownPipeline Create(HtmlFlavor flavor)
    {
        var builder = new MarkdownPipelineBuilder();

        // Common extensions required by tfplan2md output (tables, fenced code, emphasis extras, auto links).
        builder.UseAdvancedExtensions();

        // Azure DevOps requires explicit hard breaks (two spaces). Remove soft-break-as-hardline extension to avoid auto <br/>.
        RemoveSoftlineBreakExtension(builder);

        if (flavor == HtmlFlavor.GitHub)
        {
            // GitHub keeps the same base pipeline; style stripping happens post-parse.
        }

        return builder.Build();
    }

    /// <summary>
    /// Removes the soft line break extension so that single newlines remain soft (no &lt;br/&gt;) unless explicitly requested.
    /// </summary>
    /// <param name="builder">Pipeline builder to mutate.</param>
    private static void RemoveSoftlineBreakExtension(MarkdownPipelineBuilder builder)
    {
        for (var i = builder.Extensions.Count - 1; i >= 0; i--)
        {
            if (builder.Extensions[i].GetType().Name.Contains("SoftlineBreakAsHardline", StringComparison.Ordinal))
            {
                builder.Extensions.RemoveAt(i);
            }
        }
    }
}
