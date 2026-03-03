using System.Text;
using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Builds markdown content using a fluent, stream-oriented API.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class MarkdownWriter
{
    /// <summary>Compiled regex to collapse blank lines between table rows.</summary>
    private static readonly Regex BlankLineInTableRegex = new(
        @"(?<=\|[^\n]*)\n\s*\n(?=[ \t]*\|)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(2));

    /// <summary>Compiled regex to remove indentation from table rows.</summary>
    private static readonly Regex IndentedTableRowRegex = new(
        @"\n[ \t]+(\|)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>Compiled regex to collapse runs of multiple blank lines.</summary>
    private static readonly Regex MultipleBlankLinesRegex = new(
        @"\n([ \t]*\n){2,}",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    /// <summary>Compiled regex to ensure a blank line before headings.</summary>
    private static readonly Regex BlankLineBeforeHeadingRegex = new(
        @"([^\n])\n(#{1,6}\s)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>Compiled regex to ensure a blank line after headings.</summary>
    private static readonly Regex BlankLineAfterHeadingRegex = new(
        @"(#{1,6}\s.+)\n(?!\n)",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>Compiled regex to match only unescaped pipe characters (not preceded by backslash).</summary>
    private static readonly Regex UnescapedPipeRegex = new(
        @"(?<!\\)\|",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Accumulates emitted markdown lines.
    /// </summary>
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Appends a markdown heading.
    /// </summary>
    /// <param name="text">Heading text.</param>
    /// <param name="level">Heading level from 1 to 6.</param>
    /// <returns>The current writer for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="level"/> is outside 1-6.</exception>
    public MarkdownWriter Heading(string text, int level = 2)
    {
        if (level is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");
        }

        _builder.Append(new string('#', level));
        _builder.Append(' ');
        _builder.Append(text);
        _builder.Append('\n');
        return this;
    }

    /// <summary>
    /// Appends a paragraph line.
    /// </summary>
    /// <param name="text">Paragraph text.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter Paragraph(string text)
    {
        _builder.Append(text);
        _builder.Append('\n');
        return this;
    }

    /// <summary>
    /// Appends an empty line.
    /// </summary>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter BlankLine()
    {
        _builder.Append('\n');
        return this;
    }

    /// <summary>
    /// Appends a markdown table header and separator row.
    /// </summary>
    /// <param name="columns">Header columns.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter TableHeader(params string[] columns)
    {
        return TableHeader((IReadOnlyList<string>)columns);
    }

    /// <summary>
    /// Appends a markdown table header and separator row.
    /// </summary>
    /// <param name="columns">Header columns.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter TableHeader(IReadOnlyList<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        TableRow(columns);

        var separators = new string[columns.Count];
        for (var i = 0; i < separators.Length; i++)
        {
            separators[i] = "---";
        }

        TableRow(separators);
        return this;
    }

    /// <summary>
    /// Appends a markdown table header and separator row, padding each separator to
    /// <c>column-header-length + 2</c> dashes to match baseline Scriban template output.
    /// </summary>
    /// <param name="columns">Header columns.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter TableHeaderPadded(IReadOnlyList<string> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        TableRow(columns);

        var separators = new string[columns.Count];
        for (var i = 0; i < separators.Length; i++)
        {
            separators[i] = new string('-', columns[i].Length + 2);
        }

        TableRow(separators);
        return this;
    }

    /// <summary>
    /// Appends a markdown table row.
    /// </summary>
    /// <param name="cells">Row cell values.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter TableRow(IReadOnlyList<string> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        _builder.Append("| ");
        for (var i = 0; i < cells.Count; i++)
        {
            var cell = cells[i] ?? string.Empty;
            _builder.Append(EscapeTableCell(cell));

            if (i < cells.Count - 1)
            {
                _builder.Append(" | ");
            }
        }

        _builder.Append(" |\n");
        return this;
    }

    /// <summary>
    /// Appends an opening details element and summary line.
    /// </summary>
    /// <param name="summary">Details summary text.</param>
    /// <param name="open">Whether to include the open attribute.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter DetailsOpen(string summary, bool open = false)
    {
        _builder.Append(open ? "<details open>" : "<details>");
        _builder.Append("<summary>");
        _builder.Append(summary);
        _builder.Append("</summary>\n");
        return this;
    }

    /// <summary>
    /// Appends a closing details tag.
    /// </summary>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter DetailsClose()
    {
        _builder.Append("</details>\n");
        return this;
    }

    /// <summary>
    /// Appends a fenced code block.
    /// </summary>
    /// <param name="content">Code content.</param>
    /// <param name="language">Optional language identifier.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter Code(string content, string? language = null)
    {
        _builder.Append("```");
        _builder.Append(language ?? string.Empty);
        _builder.Append('\n');
        _builder.Append(content);
        _builder.Append('\n');
        _builder.Append("```\n");
        return this;
    }

    /// <summary>
    /// Formats text as inline code.
    /// </summary>
    /// <param name="content">Inline code content.</param>
    /// <returns>An inline-code formatted string.</returns>
    public static string InlineCode(string content)
    {
        return $"`{content}`";
    }

    /// <summary>
    /// Appends raw text without escaping.
    /// </summary>
    /// <param name="content">Raw content.</param>
    /// <returns>The current writer for chaining.</returns>
    public MarkdownWriter Raw(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return this;
        }

        _builder.Append(content);
        return this;
    }

    /// <summary>
    /// Builds normalized markdown output.
    /// </summary>
    /// <returns>The normalized markdown.</returns>
    public string Build()
    {
        var markdown = _builder
            .ToString()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        markdown = BlankLineInTableRegex.Replace(markdown, "\n");
        markdown = IndentedTableRowRegex.Replace(markdown, "\n$1");
        markdown = NormalizeHeadingSpacing(markdown);

        return markdown;
    }

    /// <summary>
    /// Escapes table cell text for markdown table output.
    /// Only matches pipes that are not already preceded by a backslash to avoid double-escaping
    /// values that were already processed by <see cref="ScribanHelpers.EscapeMarkdown"/>.
    /// </summary>
    /// <param name="cell">Cell text to escape.</param>
    /// <returns>Escaped cell text.</returns>
    private static string EscapeTableCell(string cell)
    {
        return UnescapedPipeRegex.Replace(cell, @"\|");
    }

    /// <summary>
    /// Normalizes heading and blank-line spacing for markdown output.
    /// </summary>
    /// <param name="markdown">Raw markdown text.</param>
    /// <returns>Normalized markdown text with trailing newline.</returns>
    private static string NormalizeHeadingSpacing(string markdown)
    {
        markdown = MultipleBlankLinesRegex.Replace(markdown, "\n\n");
        markdown = BlankLineBeforeHeadingRegex.Replace(markdown, "$1\n\n$2");
        markdown = BlankLineAfterHeadingRegex.Replace(markdown, "$1\n\n");

        markdown = markdown.TrimEnd();
        return $"{markdown}\n";
    }
}
