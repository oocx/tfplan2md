using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Oocx.TfPlan2Md.CoverageEnforcer;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Verifies Cobertura coverage parsing and threshold evaluation behavior.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
public class CoverageEnforcerTests
{
    /// <summary>
    /// Parses a valid Cobertura report and returns rounded percentages.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
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
    /// Generates an SVG badge with the expected percentage and color.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Badge_generator_includes_percentage_and_color()
    {
        var generator = new CoverageBadgeGenerator();

        var svg = generator.GenerateSvg(85.5m);

        await Assert.That(svg).Contains("85.50%", StringComparison.Ordinal);
        await Assert.That(svg).Contains("#97ca00", StringComparison.Ordinal);
    }

    /// <summary>
    /// Appends the latest entry to the coverage history file.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task History_writer_appends_entry()
    {
        var tempDirectory = CreateTempDirectory();
        var historyPath = Path.Combine(tempDirectory, "history.json");
        var entry = new CoverageHistoryEntry(
            DateTimeOffset.Parse("2026-01-20T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            "abc123",
            80m,
            70m);
        var writer = new CoverageHistoryWriter();

        writer.UpdateHistory(historyPath, entry);

        var json = File.ReadAllText(historyPath);
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("Entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);
        await Assert.That(entries[0].GetProperty("CommitSha").GetString()).IsEqualTo("abc123");
    }

    /// <summary>
    /// Replaces an existing history entry when the commit SHA matches.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task History_writer_replaces_existing_entry_for_same_commit()
    {
        var tempDirectory = CreateTempDirectory();
        var historyPath = Path.Combine(tempDirectory, "history.json");
        var writer = new CoverageHistoryWriter();
        var originalEntry = new CoverageHistoryEntry(
            DateTimeOffset.Parse("2026-01-19T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            "abc123",
            80m,
            70m);
        var updatedEntry = new CoverageHistoryEntry(
            DateTimeOffset.Parse("2026-01-20T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            "abc123",
            85m,
            72m);

        writer.UpdateHistory(historyPath, originalEntry);
        writer.UpdateHistory(historyPath, updatedEntry);

        var json = File.ReadAllText(historyPath);
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("Entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);
        await Assert.That(entries[0].GetProperty("LineCoverage").GetDecimal()).IsEqualTo(85m);
    }

    /// <summary>
    /// Throws when the history path is missing.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task History_writer_throws_when_history_path_is_missing()
    {
        var writer = new CoverageHistoryWriter();
        var entry = new CoverageHistoryEntry(
            DateTimeOffset.Parse("2026-01-20T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            "abc123",
            80m,
            70m);

        var action = () => writer.UpdateHistory(string.Empty, entry);

        await Assert.That(action).Throws<InvalidDataException>();
    }

    /// <summary>
    /// Parses full command line options including optional parameters.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Command_line_options_parse_reads_all_supported_arguments()
    {
        var args = new[]
        {
            "--report",
            "/tmp/coverage.cobertura.xml",
            "--line-threshold=81.25",
            "--branch-threshold",
            "72.50",
            "--summary-output",
            "/tmp/summary.md",
            "--report-link",
            "https://example.test/coverage",
            "--override-active",
            "true",
            "--badge-output",
            "/tmp/badge.svg",
            "--history-output",
            "/tmp/history.json",
            "--commit-sha",
            "abc123",
            "--timestamp",
            "2026-01-20T00:00:00Z",
        };

        var options = CommandLineOptions.Parse(args);

        await Assert.That(options.ReportPath).IsEqualTo("/tmp/coverage.cobertura.xml");
        await Assert.That(options.LineThreshold).IsEqualTo(81.25m);
        await Assert.That(options.BranchThreshold).IsEqualTo(72.50m);
        await Assert.That(options.SummaryOutputPath).IsEqualTo("/tmp/summary.md");
        await Assert.That(options.ReportLink?.ToString()).IsEqualTo("https://example.test/coverage");
        await Assert.That(options.OverrideActive).IsTrue();
        await Assert.That(options.BadgeOutputPath).IsEqualTo("/tmp/badge.svg");
        await Assert.That(options.HistoryOutputPath).IsEqualTo("/tmp/history.json");
        await Assert.That(options.CommitSha).IsEqualTo("abc123");
        await Assert.That(options.Timestamp).IsEqualTo(DateTimeOffset.Parse("2026-01-20T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }

    /// <summary>
    /// Uses environment variables when thresholds are not provided on the command line.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Command_line_options_parse_uses_environment_thresholds_when_missing()
    {
        var originalLine = Environment.GetEnvironmentVariable("COVERAGE_LINE_THRESHOLD");
        var originalBranch = Environment.GetEnvironmentVariable("COVERAGE_BRANCH_THRESHOLD");
        try
        {
            Environment.SetEnvironmentVariable("COVERAGE_LINE_THRESHOLD", "88.5");
            Environment.SetEnvironmentVariable("COVERAGE_BRANCH_THRESHOLD", "77.25");
            var args = new[] { "--report", "/tmp/coverage.cobertura.xml" };

            var options = CommandLineOptions.Parse(args);

            await Assert.That(options.LineThreshold).IsEqualTo(88.5m);
            await Assert.That(options.BranchThreshold).IsEqualTo(77.25m);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COVERAGE_LINE_THRESHOLD", originalLine);
            Environment.SetEnvironmentVariable("COVERAGE_BRANCH_THRESHOLD", originalBranch);
        }
    }

    /// <summary>
    /// Throws when an invalid decimal threshold is provided.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task Command_line_options_parse_throws_on_invalid_decimal_threshold()
    {
        var args = new[]
        {
            "--report",
            "/tmp/coverage.cobertura.xml",
            "--line-threshold",
            "not-a-number",
            "--branch-threshold",
            "70",
        };

        var action = () => CommandLineOptions.Parse(args);

        await Assert.That(action).Throws<InvalidDataException>();
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

    /// <summary>
    /// Creates a temporary directory under the repository root for test output.
    /// </summary>
    /// <returns>Path to the created directory.</returns>
    private static string CreateTempDirectory()
    {
        var tempPath = Path.Combine(DemoPaths.RepositoryRoot, ".tmp", "coverage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }
}
