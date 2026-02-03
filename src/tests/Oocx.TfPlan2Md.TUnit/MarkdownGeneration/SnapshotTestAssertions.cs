using System.Globalization;
using System.IO;
using System.Text;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Shared assertions for markdown snapshot tests.
/// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
/// </summary>
internal static class SnapshotTestAssertions
{
    /// <summary>
    /// Directory name that stores snapshot baselines within TestData.
    /// </summary>
    private const string SnapshotsDirectory = "Snapshots";

    /// <summary>
    /// Compares the actual output against the stored snapshot.
    /// </summary>
    /// <param name="snapshotName">The snapshot file name.</param>
    /// <param name="actual">The actual markdown output.</param>
    /// <remarks>
    /// If the snapshot file doesn't exist, creates it and fails the test with instructions.
    /// If the snapshot exists but differs, shows a detailed diff and fails.
    /// </remarks>
    internal static void AssertMatchesSnapshot(string snapshotName, string actual)
    {
        var snapshotPath = Path.Combine("TestData", SnapshotsDirectory, snapshotName);
        var absolutePath = Path.GetFullPath(snapshotPath);

        // Normalize line endings for cross-platform compatibility
        actual = NormalizeLineEndings(actual);

        if (!File.Exists(absolutePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, actual);

            Assert.Fail(
                "Snapshot '" + snapshotName + "' did not exist and has been created at:\n" +
                $"  {absolutePath}\n\n" +
                "Please review the generated snapshot and commit it if correct.\n" +
                "Re-run the test after committing to verify it passes.");
        }

        var expected = NormalizeLineEndings(File.ReadAllText(absolutePath));

        if (expected != actual)
        {
            var diff = GenerateDiff(expected, actual);

            Assert.Fail(
                "Snapshot '" + snapshotName + "' does not match the current output.\n\n" +
                "If this change is intentional:\n" +
                $"  1. Delete the snapshot file: {absolutePath}\n" +
                "  2. Re-run the test to regenerate it\n" +
                "  3. Review and commit the new snapshot\n\n" +
                $"Diff (first 50 differences):\n{diff}");
        }
    }

    /// <summary>
    /// Ensures emoji or pictographic characters are never followed by a regular breaking space (U+0020).
    /// </summary>
    /// <param name="markdown">The markdown to validate.</param>
    /// <param name="context">The snapshot name for error reporting.</param>
    internal static void AssertNoEmojiFollowedByRegularSpace(string markdown, string context)
    {
        var index = 0;
        while (index < markdown.Length)
        {
            var rune = Rune.GetRuneAt(markdown, index);
            var nextIndex = index + rune.Utf16SequenceLength;

            if (IsEmojiLike(rune)
                && TryBuildEmojiSpacingError(markdown, index, rune, context, out var errorMessage))
            {
                Assert.Fail(errorMessage);
            }

            index = nextIndex;
        }
    }

    /// <summary>
    /// Builds an error message when an emoji-like rune is followed by a regular space.
    /// </summary>
    /// <param name="markdown">The markdown being validated.</param>
    /// <param name="index">The index of the emoji-like rune.</param>
    /// <param name="rune">The emoji-like rune.</param>
    /// <param name="context">The snapshot context label.</param>
    /// <param name="errorMessage">The generated error message when invalid spacing is detected.</param>
    /// <returns><c>true</c> if invalid spacing was detected; otherwise <c>false</c>.</returns>
    private static bool TryBuildEmojiSpacingError(
        string markdown,
        int index,
        Rune rune,
        string context,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        var nextIndex = index + rune.Utf16SequenceLength;
        var lookahead = SkipPresentationModifiers(markdown, nextIndex);

        if (!IsRegularSpaceAfterEmoji(markdown, lookahead, out var nextNonSpace))
        {
            return false;
        }

        if (IsSpacingAllowed(markdown, nextNonSpace))
        {
            return false;
        }

        var snippet = BuildEmojiContextSnippet(markdown, index);
        var runeInfo = $"U+{rune.Value:X4} '{rune}'";
        errorMessage =
            $"Emoji or pictograph at position {index} ({runeInfo}) in {context} is followed by a regular space. " +
            "Icons must be followed by a non-breaking space (U+00A0). " +
            $"Context: '{snippet}'.";
        return true;
    }

    /// <summary>
    /// Advances past presentation modifiers that are part of an emoji sequence.
    /// </summary>
    /// <param name="markdown">The markdown being scanned.</param>
    /// <param name="startIndex">The index to start scanning.</param>
    /// <returns>The first index after any presentation modifiers.</returns>
    private static int SkipPresentationModifiers(string markdown, int startIndex)
    {
        var lookahead = startIndex;
        while (lookahead < markdown.Length)
        {
            var nextRune = Rune.GetRuneAt(markdown, lookahead);
            if (!IsPresentationModifier(nextRune))
            {
                break;
            }

            lookahead += nextRune.Utf16SequenceLength;
        }

        return lookahead;
    }

