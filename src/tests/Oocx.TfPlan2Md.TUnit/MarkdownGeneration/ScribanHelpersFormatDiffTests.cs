using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersFormatDiffTests
{
    [Test]
    public void FormatDiff_EqualStrings_ReturnsCodeFormattedValue()
    {
        FormatDiff("TCP", "TCP", "simple-diff").Should().Be("<code>TCP</code>");
        FormatDiff("10.0.1.0/24", "10.0.1.0/24", "simple-diff")
            .Should().Be("<code>10.0.1.0/24</code>");
    }

    [Test]
    public void FormatDiff_DifferentStrings_ReturnsBacktickWrappedSimpleDiff()
    {
        FormatDiff("TCP", "UDP", "simple-diff")
            .Should().Be("- `TCP`<br>+ `UDP`");

        FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24", "simple-diff")
            .Should().Be("- `10.0.1.0/24`<br>+ `10.0.1.0/24, 10.0.3.0/24`");
    }

    [Test]
    public void FormatDiff_NullBefore_ReturnsBacktickWrappedSimpleDiff()
    {
        FormatDiff(null, "value", "simple-diff")
            .Should().Be("- ``<br>+ `value`");
    }

    [Test]
    public void FormatDiff_NullAfter_ReturnsBacktickWrappedSimpleDiff()
    {
        FormatDiff("value", null, "simple-diff")
            .Should().Be("- `value`<br>+ ``");
    }

    [Test]
    public void FormatDiff_BothNull_ReturnsEmptyString()
    {
        FormatDiff(null, null, "simple-diff").Should().Be(string.Empty);
    }

    [Test]
    public void FormatDiff_EmptyStrings_HandledCorrectly()
    {
        FormatDiff(string.Empty, string.Empty, "simple-diff").Should().Be(string.Empty);
        FormatDiff("", "value", "simple-diff").Should().Be("- ``<br>+ `value`");
        FormatDiff("value", "", "simple-diff").Should().Be("- `value`<br>+ ``");
    }

    [Test]
    public void FormatDiff_EscapesValuesAndPreservesLineBreakTags()
    {
        FormatDiff("<before>", "<after>", "simple-diff")
            .Should().Be("- `\\<before\\>`<br>+ `\\<after\\>`");
    }

    [Test]
    public void FormatDiff_InlineDiff_UsesHtmlHighlights()
    {
        var result = FormatDiff("abc", "abz", "inline-diff");

        result.Should().Contain("<code")
            .And.Contain("background-color:")
            .And.Contain("<br>")
            .And.NotContain("```", "inline diff should be table-compatible without fenced code blocks");
    }

    [Test]
    public void FormatDiff_InlineDiff_PrefixesAddedAndRemovedLines()
    {
        var result = FormatDiff("old line", "new line", "inline-diff");

        result.Should().Contain("- ")
            .And.Contain("+ ")
            .And.Contain("<span", "still styled for inline rendering");
    }

    [Test]
    public void FormatDiff_InlineDiff_DoesNotUseNegativeMargins()
    {
        var result = FormatDiff("🌐 10.1.2.0/24", "🌐 10.2.2.0/24", "inline-diff");

        result.Should().NotContain("margin-left: -4px", "negative margins misalign inline diffs in AzDO tables");
    }

    [Test]
    public void FormatDiff_InlineDiff_UsesBlockCodeForAlignment()
    {
        var result = FormatDiff("old", "new", "inline-diff");

        result.Should().Contain("display:block", "block-level code keeps inline diffs aligned inside tables")
            .And.Contain("white-space:normal", "inline diffs should not preserve extraneous whitespace in table cells")
            .And.Contain("padding:0", "reset code padding to avoid column drift");
    }
}
