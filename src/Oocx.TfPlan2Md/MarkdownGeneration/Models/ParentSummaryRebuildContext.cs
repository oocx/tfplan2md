using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Context data for rebuilding parent summaries after parent-child merging.
/// </summary>
/// <param name="Parent">The parent resource whose summary should be rebuilt.</param>
/// <param name="IconProviderRegistry">Optional icon provider registry for icon resolution.</param>
/// <remarks>
/// Related issue: docs/issues/069-parent-child-summary-count-mismatch/analysis.md.
/// </remarks>
internal sealed record ParentSummaryRebuildContext(
    ResourceChangeModel Parent,
    IconProviderRegistry? IconProviderRegistry);
