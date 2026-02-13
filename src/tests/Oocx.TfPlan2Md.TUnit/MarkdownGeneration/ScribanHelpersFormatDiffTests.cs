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
    public void FormatDiff_DifferentStrings_ReturnsSimpleDiffWithoutBackticks()
    {
        FormatDiff("TCP", "UDP", "simple-diff")
            .Should().Be("- TCP<br>+ UDP");

        FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24", "simple-diff")
            .Should().Be("- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24");
    }

    [Test]
    public void FormatDiff_NullBefore_ReturnsSimpleDiffWithoutBackticks()
    {
        FormatDiff(null, "value", "simple-diff")
            .Should().Be("- <br>+ value");
    }

    [Test]
    public void FormatDiff_NullAfter_ReturnsSimpleDiffWithoutBackticks()
    {
        FormatDiff("value", null, "simple-diff")
            .Should().Be("- value<br>+ ");
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
        FormatDiff("", "value", "simple-diff").Should().Be("- <br>+ value");
        FormatDiff("value", "", "simple-diff").Should().Be("- value<br>+ ");
    }

    [Test]
    public void FormatDiff_EscapesValuesAndPreservesLineBreaks()
    {
        FormatDiff("<before>", "<after>", "simple-diff")
            .Should().Be("- <before><br>+ <after>");
    }

    [Test]
    public void FormatDiff_InlineDiff_UsesHtmlWithCharacterLevelHighlighting()
    {
        var result = FormatDiff("abc", "abz", "inline-diff");

        result.Should().Contain("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">")
            .And.Contain("background-color:")
            .And.Contain("<br>")
            .And.Contain("- ")
            .And.Contain("+ ")
            .And.NotContain("```", "inline diff should be table-compatible without fenced code blocks")
            .And.Contain("#ffc0c0", "should highlight removed characters")
            .And.Contain("#acf2bd", "should highlight added characters");
    }

    [Test]
    public void FormatDiff_InlineDiff_PrefixesAddedAndRemovedLines()
    {
        var result = FormatDiff("old line", "new line", "inline-diff");

        result.Should().Contain("- ")
            .And.Contain("+ ")
            .And.Contain("<span style=", "HTML format with character-level highlighting")
            .And.Contain("background-color: #fff5f5", "removed line background")
            .And.Contain("background-color: #f0fff4", "added line background");
    }

    [Test]
    public void FormatDiff_InlineDiff_DoesNotUseNegativeMargins()
    {
        var result = FormatDiff("🌐 10.1.2.0/24", "🌐 10.2.2.0/24", "inline-diff");

        result.Should().NotContain("margin-left: -4px", "negative margins misalign inline diffs in AzDO tables");
    }

    [Test]
    public void FormatDiff_InlineDiff_UsesHtmlForRichTableRendering()
    {
        var result = FormatDiff("old", "new", "inline-diff");

        result.Should().Contain("display:block", "HTML format with block display for table cells")
            .And.Contain("white-space:normal", "HTML format with normal whitespace")
            .And.Contain("padding:0", "HTML format with zero padding")
            .And.Contain("- ")
            .And.Contain("+ ")
            .And.Contain("<code style=", "HTML wrapper for diff content");
    }
}
