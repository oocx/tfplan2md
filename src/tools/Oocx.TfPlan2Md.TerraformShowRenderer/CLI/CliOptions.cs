namespace Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

/// <summary>
/// Represents parsed command-line options for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
internal sealed class CliOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliOptions"/> class.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    /// <param name="inputPath">Path to the Terraform plan JSON file.</param>
    /// <param name="outputPath">Optional output path for the rendered text; defaults to stdout when <see langword="null"/>.</param>
    /// <param name="noColor">Indicates whether ANSI color codes should be suppressed.</param>
    /// <param name="showHelp">Indicates whether help text should be displayed instead of running.</param>
    /// <param name="showVersion">Indicates whether version information should be displayed instead of running.</param>
    public CliOptions(string? inputPath, string? outputPath, bool noColor, bool showHelp, bool showVersion)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        NoColor = noColor;
        ShowHelp = showHelp;
        ShowVersion = showVersion;
    }

    /// <summary>
    /// Gets the user-supplied plan input path, when provided.
    /// </summary>
    public string? InputPath { get; }

    /// <summary>
    /// Gets the optional output path that overrides stdout when set.
    /// </summary>
    public string? OutputPath { get; }

    /// <summary>
    /// Gets a value indicating whether ANSI color codes should be suppressed.
    /// </summary>
    public bool NoColor { get; }

    /// <summary>
    /// Gets a value indicating whether help text should be shown without executing the tool.
    /// </summary>
    public bool ShowHelp { get; }

    /// <summary>
    /// Gets a value indicating whether version information should be shown without executing the tool.
    /// </summary>
    public bool ShowVersion { get; }
}
