namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a Terraform output change for markdown rendering.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
public sealed class OutputChangeModel
{
    /// <summary>
    /// Gets the name of the output variable.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the output from the configuration.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the action being performed (create, update, delete, no-op).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the icon representing the action (➕, 🔄, ❌, ⏺️).
    /// </summary>
    public required string ActionIcon { get; init; }

    /// <summary>
    /// Gets a value indicating whether this output contains sensitive data.
    /// True when before_sensitive or after_sensitive is true.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets the value before the change (for updates and deletes).
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the value after the change (for creates and updates).
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// Gets a value indicating whether the after value is unknown and will be known only after apply.
    /// </summary>
    public bool IsUnknown { get; init; }
}
