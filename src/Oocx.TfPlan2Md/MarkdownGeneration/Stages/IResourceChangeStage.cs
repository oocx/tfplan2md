using System.Collections.Generic;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Builds the initial resource change models from a parsed Terraform plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal interface IResourceChangeStage
{
    /// <summary>
    /// Creates one <see cref="ResourceChangeModel"/> per plan resource change.
    /// </summary>
    /// <param name="plan">The parsed Terraform plan.</param>
    /// <param name="preBuiltReferenceIndex">
    /// An optional pre-built configuration reference index. When provided, this index is reused
    /// instead of rebuilding it from <paramref name="plan"/>, avoiding a duplicate pass over the
    /// plan configuration. Pass <c>null</c> to let the stage build the index itself.
    /// </param>
    /// <returns>The initial resource change models for downstream pipeline stages.</returns>
    IReadOnlyList<ResourceChangeModel> Build(
        TerraformPlan plan,
        IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>? preBuiltReferenceIndex = null);
}
