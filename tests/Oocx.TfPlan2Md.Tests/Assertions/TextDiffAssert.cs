using System.Globalization;
using System.Text;

namespace Oocx.TfPlan2Md.Tests.Assertions;

/// <summary>
/// Assertion helpers for producing actionable diffs when comparing multi-line text.
/// Related feature: docs/features/030-terraform-show-approximation/
/// </summary>
internal static class TextDiffAssert
{
    /// <summary>
    /// Compares two strings line-by-line after trimming leading whitespace.
    /// When mismatched, throws with a message that includes the first differing line/column and a small context window.
    /// </summary>
    /// <param name="expected">The expected text.</param>
    /// <param name="actual">The actual text.</param>
    /// <param name="contextLines">Number of lines to show before/after the first differing line.</param>
    internal static void EqualIgnoringLeadingWhitespace(string expected, string actual, int contextLines = 3)
    {
        var expectedLines = CollapseConsecutiveBlankLines(NormalizeLines(expected)).Select(static l => l.TrimStart()).ToList();
        var actualLines = CollapseConsecutiveBlankLines(NormalizeLines(actual)).Select(static l => l.TrimStart()).ToList();


        var firstDifference = FindFirstDifference(expectedLines, actualLines);
        if (firstDifference is null)
        {
            return;
        }

        var (lineIndex, expectedLine, actualLine) = firstDifference.Value;
        var diffColumn = FindFirstDifferenceColumn(expectedLine, actualLine);

        var message = BuildMismatchMessage(
            expectedLines,
            actualLines,
            lineIndex,
            expectedLine,
            actualLine,
            diffColumn,
            contextLines,
            normalizationDescription: "Leading whitespace trimmed");

        Assert.Fail(message);
    }

