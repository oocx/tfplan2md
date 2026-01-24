using Oocx.TfPlan2Md.HtmlRenderer.CLI;
using Oocx.TfPlan2Md.HtmlRenderer.Rendering;

namespace Oocx.TfPlan2Md.HtmlRenderer;

/// <summary>
/// Coordinates CLI parsing, validation, and file IO for the HTML renderer tool.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal sealed class HtmlRendererApp
{
    /// <summary>
    /// Holds the output target for informational messages.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md.
    /// </summary>
    private readonly TextWriter _output;

    /// <summary>
    /// Holds the output target for error messages.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md.
    /// </summary>
    private readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlRendererApp"/> class.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md.
    /// </summary>
    /// <param name="output">Destination for informational messages.</param>
    /// <param name="error">Destination for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when a writer is <see langword="null"/>.</exception>
    public HtmlRendererApp(TextWriter output, TextWriter error)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Parses CLI arguments, validates inputs, and writes rendered output files.
    /// Related feature: docs/features/027-markdown-html-rendering/specification.md.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>A <see cref="Task"/> containing the process exit code.</returns>
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

        if (string.IsNullOrWhiteSpace(options.InputPath))
        {
            await _error.WriteLineAsync("Error: --input is required.").ConfigureAwait(false);
            return 1;
        }

        if (options.Flavor is null)
        {
            await _error.WriteLineAsync("Error: --flavor is required and must be either 'github' or 'azdo'.").ConfigureAwait(false);
            return 1;
        }

        if (!File.Exists(options.InputPath))
        {
            await _error.WriteLineAsync($"Error: Input file not found: {options.InputPath}").ConfigureAwait(false);
            return 1;
        }

        var markdown = await File.ReadAllTextAsync(options.InputPath).ConfigureAwait(false);
        if (string.IsNullOrEmpty(markdown))
        {
            await _output.WriteLineAsync("Warning: Input file is empty. Output will be empty as well.").ConfigureAwait(false);
        }

        var derivedOutputPath = DetermineOutputPath(options.InputPath, options.Flavor.Value, options.OutputPath);
        await EnsureOutputDirectoryExistsAsync(derivedOutputPath).ConfigureAwait(false);

        var pipelineFactory = new MarkdigPipelineFactory();
        var renderer = new MarkdownToHtmlRenderer(pipelineFactory);
        var html = renderer.RenderFragment(markdown, options.Flavor.Value);

        if (!string.IsNullOrWhiteSpace(options.TemplatePath))
        {
            if (!File.Exists(options.TemplatePath))
            {
                await _error.WriteLineAsync($"Error: Template file not found: {options.TemplatePath}").ConfigureAwait(false);
                return 1;
            }

            var template = await File.ReadAllTextAsync(options.TemplatePath!).ConfigureAwait(false);
            var applier = new WrapperTemplateApplier();

            try
            {
                html = applier.Apply(template, html);
            }
            catch (InvalidOperationException ex)
            {
                await _error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                return 1;
            }
        }

        await File.WriteAllTextAsync(derivedOutputPath, html).ConfigureAwait(false);

        return 0;
    }

    /// <summary>
    /// Ensures the output directory exists prior to file write operations.
    /// </summary>
    /// <param name="outputPath">Path for the output file.</param>
    /// <returns>A completed <see cref="Task"/> when the directory is ready.</returns>
    private static Task EnsureOutputDirectoryExistsAsync(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Computes the output path based on the provided options and flavor.
    /// </summary>
    /// <param name="inputPath">Input markdown file path.</param>
    /// <param name="flavor">Selected HTML flavor.</param>
    /// <param name="explicitOutputPath">Optional explicit output path.</param>
    /// <returns>The resolved output file path.</returns>
    private static string DetermineOutputPath(string inputPath, HtmlFlavor flavor, string? explicitOutputPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitOutputPath))
        {
            return explicitOutputPath;
        }

        var directory = Path.GetDirectoryName(inputPath);
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var suffix = flavor == HtmlFlavor.AzureDevOps ? "azdo" : "github";
        var derivedFile = $"{fileName}.{suffix}.html";

        return string.IsNullOrWhiteSpace(directory)
            ? derivedFile
            : Path.Combine(directory, derivedFile);
    }
}
