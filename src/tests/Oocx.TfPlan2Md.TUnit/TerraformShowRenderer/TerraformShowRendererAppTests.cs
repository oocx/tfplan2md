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
    /// Ensures invalid format versions produce exit code 4.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_InvalidFormatVersion_ReturnsExitCode4()
    {
        var invalidPath = await CreateTemporaryPlanAsync("not-a-version");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", invalidPath });

        await Assert.That(exitCode).IsEqualTo(4);
        await Assert.That(error.ToString()).Contains("Unsupported plan format version");
        await Assert.That(string.IsNullOrEmpty(output.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures successful rendering writes to stdout when no output file is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_ValidPlan_WritesToStdOut()
    {
        var inputPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TerraformShow", "plan1.json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", inputPath });

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(string.IsNullOrEmpty(error.ToString())).IsTrue();
        await Assert.That(output.ToString()).Contains("Terraform will perform the following actions");
    }

    /// <summary>
    /// Ensures successful rendering writes to a file when an output path is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_ValidPlan_WritesOutputFile()
    {
        var inputPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TerraformShow", "plan1.json");
        var outputPath = CreateTemporaryOutputPath("terraform-show-output", "rendered.txt");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", inputPath, "--output", outputPath });

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(outputPath)).IsTrue();
        await Assert.That(string.IsNullOrEmpty(error.ToString())).IsTrue();
    }

    /// <summary>
    /// Ensures no-color mode omits ANSI escape codes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task RunAsync_NoColor_OmitsAnsiSequences()
    {
        var inputPath = Path.Combine(AppContext.BaseDirectory, "TestData", "TerraformShow", "plan1.json");
        using var output = new StringWriter(new StringBuilder());
        using var error = new StringWriter(new StringBuilder());
        var app = new TerraformShowRendererApp(output, error);

        var exitCode = await app.RunAsync(new[] { "--input", inputPath, "--no-color" });

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).DoesNotContain("\u001b[");
        await Assert.That(string.IsNullOrEmpty(error.ToString())).IsTrue();
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

    /// <summary>
    /// Creates a temporary plan file with a custom format version.
    /// </summary>
    /// <param name="formatVersion">Format version to embed.</param>
    /// <returns>Path to the plan file.</returns>
    private static async Task<string> CreateTemporaryPlanAsync(string formatVersion)
    {
        var content = $$"""
            {
              "format_version": "{{formatVersion}}",
              "terraform_version": "1.6.0",
              "resource_changes": []
            }
            """;

        return await CreateTemporaryFileAsync(content).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a temporary output path under the test data folder.
    /// </summary>
    /// <param name="folderName">Folder name to create.</param>
    /// <param name="fileName">Output file name.</param>
    /// <returns>Absolute path to the output file.</returns>
    private static string CreateTemporaryOutputPath(string folderName, string fileName)
    {
        var root = CreateTemporaryDirectory(folderName);
        return Path.Combine(root, fileName);
    }

    /// <summary>
    /// Creates a temporary directory under the test data folder.
    /// </summary>
    /// <param name="folderName">Folder name to create.</param>
    /// <returns>Absolute path to the directory.</returns>
    private static string CreateTemporaryDirectory(string folderName)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "terraform-show", folderName, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(root);
        return root;
    }
}
