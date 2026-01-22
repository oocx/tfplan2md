using System.Text;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Helpers for large value rendering and inline/simple diffs.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Formats large attribute values according to the requested rendering format.
    /// Related feature: docs/features/006-large-attribute-value-display/specification.md.
    /// </summary>
    /// <param name="before">The value before the change (may be null or whitespace).</param>
    /// <param name="after">The value after the change (may be null or whitespace).</param>
    /// <param name="format">The rendering format to use (e.g., "inline-diff", "simple-diff").</param>
    /// <returns>Formatted markdown string representing the value change.</returns>
    public static string FormatLargeValue(string? before, string? after, string format)
    {
        var normalizedBefore = string.IsNullOrWhiteSpace(before) ? null : before;
        var normalizedAfter = string.IsNullOrWhiteSpace(after) ? null : after;
        var parsedFormat = ParseLargeValueFormat(format);

        if (normalizedAfter is null && normalizedBefore is null)
        {
            return string.Empty;
        }

        if (normalizedAfter is null)
        {
            return CodeFence(normalizedBefore ?? string.Empty);
        }

        if (normalizedBefore is null)
        {
            return CodeFence(normalizedAfter);
        }

        return parsedFormat switch
        {
            LargeValueFormat.SimpleDiff => BuildSimpleDiff(normalizedBefore, normalizedAfter),
            _ => BuildInlineDiff(normalizedBefore, normalizedAfter)
        };
    }

    /// <summary>
    /// Parses the requested large value format string into a strongly typed enum.
    /// </summary>
    /// <param name="format">Format string provided by the template.</param>
    /// <returns>Normalized large value format.</returns>
    /// <exception cref="ScribanHelperException">Thrown when an unsupported format is provided.</exception>
    private static LargeValueFormat ParseLargeValueFormat(string format)
    {
        var normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        var compact = normalized
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        return compact switch
        {
            "" => LargeValueFormat.InlineDiff,
            "inlinediff" => LargeValueFormat.InlineDiff,
            "simplediff" => LargeValueFormat.SimpleDiff,
            _ => throw new ScribanHelperException("Unsupported large value format. Use 'inline-diff' or 'simple-diff'.")
        };
    }

    /// <summary>
    /// Builds a fenced code block using the provided content and optional language identifier.
    /// </summary>
    /// <param name="content">Code body to include inside the fence.</param>
    /// <param name="language">Optional language identifier for syntax highlighting.</param>
    /// <returns>Markdown code fence string.</returns>
    private static string CodeFence(string content, string? language = null)
    {
        var fenceLang = string.IsNullOrWhiteSpace(language) ? string.Empty : language;
        var sb = new StringBuilder();
        sb.Append("```");
        sb.AppendLine(fenceLang);
        sb.AppendLine(content);
        sb.Append("```");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a simple diff fenced block showing removed and added lines.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>Diff-formatted code fence.</returns>
    private static string BuildSimpleDiff(string before, string after)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```diff");

        foreach (var line in SplitLines(before))
        {
            sb.Append("- ");
            sb.AppendLine(line);
        }

        foreach (var line in SplitLines(after))
        {
            sb.Append("+ ");
            sb.AppendLine(line);
        }

        sb.Append("```");
        return sb.ToString();
    }

    /// <summary>
    /// Splits content into lines, normalizing carriage returns.
    /// </summary>
    /// <param name="value">Content to split.</param>
    /// <returns>Array of lines.</returns>
    private static string[] SplitLines(string value)
    {
        return value.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);
    }

    /// <summary>
    /// Builds an inline HTML diff block with styled line-level changes.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>HTML block showing line and character-level differences.</returns>
    private static string BuildInlineDiff(string before, string after)
    {
        var beforeLines = SplitLines(before);
        var afterLines = SplitLines(after);
        var diff = BuildLineDiff(beforeLines, afterLines);
        var sb = new StringBuilder();
        sb.Append("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");

        var index = 0;
        while (index < diff.Count)
        {
            var entry = diff[index];
            if (entry.Kind == DiffKind.Unchanged)
            {
                sb.Append(HtmlEncode(entry.Text));
                sb.Append('\n');
                index++;
                continue;
            }

            if (entry.Kind == DiffKind.Removed && index + 1 < diff.Count && diff[index + 1].Kind == DiffKind.Added)
            {
                var addEntry = diff[index + 1];
                AppendStyledLineWithCharDiff(sb, entry.Text, addEntry.Text, removed: true);
                AppendStyledLineWithCharDiff(sb, addEntry.Text, entry.Text, removed: false);
                index += 2;
                continue;
            }

            if (entry.Kind == DiffKind.Removed)
            {
                AppendStyledLine(sb, entry.Text, removed: true);
                index++;
                continue;
            }

            if (entry.Kind == DiffKind.Added)
            {
                AppendStyledLine(sb, entry.Text, removed: false);
                index++;
                continue;
            }
        }

        sb.Append("</code></pre>");
        return sb.ToString();
    }

}
