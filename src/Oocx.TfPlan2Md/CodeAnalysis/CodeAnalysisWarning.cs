namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents a warning encountered while loading code analysis results.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisWarning
{
    /// <summary>
    /// Gets the file path that produced the warning.
    /// </summary>
    /// <value>The file path associated with the warning.</value>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the warning message describing the failure.
    /// </summary>
    /// <value>The warning message.</value>
    public required string Message { get; init; }
}
