using System.Globalization;
using System.Text;
using Oocx.TfPlan2Md.TerraformShowRenderer;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Exercises high-level CLI orchestration for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
public sealed class TerraformShowRendererAppTests
{
    /// <summary>
    /// Reusable help arguments to satisfy CA1861.
    /// </summary>
    private static readonly string[] HelpArgs = ["--help"];

    /// <summary>
    /// Reusable version arguments to satisfy CA1861.
    /// </summary>
    private static readonly string[] VersionArgs = ["--version"];

    /// <summary>
    /// Ensures help mode prints usage content and exits successfully.
    /// Related acceptance: TC-02.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_WithHelp_PrintsUsage()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(HelpArgs);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("Usage");
        await Assert.That(string.IsNullOrEmpty(error.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures version mode prints assembly informational version and exits successfully.
    /// Related acceptance: TC-03.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_WithVersion_PrintsVersion()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(VersionArgs);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).Matches("\\d+\\.\\d+");
        await Assert.That(string.IsNullOrEmpty(error.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures missing required input produces exit code 1 and a helpful message.
    /// Related acceptance: TC-12.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_MissingInput_ReturnsExitCode1()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(Array.Empty<string>());

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(error.ToString()).Contains("--input");
        await Assert.That(string.IsNullOrEmpty(output.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures referencing a missing file produces exit code 2.
    /// Related acceptance: TC-10.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_InputFileMissing_ReturnsExitCode2()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);
        var missingPath = Path.Combine(AppContext.BaseDirectory, "terraform-show", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), "missing.json");

        var exitCode = await app.RunAsync(new[] { "--input", missingPath });

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(error.ToString()).Contains("Input file not found");
        await Assert.That(string.IsNullOrEmpty(output.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures invalid JSON content produces exit code 3.
    /// Related acceptance: TC-05.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_InvalidJson_ReturnsExitCode3()
    {
        var invalidFile = await CreateTemporaryFileAsync("{not valid json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", invalidFile });

        await Assert.That(exitCode).IsEqualTo(3);
        await Assert.That(error.ToString()).Contains("Failed to parse");
        await Assert.That(string.IsNullOrEmpty(output.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures unsupported format versions produce exit code 4.
    /// Related acceptance: TC-04.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_UnsupportedVersion_ReturnsExitCode4()
    {
        var unsupportedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "unsupported-version-plan.json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", unsupportedPath });

        await Assert.That(exitCode).IsEqualTo(4);
        await Assert.That(error.ToString()).Contains("Unsupported plan format version");
        await Assert.That(string.IsNullOrEmpty(output.ToString())).IsTrue();
    }

    /// <summary>
    /// Creates a temporary file containing the specified content.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md.
    /// </summary>
    /// <param name="content">File content to write.</param>
    /// <returns>Path to the created file.</returns>
    private static async Task<string> CreateTemporaryFileAsync(string content)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "terraform-show", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "plan.json");
        await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
        return path;
    }
}
