using System.Globalization;

namespace Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

/// <summary>
/// Parses command-line arguments for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
internal static class CliParser
{
    /// <summary>
    /// Converts raw CLI arguments into structured options.
    /// </summary>
    /// <param name="args">The raw arguments supplied to the process.</param>
    /// <returns>A populated <see cref="CliOptions"/> instance.</returns>
    /// <exception cref="CliParseException">Thrown when arguments are missing or invalid.</exception>
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? inputPath = null;
        string? outputPath = null;
        var noColor = false;
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
                case "--no-color":
                    noColor = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new CliParseException(FormattableString.Invariant($"Unknown option: {arg}"));
                    }

                    if (inputPath is not null)
                    {
                        throw new CliParseException("Only one input file can be specified.");
                    }

                    inputPath = arg;
                    break;
            }
        }

        if (!showHelp && !showVersion && string.IsNullOrWhiteSpace(inputPath))
        {
            throw new CliParseException("--input is required.");
        }

        return new CliOptions(inputPath, outputPath, noColor, showHelp, showVersion);
    }

    /// <summary>
    /// Reads the next argument value for an option, throwing when missing.
    /// </summary>
    /// <param name="args">Full argument list.</param>
    /// <param name="index">Current argument index.</param>
    /// <param name="optionName">Option name for error reporting.</param>
    /// <returns>The value following the current option.</returns>
    /// <exception cref="CliParseException">Thrown when the next value is missing.</exception>
    private static string ReadNextValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
        {
            throw new CliParseException(FormattableString.Invariant($"{optionName} requires a value."));
        }

        index++;
        return args[index];
    }
}
