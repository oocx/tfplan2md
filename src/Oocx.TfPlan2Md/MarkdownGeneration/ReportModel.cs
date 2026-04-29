using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents the data model used by markdown renderers.
/// </summary>
internal class ReportModel
{
    /// <summary>
    /// Gets the Terraform version that created the plan.
    /// </summary>
    public required string TerraformVersion { get; init; }

    /// <summary>
    /// Gets the Terraform plan format version.
    /// </summary>
    public required string FormatVersion { get; init; }

    /// <summary>
    /// Gets the tfplan2md semantic version used to generate the report.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required string TfPlan2MdVersion { get; init; }

    /// <summary>
    /// Gets the Short git commit hash (7 characters) of the tfplan2md build used for rendering.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required string CommitHash { get; init; }

    /// <summary>
    /// Gets the UTC timestamp captured when the report was generated.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the metadata line should be hidden in the rendered report.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required bool HideMetadata { get; init; }

    /// <summary>
    /// Gets the timestamp string from the plan (if available).
    /// </summary>
    public string? Timestamp { get; init; }
    /// <summary>
    /// Gets the optional custom report title provided via the CLI.
    /// Related feature: docs/features/020-custom-report-title/specification.md.
    /// </summary>
    /// <value>
    /// The escaped title text used by templates; null when no custom title is provided so templates can apply defaults.
    /// </value>
    public string? ReportTitle { get; init; }

    /// <summary>
    /// Gets the list of all resource changes in the plan.
    /// </summary>
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }

    /// <summary>
    /// Gets resource changes organized by module.
    /// </summary>
    public required IReadOnlyList<ModuleChangeGroup> ModuleChanges { get; init; }

    /// <summary>
    /// Gets the summary statistics for the plan.
    /// </summary>
    public required SummaryModel Summary { get; init; }

    /// <summary>
    /// Gets the code analysis report data when SARIF inputs are provided.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    public CodeAnalysisReportModel? CodeAnalysis { get; init; }

    /// <summary>
    /// Gets a value indicating whether unchanged attribute values are included in attribute change tables.
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// </summary>
    public required bool ShowUnchangedValues { get; init; }

    /// <summary>
    /// Gets a value indicating whether attribute change rows where before and after values
    /// are Azure resource IDs that differ only in casing are suppressed.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </summary>
    public required bool IgnoreAzureIdCaseChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether sensitive values should be shown in plaintext instead of masked.
    /// When <c>false</c> (default), provider templates replace sensitive values with "(sensitive)".
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </summary>
    public required bool ShowSensitive { get; init; }

    /// <summary>
    /// Gets the target platform for markdown rendering.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    public required RenderTargets.RenderTarget RenderTarget { get; init; }

    /// <summary>
    /// Gets the display mode for resource details blocks.
    /// Related feature: docs/features/092-details-display-mode/specification.md.
    /// </summary>
    public required RenderTargets.DetailsDisplayMode DetailsDisplayMode { get; init; }

    /// <summary>
    /// Gets the refactoring operations (imports and moves) included in the plan.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    public required IReadOnlyList<RefactoringOperationModel> RefactoringOperations { get; init; }

    /// <summary>
    /// Gets the global/root outputs (outputs not belonging to any module).
    /// Ordered alphabetically by output name.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public IReadOnlyList<OutputChangeModel> GlobalOutputs { get; init; } = Array.Empty<OutputChangeModel>();

    /// <summary>
    /// Gets the number of resources suppressed from rendering because all their attribute changes
    /// were filtered out (e.g., by <c>--ignore-azure-id-case-changes</c>) and they have no other notable content.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </summary>
    public int FilteredResourceCount { get; init; }

    /// <summary>
    /// Gets the plan status information from Terraform 1.14+ plans.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public PlanStatusModel? PlanStatus { get; init; }

    /// <summary>
    /// Gets resource drift detected outside Terraform.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public IReadOnlyList<ResourceChangeModel> Drift { get; init; } = [];

    /// <summary>
    /// Gets the relevant attributes that influenced downstream resource computation.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    public IReadOnlyList<RelevantAttributeModel> RelevantAttributes { get; init; } = [];

    /// <summary>
    /// Gets the actions that do not attach to any rendered resource (invoke-mode
    /// + lifecycle orphans). Null when no such actions exist.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
    /// </summary>
    public OtherActionsModel? OtherActions { get; init; }
}
