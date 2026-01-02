using System.Globalization;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Parses command-line arguments into structured options for the screenshot generator.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal static class CliParser
{
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;
    private const int DefaultJpegQuality = 90;

    /// <summary>
    /// Parses raw command-line arguments into a <see cref="CliOptions"/> instance.
    /// </summary>
    /// <param name="args">The arguments supplied to the application.</param>
    /// <returns>Structured CLI options.</returns>
    /// <exception cref="CliParseException">Thrown when arguments are syntactically invalid.</exception>
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? inputPath = null;
        string? outputPath = null;
        int? width = null;
        int? height = null;
        var fullPage = false;
        ScreenshotFormat? format = null;
        int? quality = null;
        var showHelp = false;
        var showVersion = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];

            switch (arg)
            {
                case "--help":
                    showHelp = true;
                    break;
                case "--version":
                    showVersion = true;
                    break;
                case "--input" or "-i":
                    inputPath = ReadNextValue(args, ref index, "--input");
                    break;
                case "--output" or "-o":
                    outputPath = ReadNextValue(args, ref index, "--output");
                    break;
                case "--width" or "-w":
                    width = ParseIntegerOption(ReadNextValue(args, ref index, "--width"), "width");
                    break;
                case "--height" or "-h":
                    height = ParseIntegerOption(ReadNextValue(args, ref index, "--height"), "height");
                    break;
                case "--full-page" or "-f":
                    fullPage = true;
                    break;
                case "--format":
                    format = ParseFormat(ReadNextValue(args, ref index, "--format"));
                    break;
                case "--quality" or "-q":
                    quality = ParseIntegerOption(ReadNextValue(args, ref index, "--quality"), "quality");
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

        width ??= DefaultWidth;
        height ??= DefaultHeight;
        quality ??= ResolveDefaultQuality(format);

        return new CliOptions(inputPath, outputPath, width.Value, height.Value, fullPage, format, quality, showHelp, showVersion);
    }

    /// <summary>
    /// Reads the next argument value, ensuring it is present.
    /// </summary>
    /// <param name="args">All supplied arguments.</param>
    /// <param name="index">Current index, which is advanced when a value is consumed.</param>
    /// <param name="optionName">Current option name for error messaging.</param>
    /// <returns>The value following the current option.</returns>
    /// <exception cref="CliParseException">Thrown when the value is missing.</exception>
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
    /// Parses an integer option value using invariant culture.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="optionName">Option name used for error messaging.</param>
    /// <returns>The parsed integer.</returns>
    /// <exception cref="CliParseException">Thrown when parsing fails.</exception>
    private static int ParseIntegerOption(string value, string optionName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new CliParseException(FormattableString.Invariant($"The {optionName} option requires an integer value."));
        }

        return parsed;
    }

    /// <summary>
    /// Parses the requested format value into an enum.
    /// </summary>
    /// <param name="value">User-provided format string.</param>
    /// <returns>The parsed <see cref="ScreenshotFormat"/>.</returns>
    /// <exception cref="CliParseException">Thrown when the format is unsupported.</exception>
    private static ScreenshotFormat ParseFormat(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "png" => ScreenshotFormat.Png,
            "jpg" or "jpeg" => ScreenshotFormat.Jpeg,
            _ => throw new CliParseException("The --format option must be one of: png, jpeg."),
        };
    }

    /// <summary>
    /// Resolves default quality for lossy formats when not provided explicitly.
    /// </summary>
    /// <param name="format">The optional screenshot format.</param>
    /// <returns>A default quality value when applicable.</returns>
    private static int? ResolveDefaultQuality(ScreenshotFormat? format)
    {
        return format switch
        {
            ScreenshotFormat.Jpeg => DefaultJpegQuality,
            _ => null,
        };
    }
}
