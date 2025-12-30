namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Code formatting helpers for markdown and HTML contexts.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Formats text as HTML code for usage inside summary tags where markdown backticks are unreliable.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="text">The raw text to wrap in a code span.</param>
    /// <returns>HTML code-wrapped text, or an empty string when input is null or empty.</returns>
    public static string FormatCodeSummary(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return $"<code>{EscapeHtmlForCode(text)}</code>";
    }

    /// <summary>
    /// Formats text as markdown inline code for table rendering.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md
    /// </summary>
    /// <param name="text">The raw text to wrap in inline code.</param>
    /// <returns>Markdown inline code string, or an empty string when input is null or empty.</returns>
    public static string FormatCodeTable(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return $"`{EscapeMarkdown(text)}`";
    }
}
