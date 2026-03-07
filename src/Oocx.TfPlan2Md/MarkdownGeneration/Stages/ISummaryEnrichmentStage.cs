using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Calculates the report summary from resource change models.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal interface ISummaryEnrichmentStage
{
    /// <summary>
    /// Builds the report summary from the current resource changes.
    /// </summary>
    /// <param name="resourceChanges">The resource changes to summarize.</param>
    /// <returns>The computed report summary.</returns>
    SummaryModel Build(IReadOnlyList<ResourceChangeModel> resourceChanges);
}
