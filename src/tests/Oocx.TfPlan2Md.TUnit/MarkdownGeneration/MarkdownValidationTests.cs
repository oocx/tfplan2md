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
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Validates markdown output structure and escaping rules.
/// Related feature: docs/features/007-markdown-quality-validation/specification.md
/// </summary>
public class MarkdownValidationTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    /// <summary>
    /// Verifies that pipes and asterisks in resource names are escaped to avoid breaking tables.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_BreakingPlan_EscapesPipesAndAsterisks()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("rg-with-pipe\\|and*asterisk", "because pipes must be escaped in tables");
        markdown.Should().NotContain("rg-with-pipe|and*asterisk", "because raw pipes break markdown tables");
    }

    /// <summary>
    /// Verifies that values with newlines are moved to large attributes section.
    /// Related feature: docs/features/006-large-attribute-value-display/specification.md
    /// </summary>
    [Test]
    public void Render_BreakingPlan_ReplacesNewlinesInTableCells()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("Large values:", "because values with newlines are classified as large");
        markdown.Should().Contain("tags.note", "because the note attribute has newlines");
    }

    /// <summary>
    /// Ensures tables remain structurally valid when rendered with problematic input.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_BreakingPlan_ParsesTablesWithMarkdig()
    {
        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(markdown, pipeline);
        var tables = document.Descendants<Table>().ToList();

        tables.Should().NotBeEmpty("because resource details are rendered as tables");
        tables.Count.Should().BeGreaterThan(1, "because multiple resources should produce multiple tables");
    }

    /// <summary>
    /// Ensures headings are parsed, indicating correct spacing around heading blocks.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_DefaultPlan_HeadingsAreParsed()
    {
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(markdown, pipeline);
        var headings = document.Descendants<HeadingBlock>().ToList();

        var summaryPresent = headings.Any(h => h.Inline != null && h.Inline.FirstChild != null && h.Inline.FirstChild.ToString() == "Summary");
        summaryPresent.Should().BeTrue("because summary heading should be present");

        var resourceChangesPresent = headings.Any(h => h.Inline != null && h.Inline.FirstChild != null && h.Inline.FirstChild.ToString() == "Resource Changes");
        resourceChangesPresent.Should().BeTrue("because resource changes heading should be present");
    }

    /// <summary>
    /// Renders the comprehensive demo to HTML and verifies that markdown constructs are fully parsed.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_ComprehensiveDemo_RendersToHtmlWithoutRawMarkdown()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(markdown, pipeline);

        html.Should().Contain("<table", "because tables must render as HTML, not raw markdown");
        html.Should().Contain("<h2 id=\"summary\">Summary</h2>", "because headings must render correctly");
        html.Should().NotContain("| Action |", "because table markup should not remain in the rendered HTML");
    }

    /// <summary>
    /// Verifies that the output does not contain multiple consecutive blank lines (MD012).
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_ComprehensiveDemo_NoMultipleBlankLines()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        // Check for 2+ consecutive blank lines (MD012 rule)
        // A blank line is a line containing only whitespace or nothing
        var lines = markdown.Split('\n');
        var consecutiveBlanks = 0;
        var maxConsecutiveBlanks = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                consecutiveBlanks++;
                maxConsecutiveBlanks = Math.Max(maxConsecutiveBlanks, consecutiveBlanks);
            }
            else
            {
                consecutiveBlanks = 0;
            }
        }

        maxConsecutiveBlanks.Should().BeLessThan(2, "because multiple consecutive blank lines violate MD012");
    }

    /// <summary>
    /// Verifies that no blank lines exist between table rows, which would break table rendering.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_ComprehensiveDemo_NoBlankLinesInTables()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        // Regex matches a blank line that is immediately preceded by a table row and followed by a table row
        // (?<=\|[^\n]*)\n\s*\n(?=\|)
        var matches = Regex.Matches(markdown, @"(?<=\|[^\n]*)\n\s*\n(?=\|)", RegexOptions.None, TimeSpan.FromSeconds(1));

        matches.Should().BeEmpty("because blank lines between table rows break markdown table rendering");
    }

    /// <summary>
    /// Verifies that the number of tables parsed matches the expected number of resources.
    /// This ensures no tables are broken into text blocks.
    /// Related feature: docs/features/007-markdown-quality-validation/specification.md
    /// </summary>
    [Test]
    public void Render_ComprehensiveDemo_TableCountMatchesResources()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(markdown, pipeline);
        var tables = document.Descendants<Table>().ToList();

        // Expected: 1 summary table + 1 table for each resource change that has at least one small attribute
        var changesWithSmallAttributes = model.Changes.Count(change => change.AttributeChanges.Any(attr => !ScribanHelpers.IsLargeValue(attr.Before) && !ScribanHelpers.IsLargeValue(attr.After)));
        var expectedTableCount = 1 + changesWithSmallAttributes;

        tables.Count.Should().Be(expectedTableCount, "because every resource change should render exactly one table, plus the summary table");
    }
}
