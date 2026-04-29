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
    RenderTargets.DetailsDisplayMode DetailsDisplayMode)
{
    /// <summary>
    /// Gets the plan status information from Terraform 1.14+ plans.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public PlanStatusModel? PlanStatus { get; init; }

    /// <summary>
    /// Gets resource drift detected outside Terraform.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public IReadOnlyList<ResourceChangeModel>? Drift { get; init; }

    /// <summary>
    /// Gets the relevant attributes that influenced downstream resource computation.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public IReadOnlyList<RelevantAttributeModel>? RelevantAttributes { get; init; }

    /// <summary>
    /// Gets the action invocations that do not attach to any rendered resource
    /// (invoke-mode actions and lifecycle orphans). Null when no such actions exist.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
    /// </summary>
    public OtherActionsModel? OtherActions { get; init; }
}
