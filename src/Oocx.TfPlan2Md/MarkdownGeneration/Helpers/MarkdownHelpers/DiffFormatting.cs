namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Diff formatting helpers for before/after rendering.
/// </summary>
internal static partial class MarkdownHelpers
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
            // Special case: bare dash should not be wrapped in code tags
            if (afterValue == "-")
            {
                return "-";
            }
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
    /// Wraps inline diff content in a block-style code tag suitable for markdown tables.
    /// </summary>
    /// <param name="content">Diff content to wrap.</param>
    /// <returns>HTML block containing the diff content.</returns>
    /// <remarks>
    /// Wraps the HTML diff content in a styled code tag for proper rendering in markdown tables.
    /// The block-style display ensures the diff maintains proper formatting within table cells.
    /// </remarks>
    private static string WrapInlineDiffCode(string content)
    {
        return string.IsNullOrEmpty(content)
            ? string.Empty
            : $"<code style=\"display:block; white-space:normal; padding:0; margin:0;\">{content}</code>";
    }

    /// <summary>
    /// Builds a compact table-friendly diff line without code wrapping.
    /// </summary>
    /// <param name="escapedBefore">Escaped original value.</param>
    /// <param name="escapedAfter">Escaped updated value.</param>
    /// <returns>Formatted diff suitable for markdown tables.</returns>
    /// <remarks>
    /// GitHub and Azure DevOps automatically color lines starting with - and + in markdown tables.
    /// Backticks are not needed and actually prevent proper rendering.
    /// Uses &lt;br&gt; tag for line breaks which markdown parsers accept in table cells.
    /// </remarks>
    private static string BuildSimpleDiffTable(string escapedBefore, string escapedAfter)
    {
        return $"- {escapedBefore}<br>+ {escapedAfter}";
    }

    /// <summary>
    /// Creates an inline diff representation suitable for embedding in markdown tables using HTML formatting.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>Table-friendly inline diff string with HTML character-level highlighting.</returns>
    /// <remarks>
    /// Uses FormatLargeValue with "inline-diff" format to generate rich HTML spans with character-level diffs.
    /// The output is adapted for table cells by removing the pre/code wrapper and converting newlines to br tags.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </remarks>
    private static string BuildInlineDiffTable(string before, string after)
    {
        // Use FormatLargeValue to generate the full HTML diff with character-level highlighting
        var block = FormatLargeValue(before, after, "inline-diff");

        // Adapt the output for table cells by:
        // 1. Remove the <pre><code> wrapper
        // 2. Change "display: block" to "display: inline-block" for table compatibility
        // 3. Replace newlines with <br> tags
        var content = block
            .Replace("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>", string.Empty, StringComparison.Ordinal)
            .Replace("</code></pre>", string.Empty, StringComparison.Ordinal)
            .Replace("display: block;", "display: inline-block;", StringComparison.Ordinal);

        content = content.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", "<br>", StringComparison.Ordinal);

        if (content.EndsWith("<br>", StringComparison.Ordinal))
        {
            content = content[..^4];
        }

        return content;
    }
}
