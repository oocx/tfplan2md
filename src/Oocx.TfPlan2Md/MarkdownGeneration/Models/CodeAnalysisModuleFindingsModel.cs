using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents code analysis findings scoped to a module.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisModuleFindingsModel
{
    /// <summary>
    /// Gets the module address associated with the findings.
    /// </summary>
    public required string ModuleAddress { get; init; }

    /// <summary>
    /// Gets the findings for the module.
    /// </summary>
    public required IReadOnlyList<CodeAnalysisFindingModel> Findings { get; init; }
}
