using System.Globalization;

namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Parses command line arguments for the coverage enforcer tool.
/// Related feature: docs/features/043-code-coverage-ci/specification.md
/// </summary>
internal sealed class CommandLineOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineOptions"/> class.
    /// </summary>
    /// <param name="reportPath">Path to the Cobertura report.</param>
    /// <param name="lineThreshold">Line coverage threshold percentage.</param>
    /// <param name="branchThreshold">Branch coverage threshold percentage.</param>
    private CommandLineOptions(string reportPath, decimal lineThreshold, decimal branchThreshold, string? summaryOutputPath, Uri? reportLink)
    {
        ReportPath = reportPath;
        LineThreshold = lineThreshold;
        BranchThreshold = branchThreshold;
        SummaryOutputPath = summaryOutputPath;
        ReportLink = reportLink;
    }

    /// <summary>
    /// Gets the Cobertura report path.
    /// </summary>
    internal string ReportPath { get; }

    /// <summary>
    /// Gets the configured line coverage threshold percentage.
    /// </summary>
    internal decimal LineThreshold { get; }

    /// <summary>
    /// Gets the configured branch coverage threshold percentage.
    /// </summary>
    internal decimal BranchThreshold { get; }

    /// <summary>
    /// Gets the optional path for writing the markdown summary.
    /// </summary>
    internal string? SummaryOutputPath { get; }

    /// <summary>
    /// Gets the optional link to the detailed coverage report.
    /// </summary>
    internal Uri? ReportLink { get; }

    /// <summary>
    /// Parses command line arguments and environment variables into options.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Parsed command line options.</returns>
    /// <exception cref="InvalidDataException">Thrown when required arguments are missing or invalid.</exception>
    internal static CommandLineOptions Parse(string[] args)
    {
        var reportPath = GetArgumentValue(args, "--report") ?? GetArgumentValue(args, "-r");
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new InvalidDataException("Cobertura report path is required. Use --report <path>.");
        }

        var lineThreshold = GetDecimalArgument(args, "--line-threshold")
            ?? GetDecimalEnvironment("COVERAGE_LINE_THRESHOLD")
            ?? throw new InvalidDataException("Line coverage threshold is required. Use --line-threshold or COVERAGE_LINE_THRESHOLD.");

        var branchThreshold = GetDecimalArgument(args, "--branch-threshold")
            ?? GetDecimalEnvironment("COVERAGE_BRANCH_THRESHOLD")
            ?? throw new InvalidDataException("Branch coverage threshold is required. Use --branch-threshold or COVERAGE_BRANCH_THRESHOLD.");

        var summaryOutputPath = GetArgumentValue(args, "--summary-output");
        var reportLink = GetUriArgument(args, "--report-link");

        return new CommandLineOptions(reportPath, lineThreshold, branchThreshold, summaryOutputPath, reportLink);
    }

    /// <summary>
    /// Retrieves a string argument value from the command line, supporting --key=value and --key value formats.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="name">Argument name to search for.</param>
    /// <returns>Argument value or null if not present.</returns>
    private static string? GetArgumentValue(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index += 1)
        {
            var arg = args[index];
            if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return index + 1 < args.Length ? args[index + 1] : null;
            }

            var prefix = name + "=";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves a decimal argument value from the command line.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="name">Argument name to search for.</param>
    /// <returns>Parsed decimal value or null if not present.</returns>
    /// <exception cref="InvalidDataException">Thrown when the value cannot be parsed.</exception>
    private static decimal? GetDecimalArgument(string[] args, string name)
    {
        var value = GetArgumentValue(args, name);
        return value is null ? null : ParseDecimal(value, name);
    }

    /// <summary>
    /// Retrieves a decimal argument value from the environment.
    /// </summary>
    /// <param name="name">Environment variable name.</param>
    /// <returns>Parsed decimal value or null if not present.</returns>
    /// <exception cref="InvalidDataException">Thrown when the value cannot be parsed.</exception>
    private static decimal? GetDecimalEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value, name);
    }

    /// <summary>
    /// Parses a decimal using invariant culture and throws when invalid.
    /// </summary>
    /// <param name="value">Value to parse.</param>
    /// <param name="source">Source label for error messages.</param>
    /// <returns>Parsed decimal value.</returns>
    /// <exception cref="InvalidDataException">Thrown when parsing fails.</exception>
    private static decimal ParseDecimal(string value, string source)
    {
        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Invalid decimal value '{value}' for {source}.");
        }

        return parsed;
    }

    /// <summary>
    /// Parses a URI argument value from the command line.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="name">Argument name to search for.</param>
    /// <returns>Parsed URI or null if not present.</returns>
    /// <exception cref="InvalidDataException">Thrown when the URI cannot be parsed.</exception>
    private static Uri? GetUriArgument(string[] args, string name)
    {
        var value = GetArgumentValue(args, name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidDataException($"Invalid URI value '{value}' for {name}.");
        }

        return uri;
    }
}
