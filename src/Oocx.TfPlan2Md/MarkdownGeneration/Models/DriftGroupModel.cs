namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a deterministic group of displayable resource drift entries sharing one attribute transition.
/// </summary>
internal sealed class DriftGroupModel
{
    /// <summary>
    /// Gets the Terraform resource type shared by the group.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the normalized attribute path shared by the group.
    /// </summary>
    public required string AttributePath { get; init; }

    /// <summary>
    /// Gets the normalized value before the drift transition.
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the normalized value after the drift transition.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// Gets the ordinally ordered Terraform addresses affected by this transition.
    /// </summary>
    public required IReadOnlyList<string> Addresses { get; init; }
}
