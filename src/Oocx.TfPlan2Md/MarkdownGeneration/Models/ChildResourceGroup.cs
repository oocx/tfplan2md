using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a group of child resources rendered within a parent section.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildResourceGroup
{
    /// <summary>
    /// Gets the display label for the child group.
    /// </summary>
    /// <value>The label shown above the child table.</value>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the column definitions for the table header.
    /// </summary>
    /// <value>The ordered list of table columns.</value>
    public required IReadOnlyList<ChildTableColumn> Columns { get; init; }

    /// <summary>
    /// Gets the row data for each child entry.
    /// </summary>
    /// <value>The ordered list of child rows.</value>
    public required IReadOnlyList<ChildResourceRow> Rows { get; init; }

    /// <summary>
    /// Gets a value indicating whether both inline and separate children were detected.
    /// </summary>
    /// <value><c>true</c> when mixed sources were detected; otherwise, <c>false</c>.</value>
    public bool HasMixedSources { get; init; }
}
