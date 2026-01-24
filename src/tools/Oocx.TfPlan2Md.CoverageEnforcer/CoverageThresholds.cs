namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Defines coverage threshold percentages required for passing the CI gate.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
internal sealed record CoverageThresholds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageThresholds"/> class.
    /// Initializes a new instance of the <see cref="CoverageThresholds"/> record.
    /// </summary>
    /// <param name="lineThreshold">Line coverage threshold percentage.</param>
    /// <param name="branchThreshold">Branch coverage threshold percentage.</param>
    internal CoverageThresholds(decimal lineThreshold, decimal branchThreshold)
    {
        LineThreshold = lineThreshold;
        BranchThreshold = branchThreshold;
    }

    /// <summary>
    /// Gets the minimum line coverage percentage required for passing.
    /// </summary>
    internal decimal LineThreshold { get; }

    /// <summary>
    /// Gets the minimum branch coverage percentage required for passing.
    /// </summary>
    internal decimal BranchThreshold { get; }
}
