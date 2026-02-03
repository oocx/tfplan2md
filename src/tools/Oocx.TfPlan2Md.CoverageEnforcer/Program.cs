using Oocx.TfPlan2Md.CoverageEnforcer;

try
{
    var options = CommandLineOptions.Parse(args);
    var parser = new CoberturaCoverageParser();
    var metrics = parser.Parse(options.ReportPath);
    var thresholds = new CoverageThresholds(options.LineThreshold, options.BranchThreshold);
    var evaluator = new CoverageThresholdEvaluator();
    var evaluation = evaluator.Evaluate(metrics, thresholds);

    if (!string.IsNullOrWhiteSpace(options.SummaryOutputPath))
    {
        var summaryBuilder = new CoverageSummaryBuilder();
        var summary = summaryBuilder.BuildMarkdown(evaluation, options.ReportLink, options.OverrideActive);
        File.WriteAllText(options.SummaryOutputPath, summary);
    }

    if (!string.IsNullOrWhiteSpace(options.BadgeOutputPath))
    {
        var badgeGenerator = new CoverageBadgeGenerator();
        var badgeSvg = badgeGenerator.GenerateSvg(metrics.LinePercentage);
        File.WriteAllText(options.BadgeOutputPath, badgeSvg);
    }

    if (!string.IsNullOrWhiteSpace(options.HistoryOutputPath))
    {
        if (string.IsNullOrWhiteSpace(options.CommitSha))
        {
            throw new InvalidDataException("Commit SHA is required when updating coverage history.");
        }

        var historyWriter = new CoverageHistoryWriter();
        var timestamp = options.Timestamp ?? DateTimeOffset.UtcNow;
        var entry = new CoverageHistoryEntry(timestamp, options.CommitSha, metrics.LinePercentage, metrics.BranchPercentage);
        historyWriter.UpdateHistory(options.HistoryOutputPath, entry);
    }

    Console.WriteLine("Coverage summary:");
    Console.WriteLine($"  Line:   {metrics.LinePercentage:0.00}% (threshold {thresholds.LineThreshold:0.00}%) {(evaluation.LinePass ? "PASS" : "FAIL")}");
    Console.WriteLine($"  Branch: {metrics.BranchPercentage:0.00}% (threshold {thresholds.BranchThreshold:0.00}%) {(evaluation.BranchPass ? "PASS" : "FAIL")}");

    if (!evaluation.IsPassing)
    {
        if (options.OverrideActive)
        {
            Console.WriteLine("Coverage override active; bypassing enforcement failure.");
            return 0;
        }

        Console.Error.WriteLine("Coverage thresholds not met.");
        return 1;
    }

    Console.WriteLine("Coverage thresholds met.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Coverage enforcement failed: {exception.Message}");
    return 2;
}
