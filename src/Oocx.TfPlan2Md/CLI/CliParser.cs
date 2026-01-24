using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.CLI;

/// <summary>
/// Represents the parsed command-line options.
/// </summary>
internal record CliOptions
{
    /// <summary>
    /// Gets the input file path. If null, read from stdin.
    /// </summary>
    public string? InputFile { get; init; }

    /// <summary>
    /// Gets the output file path. If null, write to stdout.
    /// </summary>
    public string? OutputFile { get; init; }

    /// <summary>
    /// Gets the custom template file path.
    /// </summary>
    public string? TemplatePath { get; init; }

    /// <summary>
    /// Gets the optional custom report title provided via the CLI.
    /// Related feature: docs/features/020-custom-report-title/specification.md.
    /// </summary>
    /// <value>
    /// Custom level-1 heading text for the generated report. When null, templates fall back to their defaults.
    /// </value>
    public string? ReportTitle { get; init; }

    /// <summary>
    /// Gets a value indicating whether to show sensitive values unmasked.
    /// </summary>
    public bool ShowSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether to show help information.
    /// </summary>
    public bool ShowHelp { get; init; }

    /// <summary>
    /// Gets a value indicating whether to show version information.
    /// </summary>
    public bool ShowVersion { get; init; }

    /// <summary>
    /// Gets the optional principal mapping file path.
    /// </summary>
    public string? PrincipalMappingFile { get; init; }

    /// <summary>
    /// Gets a value indicating whether unchanged attribute values are included in the output.
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// </summary>
    public bool ShowUnchangedValues { get; init; }

    /// <summary>
    /// Gets a value indicating whether tfplan2md metadata should be hidden from the report header.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public bool HideMetadata { get; init; }

    /// <summary>
    /// Gets the target platform for markdown rendering.
    /// Related feature: docs/features/047-provider-code-separation/specification.md.
    /// </summary>
    public RenderTarget RenderTarget { get; init; }

    /// <summary>
    /// Gets a value indicating whether debug diagnostic information should be appended to the report.
    /// Related feature: docs/features/038-debug-output/specification.md.
    /// </summary>
    /// <remarks>
    /// When enabled, debug output includes:
    /// <list type="bullet">
    /// <item><description>Principal mapping diagnostics (load status, type counts, failed resolutions)</description></item>
    /// <item><description>Template resolution decisions (which template was used for each resource type)</description></item>
    /// </list>
    /// The debug information is appended to the markdown report as a separate "Debug Information" section.
    /// </remarks>
    public bool Debug { get; init; }
}

/// <summary>
/// Parses command-line arguments into CliOptions.
/// </summary>
internal static class CliParser
{
    /// <summary>
    /// Parses the command-line arguments into a CliOptions object.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>A CliOptions object representing the parsed arguments.</returns>
    /// <exception cref="CliParseException">Thrown when argument parsing fails.</exception>
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
        var hideMetadata = false;
        var renderTarget = RenderTarget.AzureDevOps; // Default to Azure DevOps (inline-diff)
        var debug = false;

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
                case "--hide-metadata":
                    hideMetadata = true;
                    break;
                case "--render-target":
                    if (i + 1 < args.Length)
                    {
                        var targetValue = args[++i];
                        renderTarget = ParseRenderTarget(targetValue);
                    }
                    else
                    {
                        throw new CliParseException("--render-target requires a value (github or azuredevops).");
                    }
                    break;
                case "--large-value-format":
                    throw new CliParseException("--large-value-format is deprecated. Use --render-target instead (github or azuredevops).");
                case "--debug":
                    debug = true;
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
            HideMetadata = hideMetadata,
            RenderTarget = renderTarget,
            ReportTitle = reportTitle,
            Debug = debug
        };
    }

    /// <summary>
    /// Parses the render target value from CLI input.
    /// </summary>
    /// <param name="value">The render target string (case-insensitive).</param>
    /// <returns>The parsed <see cref="RenderTarget"/> enum value.</returns>
    /// <exception cref="CliParseException">Thrown when the value is not recognized.</exception>
    private static RenderTarget ParseRenderTarget(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "github" => RenderTarget.GitHub,
            "azuredevops" or "azdo" => RenderTarget.AzureDevOps,
            _ => throw new CliParseException("--render-target must be 'github' or 'azuredevops' (alias: azdo).")
        };
    }
}

/// <summary>
/// Exception thrown when CLI parsing fails.
/// </summary>
public class CliParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class.
    /// </summary>
    public CliParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CliParseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CliParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
