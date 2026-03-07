using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Applies attribute-level filtering to resource change models before later report stages run.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal interface IAttributeFilteringStage
{
    /// <summary>
    /// Filters resource attribute changes for downstream report stages.
    /// </summary>
    /// <param name="resourceChanges">The resource change models to filter.</param>
    /// <returns>The filtered resource change models.</returns>
    IReadOnlyList<ResourceChangeModel> Build(IReadOnlyList<ResourceChangeModel> resourceChanges);
}
