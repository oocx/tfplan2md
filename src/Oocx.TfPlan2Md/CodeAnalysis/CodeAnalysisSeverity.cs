namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents normalized severity levels for code analysis findings.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal enum CodeAnalysisSeverity
{
    /// <summary>
    /// Critical severity indicating urgent remediation.
    /// </summary>
    Critical = 0,

    /// <summary>
    /// High severity indicating significant risk.
    /// </summary>
    High = 1,

    /// <summary>
    /// Medium severity indicating moderate risk.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Low severity indicating minor risk.
    /// </summary>
    Low = 3,

    /// <summary>
    /// Informational severity for non-actionable findings.
    /// </summary>
    Informational = 4
}
