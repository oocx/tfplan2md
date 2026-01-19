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
    /// Builds the absolute path for a coverage test data file.
    /// </summary>
    /// <param name="fileName">Coverage test data file name.</param>
    /// <returns>Absolute path to the test data file.</returns>
    private static string GetCoveragePath(string fileName)
    {
        return Path.Combine(DemoPaths.RepositoryRoot, "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", "Coverage", fileName);
    }
}
