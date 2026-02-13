namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Diff formatting helpers for before/after rendering.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Formats a before/after pair into a diff-style string while preserving table compatibility.
    /// Related features: docs/features/005-firewall-rule-before-after-display/specification.md, docs/features/003-consistent-value-formatting/specification.md.
    /// </summary>
    /// <param name="before">The original value.</param>
    /// <param name="after">The updated value.</param>
    /// <param name="format">Diff format: "inline-diff" or "simple-diff".</param>
    /// <returns>Code-formatted output containing plain markdown with +/- markers. Returns empty when both values are null or empty.</returns>
    public static string FormatDiff(string? before, string? after, string format)
    {
        var beforeValue = before ?? string.Empty;
        var afterValue = after ?? string.Empty;

        var parsedFormat = ParseLargeValueFormat(format);

        if (string.IsNullOrEmpty(beforeValue) && string.IsNullOrEmpty(afterValue))
        {
            return string.Empty;
        }

        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return WrapInlineCode(EscapeMarkdown(afterValue));
        }

        return parsedFormat switch
        {
            LargeValueFormat.SimpleDiff => BuildSimpleDiffTable(EscapeMarkdown(beforeValue), EscapeMarkdown(afterValue)),
            _ => WrapInlineDiffCode(BuildInlineDiffTable(beforeValue, afterValue))
        };
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
    /// Wraps inline diff content for markdown tables (passthrough since we use plain markdown now).
    /// </summary>
    /// <param name="content">Diff content to wrap.</param>
    /// <returns>The content as-is for plain markdown diff rendering.</returns>
    /// <remarks>
    /// Changed from HTML-wrapped content to plain markdown to fix rendering issues in tables.
    /// Markdown tables don't support complex HTML styling, so we use simple - and + prefixes.
    /// </remarks>
    private static string WrapInlineDiffCode(string content)
    {
        return content;
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
    /// Creates an inline diff representation suitable for embedding in markdown tables using plain markdown format.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>Table-friendly inline diff string with - and + prefixes.</returns>
    /// <remarks>
    /// Uses plain markdown diff format (- old / + new) instead of HTML styling.
    /// GitHub and Azure DevOps markdown renderers handle coloring automatically for lines starting with - and +.
    /// This approach ensures diffs display correctly in markdown tables without HTML tags appearing as literal text.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </remarks>
    private static string BuildInlineDiffTable(string before, string after)
    {
        // Use simple markdown diff format for table compatibility
        // Escape markdown characters to prevent rendering issues
        var escapedBefore = EscapeMarkdown(before);
        var escapedAfter = EscapeMarkdown(after);

        return $"- {escapedBefore}<br>+ {escapedAfter}";
    }
}
