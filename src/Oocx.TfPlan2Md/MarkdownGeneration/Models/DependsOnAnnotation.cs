namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a depends-on inline annotation for a replaced or destroyed resource card.
/// Indicates an upstream resource attribute that is referenced by this resource via
/// <c>ConfigurationReferences</c> but does not trace to a specific forced-replacement path.
/// Rendered as part of the <c>🔗 Depends on:</c> (or <c>🔗 Also depends on:</c>) blockquote line
/// inside the resource's <c>&lt;details&gt;</c> block.
/// Related feature: docs/features/660-inline-relevant-attributes/specification.md.
/// </summary>
internal sealed record DependsOnAnnotation
{
    /// <summary>
    /// Gets the upstream resource address.
    /// Matches the <c>resource</c> field of the correlated <c>relevant_attributes</c> entry.
    /// For example, <c>"data.azurerm_client_config.current"</c>.
    /// </summary>
    public required string UpstreamResource { get; init; }

    /// <summary>
    /// Gets the pre-formatted attribute path on the upstream resource.
    /// Matches the <c>AttributePath</c> of the correlated <c>RelevantAttributeModel</c>.
    /// For example, <c>"tenant_id"</c>.
    /// </summary>
    public required string UpstreamAttributePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the upstream resource is itself being replaced or destroyed in this plan.
    /// When <see langword="true"/>, the renderer appends a ⚠️ marker after the upstream reference in the output.
    /// </summary>
    public required bool IsChangingInThisPlan { get; init; }
}
