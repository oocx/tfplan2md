using AwesomeAssertions;
using Oocx.TfPlan2Md.CLI;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CLI;

public class CliParserTests
{
    private const string PlanJson = "plan.json";
    private const string OutputMd = "output.md";
    private const string OutputFlag = "--output";
    private const string CustomSbn = "custom.sbn";
    private const string TemplateFlag = "--template";
    private const string ReportTitleFlag = "--report-title";
    private const string PrincipalsJson = "principals.json";
    private const string PrincipalMappingFlag = "--principal-mapping";
    [Test]
    public void Parse_NoArgs_ReturnsDefaultOptions()
    {
        // Act
        var options = CliParser.Parse(Array.Empty<string>());

        // Assert (grouped by BeNull and BeFalse)

        options.InputFile.Should().BeNull();
        options.OutputFile.Should().BeNull();
        options.TemplatePath.Should().BeNull();
        options.ReportTitle.Should().BeNull();
        options.PrincipalMappingFile.Should().BeNull();
        options.CodeAnalysisResultsPatterns.Should().BeEmpty();
        options.CodeAnalysisMinimumLevel.Should().BeNull();
        options.FailOnStaticCodeAnalysisErrorsLevel.Should().BeNull();

        options.ShowSensitive.Should().BeFalse();
        options.ShowUnchangedValues.Should().BeFalse();
        options.ShowHelp.Should().BeFalse();
        options.ShowVersion.Should().BeFalse();
        options.HideMetadata.Should().BeFalse();
    }

    [Test]
    public void Parse_InputFileArg_SetsInputFile()
    {
        // Arrange
        var args = new[] { PlanJson };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.InputFile.Should().Be("plan.json");
    }

