using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CLI;

namespace Oocx.TfPlan2Md.Tests.CLI;

public class HelpTextProviderTests
{
    [Fact]
    public void GetHelpText_IncludesPrincipalMappingOption()
    {
        var help = HelpTextProvider.GetHelpText();
        help.Should().Contain("-p, --principal-mapping <file>");
        help.Should().Contain("Map principal IDs to names using a JSON file.");
    }

    [Fact]
    public void GetHelpText_AlignsOptionDescriptions()
    {
        var help = HelpTextProvider.GetHelpText();
        var optionLines = help.Split('\n')
            .Where(l => l.StartsWith("  -", StringComparison.Ordinal))
            .ToList();

        optionLines.Should().NotBeEmpty();
        const int expectedDescriptionIndex = 34; // 2 spaces indent + OptionPadding (32)

        foreach (var line in optionLines)
        {
            line.Should().NotContain("\t");
            line.Length.Should().BeGreaterThan(expectedDescriptionIndex);
            line[expectedDescriptionIndex].Should().NotBe(' ');
            line[expectedDescriptionIndex - 1].Should().Be(' ');
        }
    }
}
