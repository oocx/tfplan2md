namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Captures the results of evaluating coverage metrics against thresholds.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
internal sealed record CoverageEvaluation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageEvaluation"/> class.
    /// Initializes a new instance of the <see cref="CoverageEvaluation"/> record.
    /// </summary>
    /// <param name="metrics">Measured coverage metrics.</param>
    /// <param name="thresholds">Configured coverage thresholds.</param>
    /// <param name="linePass">Whether line coverage meets the threshold.</param>
    /// <param name="branchPass">Whether branch coverage meets the threshold.</param>
    internal CoverageEvaluation(CoverageMetrics metrics, CoverageThresholds thresholds, bool linePass, bool branchPass)
    {
        Metrics = metrics;
        Thresholds = thresholds;
        LinePass = linePass;
        BranchPass = branchPass;
    }

    /// <summary>
    /// Gets the measured coverage metrics.
    /// </summary>
    internal CoverageMetrics Metrics { get; }

    /// <summary>
    /// Gets the configured coverage thresholds.
    /// </summary>
    internal CoverageThresholds Thresholds { get; }

    /// <summary>
    /// Gets a value indicating whether line coverage meets the threshold.
    /// </summary>
    internal bool LinePass { get; }

    /// <summary>
    /// Gets a value indicating whether branch coverage meets the threshold.
    /// </summary>
    internal bool BranchPass { get; }

    /// <summary>
    /// Gets a value indicating whether all thresholds were satisfied.
    /// </summary>
    internal bool IsPassing => LinePass && BranchPass;
}
