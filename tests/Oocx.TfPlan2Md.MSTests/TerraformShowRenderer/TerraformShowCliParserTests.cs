using Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

namespace Oocx.TfPlan2Md.Tests.TerraformShowRenderer;

/// <summary>
/// Validates parsing behavior for the Terraform show approximation CLI.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
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
    [TestMethod]
    public void Parse_WithInputAndOutput_SetsPaths()
    {
        var options = CliParser.Parse(InputOutputArgs);

        Assert.Equal("plan.json", options.InputPath);
        Assert.Equal("rendered.txt", options.OutputPath);
        Assert.False(options.NoColor);
        Assert.False(options.ShowHelp);
        Assert.False(options.ShowVersion);
    }

    /// <summary>
    /// Ensures short option names are accepted and boolean flags are handled.
    /// Related acceptance: TC-01.
    /// </summary>
    [TestMethod]
    public void Parse_WithShortOptions_SetsValues()
    {
        var options = CliParser.Parse(ShortOptionArgs);

        Assert.Equal("input.json", options.InputPath);
        Assert.Equal("output.txt", options.OutputPath);
        Assert.True(options.NoColor);
    }

    /// <summary>
    /// Ensures help flag bypasses required input validation.
    /// Related acceptance: TC-02.
    /// </summary>
    [TestMethod]
    public void Parse_WithHelpFlag_AllowsMissingInput()
    {
        var options = CliParser.Parse(HelpArgs);

        Assert.True(options.ShowHelp);
        Assert.True(string.IsNullOrEmpty(options.InputPath));
    }

    /// <summary>
    /// Ensures version flag bypasses required input validation.
    /// Related acceptance: TC-03.
    /// </summary>
    [TestMethod]
    public void Parse_WithVersionFlag_AllowsMissingInput()
    {
        var options = CliParser.Parse(VersionArgs);

        Assert.True(options.ShowVersion);
        Assert.True(string.IsNullOrEmpty(options.InputPath));
    }

    /// <summary>
    /// Ensures omitting input while not requesting help or version fails.
    /// Related acceptance: TC-12.
    /// </summary>
    [TestMethod]
    public void Parse_MissingInput_Throws()
    {
        var action = () => CliParser.Parse(Array.Empty<string>());

        var exception = Assert.Throws<CliParseException>(action);
        Assert.Contains("--input", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures unknown flags produce a helpful error.
    /// Related acceptance: TC-01.
    /// </summary>
    [TestMethod]
    public void Parse_UnknownOption_Throws()
    {
        var action = () => CliParser.Parse(UnknownOptionArgs);

        var exception = Assert.Throws<CliParseException>(action);
        Assert.Contains("Unknown option", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
