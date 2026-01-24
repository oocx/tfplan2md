using System.Globalization;
using System.Text;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.TerraformShowRenderer.CLI;
using Oocx.TfPlan2Md.TerraformShowRenderer.Rendering;

namespace Oocx.TfPlan2Md.TerraformShowRenderer;

/// <summary>
/// Coordinates CLI parsing, validation, and initial input handling for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
internal sealed class TerraformShowRendererApp
{
    /// <summary>
    /// Destination for standard output messages.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    private readonly TextWriter _output;

    /// <summary>
    /// Destination for error output messages.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    private readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerraformShowRendererApp"/> class.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    /// <param name="output">Writer used for informational output.</param>
    /// <param name="error">Writer used for error output.</param>
    /// <exception cref="ArgumentNullException">Thrown when a writer is <see langword="null"/>.</exception>
    public TerraformShowRendererApp(TextWriter output, TextWriter error)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Parses arguments, validates inputs, and orchestrates plan loading.
    /// Related acceptance: TC-01 through TC-05, TC-10 through TC-12.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    /// <returns>An exit code aligned with the feature specification.</returns>
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
            await _error.WriteLineAsync(FormattableString.Invariant($"Error: {ex.Message}")).ConfigureAwait(false);
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

        if (!File.Exists(options.InputPath))
        {
            await _error.WriteLineAsync(FormattableString.Invariant($"Error: Input file not found: {options.InputPath}")).ConfigureAwait(false);
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(options.InputPath).ConfigureAwait(false);
            await using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                await _error.WriteLineAsync(FormattableString.Invariant($"Error: Failed to parse JSON plan file: {ex.Message}")).ConfigureAwait(false);
                return 3;
            }

            using (document)
            {
                var parser = new TerraformPlanParser();
                var plan = await parser.ParseAsync(inputStream).ConfigureAwait(false);

                if (!IsSupportedFormat(plan.FormatVersion))
                {
                    await _error.WriteLineAsync(FormattableString.Invariant($"Error: Unsupported plan format version: {plan.FormatVersion}. Expected 1.2 or later.")).ConfigureAwait(false);
                    return 4;
                }

                document.RootElement.TryGetProperty("output_changes", out var outputChanges);

                var renderer = new Rendering.TerraformShowRenderer();
                var content = renderer.Render(plan, options.NoColor, outputChanges);

                if (string.IsNullOrWhiteSpace(options.OutputPath))
                {
                    await _output.WriteAsync(content).ConfigureAwait(false);
                }
                else
                {
                    EnsureOutputDirectoryExists(options.OutputPath);
                    try
                    {
                        await File.WriteAllTextAsync(options.OutputPath, content).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        await _error.WriteLineAsync(FormattableString.Invariant($"Error: Failed to write output file: {ex.Message}")).ConfigureAwait(false);
                        return 2;
                    }
                }

                return 0;
            }
        }
        catch (TerraformPlanParseException ex)
        {
            await _error.WriteLineAsync(FormattableString.Invariant($"Error: Failed to parse JSON plan file: {ex.Message}")).ConfigureAwait(false);
            return 3;
        }
        catch (IOException ex)
        {
            await _error.WriteLineAsync(FormattableString.Invariant($"Error: Failed to read input file: {ex.Message}")).ConfigureAwait(false);
            return 2;
        }
    }

    /// <summary>
    /// Determines whether the supplied format version meets the minimum supported requirement (1.2).
    /// </summary>
    /// <param name="formatVersion">The format version string from the plan.</param>
    /// <returns><see langword="true"/> when the version is supported; otherwise <see langword="false"/>.</returns>
    private static bool IsSupportedFormat(string? formatVersion)
    {
        if (string.IsNullOrWhiteSpace(formatVersion))
        {
            return false;
        }

        if (!Version.TryParse(formatVersion, out var parsedVersion))
        {
            return false;
        }

        return parsedVersion >= new Version(1, 2);
    }

    /// <summary>
    /// Ensures the directory for the output path exists.
    /// </summary>
    /// <param name="outputPath">Target output file path.</param>
    private static void EnsureOutputDirectoryExists(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
    }
}
