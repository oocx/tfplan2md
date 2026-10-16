namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents a single logical attribute change within a resource.
/// A scalar leaf path is one attribute; a collection of primitive values is also one attribute.
/// </summary>
public class AttributeChangeModel
{
    /// <summary>
    /// Gets the name of the attribute that changed.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the value before the change, or null if the attribute is being added.
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the value after the change, or null if the attribute is being removed.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// Gets a value indicating whether this attribute contains sensitive data.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the attribute value should be rendered as a large value block (collapsible section).
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    public bool IsLarge { get; init; }

    /// <summary>
    /// Gets the structured diff when this row is a compact primitive collection; <c>null</c> for scalar rows.
    /// Related feature: docs/features/148-compact-collection-diffs/specification.md.
    /// </summary>
    public Models.CollectionChangeModel? Collection { get; init; }
}
