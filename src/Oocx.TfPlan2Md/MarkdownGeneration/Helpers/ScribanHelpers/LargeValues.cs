using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

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
            // At this point, normalizedBefore is guaranteed non-null
            var formattedBefore = NormalizeStructuredValue(normalizedBefore!, out var language);
            return CodeFence(formattedBefore, language);
        }

        if (normalizedBefore is null)
        {
            // At this point, normalizedAfter is guaranteed non-null
            var formattedAfter = NormalizeStructuredValue(normalizedAfter, out var language);
            return CodeFence(formattedAfter, language);
        }

        var diffBefore = NormalizeStructuredValue(normalizedBefore, out _);
        var diffAfter = NormalizeStructuredValue(normalizedAfter, out _);

        return parsedFormat switch
        {
            LargeValueFormat.SimpleDiff => BuildSimpleDiff(diffBefore, diffAfter),
            _ => BuildInlineDiff(diffBefore, diffAfter)
        };
    }

    /// <summary>
    /// Normalizes structured JSON or XML content for rendering, returning the formatted content and language.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="language">The detected language for syntax highlighting.</param>
    /// <returns>Formatted content suitable for rendering.</returns>
    private static string NormalizeStructuredValue(string value, out string? language)
    {
        if (TryFormatStructuredContent(value, out var formatted, out language))
        {
            return formatted;
        }

        language = null;
        return value;
    }

    /// <summary>
    /// Attempts to parse JSON or XML content and return a formatted version and language marker.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="value">Raw input content.</param>
    /// <param name="formatted">Formatted output when parsing succeeds.</param>
    /// <param name="language">Language identifier for syntax highlighting.</param>
    /// <returns>True when the content was parsed as JSON or XML; otherwise false.</returns>
    private static bool TryFormatStructuredContent(string value, out string formatted, out string? language)
    {
        if (TryFormatJson(value, out formatted))
        {
            language = "json";
            return true;
        }

        if (TryFormatXml(value, out formatted))
        {
            language = "xml";
            return true;
        }

        language = null;
        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to pretty-print JSON while preserving existing formatting when detected.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="value">Raw JSON content.</param>
    /// <param name="formatted">Formatted JSON output.</param>
    /// <returns>True when JSON parsing succeeds.</returns>
    private static bool TryFormatJson(string value, out string formatted)
    {
        var trimmed = value.Trim();
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var pretty = FormatJson(document.RootElement);
            formatted = IsAlreadyFormatted(trimmed) ? trimmed : pretty;
            return true;
        }
        catch (JsonException)
        {
            formatted = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Attempts to pretty-print XML while preserving existing formatting when detected.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="value">Raw XML content.</param>
    /// <param name="formatted">Formatted XML output.</param>
    /// <returns>True when XML parsing succeeds.</returns>
    private static bool TryFormatXml(string value, out string formatted)
    {
        var trimmed = value.Trim();
        try
        {
            var document = XDocument.Parse(trimmed);
            var pretty = document.ToString();
            formatted = IsAlreadyFormatted(trimmed) ? trimmed : pretty;
            return true;
        }
        catch (Exception)
        {
            formatted = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Determines whether structured content already appears formatted with line breaks and indentation.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="content">Content to inspect.</param>
    /// <returns>True when the content appears already formatted.</returns>
    private static bool IsAlreadyFormatted(string content)
    {
        var normalized = NormalizeLineEndings(content);
        var lines = normalized.Split('\n', StringSplitOptions.None);
        if (lines.Length < 2)
        {
            return false;
        }

        return lines.Skip(1).Any(line => line.StartsWith(' ') || line.StartsWith('\t'));
    }

    /// <summary>
    /// Formats JSON using a writer to avoid serializer trimming warnings.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="element">The JSON element to format.</param>
    /// <returns>Indented JSON string.</returns>
    private static string FormatJson(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        element.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Normalizes line endings to simplify formatting heuristics.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="value">Raw content.</param>
    /// <returns>Content with normalized line endings.</returns>
    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
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
            }
        }

        sb.Append("</code></pre>");
        return sb.ToString();
    }

}
