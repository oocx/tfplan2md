namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Represents parsed command-line options for the screenshot generator.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class CliOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliOptions"/> class.
    /// </summary>
    /// <param name="inputPath">Input HTML file path.</param>
    /// <param name="outputPath">Optional output image path.</param>
    /// <param name="width">Viewport width in pixels.</param>
    /// <param name="height">Viewport height in pixels.</param>
    /// <param name="fullPage">Value indicating whether to capture the full page.</param>
    /// <param name="format">Optional image format selection.</param>
    /// <param name="quality">Optional quality setting for lossy formats.</param>
    /// <param name="showHelp">Indicates whether help text should be displayed.</param>
    /// <param name="showVersion">Indicates whether version information should be displayed.</param>
    public CliOptions(string? inputPath, string? outputPath, int width, int height, bool fullPage, ScreenshotFormat? format, int? quality, bool showHelp, bool showVersion)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        Width = width;
        Height = height;
        FullPage = fullPage;
        Format = format;
        Quality = quality;
        ShowHelp = showHelp;
        ShowVersion = showVersion;
    }

    /// <summary>
    /// Gets the input HTML file path supplied by the caller.
    /// </summary>
    public string? InputPath { get; }

    /// <summary>
    /// Gets the optional output image path.
    /// </summary>
    public string? OutputPath { get; }

    /// <summary>
    /// Gets the viewport width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the viewport height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets a value indicating whether full-page capture is requested.
    /// </summary>
    public bool FullPage { get; }

    /// <summary>
    /// Gets the requested output format when specified.
    /// </summary>
    public ScreenshotFormat? Format { get; }

    /// <summary>
    /// Gets the quality value when specified for lossy formats.
    /// </summary>
    public int? Quality { get; }

    /// <summary>
    /// Gets a value indicating whether help text should be shown instead of executing.
    /// </summary>
    public bool ShowHelp { get; }

    /// <summary>
    /// Gets a value indicating whether version information should be shown instead of executing.
    /// </summary>
    public bool ShowVersion { get; }
}
