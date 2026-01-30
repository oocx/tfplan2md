namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents a mapped Terraform resource location derived from a SARIF logical location.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed record CodeAnalysisResourceLocation
{
    /// <summary>
    /// Gets the Terraform resource address.
    /// </summary>
    /// <value>The resource address string.</value>
    public required string ResourceAddress { get; init; }

    /// <summary>
    /// Gets the attribute path when present.
    /// </summary>
    /// <value>The attribute path, or <c>null</c> when not available.</value>
    public string? AttributePath { get; init; }

    /// <summary>
    /// Gets the module address when the resource belongs to a module.
    /// </summary>
    /// <value>The module address, or <c>null</c> for root module resources.</value>
    public string? ModuleAddress { get; init; }
}
