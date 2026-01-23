using System.Globalization;
using System.Text;

namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Builds markdown summaries for coverage reporting in CI.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
internal sealed class CoverageSummaryBuilder
{
    /// <summary>
    /// Builds a markdown summary table for the provided coverage evaluation.
    /// </summary>
    /// <param name="evaluation">Coverage evaluation results.</param>
    /// <param name="reportLink">Optional link to the detailed coverage report.</param>
    /// <param name="overrideActive">Whether a coverage override is active.</param>
    /// <returns>Markdown summary content.</returns>
    internal string BuildMarkdown(CoverageEvaluation evaluation, Uri? reportLink, bool overrideActive)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!-- coverage-summary -->");
        builder.AppendLine("## Code Coverage Summary");
        builder.AppendLine();
        builder.AppendLine("| Metric | Coverage | Threshold | Status |");
        builder.AppendLine("| --- | --- | --- | --- |");
        builder.AppendLine(BuildRow("Line", evaluation.Metrics.LinePercentage, evaluation.Thresholds.LineThreshold, evaluation.LinePass));
        builder.AppendLine(BuildRow("Branch", evaluation.Metrics.BranchPercentage, evaluation.Thresholds.BranchThreshold, evaluation.BranchPass));

        if (overrideActive)
        {
            builder.AppendLine();
            builder.AppendLine("**Override active:** coverage enforcement bypassed via `coverage-override` label.");
        }

        if (reportLink is not null)
        {
            builder.AppendLine();
            builder.AppendLine(FormattableString.Invariant($"[Coverage report artifact]({reportLink})"));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a markdown table row for a coverage metric.
    /// </summary>
    /// <param name="label">Metric label.</param>
    /// <param name="coverage">Measured coverage percentage.</param>
    /// <param name="threshold">Threshold percentage.</param>
    /// <param name="isPassing">Whether the metric meets the threshold.</param>
    /// <returns>Formatted markdown row.</returns>
    private static string BuildRow(string label, decimal coverage, decimal threshold, bool isPassing)
    {
        var status = isPassing ? "✅ Pass" : "❌ Fail";
        return string.Create(CultureInfo.InvariantCulture, $"| {label} | {coverage:0.00}% | {threshold:0.00}% | {status} |");
    }
}
