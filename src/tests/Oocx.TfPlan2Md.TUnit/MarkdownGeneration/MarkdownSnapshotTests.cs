using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Snapshot (golden file) tests that detect unexpected changes in markdown output.
/// These tests compare the current output against a previously approved baseline,
/// catching subtle regressions that other tests might miss.
/// </summary>
/// <remarks>
/// Snapshot testing works by:
/// 1. Generating markdown output from a fixed input
/// 2. Comparing against a stored "golden" file
/// 3. Failing if there are any differences
/// 
/// When intentional changes are made, developers update the snapshot files.
/// This provides a safety net against accidental output changes.
/// 
/// The snapshots directory stores the expected outputs, organized by test name.
/// </remarks>
public class MarkdownSnapshotTests
{
    private readonly TerraformPlanParser _parser = new();
    private const string SnapshotsDirectory = "Snapshots";

    /// <summary>
    /// Verifies the comprehensive demo output matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_ComprehensiveDemo_MatchesBaseline()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var principalMapper = new PrincipalMapper(DemoPaths.DemoPrincipalsPath);
        var model = new ReportModelBuilder(principalMapper: principalMapper, metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer(principalMapper);

        var markdown = renderer.Render(model);

        AssertNoEmojiFollowedByRegularSpace(markdown, "comprehensive-demo.md");
        AssertMatchesSnapshot("comprehensive-demo.md", markdown);
    }

    /// <summary>
    /// Verifies the summary template output matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_SummaryTemplate_MatchesBaseline()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder(metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model, "summary");

        AssertNoEmojiFollowedByRegularSpace(markdown, "summary-template.md");
        AssertMatchesSnapshot("summary-template.md", markdown);
    }

    /// <summary>
    /// Verifies the breaking plan output matches the approved snapshot.
    /// This ensures escaping behavior is consistent.
    /// </summary>
    [Test]
    public void Snapshot_BreakingPlan_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder(metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertNoEmojiFollowedByRegularSpace(markdown, "breaking-plan.md");
        AssertMatchesSnapshot("breaking-plan.md", markdown);
    }

    /// <summary>
    /// Verifies role assignment rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_RoleAssignments_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/role-assignments.json");
        var plan = _parser.Parse(json);
        var principalMapper = new PrincipalMapper(DemoPaths.DemoPrincipalsPath);
        var model = new ReportModelBuilder(principalMapper: principalMapper, metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer(principalMapper);

        var markdown = renderer.Render(model);

        AssertNoEmojiFollowedByRegularSpace(markdown, "role-assignments.md");
        AssertMatchesSnapshot("role-assignments.md", markdown);
    }

    /// <summary>
    /// Verifies firewall rule rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_FirewallRules_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder(metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertNoEmojiFollowedByRegularSpace(markdown, "firewall-rules.md");
        AssertMatchesSnapshot("firewall-rules.md", markdown);
    }

    /// <summary>
    /// Verifies multi-module plan rendering matches the approved snapshot.
    /// </summary>
    [Test]
    public void Snapshot_MultiModule_MatchesBaseline()
    {
        var json = File.ReadAllText("TestData/multi-module-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder(metadataProvider: TestMetadataProvider.Instance).Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);

        AssertNoEmojiFollowedByRegularSpace(markdown, "multi-module.md");
        AssertMatchesSnapshot("multi-module.md", markdown);
    }

    /// <summary>
    /// Compares the actual output against the stored snapshot.
    /// </summary>
    /// <param name="snapshotName">The name of the snapshot file.</param>
    /// <param name="actual">The actual markdown output.</param>
    /// <remarks>
    /// If the snapshot file doesn't exist, creates it and fails the test with instructions.
    /// If the snapshot exists but differs, shows a detailed diff and fails.
    /// 
    /// To update snapshots after intentional changes:
    /// 1. Delete the old snapshot file
    /// 2. Run the test (it will create a new snapshot)
    /// 3. Review and commit the new snapshot.
    /// </remarks>
    private void AssertMatchesSnapshot(string snapshotName, string actual)
    {
        var snapshotPath = Path.Combine("TestData", SnapshotsDirectory, snapshotName);
        var absolutePath = Path.GetFullPath(snapshotPath);

        // Normalize line endings for cross-platform compatibility
        actual = NormalizeLineEndings(actual);

        if (!File.Exists(absolutePath))
        {
            // Create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            // Write the new snapshot
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
            // Generate a helpful diff
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
    /// Normalizes line endings to LF for cross-platform comparison.
    /// </summary>
    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Generates a simple line-by-line diff between expected and actual.
    /// </summary>
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
            // Content is same but maybe whitespace differs
            sb.AppendLine("(Whitespace-only differences detected)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Expected length: {expected.Length}, Actual length: {actual.Length}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if needed.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "(empty)";
        }

        // Show non-printable characters
        value = value.Replace("\t", "→").Replace(" ", "·");

        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, maxLength - 3), "...");
    }

    /// <summary>
    /// Ensures emoji or pictographic characters are never followed by a regular breaking space (U+0020).
    /// This guards against accidental regressions where semantic icons lose their non-breaking space.
    /// </summary>
    private static void AssertNoEmojiFollowedByRegularSpace(string markdown, string context)
    {
        var index = 0;
        while (index < markdown.Length)
        {
            var rune = Rune.GetRuneAt(markdown, index);
            var nextIndex = index + rune.Utf16SequenceLength;

            if (IsEmojiLike(rune))
            {
                var lookahead = nextIndex;
                while (lookahead < markdown.Length)
                {
                    var nextRune = Rune.GetRuneAt(markdown, lookahead);
                    if (!IsPresentationModifier(nextRune))
                    {
                        break;
                    }

                    lookahead += nextRune.Utf16SequenceLength;
                }

                if (lookahead < markdown.Length && markdown[lookahead] == ' ')
                {
                    var nextNonSpace = lookahead;
                    while (nextNonSpace < markdown.Length && markdown[nextNonSpace] == ' ')
                    {
                        nextNonSpace++;
                    }

                    if (nextNonSpace >= markdown.Length || markdown[nextNonSpace] is '|' or '\n' or '\r')
                    {
                        // Allow spacing when the icon is the entire cell or immediately before a pipe/newline.
                        index = nextIndex;
                        continue;
                    }

                    var snippetStart = Math.Max(0, index - 5);
                    var snippetLength = Math.Min(20, markdown.Length - snippetStart);
                    var snippet = markdown.Substring(snippetStart, snippetLength).Replace("\n", "⏎");
                    var runeInfo = $"U+{rune.Value:X4} '{rune}'";

                    Assert.Fail(
                        $"Emoji or pictograph at position {index} ({runeInfo}) in {context} is followed by a regular space. " +
                        "Icons must be followed by a non-breaking space (U+00A0). " +
                        $"Context: '{snippet}'.");
                }
            }

            index = nextIndex;
        }
    }

    /// <summary>
    /// Detects emoji-like runes by Unicode category to avoid missing semantic icons in markdown.
    /// </summary>
    private static bool IsEmojiLike(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        return category is UnicodeCategory.OtherSymbol && rune.Value >= 0x2600;
    }

    /// <summary>
    /// Skips over variation selectors or joiners that are part of an emoji sequence.
    /// </summary>
    private static bool IsPresentationModifier(Rune rune)
    {
        return rune.Value is 0xFE0F or 0xFE0E or 0x200D
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.EnclosingMark
            || Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format;
    }
}
