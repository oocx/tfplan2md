using System;

namespace Oocx.TfPlan2Md.RenderTargets.AzureDevOps;

/// <summary>
/// Diff formatter for Azure DevOps Pull Request rendering using simple +/- notation.
/// </summary>
/// <remarks>
/// Azure DevOps markdown renderer handles simple before/after diffs using
/// lines with +/- prefixes separated by line breaks. HTML styling is not
/// needed and causes rendering issues in markdown tables.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal sealed class AzureDevOpsDiffFormatter : IDiffFormatter
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
    /// Builds a compact table-friendly diff line without code wrapping.
    /// </summary>
    /// <param name="escapedBefore">Escaped original value.</param>
    /// <param name="escapedAfter">Escaped updated value.</param>
    /// <returns>Formatted diff suitable for markdown tables.</returns>
    /// <remarks>
    /// Azure DevOps automatically colors lines starting with - and + in markdown tables.
    /// Backticks are not needed and actually prevent proper rendering.
    /// </remarks>
    private static string BuildSimpleDiffTable(string escapedBefore, string escapedAfter)
    {
        return $"- {escapedBefore}<br>+ {escapedAfter}";
    }

    /// <summary>
    /// Escapes markdown special characters to prevent rendering issues.
    /// </summary>
    /// <param name="value">Value to escape.</param>
    /// <returns>Escaped string safe for markdown rendering.</returns>
    /// <remarks>
    /// Note: + and - are NOT escaped because they're used for diff markers.
    /// Azure DevOps markdown renderer recognizes lines starting with - and + for diff coloring.
    /// </remarks>
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
