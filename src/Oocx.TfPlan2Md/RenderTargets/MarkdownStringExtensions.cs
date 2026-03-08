namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// String extension methods for escaping markdown special characters.
/// </summary>
/// <remarks>
/// This extension provides a broader escape set than <c>MarkdownHelpers.EscapeMarkdown</c>:
/// all characters that could trigger markdown rendering in a code-span or table cell are
/// escaped so that raw values are displayed literally.
/// </remarks>
internal static class MarkdownStringExtensions
{
    /// <summary>
    /// Escapes markdown special characters so that a raw value renders literally inside a
    /// code span or markdown table cell.
    /// </summary>
    /// <param name="value">The value to escape, or <c>null</c>.</param>
    /// <returns>The escaped string, or an empty string when <paramref name="value"/> is null or empty.</returns>
    /// <remarks>
    /// The full character set (including <c>+</c> and <c>-</c>) is escaped here because
    /// callers wrap values in HTML code spans; the +/- markers are emitted by the caller
    /// outside the escaped value, not by the value content.
    /// </remarks>
    internal static string EscapeMarkdown(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("{", "\\{", StringComparison.Ordinal)
            .Replace("}", "\\}", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal)
            .Replace("#", "\\#", StringComparison.Ordinal)
            .Replace("+", "\\+", StringComparison.Ordinal)
            .Replace("-", "\\-", StringComparison.Ordinal)
            .Replace(".", "\\.", StringComparison.Ordinal)
            .Replace("!", "\\!", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }
}
