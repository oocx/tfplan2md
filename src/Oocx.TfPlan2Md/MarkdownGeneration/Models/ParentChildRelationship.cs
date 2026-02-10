using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Describes a parent-child resource relationship for inline rendering.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal sealed record ParentChildRelationship
{
    /// <summary>
    /// Gets the parent resource type (e.g., "azuread_group").
    /// </summary>
    /// <value>The Terraform resource type for the parent.</value>
    public required string ParentResourceType { get; init; }

    /// <summary>
    /// Gets the child resource type (e.g., "azuread_group_member").
    /// </summary>
    /// <value>The Terraform resource type for the child.</value>
    public required string ChildResourceType { get; init; }

    /// <summary>
    /// Gets the inline attribute name on the parent that contains child data.
    /// </summary>
    /// <value>The parent attribute name, or null if children exist only as separate resources.</value>
    public string? InlineAttributeName { get; init; }

    /// <summary>
    /// Gets the child attribute that references the parent's ID.
    /// </summary>
    /// <value>The child attribute name used to match separate resources, or null when unused.</value>
    public string? ChildReferenceAttribute { get; init; }

    /// <summary>
    /// Gets the parent attribute that provides the ID referenced by children.
    /// </summary>
    /// <value>The parent ID attribute name, defaulting to <c>id</c>.</value>
    public string ParentIdAttribute { get; init; } = "id";

    /// <summary>
    /// Gets the display label for this child group.
    /// </summary>
    /// <value>The label shown above the child table.</value>
    public required string ChildGroupLabel { get; init; }

    /// <summary>
    /// Gets the column definitions for the child table.
    /// </summary>
    /// <value>The ordered list of table columns.</value>
    public required IReadOnlyList<ChildTableColumn> TableColumns { get; init; }

    /// <summary>
    /// Gets the row extractor used to format child values.
    /// </summary>
    /// <value>The row extraction strategy for child rows.</value>
    public required IChildRowExtractor RowExtractor { get; init; }
}
