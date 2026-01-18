using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CLI;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CLI;

public class HelpTextProviderTests
{
    [Test]
    public void GetHelpText_IncludesPrincipalMappingOption()
    {
        var help = HelpTextProvider.GetHelpText();
        help.Should().Contain("-p, --principal-mapping <file>");
        help.Should().Contain("Map principal IDs to names using a JSON file.");
    }

    [Test]
    public void GetHelpText_IncludesReportTitleOption()
    {
        var help = HelpTextProvider.GetHelpText();

        help.Should().Contain("--report-title <title>")
            .And.Contain("Override the report title");
    }

    [Test]
    public void GetHelpText_AlignsOptionDescriptions()
    {
        var help = HelpTextProvider.GetHelpText();
        var optionLines = help.Split('\n')
            .Where(l => l.StartsWith("  -", StringComparison.Ordinal))
            .ToList();

        optionLines.Should().NotBeEmpty();
        const int expectedDescriptionIndex = 52; // 2 spaces indent + OptionPadding (50)

        foreach (var line in optionLines)
        {
            line.Should().NotContain("\t");
            line.Length.Should().BeGreaterThan(expectedDescriptionIndex);
            line[expectedDescriptionIndex].Should().NotBe(' ');
            line[expectedDescriptionIndex - 1].Should().Be(' ');
        }
    }

    [Test]
    public void GetHelpText_IncludesBuiltInTemplatesSection()
    {
        var help = HelpTextProvider.GetHelpText();

        help.Should().Contain("Built-in templates:")
            .And.Contain("default")
            .And.Contain("summary");
    }

    [Test]
    public void GetHelpText_IncludesLargeValueFormatOption()
    {
        var help = HelpTextProvider.GetHelpText();

        help.Should().Contain("--large-value-format <inline-diff|simple-diff>")
            .And.Contain("Controls rendering of large attribute values");
    }

    /// <summary>
    /// TC-02: Help text includes --debug flag documentation.
    /// </summary>
    [Test]
    public void GetHelpText_IncludesDebugFlag()
    {
        // Arrange & Act
        var help = HelpTextProvider.GetHelpText();

        // Assert
        help.Should().Contain("--debug")
            .And.Contain("diagnostic information");
    }
}
