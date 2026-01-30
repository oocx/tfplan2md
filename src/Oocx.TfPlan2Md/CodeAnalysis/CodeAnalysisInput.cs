using System.Collections.Generic;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents parsed code analysis inputs and filtering options.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisInput
{
    /// <summary>
    /// Gets the aggregated code analysis model for SARIF inputs.
    /// </summary>
    /// <value>The aggregated code analysis model.</value>
    public required CodeAnalysisModel Model { get; init; }

    /// <summary>
    /// Gets the warnings encountered while loading SARIF inputs.
    /// </summary>
    /// <value>The list of warnings, or an empty list when none were encountered.</value>
    public required IReadOnlyList<CodeAnalysisWarning> Warnings { get; init; }

    /// <summary>
    /// Gets the minimum severity level to display.
    /// </summary>
    /// <value>The minimum severity level, or <c>null</c> when not specified.</value>
    public CodeAnalysisSeverity? MinimumLevel { get; init; }

    /// <summary>
    /// Gets the severity level that triggers a failure.
    /// </summary>
    /// <value>The failure severity threshold, or <c>null</c> when not specified.</value>
    public CodeAnalysisSeverity? FailOnLevel { get; init; }
}
