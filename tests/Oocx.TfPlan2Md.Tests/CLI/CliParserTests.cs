using AwesomeAssertions;
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
        options.InputFile.Should().BeNull();
        options.OutputFile.Should().BeNull();
        options.TemplatePath.Should().BeNull();
        options.ShowSensitive.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
        options.ShowVersion.Should().BeFalse();
    }

    [Fact]
    public void Parse_InputFileArg_SetsInputFile()
    {
        // Arrange
        var args = new[] { "plan.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.InputFile.Should().Be("plan.json");
    }

    [Fact]
    public void Parse_OutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { "--output", "output.md" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.OutputFile.Should().Be("output.md");
    }

    [Fact]
    public void Parse_ShortOutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { "-o", "output.md" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.OutputFile.Should().Be("output.md");
    }

    [Fact]
    public void Parse_TemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { "--template", "custom.sbn" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.TemplatePath.Should().Be("custom.sbn");
    }

    [Fact]
    public void Parse_ShortTemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { "-t", "custom.sbn" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.TemplatePath.Should().Be("custom.sbn");
    }

    [Fact]
    public void Parse_ShowSensitiveFlag_SetsShowSensitive()
    {
        // Arrange
        var args = new[] { "--show-sensitive" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowSensitive.Should().BeTrue();
    }

    [Fact]
    public void Parse_HelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_ShortHelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_VersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowVersion.Should().BeTrue();
    }

    [Fact]
    public void Parse_ShortVersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "-v" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowVersion.Should().BeTrue();
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
        options.InputFile.Should().Be("plan.json");
        options.OutputFile.Should().Be("output.md");
        options.TemplatePath.Should().Be("custom.sbn");
        options.ShowSensitive.Should().BeTrue();
    }

    [Fact]
    public void Parse_UnknownOption_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--unknown-option" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Fact]
    public void Parse_OutputWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--output" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Fact]
    public void Parse_TemplateWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--template" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Fact]
    public void Parse_PrincipalMappingFlag_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { "--principal-mapping", "principals.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }

    [Fact]
    public void Parse_PrincipalMappingShortFlag_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { "-p", "principals.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }

    [Fact]
    public void Parse_PrincipalsAlias_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { "--principals", "principals.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }
}
