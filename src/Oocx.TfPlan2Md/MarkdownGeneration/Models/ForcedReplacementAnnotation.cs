namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a forced-replacement inline annotation for a replaced or destroyed resource card.
/// Indicates that a specific local attribute reads from an upstream resource, and that upstream
/// reference appears in <c>replace_paths</c>, causing forced replacement of this resource.
/// Rendered as a blockquote callout above the diff table inside the resource's <c>&lt;details&gt;</c> block.
/// Related feature: docs/features/660-inline-relevant-attributes/specification.md.
/// </summary>
internal sealed record ForcedReplacementAnnotation
{
    /// <summary>
    /// Gets the local top-level attribute name on the replaced resource that triggered the replacement.
    /// Corresponds to the first path segment of the matching <c>replace_paths</c> entry.
    /// For example, <c>"network_interface_ids"</c> when <c>replace_paths = [["network_interface_ids", 0]]</c>.
    /// </summary>
    public required string LocalAttribute { get; init; }

    /// <summary>
    /// Gets the upstream resource address whose attribute value is read by <see cref="LocalAttribute"/>.
    /// Matches the <c>resource</c> field of the correlated <c>relevant_attributes</c> entry.
    /// For example, <c>"azurerm_network_interface.web"</c>.
    /// </summary>
    public required string UpstreamResource { get; init; }

    /// <summary>
    /// Gets the pre-formatted attribute path on the upstream resource.
    /// Matches the <c>AttributePath</c> of the correlated <c>RelevantAttributeModel</c>.
    /// For example, <c>"id"</c> or <c>"network_interface[0].ip_configurations[0].private_ip_address"</c>.
    /// </summary>
    public required string UpstreamAttributePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the upstream resource is itself being replaced or destroyed in this plan.
    /// When <see langword="true"/>, the renderer appends <c>, which is <b>changing in this plan</b>.</c>
    /// to the callout line.
    /// </summary>
    public required bool IsChangingInThisPlan { get; init; }
}
