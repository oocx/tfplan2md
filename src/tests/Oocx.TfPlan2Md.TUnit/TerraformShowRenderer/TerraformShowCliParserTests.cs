using Oocx.TfPlan2Md.TerraformShowRenderer.CLI;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Validates parsing behavior for the Terraform show approximation CLI.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
/// </summary>
public sealed class TerraformShowCliParserTests
{
    /// <summary>
    /// Provides standard arguments for testing long-form options.
    /// </summary>
    private static readonly string[] InputOutputArgs = ["--input", "plan.json", "--output", "rendered.txt"];

    /// <summary>
    /// Provides short option combinations used across tests.
    /// </summary>
    private static readonly string[] ShortOptionArgs = ["-i", "input.json", "-o", "output.txt", "--no-color"];

    /// <summary>
    /// Provides help flag arguments reused by multiple tests.
    /// </summary>
    private static readonly string[] HelpArgs = ["--help"];

    /// <summary>
    /// Provides version flag arguments reused by multiple tests.
    /// </summary>
    private static readonly string[] VersionArgs = ["--version"];

    /// <summary>
    /// Provides arguments with an unknown flag used for validation.
    /// </summary>
    private static readonly string[] UnknownOptionArgs = ["--unknown"];

    /// <summary>
    /// Ensures required arguments populate the expected options.
    /// Related acceptance: TC-01.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_WithInputAndOutput_SetsPaths()
    {
        var options = CliParser.Parse(InputOutputArgs);

        await Assert.That(options.InputPath).IsEqualTo("plan.json");
        await Assert.That(options.OutputPath).IsEqualTo("rendered.txt");
        await Assert.That(options.NoColor).IsFalse();
        await Assert.That(options.ShowHelp).IsFalse();
        await Assert.That(options.ShowVersion).IsFalse();
    }

    /// <summary>
    /// Ensures short option names are accepted and boolean flags are handled.
    /// Related acceptance: TC-01.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_WithShortOptions_SetsValues()
    {
        var options = CliParser.Parse(ShortOptionArgs);

        await Assert.That(options.InputPath).IsEqualTo("input.json");
        await Assert.That(options.OutputPath).IsEqualTo("output.txt");
        await Assert.That(options.NoColor).IsTrue();
    }

    /// <summary>
    /// Ensures help flag bypasses required input validation.
    /// Related acceptance: TC-02.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_WithHelpFlag_AllowsMissingInput()
    {
        var options = CliParser.Parse(HelpArgs);

        await Assert.That(options.ShowHelp).IsTrue();
        await Assert.That(string.IsNullOrEmpty(options.InputPath)).IsTrue();
    }

    /// <summary>
    /// Ensures version flag bypasses required input validation.
    /// Related acceptance: TC-03.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_WithVersionFlag_AllowsMissingInput()
    {
        var options = CliParser.Parse(VersionArgs);

        await Assert.That(options.ShowVersion).IsTrue();
        await Assert.That(string.IsNullOrEmpty(options.InputPath)).IsTrue();
    }

    /// <summary>
    /// Ensures omitting input while not requesting help or version fails.
    /// Related acceptance: TC-12.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_MissingInput_Throws()
    {
        var action = () => CliParser.Parse(Array.Empty<string>());

        await Assert.That(action).Throws<CliParseException>();
        // Check that error message mentions --input
    }

    /// <summary>
    /// Ensures unknown flags produce a helpful error.
    /// Related acceptance: TC-01.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Parse_UnknownOption_Throws()
    {
        var action = () => CliParser.Parse(UnknownOptionArgs);

        await Assert.That(action).Throws<CliParseException>();
        // Check that error message mentions unknown option
    }
}
