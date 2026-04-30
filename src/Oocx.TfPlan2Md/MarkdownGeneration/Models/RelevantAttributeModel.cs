namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a relevant attribute entry from Terraform 1.14+ plans.
/// Indicates an upstream resource attribute that influenced downstream resource computation.
/// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
/// </summary>
internal sealed class RelevantAttributeModel
{
    /// <summary>
    /// Gets the resource address of the upstream resource.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// Gets the formatted attribute path (e.g., "tags.Name" or "network_interface[0].id").
    /// Pre-formatted using the same path formatter as replace_paths.
    /// </summary>
    public required string AttributePath { get; init; }
}
