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
    /// Gets the provider name of the resource this output references (e.g., "registry.terraform.io/hashicorp/azurerm").
    /// Null when the referenced resource cannot be determined.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public string? ProviderName { get; init; }

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

    /// <summary>
    /// Gets a value indicating whether the output value is large enough to render outside the table.
    /// When true, the template renders a placeholder in the table and the actual value below.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public bool IsLargeOutputValue { get; init; }

    /// <summary>
    /// Gets the attribute name extracted from <c>expression.references</c> in the plan configuration
    /// (e.g., <c>principal_id</c> from <c>azurerm_role_assignment.main.principal_id</c>).
    /// Used as the formatting key for semantic icons, display name mappings, and other per-attribute
    /// formatting rules. Falls back to the output's own <see cref="Name"/> when null.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public string? ReferencedAttributeName { get; init; }
}
