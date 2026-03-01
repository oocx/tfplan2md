using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps.Renderers;

/// <summary>
/// Base class for Azure DevOps resource renderers that currently delegate to the default renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal abstract class AzureDevOpsDelegatingRenderer(string resourceType) : IResourceRenderer
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
/// Renders <c>azuredevops_variable_group</c> resources.
/// </summary>
internal sealed class VariableGroupRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VariableGroupRenderer"/> class.
    /// </summary>
    public VariableGroupRenderer()
        : base("azuredevops_variable_group")
    {
    }
}

/// <summary>
/// Renders <c>azuredevops_build_definition</c> resources.
/// </summary>
internal sealed class BuildDefinitionRenderer : AzureDevOpsDelegatingRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildDefinitionRenderer"/> class.
    /// </summary>
    public BuildDefinitionRenderer()
        : base("azuredevops_build_definition")
    {
    }
}
