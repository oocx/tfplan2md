using System;

namespace Oocx.TfPlan2Md.RenderTargets.GitHub;

/// <summary>
/// Diff formatter for GitHub Pull Request rendering using simple +/- notation.
/// </summary>
/// <remarks>
/// GitHub's markdown renderer handles simple before/after diffs well using
/// code-wrapped values with +/- prefixes separated by line breaks.
/// This format is more compact and readable in GitHub's UI.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal sealed class GitHubDiffFormatter : IDiffFormatter
{
    /// <inheritdoc />
    public string FormatDiff(string? before, string? after)
    {
        var beforeValue = before ?? string.Empty;
        var afterValue = after ?? string.Empty;

        // Return empty when both values are null or empty
        if (string.IsNullOrEmpty(beforeValue) && string.IsNullOrEmpty(afterValue))
        {
            return string.Empty;
        }

        // Return the unchanged value wrapped in code when both are identical
        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return WrapInlineCode(EscapeMarkdown(afterValue));
        }

        // Build simple diff with +/- notation
        return BuildSimpleDiffTable(EscapeMarkdown(beforeValue), EscapeMarkdown(afterValue));
    }

    /// <summary>
    /// Wraps content in a minimal inline code HTML tag, returning an empty string when the content is empty.
    /// </summary>
    /// <param name="content">Content to wrap.</param>
    /// <returns>Inline code HTML string.</returns>
    private static string WrapInlineCode(string content)
    {
        return string.IsNullOrEmpty(content) ? string.Empty : $"<code>{content}</code>";
    }

    /// <summary>
    /// Builds a compact table-friendly diff line with markdown code wrapping.
    /// </summary>
    /// <param name="escapedBefore">Escaped original value.</param>
    /// <param name="escapedAfter">Escaped updated value.</param>
    /// <returns>Formatted diff suitable for markdown tables.</returns>
    private static string BuildSimpleDiffTable(string escapedBefore, string escapedAfter)
    {
        return $"- `{escapedBefore}`<br>+ `{escapedAfter}`";
    }

    /// <summary>
    /// Escapes markdown special characters to prevent rendering issues.
    /// </summary>
    /// <param name="value">Value to escape.</param>
    /// <returns>Escaped string safe for markdown rendering.</returns>
    private static string EscapeMarkdown(string? value)
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
