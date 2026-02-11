using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a single child resource row in a parent table.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildResourceRow
{
    /// <summary>
    /// Gets the change indicator for the child resource (e.g., "➕", "🔄").
    /// </summary>
    /// <value>The change indicator symbol.</value>
    public required string ChangeIndicator { get; init; }

    /// <summary>
    /// Gets the column values keyed by <see cref="ChildTableColumn.PropertyName"/>.
    /// </summary>
    /// <value>A map of column property names to formatted values.</value>
    public required IReadOnlyDictionary<string, string> Values { get; init; }

    /// <summary>
    /// Gets the Terraform resource label shown in the table.
    /// </summary>
    /// <value>The resource address or inline attribute label.</value>
    public required string TerraformResource { get; init; }

    /// <summary>
    /// Gets the original Terraform resource address for findings attribution.
    /// </summary>
    /// <value>The original resource address, if available.</value>
    public string? OriginalResourceAddress { get; init; }

    /// <summary>
    /// Gets the Azure AD member object ID for parent-child summary rebuilding.
    /// </summary>
    /// <value>The member object ID, or <c>null</c> if not an Azure AD member.</value>
    /// <remarks>
    /// Used to resolve principal types when rebuilding Azure AD group summaries after parent-child merging.
    /// Related issue: docs/issues/069-parent-child-summary-count-mismatch/analysis.md.
    /// </remarks>
    public string? MemberId { get; init; }
}
