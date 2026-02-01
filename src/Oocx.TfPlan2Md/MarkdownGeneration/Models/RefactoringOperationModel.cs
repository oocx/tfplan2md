namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a refactoring operation (import or move) for report rendering.
/// </summary>
internal sealed class RefactoringOperationModel
{
    /// <summary>
    /// Gets the operation type, either "Import" or "Move".
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the Terraform resource address used for sorting.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the resource type shown in the summary table.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the resource name shown in the summary table.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the details column value (import ID or previous address).
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string Details { get; init; }

    /// <summary>
    /// Gets the status label for the refactoring operation.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets a value indicating whether the refactoring block is already applied.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required bool IsAlreadyApplied { get; init; }
}
