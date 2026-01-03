using System.Globalization;
using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Tests;

/// <summary>
/// Verifies parsing behaviors for the screenshot generator CLI.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
public sealed class CliParserTests
{
    /// <summary>
    /// Ensures required arguments are parsed and defaults are applied.
    /// Related acceptance: TC-01, TC-03, TC-04.
    /// </summary>
    [Fact]
    public void Parse_WithRequiredArguments_UsesDefaults()
    {
        var options = CliParser.Parse(new List<string> { "--input", "/tmp/report.html" });

        Assert.Equal("/tmp/report.html", options.InputPath);
        Assert.Null(options.OutputPath);
        Assert.Equal(1920, options.Width);
        Assert.Equal(1080, options.Height);
        Assert.False(options.FullPage);
        Assert.Null(options.Format);
        Assert.Null(options.Quality);
        Assert.False(options.ShowHelp);
        Assert.False(options.ShowVersion);
    }

    /// <summary>
    /// Confirms custom viewport and full-page flag are honored.
    /// Related acceptance: TC-03, TC-04.
    /// </summary>
    [Fact]
    public void Parse_WithCustomViewportAndFullPage_ParsesValues()
    {
        var options = CliParser.Parse(new List<string> { "--input", "input.html", "--width", "1280", "--height", "720", "--full-page" });

        Assert.Equal(1280, options.Width);
        Assert.Equal(720, options.Height);
        Assert.True(options.FullPage);
    }

    /// <summary>
    /// Verifies that JPEG format applies the default quality when none is provided.
    /// Related acceptance: TC-06.
    /// </summary>
    [Fact]
    public void Parse_WithJpegFormat_AssignsDefaultQuality()
    {
        var options = CliParser.Parse(new List<string> { "--input", "input.html", "--format", "jpeg" });

        Assert.Equal(ScreenshotFormat.Jpeg, options.Format);
        Assert.Equal(90, options.Quality);
    }

    /// <summary>
    /// Confirms explicitly supplied quality overrides defaults.
    /// Related acceptance: TC-06.
    /// </summary>
    [Fact]
    public void Parse_WithExplicitQuality_UsesProvidedValue()
    {
        var options = CliParser.Parse(new List<string> { "--input", "input.html", "--format", "jpeg", "--quality", "50" });

        Assert.Equal(50, options.Quality);
    }

    /// <summary>
    /// Ensures help bypasses required input validation so users can see usage.
    /// Related acceptance: TC-01.
    /// </summary>
    [Fact]
    public void Parse_WithHelpOnly_DoesNotRequireInput()
    {
        var options = CliParser.Parse(new List<string> { "--help" });

        Assert.True(options.ShowHelp);
        Assert.Null(options.InputPath);
    }

    /// <summary>
    /// Ensures invalid formats are rejected with a descriptive error.
    /// Related acceptance: TC-10.
    /// </summary>
    [Fact]
    public void Parse_WithInvalidFormat_Throws()
    {
        var exception = Assert.Throws<CliParseException>(() => CliParser.Parse(new List<string> { "--input", "input.html", "--format", "gif" }));

        Assert.Contains("png", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("jpeg", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures a missing required input triggers a parse exception when not showing help or version.
    /// Related acceptance: TC-01.
    /// </summary>
    [Fact]
    public void Parse_WithoutInput_Throws()
    {
        var exception = Assert.Throws<CliParseException>(() => CliParser.Parse(new List<string>()));

        Assert.Contains("--input", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that non-numeric width arguments result in parse errors.
    /// Related acceptance: TC-08.
    /// </summary>
    [Fact]
    public void Parse_WithNonNumericWidth_Throws()
    {
        var exception = Assert.Throws<CliParseException>(() => CliParser.Parse(new List<string> { "--input", "input.html", "--width", "abc" }));

        Assert.Contains("width", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that non-numeric height arguments result in parse errors.
    /// Related acceptance: TC-08.
    /// </summary>
    [Fact]
    public void Parse_WithNonNumericHeight_Throws()
    {
        var exception = Assert.Throws<CliParseException>(() => CliParser.Parse(new List<string> { "--input", "input.html", "--height", "xyz" }));

        Assert.Contains("height", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures Terraform resource targeting is parsed.
    /// Related acceptance: TC-05.
    /// </summary>
    [Fact]
    public void Parse_WithTargetTerraformResourceId_ParsesValue()
    {
        var options = CliParser.Parse(new List<string> { "--input", "input.html", "--target-terraform-resource-id", "azurerm_firewall.example" });

        Assert.Equal("azurerm_firewall.example", options.TargetTerraformResourceId);
        Assert.Null(options.TargetSelector);
    }

    /// <summary>
    /// Ensures selector targeting is parsed.
    /// Related acceptance: TC-06.
    /// </summary>
    [Fact]
    public void Parse_WithTargetSelector_ParsesValue()
    {
        var options = CliParser.Parse(new List<string> { "--input", "input.html", "--target-selector", "summary:has-text('firewall')" });

        Assert.Equal("summary:has-text('firewall')", options.TargetSelector);
        Assert.Null(options.TargetTerraformResourceId);
    }
}
