using System.Globalization;
using Oocx.TfPlan2Md.HtmlRenderer;

namespace Oocx.TfPlan2Md.HtmlRenderer.Tests;

/// <summary>
/// Validates command-line parsing and orchestration behaviors for the HTML renderer CLI.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
public sealed class CliParsingTests
{
    /// <summary>
    /// Verifies that when a valid input file and flavor are supplied the CLI derives the default output path and succeeds.
    /// Related acceptance: TC-01, TC-04.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RunAsync_WithValidArguments_CreatesDerivedOutput()
    {
        using var context = await CreateTempMarkdownAsync("Sample content", HtmlFlavor.GitHub);
        var app = CreateApp(out var outputWriter, out var errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath, "--flavor", "github" });

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(context.ExpectedDerivedOutputPath));
        Assert.True(string.IsNullOrEmpty(errorWriter.ToString()), "No errors expected for valid arguments.");
        Assert.Contains("github", Path.GetFileName(context.ExpectedDerivedOutputPath), StringComparison.Ordinal);
        Assert.NotEqual(0, new FileInfo(context.ExpectedDerivedOutputPath).Length);
    }

    /// <summary>
    /// Verifies that omitting the required flavor argument results in a failure exit code and a helpful error message.
    /// Related acceptance: TC-02.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RunAsync_MissingFlavor_ReturnsError()
    {
        using var context = await CreateTempMarkdownAsync("Content");
        var app = CreateApp(out _, out var errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath });

        Assert.Equal(1, exitCode);
        Assert.Contains("flavor", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an invalid flavor value produces an error and exit code 1.
    /// Related acceptance: TC-03.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RunAsync_InvalidFlavor_ReturnsError()
    {
        using var context = await CreateTempMarkdownAsync("Content");
        var app = CreateApp(out _, out var errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath, "--flavor", "invalid" });

        Assert.Equal(1, exitCode);
        Assert.Contains("github", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("azdo", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that referencing a missing input file yields an error and exit code 1.
    /// Related acceptance: TC-13.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RunAsync_MissingInputFile_ReturnsError()
    {
        var missingPath = Path.Combine(AppContext.BaseDirectory, "cli-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), "missing.md");
        var app = CreateApp(out _, out var errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", missingPath, "--flavor", "github" });

        Assert.Equal(1, exitCode);
        Assert.Contains("not found", errorWriter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that empty input content produces a warning but still exits successfully.
    /// Related acceptance: TC-14.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RunAsync_EmptyInput_WarnsAndSucceeds()
    {
        using var context = await CreateTempMarkdownAsync(string.Empty, HtmlFlavor.AzureDevOps);
        var app = CreateApp(out var outputWriter, out var errorWriter);

        var exitCode = await app.RunAsync(new[] { "--input", context.InputPath, "--flavor", "azdo" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Warning", outputWriter.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(context.ExpectedDerivedOutputPath));
        Assert.True(string.IsNullOrEmpty(errorWriter.ToString()), "Errors should not be present for an empty input warning.");
    }

    /// <summary>
    /// Creates the HTML renderer application instance with captured output streams.
    /// </summary>
    /// <param name="outputWriter">Receives the standard output capture instance.</param>
    /// <param name="errorWriter">Receives the standard error capture instance.</param>
    /// <returns>An initialized <see cref="HtmlRendererApp"/> instance.</returns>
    private static HtmlRendererApp CreateApp(out StringWriter outputWriter, out StringWriter errorWriter)
    {
        outputWriter = new StringWriter(CultureInfo.InvariantCulture);
        errorWriter = new StringWriter(CultureInfo.InvariantCulture);
        return new HtmlRendererApp(outputWriter, errorWriter);
    }

    /// <summary>
    /// Creates a temporary markdown file for testing and computes the expected derived output path.
    /// </summary>
    /// <param name="content">Markdown content to write to the file.</param>
    /// <param name="flavor">Rendering flavor that determines the derived output suffix.</param>
    /// <returns>A disposable context that tracks paths for cleanup.</returns>
    private static async Task<TempFileContext> CreateTempMarkdownAsync(string content, HtmlFlavor flavor = HtmlFlavor.GitHub)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "cli-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var inputPath = Path.Combine(root, "plan.md");
        await File.WriteAllTextAsync(inputPath, content).ConfigureAwait(false);
        var suffix = flavor == HtmlFlavor.AzureDevOps ? "azdo" : "github";
        var expectedOutput = Path.Combine(root, $"plan.{suffix}.html");
        return new TempFileContext(root, inputPath, expectedOutput);
    }

    /// <summary>
    /// Holds file paths for a temporary markdown file and ensures cleanup after use.
    /// </summary>
    private sealed class TempFileContext : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TempFileContext"/> class.
        /// </summary>
        /// <param name="rootDirectory">Root directory that contains the test files.</param>
        /// <param name="inputPath">Full path to the input markdown file.</param>
        /// <param name="expectedDerivedOutputPath">Path the CLI should generate when output is derived.</param>
        public TempFileContext(string rootDirectory, string inputPath, string expectedDerivedOutputPath)
        {
            RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            InputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
            ExpectedDerivedOutputPath = expectedDerivedOutputPath ?? throw new ArgumentNullException(nameof(expectedDerivedOutputPath));
        }

        /// <summary>
        /// Gets the root directory used for this test context.
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Gets the full path of the input markdown file.
        /// </summary>
        public string InputPath { get; }

        /// <summary>
        /// Gets the expected derived output file path the CLI should produce when no output argument is provided.
        /// </summary>
        public string ExpectedDerivedOutputPath { get; }

        /// <summary>
        /// Deletes the created test directory and all contained files.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, true);
            }
        }
    }
}