    /// <summary>
    /// Determines whether a regular space follows the emoji sequence.
    /// </summary>
    /// <param name="markdown">The markdown being scanned.</param>
    /// <param name="lookahead">The index after any emoji modifiers.</param>
    /// <param name="nextNonSpace">The next non-space character index.</param>
    /// <returns><c>true</c> if a regular space follows; otherwise <c>false</c>.</returns>
    private static bool IsRegularSpaceAfterEmoji(string markdown, int lookahead, out int nextNonSpace)
    {
        nextNonSpace = lookahead;
        if (lookahead >= markdown.Length || markdown[lookahead] != ' ')
        {
            return false;
        }

        nextNonSpace = SkipSpaces(markdown, lookahead);
        return true;
    }

    /// <summary>
    /// Skips over consecutive regular spaces.
    /// </summary>
    /// <param name="markdown">The markdown being scanned.</param>
    /// <param name="startIndex">The index to start scanning.</param>
    /// <returns>The index of the next non-space character.</returns>
    private static int SkipSpaces(string markdown, int startIndex)
    {
        var index = startIndex;
        while (index < markdown.Length && markdown[index] == ' ')
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Determines whether regular spacing is acceptable for the current emoji position.
    /// </summary>
    /// <param name="markdown">The markdown being scanned.</param>
    /// <param name="nextNonSpace">The index of the next non-space character.</param>
    /// <returns><c>true</c> if spacing is allowed; otherwise <c>false</c>.</returns>
    private static bool IsSpacingAllowed(string markdown, int nextNonSpace)
    {
        return nextNonSpace >= markdown.Length
            || markdown[nextNonSpace] is '|' or '\n' or '\r';
    }

    /// <summary>
    /// Builds a small snippet around the emoji for error diagnostics.
    /// </summary>
    /// <param name="markdown">The markdown being scanned.</param>
    /// <param name="index">The index of the emoji-like rune.</param>
    /// <returns>A short context snippet with line breaks visualized.</returns>
    private static string BuildEmojiContextSnippet(string markdown, int index)
    {
        var snippetStart = Math.Max(0, index - 5);
        var snippetLength = Math.Min(20, markdown.Length - snippetStart);
        return markdown.Substring(snippetStart, snippetLength).Replace("\n", "⏎");
    }

    /// <summary>
    /// Normalizes line endings to LF for cross-platform comparison.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Generates a line-by-line diff between expected and actual output.
    /// </summary>
    /// <param name="expected">The expected snapshot content.</param>
    /// <param name="actual">The actual rendered content.</param>
    /// <returns>A diff string limited to the first 50 differences.</returns>
    private static string GenerateDiff(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var sb = new StringBuilder();
        var diffCount = 0;
        const int maxDiffs = 50;

        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (var i = 0; i < maxLines && diffCount < maxDiffs; i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "(end of file)";
            var actualLine = i < actualLines.Length ? actualLines[i] : "(end of file)";

            if (expectedLine != actualLine)
            {
                diffCount++;
                sb.AppendLine(CultureInfo.InvariantCulture, $"Line {i + 1}:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  - {Truncate(expectedLine, 80)}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  + {Truncate(actualLine, 80)}");
            }
        }

        if (diffCount >= maxDiffs)
        {
            sb.AppendLine("... (more differences truncated)");
        }

        if (diffCount == 0)
        {
            sb.AppendLine("(Whitespace-only differences detected)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Expected length: {expected.Length}, Actual length: {actual.Length}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if needed.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length before truncation.</param>
    /// <returns>The truncated string.</returns>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        value = value.Replace("\t", "→").Replace(" ", "·");

        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, maxLength - 3), "...");
    }

    /// <summary>
    /// Detects emoji-like runes by Unicode category to avoid missing semantic icons in markdown.
    /// </summary>
    /// <param name="rune">The rune to inspect.</param>
    /// <returns><c>true</c> if the rune is emoji-like; otherwise <c>false</c>.</returns>
    private static bool IsEmojiLike(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        return category is UnicodeCategory.OtherSymbol && rune.Value >= 0x2600;
    }

    /// <summary>
    /// Skips over variation selectors or joiners that are part of an emoji sequence.
    /// </summary>
    /// <param name="rune">The rune to inspect.</param>
    /// <returns><c>true</c> if the rune is a presentation modifier; otherwise <c>false</c>.</returns>
    private static bool IsPresentationModifier(Rune rune)
    {
        return rune.Value is 0xFE0F or 0xFE0E or 0x200D
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.EnclosingMark
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format;
    }
}
