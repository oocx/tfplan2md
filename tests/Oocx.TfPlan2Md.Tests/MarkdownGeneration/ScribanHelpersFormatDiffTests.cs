using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersFormatDiffTests
{
    [Fact]
    public void FormatDiff_EqualStrings_ReturnsCodeFormattedValue()
    {
        ScribanHelpers.FormatDiff("TCP", "TCP", "simple-diff").Should().Be("<code>TCP</code>");
        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24", "simple-diff")
            .Should().Be("<code>10.0.1.0/24</code>");
    }

    [Fact]
    public void FormatDiff_DifferentStrings_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff("TCP", "UDP", "simple-diff")
            .Should().Be("- `TCP`<br>+ `UDP`");

        ScribanHelpers.FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24", "simple-diff")
            .Should().Be("- `10.0.1.0/24`<br>+ `10.0.1.0/24, 10.0.3.0/24`");
    }

    [Fact]
    public void FormatDiff_NullBefore_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff(null, "value", "simple-diff")
            .Should().Be("- ``<br>+ `value`");
    }

    [Fact]
    public void FormatDiff_NullAfter_ReturnsBacktickWrappedStandardDiff()
    {
        ScribanHelpers.FormatDiff("value", null, "simple-diff")
            .Should().Be("- `value`<br>+ ``");
    }

    [Fact]
    public void FormatDiff_BothNull_ReturnsEmptyString()
    {
        ScribanHelpers.FormatDiff(null, null, "simple-diff").Should().Be(string.Empty);
    }

    [Fact]
    public void FormatDiff_EmptyStrings_HandledCorrectly()
    {
        ScribanHelpers.FormatDiff(string.Empty, string.Empty, "simple-diff").Should().Be(string.Empty);
        ScribanHelpers.FormatDiff("", "value", "simple-diff").Should().Be("- ``<br>+ `value`");
        ScribanHelpers.FormatDiff("value", "", "simple-diff").Should().Be("- `value`<br>+ ``");
    }

    [Fact]
    public void FormatDiff_EscapesValuesAndPreservesLineBreakTags()
    {
        ScribanHelpers.FormatDiff("<before>", "<after>", "simple-diff")
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

    [Fact]
    public void FormatDiff_InlineDiff_PrefixesAddedAndRemovedLines()
    {
        var result = ScribanHelpers.FormatDiff("old line", "new line", "inline-diff");

        result.Should().Contain("- ")
            .And.Contain("+ ")
            .And.Contain("<span", "still styled for inline rendering");
    }

    [Fact]
    public void FormatDiff_InlineDiff_DoesNotUseNegativeMargins()
    {
        var result = ScribanHelpers.FormatDiff("🌐 10.1.2.0/24", "🌐 10.2.2.0/24", "inline-diff");

        result.Should().NotContain("margin-left: -4px", "negative margins misalign inline diffs in AzDO tables");
    }

    [Fact]
    public void FormatDiff_InlineDiff_UsesBlockCodeForAlignment()
    {
        var result = ScribanHelpers.FormatDiff("old", "new", "inline-diff");

        result.Should().Contain("display:block", "block-level code keeps inline diffs aligned inside tables")
            .And.Contain("white-space:normal", "inline diffs should not preserve extraneous whitespace in table cells")
            .And.Contain("padding:0", "reset code padding to avoid column drift");
    }
}
