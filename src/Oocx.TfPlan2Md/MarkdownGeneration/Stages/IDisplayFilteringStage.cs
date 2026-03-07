using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Pipeline stage that filters merged resource changes for display
/// and normalizes module addresses.
/// </summary>
/// <remarks>
/// Related features:
/// - docs/features/068-parent-child-resource-grouping/specification.md (no-op parent preservation).
/// - docs/features/103-azure-id-case-insensitive-filter/specification.md (Azure ID case filtering).
/// </remarks>
internal interface IDisplayFilteringStage
{
    /// <summary>
    /// Filters merged resource changes to produce the final display set.
    /// </summary>
    /// <param name="mergedChanges">Resource changes after parent-child merging.</param>
    /// <returns>Filtered changes and count of suppressed resources.</returns>
    DisplayFilteringResult Build(IReadOnlyList<ResourceChangeModel> mergedChanges);
}

/// <summary>
/// Result of display filtering containing filtered changes and suppression count.
/// </summary>
/// <param name="DisplayChanges">Resource changes to display after filtering.</param>
/// <param name="FilteredResourceCount">Number of resources suppressed by filtering.</param>
internal record DisplayFilteringResult(
    IReadOnlyList<ResourceChangeModel> DisplayChanges,
    int FilteredResourceCount);
