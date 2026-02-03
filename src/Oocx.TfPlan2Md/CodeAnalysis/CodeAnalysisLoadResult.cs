using System.Collections.Generic;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents the aggregated result of loading one or more SARIF inputs.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisLoadResult
{
    /// <summary>
    /// Gets the aggregated code analysis model from parsed SARIF inputs.
    /// </summary>
    /// <value>The aggregated model.</value>
    public required CodeAnalysisModel Model { get; init; }

    /// <summary>
    /// Gets the warnings encountered while parsing SARIF inputs.
    /// </summary>
    /// <value>The list of warnings, or an empty list when none were encountered.</value>
    public required IReadOnlyList<CodeAnalysisWarning> Warnings { get; init; }
}
