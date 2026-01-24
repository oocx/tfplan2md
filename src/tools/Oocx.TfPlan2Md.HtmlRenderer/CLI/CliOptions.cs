namespace Oocx.TfPlan2Md.HtmlRenderer.CLI;

/// <summary>
/// Represents parsed command-line options for the HTML renderer.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal sealed class CliOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliOptions"/> class.
    /// </summary>
    /// <param name="inputPath">Markdown input file path.</param>
    /// <param name="outputPath">Optional output path for the rendered HTML.</param>
    /// <param name="templatePath">Optional template path for wrapper rendering.</param>
    /// <param name="flavor">Rendering flavor to approximate platform output.</param>
    /// <param name="showHelp">Indicates whether help text should be displayed.</param>
    /// <param name="showVersion">Indicates whether version information should be displayed.</param>
    public CliOptions(string? inputPath, string? outputPath, string? templatePath, HtmlFlavor? flavor, bool showHelp, bool showVersion)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        TemplatePath = templatePath;
        Flavor = flavor;
        ShowHelp = showHelp;
        ShowVersion = showVersion;
    }

    /// <summary>
    /// Gets the markdown input file path provided by the caller.
    /// </summary>
    public string? InputPath { get; }

    /// <summary>
    /// Gets the optional output path; when null a path is derived from the input.
    /// </summary>
    public string? OutputPath { get; }

    /// <summary>
    /// Gets the optional wrapper template path supplied by the user.
    /// </summary>
    public string? TemplatePath { get; }

    /// <summary>
    /// Gets the requested rendering flavor, or <see langword="null"/> when not specified.
    /// </summary>
    public HtmlFlavor? Flavor { get; }

    /// <summary>
    /// Gets a value indicating whether help text should be rendered instead of executing the tool.
    /// </summary>
    public bool ShowHelp { get; }

    /// <summary>
    /// Gets a value indicating whether version information should be rendered instead of executing the tool.
    /// </summary>
    public bool ShowVersion { get; }
}
