namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Defines a renderer for a specific Terraform resource type.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal interface IResourceRenderer
{
    /// <summary>
    /// Gets the Terraform resource type handled by this renderer.
    /// </summary>
    string ResourceType { get; }

    /// <summary>
    /// Renders markdown for a resource change.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model to render.</param>
    /// <param name="context">Global render context.</param>
    void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context);
}
