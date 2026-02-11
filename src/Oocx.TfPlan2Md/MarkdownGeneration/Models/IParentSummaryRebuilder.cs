namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Interface for provider-specific parent summary rebuilders.
/// </summary>
/// <remarks>
/// Providers can implement this interface to rebuild parent summaries after parent-child merging.
/// This allows summaries to include data from both inline attributes and child resources.
/// Related issue: docs/issues/069-parent-child-summary-count-mismatch/analysis.md.
/// </remarks>
internal interface IParentSummaryRebuilder
{
    /// <summary>
    /// Determines if this rebuilder can handle the given parent resource.
    /// </summary>
    /// <param name="parent">The parent resource to check.</param>
    /// <returns><c>true</c> if this rebuilder can rebuild the parent's summary.</returns>
    bool CanRebuild(ResourceChangeModel parent);

    /// <summary>
    /// Rebuilds the parent's summary HTML based on merged child data.
    /// </summary>
    /// <param name="context">The rebuild context containing parent and dependencies.</param>
    void RebuildSummary(ParentSummaryRebuildContext context);
}
