using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Builds a fully-populated report model from a parsed Terraform plan.
    /// </summary>
    /// <param name="plan">Terraform plan to transform into a report model.</param>
    /// <returns>A model containing change details, summaries, and optional custom title.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Build remains the orchestration boundary while explicit report stages are extracted incrementally.")]
    public ReportModel Build(TerraformPlan plan)
    {
        _configurationReferenceIndex.Clear();
        foreach (var (key, value) in ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration))
        {
            _configurationReferenceIndex[key] = value;
        }

        var allChanges = (_resourceChangeStage ?? CreateResourceChangeStage())
            .Build(plan, _configurationReferenceIndex)
            .ToList();

        allChanges = (_attributeFilteringStage ?? CreateAttributeFilteringStage())
            .Build(allChanges)
            .ToList();

        var codeAnalysisReport = BuildCodeAnalysisReport(allChanges);

        // CRITICAL: Calculate summary BEFORE parent-child merging.
        // Parent-child grouping is visual only and must NOT affect resource counts.
        // The summary must reflect actual Terraform changes (all resources).
        // Related feature: docs/features/068-parent-child-resource-grouping/specification.md
        var summary = (_summaryEnrichmentStage ?? CreateSummaryEnrichmentStage()).Build(allChanges);

        // Now merge parent-child relationships (this removes children from allChanges for display)
        MergeParentChildRelationships(allChanges);

        // Filter merged changes for display (removes no-op resources and Azure ID case-only changes)
        // and normalize module addresses. Runs after merging to ensure visual grouping doesn't
        // affect which resources are displayed.
        var filteringResult = (_displayFilteringStage ?? CreateDisplayFilteringStage()).Build(allChanges);
        var displayChanges = filteringResult.DisplayChanges.ToList();
        var filteredResourceCount = filteringResult.FilteredResourceCount;

        // Build output models from the plan
        // Related feature: docs/features/097-terraform-outputs/specification.md
        var allOutputs = BuildOutputModels(plan);

        var escapedReportTitle = _reportTitle is null ? null : EscapeMarkdownHeading(_reportTitle);
        var metadata = _metadataProvider.GetMetadata();
        return (_reportAssemblyStage ?? CreateReportAssemblyStage()).Build(
            new ReportAssemblyInput(
                Plan: plan,
                AllChanges: allChanges,
                DisplayChanges: displayChanges,
                AllOutputs: allOutputs,
                Summary: summary,
                CodeAnalysisReport: codeAnalysisReport,
                FilteredResourceCount: filteredResourceCount,
                EscapedReportTitle: escapedReportTitle,
                Metadata: metadata,
                HideMetadata: _hideMetadata,
                ShowUnchangedValues: _showUnchangedValues,
                IgnoreAzureIdCaseChanges: _ignoreAzureIdCaseChanges,
                ShowSensitive: _showSensitive,
                RenderTarget: _renderTarget,
                DetailsDisplayMode: _detailsDisplayMode));
    }
}
