using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.CLI;

/// <summary>
/// Represents the parsed command-line options.
/// </summary>
public record CliOptions
{
    /// <summary>
    /// Input file path. If null, read from stdin.
    /// </summary>
    public string? InputFile { get; init; }

    /// <summary>
    /// Output file path. If null, write to stdout.
    /// </summary>
    public string? OutputFile { get; init; }

    /// <summary>
    /// Custom template file path.
    /// </summary>
    public string? TemplatePath { get; init; }

    /// <summary>
    /// Optional custom report title provided via the CLI.
    /// Related feature: docs/features/custom-report-title/specification.md
    /// </summary>
    public string? ReportTitle { get; init; }

    /// <summary>
    /// Whether to show sensitive values unmasked.
    /// </summary>
    public bool ShowSensitive { get; init; }

    /// <summary>
    /// Whether to show help information.
    /// </summary>
    public bool ShowHelp { get; init; }

    /// <summary>
    /// Whether to show version information.
    /// </summary>
    public bool ShowVersion { get; init; }

    /// <summary>
    /// Optional principal mapping file path.
    /// </summary>
    public string? PrincipalMappingFile { get; init; }

    /// <summary>
    /// Determines whether unchanged attribute values are included in the output.
    /// Related feature: docs/features/unchanged-values-cli-option/specification.md
    /// </summary>
    public bool ShowUnchangedValues { get; init; }

    /// <summary>
    /// Controls the rendering format for large attribute values.
    /// Related feature: docs/features/large-attribute-value-display/specification.md
    /// </summary>
    public LargeValueFormat LargeValueFormat { get; init; }
}

/// <summary>
/// Parses command-line arguments into CliOptions.
/// </summary>
public static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string? templatePath = null;
        string? principalMappingFile = null;
        string? reportTitle = null;
        var showSensitive = false;
        var showHelp = false;
        var showVersion = false;
        var showUnchangedValues = false;
        var largeValueFormat = LargeValueFormat.InlineDiff;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--help" or "-h":
                    showHelp = true;
                    break;
                case "--version" or "-v":
                    showVersion = true;
                    break;
                case "--show-sensitive":
                    showSensitive = true;
                    break;
                case "--output" or "-o":
                    if (i + 1 < args.Length)
                    {
                        outputFile = args[++i];
                    }
                    else
                    {
                        throw new CliParseException("--output requires a file path argument.");
                    }
                    break;
                case "--template" or "-t":
                    if (i + 1 < args.Length)
                    {
                        templatePath = args[++i];
                    }
                    else
                    {
                        throw new CliParseException("--template requires a file path argument.");
                    }
                    break;
                case "--report-title":
                    if (i + 1 < args.Length)
                    {
                        var titleCandidate = args[++i];
                        if (string.IsNullOrWhiteSpace(titleCandidate))
                        {
                            throw new CliParseException("--report-title cannot be empty.");
                        }

                        if (titleCandidate.Contains('\n', StringComparison.Ordinal) || titleCandidate.Contains('\r', StringComparison.Ordinal))
                        {
                            throw new CliParseException("--report-title cannot contain newlines.");
                        }

                        reportTitle = titleCandidate;
                    }
                    else
                    {
                        throw new CliParseException("--report-title requires a value.");
                    }
                    break;
                case "--principal-mapping" or "--principals" or "-p":
                    if (i + 1 < args.Length)
                    {
                        principalMappingFile = args[++i];
                    }
                    else
                    {
                        throw new CliParseException("--principal-mapping requires a file path argument.");
                    }
                    break;
                case "--show-unchanged-values":
                    showUnchangedValues = true;
                    break;
                case "--large-value-format":
                    if (i + 1 < args.Length)
                    {
                        var formatValue = args[++i];
                        largeValueFormat = ParseLargeValueFormat(formatValue);
                    }
                    else
                    {
                        throw new CliParseException("--large-value-format requires a format argument (inline-diff or standard-diff).");
                    }
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliParseException($"Unknown option: {arg}");
                    }
                    // Positional argument is the input file
                    inputFile = arg;
                    break;
            }
        }

        return new CliOptions
        {
            InputFile = inputFile,
            OutputFile = outputFile,
            TemplatePath = templatePath,
            ShowSensitive = showSensitive,
            ShowHelp = showHelp,
            ShowVersion = showVersion,
            PrincipalMappingFile = principalMappingFile,
            ShowUnchangedValues = showUnchangedValues,
            LargeValueFormat = largeValueFormat,
            ReportTitle = reportTitle
        };
    }

    private static LargeValueFormat ParseLargeValueFormat(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "inline-diff" => LargeValueFormat.InlineDiff,
            "standard-diff" => LargeValueFormat.StandardDiff,
            _ => throw new CliParseException("--large-value-format must be 'inline-diff' or 'standard-diff'.")
        };
    }
}

/// <summary>
/// Exception thrown when CLI parsing fails.
/// </summary>
public class CliParseException : ApplicationException
{
    public CliParseException(string message) : base(message)
    {
    }
}
