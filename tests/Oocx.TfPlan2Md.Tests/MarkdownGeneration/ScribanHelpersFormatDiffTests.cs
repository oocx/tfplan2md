using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersFormatDiffTests
{
    [Fact]
    public void FormatDiff_EqualStrings_ReturnsSingleValue()
    {
        ScribanHelpers.FormatDiff("TCP", "TCP").Should().Be("TCP");
        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24").Should().Be("10.0.1.0/24");
    }

    [Fact]
    public void FormatDiff_DifferentStrings_ReturnsDiffFormat()
    {
        ScribanHelpers.FormatDiff("TCP", "UDP").Should().Be("- TCP<br>+ UDP");
        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24")
            .Should().Be("- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24");
    }

    [Fact]
    public void FormatDiff_NullBefore_ReturnsDiffFormat()
    {
        ScribanHelpers.FormatDiff(null, "value").Should().Be("- <br>+ value");
    }

    [Fact]
    public void FormatDiff_NullAfter_ReturnsDiffFormat()
    {
        ScribanHelpers.FormatDiff("value", null).Should().Be("- value<br>+ ");
    }

    [Fact]
    public void FormatDiff_BothNull_ReturnsEmptyString()
    {
        ScribanHelpers.FormatDiff(null, null).Should().Be(string.Empty);
    }

    [Fact]
    public void FormatDiff_EmptyStrings_HandledCorrectly()
    {
        ScribanHelpers.FormatDiff(string.Empty, string.Empty).Should().Be(string.Empty);
        ScribanHelpers.FormatDiff("", "value").Should().Be("- <br>+ value");
        ScribanHelpers.FormatDiff("value", "").Should().Be("- value<br>+ ");
    }
}
