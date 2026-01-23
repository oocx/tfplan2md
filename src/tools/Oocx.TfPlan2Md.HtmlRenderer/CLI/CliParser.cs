using System.Globalization;

namespace Oocx.TfPlan2Md.HtmlRenderer.CLI;

/// <summary>
/// Parses command-line arguments into a <see cref="CliOptions"/> instance.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal static class CliParser
{
    /// <summary>
    /// Parses the provided arguments into structured CLI options.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>A populated <see cref="CliOptions"/> instance.</returns>
    /// <exception cref="CliParseException">Thrown when arguments are invalid.</exception>
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? inputPath = null;
        string? outputPath = null;
        string? templatePath = null;
        HtmlFlavor? flavor = null;
        var showHelp = false;
        var showVersion = false;

        for (var i = 0; i < args.Count; i++)
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
                case "--input" or "-i":
                    inputPath = ReadNextValue(args, ref i, "--input");
                    break;
                case "--output" or "-o":
                    outputPath = ReadNextValue(args, ref i, "--output");
                    break;
                case "--template" or "-t":
                    templatePath = ReadNextValue(args, ref i, "--template");
                    break;
                case "--flavor" or "-f":
                    var flavorValue = ReadNextValue(args, ref i, "--flavor");
                    flavor = ParseFlavor(flavorValue);
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliParseException($"Unknown option: {arg}");
                    }

                    if (inputPath is not null)
                    {
                        throw new CliParseException("Only one input file can be specified.");
                    }

                    inputPath = arg;
                    break;
            }
        }

        if (!showHelp && !showVersion)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new CliParseException("--input is required.");
            }

            if (flavor is null)
            {
                throw new CliParseException("--flavor is required and must be either 'github' or 'azdo'.");
            }
        }

        return new CliOptions(inputPath, outputPath, templatePath, flavor, showHelp, showVersion);
    }

    /// <summary>
    /// Reads the next argument value, throwing when missing.
    /// </summary>
    /// <param name="args">Full argument list.</param>
    /// <param name="index">Current index, advanced when a value is consumed.</param>
    /// <param name="optionName">Current option name for error reporting.</param>
    /// <returns>The string value following the current option.</returns>
    private static string ReadNextValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
        {
            throw new CliParseException(FormattableString.Invariant($"{optionName} requires a value."));
        }

        index++;
        return args[index];
    }

    /// <summary>
    /// Converts a flavor string into the corresponding <see cref="HtmlFlavor"/> value.
    /// </summary>
    /// <param name="value">User-supplied flavor string.</param>
    /// <returns>The parsed <see cref="HtmlFlavor"/> value.</returns>
    /// <exception cref="CliParseException">Thrown when the flavor is not recognized.</exception>
    private static HtmlFlavor ParseFlavor(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "github" => HtmlFlavor.GitHub,
            "azdo" or "azuredevops" or "azure-devops" => HtmlFlavor.AzureDevOps,
            _ => throw new CliParseException("--flavor must be 'github' or 'azdo'.")
        };
    }
}
