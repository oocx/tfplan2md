namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Registry of resource-specific markdown renderers keyed by Terraform resource type.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class ResourceRendererRegistry
{
    /// <summary>
    /// Stores registered renderers by Terraform resource type.
    /// </summary>
    private readonly Dictionary<string, IResourceRenderer> _renderers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a renderer for its declared resource type.
    /// </summary>
    /// <param name="renderer">Renderer to register.</param>
    /// <remarks>
    /// If a renderer is already registered for the same type, it is replaced by the new instance.
    /// </remarks>
    public void Register(IResourceRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        if (string.IsNullOrWhiteSpace(renderer.ResourceType))
        {
            throw new ArgumentException("Renderer resource type must be non-empty.", nameof(renderer));
        }

        _renderers[renderer.ResourceType] = renderer;
    }

    /// <summary>
    /// Resolves a renderer for a Terraform resource type.
    /// </summary>
    /// <param name="resourceType">Terraform resource type, for example <c>azurerm_resource_group</c>.</param>
    /// <returns>The registered renderer, or null when none is registered.</returns>
    public IResourceRenderer? GetRenderer(string resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);

        return _renderers.TryGetValue(resourceType, out var renderer)
            ? renderer
            : null;
    }
}
