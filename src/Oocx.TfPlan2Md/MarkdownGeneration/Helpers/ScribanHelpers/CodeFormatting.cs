namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Code formatting helpers for markdown and HTML contexts.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Formats text as HTML code for usage inside summary tags where markdown backticks are unreliable.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
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
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
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

    /// <summary>
    /// Formats child resource table values, preserving HTML diffs while wrapping plain text in backticks.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    /// <param name="value">The value from the child row extractor (may be HTML diff or plain text).</param>
    /// <returns>Formatted value suitable for markdown tables - HTML diffs pass through, plain values get backticks.</returns>
    /// <remarks>
    /// Child row extractors return two types of values:
    /// 1. HTML-formatted diffs (from FormatDiff) containing code/span tags - these pass through unchanged
    /// 2. Plain formatted values (from FormatAttributeValueTableWithRegistry) already with backticks - these pass through
    /// 3. Unformatted plain text - these need backtick wrapping
    /// This method ensures consistent backtick usage across all non-diff values.
    /// </remarks>
    public static string FormatChildValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        // Special case: FormatDiff can return <code>\`value\`</code> when values are equal
        // (escaped backticks inside code tags). Remove the escaped backticks.
        if (value.Contains(@"\`", StringComparison.Ordinal))
        {
            value = value.Replace(@"\`", string.Empty, StringComparison.Ordinal);
        }

        // If the value contains HTML tags (diff or code formatting), pass it through unchanged
        if (value.Contains("<code", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("<span", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("</code>", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        // If the value is a simple diff (starts with "- " and contains <br>), pass it through unchanged
        // This is the GitHub simple diff format: "- value1<br>+ value2"
        if (value.StartsWith("- ", StringComparison.Ordinal) && value.Contains("<br>", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        // If the value already has backticks (from FormatAttributeValueTableWithRegistry), pass it through
        if (value.StartsWith('`') && value.EndsWith('`'))
        {
            return value;
        }

        // Plain text values without formatting need wrapping
        // Special case: bare dash should stay as-is
        if (value == "-")
        {
            return value;
        }

        // Wrap in backticks
        return $"`{value}`";
    }
}