    /// <summary>
    /// Splits text into lines in a consistent way, normalizing Windows line endings.
    /// </summary>
    /// <param name="text">Text to normalize.</param>
    /// <returns>Normalized lines without trailing carriage returns.</returns>
    private static string[] NormalizeLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return normalized.Split('\n');
    }

    /// <summary>
    /// Collapses consecutive blank lines into a single blank line to make tests
    /// tolerant to minor differences in blank-line spacing while still detecting
    /// duplicated blank lines.
    /// </summary>
    /// <param name="lines">Lines to collapse.</param>
    /// <returns>New array with collapsed blank lines.</returns>
    private static string[] CollapseConsecutiveBlankLines(string[] lines)
    {
        var list = new List<string>(lines.Length);
        var previousWasBlank = false;
        foreach (var line in lines)
        {
            var isBlank = string.IsNullOrWhiteSpace(line);
            if (isBlank)
            {
                if (!previousWasBlank)
                {
                    list.Add(string.Empty);
                    previousWasBlank = true;
                }
            }
            else
            {
                list.Add(line);
                previousWasBlank = false;
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Finds the first line index where the two line collections differ.
    /// </summary>
    /// <param name="expectedLines">Expected lines.</param>
    /// <param name="actualLines">Actual lines.</param>
    /// <returns>The first differing line index and the respective line values.</returns>
    private static (int LineIndex, string? ExpectedLine, string? ActualLine)? FindFirstDifference(
        List<string> expectedLines,
        List<string> actualLines)
    {
        var maxLines = Math.Max(expectedLines.Count, actualLines.Count);

        for (var i = 0; i < maxLines; i++)
        {
            var hasExpected = i < expectedLines.Count;
            var hasActual = i < actualLines.Count;

            var expectedLine = hasExpected ? expectedLines[i] : null;
            var actualLine = hasActual ? actualLines[i] : null;

            if (!hasExpected || !hasActual)
            {
                return (i, expectedLine, actualLine);
            }

            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                return (i, expectedLine, actualLine);
            }
        }

        return null;
    }

    /// <summary>
    /// Computes a 1-based column index of the first differing character for two strings.
    /// Returns 1 when one side is missing.</summary>
    /// <param name="expectedLine">Expected line text or null.</param>
    /// <param name="actualLine">Actual line text or null.</param>
    /// <returns>1-based column index of first difference.</returns>
    private static int FindFirstDifferenceColumn(string? expectedLine, string? actualLine)
    {
        if (expectedLine is null || actualLine is null)
        {
            return 1;
        }

        var min = Math.Min(expectedLine.Length, actualLine.Length);
        for (var i = 0; i < min; i++)
        {
            if (expectedLine[i] != actualLine[i])
            {
                return i + 1;
            }
        }

        return min + 1;
    }

    /// <summary>
    /// Builds a message describing the first mismatch and surrounding context.
    /// </summary>
    /// <param name="expectedLines">Expected lines.</param>
    /// <param name="actualLines">Actual lines.</param>
    /// <param name="lineIndex">0-based line index of first mismatch.</param>
    /// <param name="expectedLine">Expected line text.</param>
    /// <param name="actualLine">Actual line text.</param>
    /// <param name="diffColumn">1-based column index of first mismatch.</param>
    /// <param name="contextLines">Lines of context before/after the mismatch to include.</param>
    /// <param name="normalizationDescription">Human-readable description of normalization applied.</param>
    /// <returns>Formatted mismatch message.</returns>
    private static string BuildMismatchMessage(
        List<string> expectedLines,
        List<string> actualLines,
        int lineIndex,
        string? expectedLine,
        string? actualLine,
        int diffColumn,
        int contextLines,
        string normalizationDescription)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Content mismatch.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Normalization: {normalizationDescription}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Expected lines: {expectedLines.Count}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Actual lines:   {actualLines.Count}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"First difference: line {lineIndex + 1}, column {diffColumn}");
        sb.AppendLine();

        AppendContext(sb, expectedLines, actualLines, lineIndex, contextLines);

        sb.AppendLine();
        sb.AppendLine("First differing line (escaped):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  - expected: '{EscapeForDebug(expectedLine)}'");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  + actual:   '{EscapeForDebug(actualLine)}'");

        sb.AppendLine();
        sb.AppendLine("Codepoint diagnostics at first difference:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  - expected length: {expectedLine?.Length ?? 0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  + actual length:   {actualLine?.Length ?? 0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  - expected @ col {diffColumn}: {DescribeCodepointAt(expectedLine, diffColumn)}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  + actual   @ col {diffColumn}: {DescribeCodepointAt(actualLine, diffColumn)}");

        sb.AppendLine();
        sb.AppendLine("Codepoints near difference (window):");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  - expected: {DescribeCodepointWindow(expectedLine, diffColumn, radius: 8)}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  + actual:   {DescribeCodepointWindow(actualLine, diffColumn, radius: 8)}");

        if (expectedLine is not null || actualLine is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Difference marker:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {new string(' ', Math.Max(0, diffColumn - 1))}^");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Appends a small around-the-failure context with line numbers.
    /// </summary>
    /// <param name="sb">Target string builder.</param>
    /// <param name="expectedLines">Expected lines.</param>
    /// <param name="actualLines">Actual lines.</param>
    /// <param name="lineIndex">First differing line index.</param>
    /// <param name="contextLines">Number of context lines before/after.</param>
    private static void AppendContext(
        StringBuilder sb,
        List<string> expectedLines,
        List<string> actualLines,
        int lineIndex,
        int contextLines)
    {
        sb.AppendLine("Context (escaped, expected vs actual):");

        var start = Math.Max(0, lineIndex - contextLines);
        var end = Math.Min(Math.Max(expectedLines.Count, actualLines.Count) - 1, lineIndex + contextLines);

        for (var i = start; i <= end; i++)
        {
            var expected = i < expectedLines.Count ? expectedLines[i] : null;
            var actual = i < actualLines.Count ? actualLines[i] : null;
            var marker = i == lineIndex ? '>' : ' ';

            sb.AppendLine(CultureInfo.InvariantCulture, $"{marker} L{i + 1:0000} - expected: '{EscapeForDebug(expected)}'");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{marker} L{i + 1:0000} + actual:   '{EscapeForDebug(actual)}'");
        }
    }

    /// <summary>
    /// Escapes tabs and control characters to make whitespace differences visible.
    /// </summary>
    /// <param name="value">Value to escape.</param>
    /// <returns>Escaped value (or &lt;null&gt;).</returns>
    private static string EscapeForDebug(string? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }

    /// <summary>
    /// Describes the Unicode codepoint at the given 1-based column.
    /// This makes invisible differences (e.g., trailing spaces) observable.
    /// </summary>
    /// <param name="line">Line content.</param>
    /// <param name="column">1-based column.</param>
    /// <returns>Human-readable codepoint description.</returns>
    private static string DescribeCodepointAt(string? line, int column)
    {
        if (line is null)
        {
            return "<null>";
        }

        if (column <= 0)
        {
            return "<invalid column>";
        }

        var index = column - 1;
        if (index >= line.Length)
        {
            return "<EOL>";
        }

        var c = line[index];
        return $"U+{(int)c:X4} '{EscapeForDebug(c.ToString(CultureInfo.InvariantCulture))}'";
    }

    /// <summary>
    /// Describes a small window of codepoints around the given 1-based column.
    /// </summary>
    /// <param name="line">Line content.</param>
    /// <param name="column">1-based column to center around.</param>
    /// <param name="radius">Number of characters before/after to include.</param>
    /// <returns>Compact window description.</returns>
    private static string DescribeCodepointWindow(string? line, int column, int radius)
    {
        if (line is null)
        {
            return "<null>";
        }

        if (line.Length == 0)
        {
            return "<empty>";
        }

        var index = Math.Clamp(column - 1, 0, line.Length);
        var start = Math.Max(0, index - radius);
        var endExclusive = Math.Min(line.Length, index + radius);

        var sb = new StringBuilder();
        for (var i = start; i < endExclusive; i++)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            var c = line[i];
            sb.Append(CultureInfo.InvariantCulture, $"[{i + 1}:U+{(int)c:X4}]");
        }

        if (index >= line.Length)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(CultureInfo.InvariantCulture, $"[{line.Length + 1}:<EOL>]");
        }

        return sb.ToString();
    }
}
