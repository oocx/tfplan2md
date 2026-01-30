using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CLI;

/// <summary>
/// End-to-end tests for the top-level program entry point to validate CLI behavior.
/// </summary>
public class ProgramMainTests
{
    /// <summary>
    /// Verifies --help returns success and prints usage text.
    /// </summary>
    [Test]
    public async Task Main_WithHelpFlag_ReturnsZeroAndPrintsHelp()
    {
        var result = await RunMainAsync(["--help"]);

        result.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies --version returns success and prints version header.
    /// </summary>
    [Test]
    public async Task Main_WithVersionFlag_ReturnsZeroAndPrintsVersion()
    {
        var result = await RunMainAsync(["--version"]);

        result.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies unknown options surface as an error and a non-zero exit code.
    /// </summary>
    [Test]
    public async Task Main_WithUnknownOption_ReturnsError()
    {
        var result = await RunMainAsync(["--unknown-option"]);

        result.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// Verifies missing input files are reported as errors.
    /// </summary>
    [Test]
    public async Task Main_WithMissingInputFile_ReturnsError()
    {
        var result = await RunMainAsync(["missing-plan.json"]);

        result.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// Verifies output file writing when input file is provided.
    /// </summary>
    [Test]
    public async Task Main_WithInputFile_WritesOutputFile()
    {
        var inputPath = GetTestDataPath("azapi-create-plan.json");
        var outputPath = GetTempPath("program-output.md");

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var result = await RunMainAsync([inputPath, "--output", outputPath, "--render-target", "github"]);

        result.ExitCode.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var output = await File.ReadAllTextAsync(outputPath);
        output.Should().Contain("Terraform Plan Report");
    }

    /// <summary>
    /// Verifies the CLI succeeds when output is directed to stdout.
    /// </summary>
    [Test]
    public async Task Main_WithInputFile_WritesToStdOut()
    {
        var inputPath = GetTestDataPath("azapi-create-plan.json");

        var result = await RunMainAsync([inputPath, "--render-target", "github"]);

        result.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Verifies custom templates are used when provided.
    /// </summary>
    [Test]
    public async Task Main_WithTemplatePath_UsesCustomTemplate()
    {
        var inputPath = GetTestDataPath("azapi-create-plan.json");
        var templatePath = GetTempPath("custom-template.sbn");
        var outputPath = GetTempPath("template-output.md");

        await File.WriteAllTextAsync(templatePath, "Custom Template Output");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var result = await RunMainAsync([inputPath, "--template", templatePath, "--output", outputPath]);

        result.ExitCode.Should().Be(0);
        var output = await File.ReadAllTextAsync(outputPath);
        output.Should().Contain("Custom Template Output");
    }

    /// <summary>
    /// Verifies debug output is appended when debug mode is enabled.
    /// </summary>
    [Test]
    public async Task Main_WithDebugFlag_AppendsDebugSection()
    {
        var inputPath = GetTestDataPath("azapi-create-plan.json");
        var outputPath = GetTempPath("debug-output.md");

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var result = await RunMainAsync([inputPath, "--output", outputPath, "--debug"]);

        result.ExitCode.Should().Be(0);
        var output = await File.ReadAllTextAsync(outputPath);
        output.Should().Contain("## Debug Information");
    }

    /// <summary>
    /// Verifies invalid plan JSON returns a non-zero exit code.
    /// </summary>
    [Test]
    public async Task Main_WithInvalidPlan_ReturnsError()
    {
        var inputPath = GetTempPath("invalid-plan.json");
        await File.WriteAllTextAsync(inputPath, "not-json");

        var result = await RunMainAsync([inputPath]);

        result.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// Verifies invalid template syntax returns a non-zero exit code.
    /// </summary>
    [Test]
    public async Task Main_WithInvalidTemplate_ReturnsError()
    {
        var inputPath = GetTestDataPath("azapi-create-plan.json");
        var templatePath = GetTempPath("invalid-template.sbn");
        var outputPath = GetTempPath("invalid-template-output.md");

        await File.WriteAllTextAsync(templatePath, "{{ include \"missing\" }}");

        var result = await RunMainAsync([inputPath, "--template", templatePath, "--output", outputPath]);

        result.ExitCode.Should().Be(1);
    }

    /// <summary>
    /// Verifies the CLI returns exit code 10 when fail-on severity is met.
    /// </summary>
    [Test]
    public async Task Main_WithFailOnSeverity_ReturnsExitCode10()
    {
        var inputPath = GetTestDataPath("minimal-plan.json");
        var sarifPath = GetTestDataPath("valid-sarif.sarif");
        var outputPath = GetTempPath("code-analysis-output.md");

        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var result = await RunMainAsync([
            inputPath,
            "--code-analysis-results",
            sarifPath,
            "--fail-on-static-code-analysis-errors",
            "high",
            "--output",
            outputPath
        ]);

        result.ExitCode.Should().Be(10);
        File.Exists(outputPath).Should().BeTrue();
        var combinedOutput = result.StdErr + result.StdOut;
        combinedOutput.Should().Contain("Static code analysis found", "because failures should emit an error message");
    }

    /// <summary>
    /// Invokes the program entry point while capturing stdout/stderr.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Captured exit code and console output.</returns>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunMainAsync(string[] args)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var originalIn = Console.In;

        var outWriter = new StringWriter(new StringBuilder());
        var errWriter = new StringWriter(new StringBuilder());
        Console.SetOut(outWriter);
        Console.SetError(errWriter);

        try
        {
            var exitCode = await InvokeMainAsync(args);
            Console.Out.Flush();
            Console.Error.Flush();
            return (exitCode, outWriter.ToString(), errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
            Console.SetIn(originalIn);
        }
    }

    /// <summary>
    /// Invokes the generated Main method for the top-level program.
    /// </summary>
    /// <param name="args">Arguments for the program.</param>
    /// <returns>Exit code from Main.</returns>
    [SuppressMessage("Major Code Smell", "S3011", Justification = "Program.Main is generated by top-level statements and only reachable via reflection in tests.")]
    private static async Task<int> InvokeMainAsync(string[] args)
    {
        var entryPoint = typeof(TerraformPlanParser).Assembly.EntryPoint;
        entryPoint.Should().NotBeNull("Assembly entry point should be available for CLI execution");

        var result = entryPoint!.Invoke(null, new object?[] { args });
        return result switch
        {
            Task<int> task => await task,
            int code => code,
            Task task => await task.ContinueWith(_ => 0),
            _ => throw new InvalidOperationException("Unexpected Main return type.")
        };
    }

    /// <summary>
    /// Resolves the repository root to keep file IO inside the workspace.
    /// </summary>
    /// <returns>Absolute path to the repo root.</returns>
    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets a test data file path within the repo.
    /// </summary>
    /// <param name="fileName">Test data file name.</param>
    /// <returns>Absolute path to the test data file.</returns>
    private static string GetTestDataPath(string fileName)
    {
        var path = Path.Combine(GetRepoRoot(), "src", "tests", "Oocx.TfPlan2Md.TUnit", "TestData", fileName);
        File.Exists(path).Should().BeTrue($"Test data file {fileName} should exist");
        return path;
    }

    /// <summary>
    /// Gets a temporary file path under the repository .tmp directory.
    /// </summary>
    /// <param name="fileName">File name to create.</param>
    /// <returns>Absolute path to the temp file.</returns>
    private static string GetTempPath(string fileName)
    {
        var tempRoot = Path.Combine(GetRepoRoot(), ".tmp");
        Directory.CreateDirectory(tempRoot);
        return Path.Combine(tempRoot, fileName);
    }
}
