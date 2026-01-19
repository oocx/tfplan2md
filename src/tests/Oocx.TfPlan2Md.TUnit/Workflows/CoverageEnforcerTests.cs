using System;
using System.IO;
using Oocx.TfPlan2Md.CoverageEnforcer;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Verifies Cobertura coverage parsing and threshold evaluation behavior.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
public class CoverageEnforcerTests
{
    /// <summary>
    /// Parses a valid Cobertura report and returns rounded percentages.
    /// </summary>
    [Test]
    public async Task Parses_cobertura_report_with_expected_percentages()
    {
        var parser = new CoberturaCoverageParser();
        var reportPath = GetCoveragePath("cobertura-sample.xml");

        var metrics = parser.Parse(reportPath);

        await Assert.That(metrics.LinePercentage).IsEqualTo(85.50m);
        await Assert.That(metrics.BranchPercentage).IsEqualTo(70.20m);
    }

    /// <summary>
    /// Treats missing branch data as zero percent rather than throwing.
    /// </summary>
    [Test]
    public async Task Handles_missing_branch_rate_gracefully()
    {
        var parser = new CoberturaCoverageParser();
        var reportPath = GetCoveragePath("cobertura-no-branch.xml");

        var metrics = parser.Parse(reportPath);

        await Assert.That(metrics.LinePercentage).IsEqualTo(91.20m);
        await Assert.That(metrics.BranchPercentage).IsEqualTo(0.00m);
    }

    /// <summary>
    /// Throws a descriptive exception when the Cobertura report is malformed.
    /// </summary>
    [Test]
    public async Task Throws_when_cobertura_report_is_malformed()
    {
        var parser = new CoberturaCoverageParser();
        var reportPath = GetCoveragePath("cobertura-malformed.xml");

        var action = () => parser.Parse(reportPath);

        await Assert.That(action).Throws<InvalidDataException>();
    }

    /// <summary>
    /// Fails evaluation when coverage falls below the threshold.
    /// </summary>
    [Test]
    public async Task Evaluator_fails_when_thresholds_not_met()
    {
        var evaluator = new CoverageThresholdEvaluator();
        var metrics = new CoverageMetrics(79.5m, 80.0m);
        var thresholds = new CoverageThresholds(80m, 80m);

        var evaluation = evaluator.Evaluate(metrics, thresholds);

        await Assert.That(evaluation.IsPassing).IsFalse();
        await Assert.That(evaluation.LinePass).IsFalse();
        await Assert.That(evaluation.BranchPass).IsTrue();
    }

    /// <summary>
    /// Passes evaluation when coverage meets or exceeds thresholds.
    /// </summary>
    [Test]
    public async Task Evaluator_passes_when_thresholds_met()
    {
        var evaluator = new CoverageThresholdEvaluator();
        var metrics = new CoverageMetrics(80m, 82.5m);
        var thresholds = new CoverageThresholds(80m, 80m);

        var evaluation = evaluator.Evaluate(metrics, thresholds);

        await Assert.That(evaluation.IsPassing).IsTrue();
        await Assert.That(evaluation.LinePass).IsTrue();
        await Assert.That(evaluation.BranchPass).IsTrue();
    }

    /// <summary>
    /// Builds a markdown summary table including metrics and thresholds.
    /// </summary>
    [Test]
    public async Task Summary_builder_includes_metric_rows()
    {
        var evaluation = new CoverageEvaluation(
            new CoverageMetrics(85.5m, 70.2m),
            new CoverageThresholds(80m, 75m),
            linePass: true,
            branchPass: false);
        var builder = new CoverageSummaryBuilder();

        var summary = builder.BuildMarkdown(evaluation, new Uri("https://example.test/coverage"), overrideActive: false);

        await Assert.That(summary).Contains("Code Coverage Summary", StringComparison.Ordinal);
        await Assert.That(summary).Contains("Line", StringComparison.Ordinal);
        await Assert.That(summary).Contains("85.50%", StringComparison.Ordinal);
        await Assert.That(summary).Contains("70.20%", StringComparison.Ordinal);
        await Assert.That(summary).Contains("Coverage report artifact", StringComparison.Ordinal);
    }

    /// <summary>
    /// Includes an override note when enforcement is bypassed.
    /// </summary>
    [Test]
    public async Task Summary_builder_includes_override_note_when_active()
    {
        var evaluation = new CoverageEvaluation(
            new CoverageMetrics(70m, 60m),
            new CoverageThresholds(80m, 75m),
            linePass: false,
            branchPass: false);
        var builder = new CoverageSummaryBuilder();

        var summary = builder.BuildMarkdown(evaluation, reportLink: null, overrideActive: true);

        await Assert.That(summary).Contains("Override active", StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds the absolute path for a coverage test data file.
    /// </summary>
    /// <param name="fileName">Coverage test data file name.</param>
    /// <returns>Absolute path to the test data file.</returns>
    private static string GetCoveragePath(string fileName)
    {
        return Path.Combine(DemoPaths.RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "Coverage", fileName);
    }
}
