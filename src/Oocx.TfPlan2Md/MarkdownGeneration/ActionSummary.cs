using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Summary details for a specific action (e.g., add, change).
/// </summary>
/// <param name="Count">The number of resources in the action.</param>
/// <param name="Breakdown">The resource type breakdown for the action.</param>
public record ActionSummary(int Count, IReadOnlyList<ResourceTypeBreakdown> Breakdown);
