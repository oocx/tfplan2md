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
    /// <returns>The initial resource change models for downstream pipeline stages.</returns>
    IReadOnlyList<ResourceChangeModel> Build(TerraformPlan plan);
}
