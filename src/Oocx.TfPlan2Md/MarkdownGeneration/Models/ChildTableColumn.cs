namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Defines a column in a child resource table.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildTableColumn
{
    /// <summary>
    /// Gets the column header displayed in the table.
    /// </summary>
    /// <value>The user-facing header text.</value>
    public required string Header { get; init; }

    /// <summary>
    /// Gets the property name used to map row values to this column.
    /// </summary>
    /// <value>The key used in <see cref="ChildResourceRow.Values"/> for column lookup.</value>
    public required string PropertyName { get; init; }
}
