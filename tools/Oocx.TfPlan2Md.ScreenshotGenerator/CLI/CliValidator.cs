namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Performs semantic validation of parsed CLI options.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal static class CliValidator
{
    /// <summary>
    /// Validates a parsed <see cref="CliOptions"/> instance, throwing when validation fails.
    /// </summary>
    /// <param name="options">Options to validate.</param>
    /// <exception cref="CliValidationException">Thrown when validation detects invalid input.</exception>
    public static void Validate(CliOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ShowHelp || options.ShowVersion)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            throw new CliValidationException("--input is required.");
        }

        if (!File.Exists(options.InputPath))
        {
            throw new CliValidationException(FormattableString.Invariant($"Input file not found: {options.InputPath}"));
        }

        if (options.Width <= 0)
        {
            throw new CliValidationException("Viewport width must be a positive integer.");
        }

        if (options.Height <= 0)
        {
            throw new CliValidationException("Viewport height must be a positive integer.");
        }

        if (options.Quality is int quality && (quality < 0 || quality > 100))
        {
            throw new CliValidationException("Quality must be between 0 and 100.");
        }
    }
}
