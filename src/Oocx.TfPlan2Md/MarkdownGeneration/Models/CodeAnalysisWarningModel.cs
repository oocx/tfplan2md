namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents a warning generated while processing code analysis inputs.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public sealed class CodeAnalysisWarningModel
{
    /// <summary>
    /// Gets the SARIF file path that produced the warning.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public required string Message { get; init; }
}
