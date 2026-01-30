namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Summarizes code analysis findings by severity.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisSummaryModel
{
    /// <summary>
    /// Gets the number of critical findings.
    /// </summary>
    public required int CriticalCount { get; init; }

    /// <summary>
    /// Gets the number of high findings.
    /// </summary>
    public required int HighCount { get; init; }

    /// <summary>
    /// Gets the number of medium findings.
    /// </summary>
    public required int MediumCount { get; init; }

    /// <summary>
    /// Gets the number of low findings.
    /// </summary>
    public required int LowCount { get; init; }

    /// <summary>
    /// Gets the number of informational findings.
    /// </summary>
    public required int InformationalCount { get; init; }

    /// <summary>
    /// Gets the total number of findings.
    /// </summary>
    public required int TotalCount { get; init; }
}
