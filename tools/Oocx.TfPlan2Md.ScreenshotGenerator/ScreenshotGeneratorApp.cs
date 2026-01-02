using Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator;

/// <summary>
/// Coordinates CLI parsing, validation, and screenshot capture.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class ScreenshotGeneratorApp
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly HtmlScreenshotCapturer _capturer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotGeneratorApp"/> class.
    /// </summary>
    /// <param name="output">Destination for informational output.</param>
    /// <param name="error">Destination for error output.</param>
    public ScreenshotGeneratorApp(TextWriter output, TextWriter error)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _capturer = new HtmlScreenshotCapturer();
    }

    /// <summary>
    /// Executes the application pipeline.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>Exit code (0 success, 1 failure).</returns>
    public async Task<int> RunAsync(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        CliOptions options;
        try
        {
            options = CliParser.Parse(args);
        }
        catch (CliParseException ex)
        {
            await _error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            await _error.WriteLineAsync("Use --help for usage information.").ConfigureAwait(false);
            return 1;
        }

        if (options.ShowHelp)
        {
            await _output.WriteLineAsync(HelpTextProvider.GetHelpText()).ConfigureAwait(false);
            return 0;
        }

        if (options.ShowVersion)
        {
            await _output.WriteLineAsync(VersionProvider.GetVersion()).ConfigureAwait(false);
            return 0;
        }

        try
        {
            CliValidator.Validate(options);
        }
        catch (CliValidationException ex)
        {
            await _error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }

        var outputPath = CaptureOptionsResolver.DetermineOutputPath(options.InputPath!, options.OutputPath);
        var format = CaptureOptionsResolver.ResolveFormat(options.Format, outputPath);
        var quality = CaptureOptionsResolver.ResolveQuality(format, options.Quality);

        var settings = new CaptureSettings(
            options.InputPath!,
            outputPath,
            options.Width,
            options.Height,
            options.FullPage,
            format,
            quality);

        try
        {
            await _capturer.CaptureAsync(settings, CancellationToken.None).ConfigureAwait(false);
            return 0;
        }
        catch (ScreenshotCaptureException ex)
        {
            await _error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
    }
}
