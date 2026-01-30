namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Evaluates code analysis findings against failure thresholds.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class CodeAnalysisFailureEvaluator
{
    /// <summary>
    /// Counts findings at or above a severity threshold.
    /// </summary>
    /// <param name="model">The code analysis model containing findings.</param>
    /// <param name="threshold">The severity threshold.</param>
    /// <returns>The number of findings at or above the threshold.</returns>
    internal static int CountFindingsAtOrAbove(CodeAnalysisModel model, CodeAnalysisSeverity threshold)
    {
        var thresholdRank = GetSeverityRank(threshold);
        var count = 0;

        foreach (var finding in model.Findings)
        {
            var severity = SeverityMapper.DeriveSeverity(finding);
            if (GetSeverityRank(severity) >= thresholdRank)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Formats a severity label for error messaging.
    /// </summary>
    /// <param name="severity">The severity to format.</param>
    /// <returns>The lowercase severity label.</returns>
    internal static string FormatSeverityLabel(CodeAnalysisSeverity severity)
    {
        return severity switch
        {
            CodeAnalysisSeverity.Critical => "critical",
            CodeAnalysisSeverity.High => "high",
            CodeAnalysisSeverity.Medium => "medium",
            CodeAnalysisSeverity.Low => "low",
            _ => "informational"
        };
    }

    /// <summary>
    /// Converts a severity value to its comparable rank.
    /// </summary>
    /// <param name="severity">The severity value.</param>
    /// <returns>The severity rank.</returns>
    private static int GetSeverityRank(CodeAnalysisSeverity severity)
    {
        return severity switch
        {
            CodeAnalysisSeverity.Critical => 5,
            CodeAnalysisSeverity.High => 4,
            CodeAnalysisSeverity.Medium => 3,
            CodeAnalysisSeverity.Low => 2,
            _ => 1
        };
    }
}