    [Test]
    public void Parse_OutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { OutputFlag, OutputMd };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.OutputFile.Should().Be("output.md");
    }

    [Test]
    public void Parse_ShortOutputFlag_SetsOutputFile()
    {
        // Arrange
        var args = new[] { "-o", OutputMd };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.OutputFile.Should().Be("output.md");
    }

    [Test]
    public void Parse_TemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { TemplateFlag, CustomSbn };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.TemplatePath.Should().Be("custom.sbn");
    }

    [Test]
    public void Parse_ShortTemplateFlag_SetsTemplatePath()
    {
        // Arrange
        var args = new[] { "-t", CustomSbn };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.TemplatePath.Should().Be("custom.sbn");
    }

    [Test]
    public void Parse_ReportTitleFlag_SetsReportTitle()
    {
        // Arrange
        var args = new[] { ReportTitleFlag, "Custom Title" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ReportTitle.Should().Be("Custom Title");
    }

    [Test]
    public void Parse_ReportTitleEmpty_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { ReportTitleFlag, string.Empty };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>().Which.Message.Should().Contain("cannot be empty");
    }

    [Test]
    public void Parse_ReportTitleWithNewlines_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { ReportTitleFlag, "Line 1\nLine 2" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>().Which.Message.Should().Contain("cannot contain newlines");
    }

    [Test]
    public void Parse_ReportTitleWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { ReportTitleFlag };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>().Which.Message.Should().Contain("requires a value");
    }

    [Test]
    public void Parse_ShowSensitiveFlag_SetsShowSensitive()
    {
        // Arrange
        var args = new[] { "--show-sensitive" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowSensitive.Should().BeTrue();
    }

    [Test]
    public void Parse_ShowUnchangedValuesFlag_SetsShowUnchangedValues()
    {
        // Arrange
        var args = new[] { "--show-unchanged-values" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowUnchangedValues.Should().BeTrue();
    }

    [Test]
    public void Parse_HideMetadataFlag_SetsHideMetadata()
    {
        // Arrange
        var args = new[] { "--hide-metadata" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.HideMetadata.Should().BeTrue();
    }

    [Test]
    public void Parse_HelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Test]
    public void Parse_ShortHelpFlag_SetsShowHelp()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowHelp.Should().BeTrue();
    }

    [Test]
    public void Parse_VersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowVersion.Should().BeTrue();
    }

    [Test]
    public void Parse_ShortVersionFlag_SetsShowVersion()
    {
        // Arrange
        var args = new[] { "-v" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.ShowVersion.Should().BeTrue();
    }

    [Test]
    public void Parse_AllOptions_ParsesCorrectly()
    {
        // Arrange
        var args = new[]
        {
            "plan.json",
            "--output", "output.md",
            "--template", "custom.sbn",
            "--show-sensitive",
            "--show-unchanged-values"
        };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.InputFile.Should().Be("plan.json");
        options.OutputFile.Should().Be("output.md");
        options.TemplatePath.Should().Be("custom.sbn");
        options.ShowSensitive.Should().BeTrue();
        options.ShowUnchangedValues.Should().BeTrue();
    }

    [Test]
    public void Parse_UnknownOption_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--unknown-option" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Test]
    public void Parse_OutputWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--output" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Test]
    public void Parse_TemplateWithoutValue_ThrowsCliParseException()
    {
        // Arrange
        var args = new[] { "--template" };

        // Act
        var act = () => CliParser.Parse(args);

        // Assert
        act.Should().Throw<CliParseException>();
    }

    [Test]
    public void Parse_PrincipalMappingFlag_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { PrincipalMappingFlag, PrincipalsJson };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }

    [Test]
    public void Parse_PrincipalMappingShortFlag_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { "-p", "principals.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }

    [Test]
    public void Parse_PrincipalsAlias_SetsPrincipalMappingFile()
    {
        // Arrange
        var args = new[] { "--principals", "principals.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.PrincipalMappingFile.Should().Be("principals.json");
    }

    [Test]
    public void Parse_RenderTargetGitHub_SetsOption()
    {
        // Arrange
        var args = new[] { "plan.json", "--render-target", "github" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.RenderTarget.Should().Be(RenderTarget.GitHub);
    }

    [Test]
    public void Parse_RenderTargetDefault_IsAzureDevOps()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.RenderTarget.Should().Be(RenderTarget.AzureDevOps);
    }

    [Test]
    public void Parse_RenderTarget_IsCaseInsensitive()
    {
        // Arrange
        var args = new[] { "--render-target", "AZUREDEVOPS" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.RenderTarget.Should().Be(RenderTarget.AzureDevOps);
    }

    [Test]
    public void Parse_LargeValueFormat_ThrowsError()
    {
        // Arrange
        var args = new[] { "plan.json", "--large-value-format", "simple-diff" };

        // Act & Assert
        var act = () => CliParser.Parse(args);
        act.Should().Throw<CliParseException>()
            .WithMessage("*--large-value-format*deprecated*--render-target*");
    }

    /// <summary>
    /// TC-01: --debug flag in various positions is parsed correctly.
    /// </summary>
    [Test]
    public void Parse_DebugFlagBeforeInput_SetsDebugTrue()
    {
        // Arrange
        var args = new[] { "--debug", "plan.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.Debug.Should().BeTrue();
        options.InputFile.Should().Be("plan.json");
    }

    /// <summary>
    /// TC-01: --debug flag after input is parsed correctly.
    /// </summary>
    [Test]
    public void Parse_DebugFlagAfterInput_SetsDebugTrue()
    {
        // Arrange
        var args = new[] { "plan.json", "--debug" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.Debug.Should().BeTrue();
        options.InputFile.Should().Be("plan.json");
    }

    /// <summary>
    /// TC-01: Default (no --debug flag) sets Debug to false.
    /// </summary>
    [Test]
    public void Parse_NoDebugFlag_SetsDebugFalse()
    {
        // Arrange
        var args = new[] { "plan.json" };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.Debug.Should().BeFalse();
    }

    /// <summary>
    /// Test --debug with multiple other flags.
    /// </summary>
    [Test]
    public void Parse_DebugWithMultipleFlags_ParsesAll()
    {
        // Arrange
        var args = new[]
        {
            "plan.json",
            "--debug",
            "--output", "output.md",
            "--show-sensitive"
        };

        // Act
        var options = CliParser.Parse(args);

        // Assert
        options.Debug.Should().BeTrue();
        options.InputFile.Should().Be("plan.json");
        options.OutputFile.Should().Be("output.md");
        options.ShowSensitive.Should().BeTrue();
    }
}
