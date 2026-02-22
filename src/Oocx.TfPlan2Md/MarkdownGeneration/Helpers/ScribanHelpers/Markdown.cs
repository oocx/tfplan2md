using System.Text;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Markdown escaping helpers.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Escapes only markdown-breaking characters to keep generated tables and headings valid while preserving readability.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md.
    /// </summary>
    /// <param name="input">The raw value to escape.</param>
    /// <returns>A markdown-safe string with newlines replaced by &lt;br/&gt;.</returns>
    /// <remarks>
    /// The helper intentionally avoids escaping '&gt;' and '&lt;' so inline code spans do not render visible backslashes.
    /// Related issue: docs/issues/058-nsg-rendering-issues/analysis.md.
    /// </remarks>
    public static string EscapeMarkdown(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = input;

        value = value.Replace("\\", "\\\\");
        value = value.Replace("|", "\\|");
        value = value.Replace("`", "\\`");
        value = value.Replace("&", "&amp;");

        value = value.Replace("\r\n", "<br/>");
        value = value.Replace("\n", "<br/>");
        value = value.Replace("\r", "<br/>");

        return value;
    }

    /// <summary>
    /// Escapes markdown characters for table cell content while preserving multi-line readability.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    /// <param name="input">The raw table cell content to escape.</param>
    /// <returns>A markdown-safe string with table separators encoded.</returns>
    public static string EscapeMarkdownTableCell(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = EscapeMarkdown(input);
        return value.Replace("\\|", "&#124;", StringComparison.Ordinal);
    }

    /// <summary>
    /// Escapes markdown characters specifically for headings so literal text renders correctly.
    /// Related feature: docs/features/020-custom-report-title/specification.md.
    /// </summary>
    /// <param name="input">The raw heading text.</param>
    /// <returns>Heading-safe text with special characters escaped.</returns>
    /// <remarks>
    /// Templates add the heading marker (#). This helper keeps the user-provided title literal so it renders as text instead of markdown syntax.
    /// </remarks>
    public static string EscapeMarkdownHeading(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = EscapeMarkdown(input);
        value = value.Replace("#", "\\#");
        value = value.Replace("[", "\\[");
        value = value.Replace("]", "\\]");
        value = value.Replace("*", "\\*");
        value = value.Replace("_", "\\_");

        return value;
    }

    /// <summary>
    /// Escapes a markdown link destination for angle-bracket form <c>(&lt;...&gt;)</c>.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    /// <param name="input">The raw URL or destination value.</param>
    /// <returns>A destination-safe value that cannot prematurely terminate the link target.</returns>
    public static string EscapeMarkdownLinkDestination(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = input;
        value = value.Replace("<", "%3C", StringComparison.Ordinal);
        value = value.Replace(">", "%3E", StringComparison.Ordinal);
        value = value.Replace("\r\n", string.Empty, StringComparison.Ordinal);
        value = value.Replace("\n", string.Empty, StringComparison.Ordinal);
        value = value.Replace("\r", string.Empty, StringComparison.Ordinal);

        return value;
    }

    /// <summary>
    /// HTML encodes a string while preserving emoji characters.
    /// Manually escapes HTML special characters without encoding Unicode emoji.
    /// </summary>
    /// <param name="value">Value to encode.</param>
    /// <returns>HTML-encoded string.</returns>
    private static string HtmlEncode(string value)
    {
        var sb = new StringBuilder(value.Length + (value.Length / 10));

        foreach (var ch in value)
        {
            switch (ch)
            {
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '&':
                    sb.Append("&amp;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&#39;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes characters that break HTML code spans while preserving emoji glyphs.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="text">The raw text to escape.</param>
    /// <returns>HTML-safe text with emoji preserved.</returns>
    private static string EscapeHtmlForCode(string text)
    {
        var result = text.Replace("&", "&amp;", StringComparison.Ordinal);
        result = result.Replace("<", "&lt;", StringComparison.Ordinal);
        result = result.Replace(">", "&gt;", StringComparison.Ordinal);
        result = result.Replace("|", "&#124;", StringComparison.Ordinal);
        return result;
    }
}
