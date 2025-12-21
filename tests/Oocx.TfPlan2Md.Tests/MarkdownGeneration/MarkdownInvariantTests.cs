using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Property-based invariant tests that verify markdown properties that must ALWAYS hold,
/// regardless of input. These tests define the "contract" of valid markdown output.
/// </summary>
/// <remarks>
/// Unlike tests that check for specific bugs, these tests verify fundamental invariants:
/// - No consecutive blank lines (MD012)
/// - All pipes escaped in table cells
/// - No raw newlines in table cells
/// - Proper heading spacing
/// - Valid HTML tags balanced
///
/// Each invariant is tested against multiple plans to ensure it holds universally.
/// </remarks>
public class MarkdownInvariantTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Gets all test plan files for invariant testing.
    /// </summary>
    private static IEnumerable<string> GetTestPlanPaths()
    {
        var testDataDir = Path.GetFullPath("TestData");
        return Directory.GetFiles(testDataDir, "*.json")
            .Where(f => !f.EndsWith("demo-principals.json", StringComparison.Ordinal))
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains("\"resource_changes\"");
            });
    }

    /// <summary>
    /// Renders a plan file to markdown.
    /// </summary>
    private string RenderPlan(string planPath, string? principalsPath = null)
    {
        var json = File.ReadAllText(planPath);
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = principalsPath != null
            ? new MarkdownRenderer(new PrincipalMapper(principalsPath))
            : new MarkdownRenderer();
        return renderer.Render(model);
    }

    #region MD012 - No Multiple Consecutive Blank Lines

    /// <summary>
    /// INVARIANT: Markdown output must never contain more than one consecutive blank line.
    /// This is enforced by markdownlint rule MD012.
    /// </summary>
    [Fact]
    public void Invariant_NoConsecutiveBlankLines_AllPlans()
    {
        var violations = new List<(string File, int MaxBlanks, int Line)>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);
            var (maxBlanks, lineNumber) = CountMaxConsecutiveBlanks(markdown);

            if (maxBlanks > 1)
            {
                violations.Add((Path.GetFileName(planPath), maxBlanks, lineNumber));
            }
        }

        violations.Should().BeEmpty(
            $"MD012 violation - found consecutive blank lines:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File}: {v.MaxBlanks} blanks at line {v.Line}")));
    }

    /// <summary>
    /// INVARIANT: Comprehensive demo must never have consecutive blank lines.
    /// </summary>
    [Fact]
    public void Invariant_NoConsecutiveBlankLines_ComprehensiveDemo()
    {
        var markdown = RenderPlan(DemoPaths.DemoPlanPath, DemoPaths.DemoPrincipalsPath);
        var (maxBlanks, lineNumber) = CountMaxConsecutiveBlanks(markdown);

        maxBlanks.Should().BeLessThanOrEqualTo(1,
            $"MD012 violation: found {maxBlanks} consecutive blank lines at line {lineNumber}");
    }

    private static (int MaxBlanks, int FirstOccurrenceLine) CountMaxConsecutiveBlanks(string markdown)
    {
        var lines = markdown.Split('\n');
        var consecutiveBlanks = 0;
        var maxConsecutiveBlanks = 0;
        var firstOccurrenceLine = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                consecutiveBlanks++;
                if (consecutiveBlanks > maxConsecutiveBlanks)
                {
                    maxConsecutiveBlanks = consecutiveBlanks;
                    firstOccurrenceLine = i + 1 - consecutiveBlanks + 1;
                }
            }
            else
            {
                consecutiveBlanks = 0;
            }
        }

        return (maxConsecutiveBlanks, firstOccurrenceLine);
    }

    #endregion

    #region Table Integrity Invariants

    /// <summary>
    /// INVARIANT: All tables must parse correctly with Markdig.
    /// If a table is broken (e.g., by blank lines), Markdig won't parse it as a Table element.
    /// </summary>
    [Fact]
    public void Invariant_AllTablesParseCorrectly_AllPlans()
    {
        var violations = new List<(string File, int Expected, int Actual)>();
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);
            var document = Markdown.Parse(markdown, pipeline);
            var tables = document.Descendants<Table>().ToList();

            // Count expected tables by counting "| Attribute |" headers (1 per resource) + 1 summary
            var expectedResourceTables = Regex.Matches(markdown, @"\| Attribute \|").Count;
            var expectedSummaryTable = markdown.Contains("| Action |") ? 1 : 0;
            var expectedTotal = expectedResourceTables + expectedSummaryTable;

            if (tables.Count < expectedTotal)
            {
                violations.Add((Path.GetFileName(planPath), expectedTotal, tables.Count));
            }
        }

        violations.Should().BeEmpty(
            $"Some tables failed to parse (likely broken by blank lines):\n" +
            string.Join("\n", violations.Select(v => $"  {v.File}: expected {v.Expected} tables, parsed {v.Actual}")));
    }

    /// <summary>
    /// INVARIANT: Table rows must not have blank lines between them.
    /// A blank line between |...| rows breaks the table.
    /// </summary>
    [Fact]
    public void Invariant_NoBlankLinesBetweenTableRows_AllPlans()
    {
        var violations = new List<(string File, int Count)>();
        var pattern = new Regex(@"(?<=\|[^\n]*)\n[ \t]*\n(?=[ \t]*\|)");

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);
            var matches = pattern.Matches(markdown);

            if (matches.Count > 0)
            {
                violations.Add((Path.GetFileName(planPath), matches.Count));
            }
        }

        violations.Should().BeEmpty(
            $"Found blank lines between table rows:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File}: {v.Count} occurrences")));
    }

    /// <summary>
    /// INVARIANT: No raw newlines inside table cells.
    /// Newlines must be replaced with &lt;br/&gt;.
    /// </summary>
    [Fact]
    public void Invariant_NoRawNewlinesInTableCells_AllPlans()
    {
        var violations = new List<(string File, int Line)>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);
            var lines = markdown.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip non-table lines
                if (!line.TrimStart().StartsWith('|'))
                {
                    continue;
                }

                // Check if this line starts with | but doesn't end with |
                // That would indicate a broken row (newline inside cell)
                if (line.TrimStart().StartsWith('|') && !line.TrimEnd().EndsWith('|'))
                {
                    // Exception: separator row like |---|---|
                    if (!Regex.IsMatch(line, @"^\|[-:\s|]+$"))
                    {
                        violations.Add((Path.GetFileName(planPath), i + 1));
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            $"Found potential raw newlines in table cells:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File} line {v.Line}")));
    }

    #endregion

    #region Escaping Invariants

    /// <summary>
    /// INVARIANT: All pipes in table cell content must be escaped.
    /// Unescaped pipes break table column structure.
    /// </summary>
    [Fact]
    public void Invariant_PipesEscapedInTableCells_BreakingPlan()
    {
        var markdown = RenderPlan("TestData/markdown-breaking-plan.json");

        // The test data contains "rg-with-pipe|and*asterisk"
        // After escaping, it should be "rg-with-pipe\|and*asterisk"
        markdown.Should().Contain(@"rg-with-pipe\|and*asterisk",
            "because pipes must be escaped in table cells");
        markdown.Should().NotContain("rg-with-pipe|and*asterisk",
            "because unescaped pipes break table structure");
    }

    /// <summary>
    /// INVARIANT: Newlines in values must be converted to &lt;br/&gt;.
    /// </summary>
    [Fact]
    public void Invariant_NewlinesConvertedToBr_BreakingPlan()
    {
        var markdown = RenderPlan("TestData/markdown-breaking-plan.json");

        // The test data contains "line1\nline2"
        // After conversion, it should be "line1<br/>line2"
        markdown.Should().Contain("line1<br/>line2",
            "because newlines must be converted to <br/> in table cells");
    }

    #endregion

    #region Heading Invariants

    /// <summary>
    /// INVARIANT: Headings must be surrounded by blank lines.
    /// </summary>
    [Fact]
    public void Invariant_HeadingsSurroundedByBlankLines_AllPlans()
    {
        var violations = new List<(string File, int Line, string Issue)>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);
            var lines = markdown.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for headings (# at start of line)
                if (Regex.IsMatch(line, @"^#{1,6}\s"))
                {
                    // Check line before (except for first line)
                    if (i > 0 && !string.IsNullOrWhiteSpace(lines[i - 1]))
                    {
                        violations.Add((Path.GetFileName(planPath), i + 1, "no blank line before heading"));
                    }

                    // Check line after (except for last line)
                    if (i < lines.Length - 1 && !string.IsNullOrWhiteSpace(lines[i + 1]))
                    {
                        violations.Add((Path.GetFileName(planPath), i + 1, "no blank line after heading"));
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            $"Headings must be surrounded by blank lines:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File} line {v.Line}: {v.Issue}")));
    }

    #endregion

    #region HTML Tag Invariants

    /// <summary>
    /// INVARIANT: All &lt;details&gt; tags must have matching &lt;/details&gt; tags.
    /// </summary>
    [Fact]
    public void Invariant_DetailsTagsBalanced_AllPlans()
    {
        var violations = new List<(string File, int Open, int Close)>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);

            var openTags = Regex.Matches(markdown, @"<details>").Count;
            var closeTags = Regex.Matches(markdown, @"</details>").Count;

            if (openTags != closeTags)
            {
                violations.Add((Path.GetFileName(planPath), openTags, closeTags));
            }
        }

        violations.Should().BeEmpty(
            $"Unbalanced <details> tags:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File}: {v.Open} open, {v.Close} close")));
    }

    /// <summary>
    /// INVARIANT: All &lt;summary&gt; tags must have matching &lt;/summary&gt; tags.
    /// </summary>
    [Fact]
    public void Invariant_SummaryTagsBalanced_AllPlans()
    {
        var violations = new List<(string File, int Open, int Close)>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);

            var openTags = Regex.Matches(markdown, @"<summary>").Count;
            var closeTags = Regex.Matches(markdown, @"</summary>").Count;

            if (openTags != closeTags)
            {
                violations.Add((Path.GetFileName(planPath), openTags, closeTags));
            }
        }

        violations.Should().BeEmpty(
            $"Unbalanced <summary> tags:\n" +
            string.Join("\n", violations.Select(v => $"  {v.File}: {v.Open} open, {v.Close} close")));
    }

    #endregion

    #region Structural Invariants

    /// <summary>
    /// INVARIANT: Every rendered plan must have a "Terraform Plan" heading.
    /// </summary>
    [Fact]
    public void Invariant_HasTerraformPlanHeading_AllPlans()
    {
        var violations = new List<string>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var markdown = RenderPlan(planPath);

            if (!markdown.Contains("# Terraform Plan"))
            {
                violations.Add(Path.GetFileName(planPath));
            }
        }

        violations.Should().BeEmpty(
            $"Plans missing '# Terraform Plan' heading:\n" +
            string.Join("\n", violations.Select(v => $"  {v}")));
    }

    /// <summary>
    /// INVARIANT: Every non-empty plan must have a Summary section.
    /// </summary>
    [Fact]
    public void Invariant_HasSummarySection_NonEmptyPlans()
    {
        var violations = new List<string>();

        foreach (var planPath in GetTestPlanPaths())
        {
            var json = File.ReadAllText(planPath);

            // Skip empty plans
            if (json.Contains("\"resource_changes\": []"))
            {
                continue;
            }

            var markdown = RenderPlan(planPath);

            if (!markdown.Contains("## Summary"))
            {
                violations.Add(Path.GetFileName(planPath));
            }
        }

        violations.Should().BeEmpty(
            $"Non-empty plans missing '## Summary' heading:\n" +
            string.Join("\n", violations.Select(v => $"  {v}")));
    }

    #endregion
}
