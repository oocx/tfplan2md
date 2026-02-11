namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Defines a column in a child resource table.
/// </summary>
/// <param name="Header">The column header displayed in the table.</param>
/// <param name="PropertyName">The key used in <see cref="ChildResourceRow.Values"/> for column lookup.</param>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
public sealed record ChildTableColumn(string Header, string PropertyName);
