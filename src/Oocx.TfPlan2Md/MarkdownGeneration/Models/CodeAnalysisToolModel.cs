namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Describes a static analysis tool for report rendering.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisToolModel
{
    /// <summary>
    /// Gets the tool display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the tool version when available.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the combined tool display text.
    /// </summary>
    public required string DisplayName { get; init; }
}
