using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Pipeline stage that assembles the final report model from precomputed inputs.
/// </summary>
/// <remarks>
/// Related features:
/// - docs/features/057-terraform-import-moved-blocks/specification.md.
/// - docs/features/097-terraform-outputs/specification.md.
/// </remarks>
internal interface IReportAssemblyStage
{
    /// <summary>
    /// Builds the final <see cref="ReportModel"/> from the supplied pipeline outputs.
    /// </summary>
    /// <param name="input">All data required to assemble the final report model.</param>
    /// <returns>The fully assembled report model.</returns>
    ReportModel Build(ReportAssemblyInput input);
}

/// <summary>
/// Immutable inputs required to assemble the final report model.
/// </summary>
internal sealed record ReportAssemblyInput(
    TerraformPlan Plan,
    IReadOnlyList<ResourceChangeModel> AllChanges,
    IReadOnlyList<ResourceChangeModel> DisplayChanges,
    IReadOnlyList<OutputChangeModel> AllOutputs,
    SummaryModel Summary,
    CodeAnalysisReportModel? CodeAnalysisReport,
    int FilteredResourceCount,
    string? EscapedReportTitle,
    ReportMetadata Metadata,
    bool HideMetadata,
    bool ShowUnchangedValues,
    bool IgnoreAzureIdCaseChanges,
    bool ShowSensitive,
    RenderTargets.RenderTarget RenderTarget,
    RenderTargets.DetailsDisplayMode DetailsDisplayMode);
