using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersFormatDiffTests
{
    [Fact]
    public void FormatDiff_EqualStrings_ReturnsCodeFormattedValue()
    {
        ScribanHelpers.FormatDiff("TCP", "TCP", "standard-diff").Should().Be("<code>TCP</code>");
        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24", "standard-diff")
            .Should().Be("<code>10.0.1.0/24</code>");
    }

    [Fact]
    public void FormatDiff_DifferentStrings_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff("TCP", "UDP", "standard-diff")
            .Should().Be("- `TCP`<br>+ `UDP`");

        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24", "standard-diff")
            .Should().Be("- `10.0.1.0/24`<br>+ `10.0.1.0/24, 10.0.3.0/24`");
    }

    [Fact]
    public void FormatDiff_NullBefore_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff(null, "value", "standard-diff")
            .Should().Be("- ``<br>+ `value`");
    }

    [Fact]
    public void FormatDiff_NullAfter_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff("value", null, "standard-diff")
            .Should().Be("- `value`<br>+ ``");
    }

    [Fact]
    public void FormatDiff_BothNull_ReturnsEmptyString()
    {
        ScribanHelpers.FormatDiff(null, null, "standard-diff").Should().Be(string.Empty);
    }

    [Fact]
    public void FormatDiff_EmptyStrings_HandledCorrectly()
    {
        ScribanHelpers.FormatDiff(string.Empty, string.Empty, "standard-diff").Should().Be(string.Empty);
        ScribanHelpers.FormatDiff("", "value", "standard-diff").Should().Be("- ``<br>+ `value`");
        ScribanHelpers.FormatDiff("value", "", "standard-diff").Should().Be("- `value`<br>+ ``");
    }

    [Fact]
    public void FormatDiff_EscapesValuesAndPreservesLineBreakTags()
    {
        ScribanHelpers.FormatDiff("<before>", "<after>", "standard-diff")
            .Should().Be("- `\\<before\\>`<br>+ `\\<after\\>`");
    }

    [Fact]
    public void FormatDiff_InlineDiff_UsesHtmlHighlights()
    {
        var result = ScribanHelpers.FormatDiff("abc", "abz", "inline-diff");

        result.Should().Contain("<code")
            .And.Contain("background-color:")
            .And.Contain("<br>")
            .And.NotContain("```", "inline diff should be table-compatible without fenced code blocks");
    }
}
