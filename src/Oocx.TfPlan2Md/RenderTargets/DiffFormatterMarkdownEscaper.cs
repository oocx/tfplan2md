using System;

namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Escapes markdown-sensitive characters for diff formatter output.
/// </summary>
/// <remarks>
/// This helper remains scoped to <c>RenderTargets</c> because it preserves the historical escaping
/// behavior used by render-target-specific diff output without broadening it into a general markdown
/// utility. Related feature: docs/features/112-low-risk-code-quality-improvements/specification.md.
/// </remarks>
internal static class DiffFormatterMarkdownEscaper
{
    /// <summary>
    /// Escapes the current shared markdown character set used by diff formatters.
    /// </summary>
    /// <param name="value">The value to escape for render-target diff output.</param>
    /// <returns>The escaped value, or an empty string when the input is null or empty.</returns>
    internal static string Escape(string? value)
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
