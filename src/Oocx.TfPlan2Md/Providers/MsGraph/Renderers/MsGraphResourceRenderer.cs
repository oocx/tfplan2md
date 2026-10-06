using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.MsGraph.Renderers;

/// <summary>
/// Adds the Microsoft Graph provider settings section while preserving default resource rendering.
/// Related feature: docs/features/146-generic-resource-review-clarity/specification.md.
/// </summary>
internal sealed class MsGraphResourceRenderer : IResourceRenderer
{
    /// <summary>
    /// Default renderer configured with the provider's additional settings section.
    /// </summary>
    private readonly DefaultResourceRenderer _defaultRenderer = new(renderAdditionalSection: RenderProviderSettings);

    /// <summary>
    /// Initializes a new instance of the <see cref="MsGraphResourceRenderer"/> class.
    /// </summary>
    /// <param name="resourceType">Terraform resource type handled by this renderer.</param>
    public MsGraphResourceRenderer(string resourceType)
    {
        ResourceType = resourceType;
    }

    /// <inheritdoc />
    public string ResourceType { get; }

    /// <inheritdoc />
    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        _defaultRenderer.Render(writer, change, context);
    }

    /// <summary>
    /// Renders classified provider settings in the default renderer's collapsed single-value section.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="context">Global render context.</param>
    private static void RenderProviderSettings(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        DefaultResourceRenderer.RenderAdditionalAttributeSection(
            writer,
            "Provider settings",
            change,
            change.SecondaryAttributeChanges,
            context);
    }
}
