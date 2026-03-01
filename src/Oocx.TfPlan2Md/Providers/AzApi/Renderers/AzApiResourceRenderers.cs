using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzApi.Renderers;

/// <summary>
/// Base class for AzApi resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzApiDelegatingRenderer(string resourceType) : IResourceRenderer
{
    /// <summary>
    /// Default fallback renderer.
    /// </summary>
    private readonly DefaultResourceRenderer _defaultRenderer = new();

    /// <inheritdoc />
    public string ResourceType { get; } = resourceType;

    /// <inheritdoc />
    public virtual void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        _defaultRenderer.Render(writer, change, context);
    }
}

/// <summary>
/// Renders <c>azapi_resource</c> resources.
/// </summary>
internal sealed class AzApiResourceRenderer : AzApiDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiResourceRenderer"/> class.
    /// </summary>
    public AzApiResourceRenderer()
        : base("azapi_resource")
    {
    }
}

/// <summary>
/// Renders <c>azapi_update_resource</c> resources.
/// </summary>
internal sealed class AzApiUpdateResourceRenderer : AzApiDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiUpdateResourceRenderer"/> class.
    /// </summary>
    public AzApiUpdateResourceRenderer()
        : base("azapi_update_resource")
    {
    }
}

/// <summary>
/// Renders pseudo output-values rows represented by <c>azapi_output_values</c>.
/// </summary>
internal sealed class AzApiOutputValuesRenderer : AzApiDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiOutputValuesRenderer"/> class.
    /// </summary>
    public AzApiOutputValuesRenderer()
        : base("azapi_output_values")
    {
    }
}
