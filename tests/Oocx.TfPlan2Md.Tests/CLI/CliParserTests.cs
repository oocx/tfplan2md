using Oocx.TfPlan2Md.CLI;

namespace Oocx.TfPlan2Md.Tests.CLI;

public class CliParserTests
{
    [Fact]
    public void Parse_NoArgs_ReturnsDefaultOptions()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Null(options.InputFile);
        Assert.Null(options.OutputFile);
        Assert.Null(options.TemplatePath);
        Assert.False(options.ShowSensitive);
        Assert.False(options.ShowHelp);
        Assert.False(options.ShowVersion);
    }

    [Fact]
    public void Parse_InputFileArg_SetsInputFile()
    {
        // Arrange
        var args = new[] { "plan.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("plan.json", options.InputFile);
    }

    [Fact]
    public void Parse_OutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { "--output", "output.md" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("output.md", options.OutputFile);
    }

    [Fact]
    public void Parse_ShortOutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { "-o", "output.md" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("output.md", options.OutputFile);
    }

    [Fact]
    public void Parse_TemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { "--template", "custom.sbn" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("custom.sbn", options.TemplatePath);
    }

    [Fact]
    public void Parse_ShortTemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { "-t", "custom.sbn" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("custom.sbn", options.TemplatePath);
    }

    [Fact]
    public void Parse_ShowSensitiveFlag_SetsShowSensitive()
    {
        // Arrange
        var args = new[] { "--show-sensitive" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.True(options.ShowSensitive);
    }

    [Fact]
    public void Parse_HelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void Parse_ShortHelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void Parse_VersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.True(options.ShowVersion);
    }

    [Fact]
    public void Parse_ShortVersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "-v" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.True(options.ShowVersion);
    }

    [Fact]
    public void Parse_AllOptions_ParsesCorrectly()
    {
        // Arrange
        var args = new[]
        {
            "plan.json",
            "--output", "output.md",
            "--template", "custom.sbn",
            "--show-sensitive"
        };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        Assert.Equal("plan.json", options.InputFile);
        Assert.Equal("output.md", options.OutputFile);
        Assert.Equal("custom.sbn", options.TemplatePath);
        Assert.True(options.ShowSensitive);
    }

    [Fact]
    public void Parse_UnknownOption_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--unknown-option" };

        // Act & Assert
        Assert.Throws<CliParseException>(() => CliParser.Parse(args));
    }

    [Fact]
    public void Parse_OutputWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--output" };

        // Act & Assert
        Assert.Throws<CliParseException>(() => CliParser.Parse(args));
    }

    [Fact]
    public void Parse_TemplateWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--template" };

        // Act & Assert
        Assert.Throws<CliParseException>(() => CliParser.Parse(args));
    }
}
