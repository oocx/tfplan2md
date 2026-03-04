using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for code formatting helpers (FormatChildValue, FormatCodeSummary, FormatCodeTable).
/// Related: Fix for escaped backtick handling in plain text values.
/// </summary>
public class MarkdownHelpersCodeFormattingTests
{
    /// <summary>
    /// Verifies that plain text with escaped backtick preserves the backslash-backtick sequence.
    /// Bug: FormatChildValue was stripping \` from plain text before checking if it's HTML.
    /// Expected: Plain text "some\`text" should become "`some\`text`" (backtick-wrapped, preserving \`).
    /// </summary>
    [Test]
    public void FormatChildValue_PlainTextWithEscapedBacktick_PreservesEscapedBacktick()
    {
        // Arrange
        var input = @"some\`text";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be(@"`some\`text`");
    }

    /// <summary>
    /// Verifies that HTML code tags with escaped backticks have the backticks properly cleaned.
    /// This is the original intended behavior: FormatDiff returns "&lt;code&gt;\`value\`&lt;/code&gt;" for equal values.
    /// Expected: "&lt;code&gt;\`value\`&lt;/code&gt;" should become "&lt;code&gt;value&lt;/code&gt;" (backticks removed from HTML).
    /// </summary>
    [Test]
    public void FormatChildValue_HtmlCodeWithEscapedBackticks_RemovesEscapedBackticks()
    {
        // Arrange
        var input = @"<code>\`value\`</code>";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("<code>value</code>");
    }

    /// <summary>
    /// Verifies that HTML span tags with escaped backticks also get cleaned.
    /// </summary>
    [Test]
    public void FormatChildValue_HtmlSpanWithEscapedBackticks_RemovesEscapedBackticks()
    {
        // Arrange
        var input = @"<span>\`test\`</span>";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("<span>test</span>");
    }

    /// <summary>
    /// Verifies that plain text values without special characters get backtick-wrapped.
    /// </summary>
    [Test]
    public void FormatChildValue_PlainText_WrapsInBackticks()
    {
        // Arrange
        var input = "simple-value";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("`simple-value`");
    }

    /// <summary>
    /// Verifies that null input returns empty string.
    /// </summary>
    [Test]
    public void FormatChildValue_Null_ReturnsEmpty()
    {
        // Act
        var result = MarkdownHelpers.FormatChildValue(null);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that empty string returns empty string.
    /// </summary>
    [Test]
    public void FormatChildValue_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = MarkdownHelpers.FormatChildValue(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that values already wrapped in backticks pass through unchanged.
    /// </summary>
    [Test]
    public void FormatChildValue_AlreadyBackticked_PassesThrough()
    {
        // Arrange
        var input = "`already-formatted`";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("`already-formatted`");
    }

    /// <summary>
    /// Verifies that simple diff format (- value&lt;br&gt;+ value) passes through unchanged.
    /// </summary>
    [Test]
    public void FormatChildValue_SimpleDiff_PassesThrough()
    {
        // Arrange
        var input = "- old<br>+ new";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("- old<br>+ new");
    }

    /// <summary>
    /// Verifies that bare dash passes through unchanged.
    /// </summary>
    [Test]
    public void FormatChildValue_BareDash_PassesThrough()
    {
        // Arrange
        var input = "-";

        // Act
        var result = MarkdownHelpers.FormatChildValue(input);

        // Assert
        result.Should().Be("-");
    }

    /// <summary>
    /// Verifies FormatCodeSummary wraps text in HTML code tags.
    /// </summary>
    [Test]
    public void FormatCodeSummary_PlainText_WrapsInHtmlCode()
    {
        // Arrange
        var input = "test-value";

        // Act
        var result = MarkdownHelpers.FormatCodeSummary(input);

        // Assert
        result.Should().Be("<code>test-value</code>");
    }

    /// <summary>
    /// Verifies FormatCodeSummary returns empty for null input.
    /// </summary>
    [Test]
    public void FormatCodeSummary_Null_ReturnsEmpty()
    {
        // Act
        var result = MarkdownHelpers.FormatCodeSummary(null);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies FormatCodeTable wraps text in markdown backticks.
    /// </summary>
    [Test]
    public void FormatCodeTable_PlainText_WrapsInBackticks()
    {
        // Arrange
        var input = "test-value";

        // Act
        var result = MarkdownHelpers.FormatCodeTable(input);

        // Assert
        result.Should().Be("`test-value`");
    }

    /// <summary>
    /// Verifies FormatCodeTable returns empty for null input.
    /// </summary>
    [Test]
    public void FormatCodeTable_Null_ReturnsEmpty()
    {
        // Act
        var result = MarkdownHelpers.FormatCodeTable(null);

        // Assert
        result.Should().BeEmpty();
    }
}
