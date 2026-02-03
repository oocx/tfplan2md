using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents aggregated code analysis data for report rendering.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisReportModel
{
    /// <summary>
    /// Gets the summary metrics for code analysis findings.
    /// </summary>
    public required CodeAnalysisSummaryModel Summary { get; init; }

    /// <summary>
    /// Gets the list of tools that produced findings.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisToolModel> Tools { get; init; }

    /// <summary>
    /// Gets the list of warnings encountered while loading SARIF files.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisWarningModel> Warnings { get; init; }

    /// <summary>
    /// Gets the list of findings after filtering and mapping.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisFindingModel> Findings { get; init; }

    /// <summary>
    /// Gets the findings grouped by module for unmatched module-level entries.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisModuleFindingsModel> ModuleFindings { get; init; }

    /// <summary>
    /// Gets the findings that could not be mapped to a resource or module.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisFindingModel> UnmatchedFindings { get; init; }
}
