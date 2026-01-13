using System.Globalization;
using System.Text;
using Oocx.TfPlan2Md.TerraformShowRenderer;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Exercises high-level CLI orchestration for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
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
    [Test]
    public async Task RunAsync_WithHelp_PrintsUsage()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(HelpArgs);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(error.ToString()), "Help mode should not write errors.");
    }

    /// <summary>
    /// Ensures version mode prints assembly informational version and exits successfully.
    /// Related acceptance: TC-03.
    /// </summary>
    [Test]
    public async Task RunAsync_WithVersion_PrintsVersion()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(VersionArgs);

        Assert.Equal(0, exitCode);
        Assert.Matches("\\d+\\.\\d+", output.ToString());
        Assert.True(string.IsNullOrEmpty(error.ToString()), "Version mode should not write errors.");
    }

    /// <summary>
    /// Ensures missing required input produces exit code 1 and a helpful message.
    /// Related acceptance: TC-12.
    /// </summary>
    [Test]
    public async Task RunAsync_MissingInput_ReturnsExitCode1()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(Array.Empty<string>());

        Assert.Equal(1, exitCode);
        Assert.Contains("--input", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(output.ToString()));
    }

    /// <summary>
    /// Ensures referencing a missing file produces exit code 2.
    /// Related acceptance: TC-10.
    /// </summary>
    [Test]
    public async Task RunAsync_InputFileMissing_ReturnsExitCode2()
    {
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);
        var missingPath = Path.Combine(AppContext.BaseDirectory, "terraform-show", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), "missing.json");

        var exitCode = await app.RunAsync(new[] { "--input", missingPath });

        Assert.Equal(2, exitCode);
        Assert.Contains("Input file not found", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(output.ToString()));
    }

    /// <summary>
    /// Ensures invalid JSON content produces exit code 3.
    /// Related acceptance: TC-05.
    /// </summary>
    [Test]
    public async Task RunAsync_InvalidJson_ReturnsExitCode3()
    {
        var invalidFile = await CreateTemporaryFileAsync("{not valid json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", invalidFile });

        Assert.Equal(3, exitCode);
        Assert.Contains("Failed to parse", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(output.ToString()));
    }

    /// <summary>
    /// Ensures unsupported format versions produce exit code 4.
    /// Related acceptance: TC-04.
    /// </summary>
    [Test]
    public async Task RunAsync_UnsupportedVersion_ReturnsExitCode4()
    {
        var unsupportedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "unsupported-version-plan.json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", unsupportedPath });

        Assert.Equal(4, exitCode);
        Assert.Contains("Unsupported plan format version", error.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrEmpty(output.ToString()));
    }

    /// <summary>
    /// Creates a temporary file containing the specified content.
    /// Related feature: docs/features/030-terraform-show-approximation/specification.md
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
