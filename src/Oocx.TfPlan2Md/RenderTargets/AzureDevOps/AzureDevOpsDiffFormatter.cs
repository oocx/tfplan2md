using System;
using Oocx.TfPlan2Md.RenderTargets;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.RenderTargets.AzureDevOps;

/// <summary>
/// Diff formatter for Azure DevOps Pull Request rendering using inline HTML diffs.
/// </summary>
/// <remarks>
/// Azure DevOps supports rich HTML rendering in markdown, allowing for styled
/// inline diffs with character-level highlighting. This provides a more detailed
/// visual representation of changes than simple +/- notation.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal sealed class AzureDevOpsDiffFormatter : IDiffFormatter
{
    /// <summary>
    /// Maximum length for the short single-line fast path.
    /// Values shorter than this bypass the full LCS pipeline.
    /// </summary>
    private const int FastPathMaxLength = 50;

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
            return WrapInlineCode(afterValue.EscapeMarkdown());
        }

        // Fast path: short single-line values don't need LCS character-level diffing
        if (!beforeValue.Contains('\n') && !afterValue.Contains('\n')
            && beforeValue.Length < FastPathMaxLength && afterValue.Length < FastPathMaxLength)
        {
            return WrapInlineDiffCode(
                $"<span style=\"background-color: #fff5f5; color: #d73a49;\">- {HtmlEncode(beforeValue)}</span><br>"
                + $"<span style=\"background-color: #f0fff4; color: #28a745;\">+ {HtmlEncode(afterValue)}</span>");
        }

        // Full LCS pipeline for multi-line or large values
        return WrapInlineDiffCode(BuildInlineDiffTable(beforeValue, afterValue));
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
    private static string WrapInlineDiffCode(string content)
    {
        return string.IsNullOrEmpty(content)
            ? string.Empty
            : $"<code style=\"display:block; white-space:normal; padding:0; margin:0;\">{content}</code>";
    }

    /// <summary>
    /// Creates an inline diff representation suitable for embedding in markdown tables.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>Table-friendly inline diff string with HTML character-level highlighting.</returns>
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
