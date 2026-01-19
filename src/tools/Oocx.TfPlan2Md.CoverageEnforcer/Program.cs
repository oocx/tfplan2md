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
        var summary = summaryBuilder.BuildMarkdown(evaluation, options.ReportLink);
        File.WriteAllText(options.SummaryOutputPath, summary);
    }

    Console.WriteLine("Coverage summary:");
    Console.WriteLine($"  Line:   {metrics.LinePercentage:0.00}% (threshold {thresholds.LineThreshold:0.00}%) {(evaluation.LinePass ? "PASS" : "FAIL")}");
    Console.WriteLine($"  Branch: {metrics.BranchPercentage:0.00}% (threshold {thresholds.BranchThreshold:0.00}%) {(evaluation.BranchPass ? "PASS" : "FAIL")}");

    if (!evaluation.IsPassing)
    {
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
