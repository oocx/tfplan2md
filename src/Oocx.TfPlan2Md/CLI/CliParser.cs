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
        var showSensitive = false;
        var showHelp = false;
        var showVersion = false;

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
            PrincipalMappingFile = principalMappingFile
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
