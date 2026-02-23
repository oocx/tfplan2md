namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents an output value change for rendering in the report.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
public class OutputChangeModel
{
    /// <summary>
    /// Gets the output name.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description from configuration.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this output is marked as sensitive in the configuration.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets the primary action for this output (create, update, delete, no-op).
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the icon symbol representing the action (➕, 🔄, ❌, etc.).
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public required string ActionSymbol { get; init; }

    /// <summary>
    /// Gets the output value (before or after depending on action).
    /// This is the raw value; templates will format it via helpers.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value is computed (known after apply).
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public bool IsComputed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value should be masked (sensitive and not --show-sensitive).
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public bool IsMasked { get; init; }

    /// <summary>
    /// Gets the module address this output belongs to (empty string for root).
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public required string ModuleAddress { get; init; }
}
