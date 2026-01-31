using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for markdown escaping helpers used by templates.
/// Related feature: docs/features/007-markdown-quality-validation/specification.md.
/// </summary>
public class ScribanHelpersMarkdownTests
{
    /// <summary>
    /// Verifies null and empty inputs return empty output.
    /// </summary>
    [Test]
    public void EscapeMarkdown_NullOrEmpty_ReturnsEmpty()
    {
        ScribanHelpers.EscapeMarkdown(null).Should().BeEmpty();
        ScribanHelpers.EscapeMarkdown(string.Empty).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies special characters and newlines are escaped for markdown tables.
    /// </summary>
    [Test]
    public void EscapeMarkdown_EscapesSpecialCharactersAndNewlines()
    {
        var input = "value|`test`\\\nline<end>&";

        var escaped = ScribanHelpers.EscapeMarkdown(input);

        escaped.Should().Be("value\\|\\`test\\`\\\\<br/>line\\<end\\>&amp;");
    }

    /// <summary>
    /// Verifies table cell escaping replaces pipe separators with HTML entities.
    /// </summary>
    [Test]
    public void EscapeMarkdownTableCell_ReplacesPipeSeparators()
    {
        var input = "column|value";

        var escaped = ScribanHelpers.EscapeMarkdownTableCell(input);

        escaped.Should().Be("column&#124;value");
    }

    /// <summary>
    /// Verifies heading-specific escapes are applied after markdown escaping.
    /// </summary>
    [Test]
    public void EscapeMarkdownHeading_EscapesHeadingCharacters()
    {
        var input = "# [Title]_";

        var escaped = ScribanHelpers.EscapeMarkdownHeading(input);

        escaped.Should().Be("\\# \\[Title\\]\\_");
    }
}
