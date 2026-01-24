using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

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

        // Build inline diff with HTML styling
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
    /// <returns>Table-friendly inline diff string.</returns>
    private static string BuildInlineDiffTable(string before, string after)
    {
        var block = BuildInlineDiff(before, after);
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

    /// <summary>
    /// Builds an inline HTML diff block with styled line-level changes.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>HTML block showing line and character-level differences.</returns>
    private static string BuildInlineDiff(string before, string after)
    {
        // Delegate to ScribanHelpers for the actual diff computation
        // This uses the existing diff logic until we fully extract shared utilities
        // For now, we'll reconstruct it using the public FormatLargeValue method
        // This is a temporary approach - full extraction will happen when we move shared utilities
        return FormatLargeValue(before, after, "inline-diff")
            .Replace("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>", string.Empty, StringComparison.Ordinal)
            .Replace("</code></pre>", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
