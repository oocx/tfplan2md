namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents a code analysis finding mapped to a Terraform resource location.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisMappedFinding
{
    /// <summary>
    /// Gets the source SARIF finding that produced this mapped entry.
    /// </summary>
    /// <value>The source finding.</value>
    public required CodeAnalysisFinding Source { get; init; }

    /// <summary>
    /// Gets the derived severity for the finding.
    /// </summary>
    /// <value>The normalized severity.</value>
    public required CodeAnalysisSeverity Severity { get; init; }

    /// <summary>
    /// Gets the mapped resource address when available.
    /// </summary>
    /// <value>The resource address, or <c>null</c> when not mapped.</value>
    public string? ResourceAddress { get; init; }

    /// <summary>
    /// Gets the mapped module address when available.
    /// </summary>
    /// <value>The module address, or <c>null</c> when not mapped.</value>
    public string? ModuleAddress { get; init; }

    /// <summary>
    /// Gets the mapped attribute path when available.
    /// </summary>
    /// <value>The attribute path, or <c>null</c> when not mapped.</value>
    public string? AttributePath { get; init; }
}
