namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Evaluates coverage metrics against configured thresholds.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
internal sealed class CoverageThresholdEvaluator
{
    /// <summary>
    /// Evaluates whether the provided metrics meet the specified thresholds.
    /// </summary>
    /// <param name="metrics">Coverage metrics extracted from the report.</param>
    /// <param name="thresholds">Configured coverage thresholds.</param>
    /// <returns>Evaluation results indicating pass/fail and details.</returns>
    internal CoverageEvaluation Evaluate(CoverageMetrics metrics, CoverageThresholds thresholds)
    {
        var linePass = metrics.LinePercentage >= thresholds.LineThreshold;
        var branchPass = metrics.BranchPercentage >= thresholds.BranchThreshold;

        return new CoverageEvaluation(metrics, thresholds, linePass, branchPass);
    }
}
