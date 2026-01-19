namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Represents line and branch coverage percentages extracted from a Cobertura report.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed record CoverageMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageMetrics"/> record.
    /// </summary>
    /// <param name="linePercentage">Line coverage percentage in the 0-100 range.</param>
    /// <param name="branchPercentage">Branch coverage percentage in the 0-100 range.</param>
    internal CoverageMetrics(decimal linePercentage, decimal branchPercentage)
    {
        LinePercentage = linePercentage;
        BranchPercentage = branchPercentage;
    }

    /// <summary>
    /// Gets the line coverage percentage in the 0-100 range.
    /// </summary>
    internal decimal LinePercentage { get; }

    /// <summary>
    /// Gets the branch coverage percentage in the 0-100 range.
    /// </summary>
    internal decimal BranchPercentage { get; }
}
