using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for <see cref="MarkdownWriter"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// Related test plan: docs/features/107-remove-scriban/test-plan.md (TC-MW-01..10).
/// </summary>
public class MarkdownWriterTests
{
    /// <summary>
    /// Verifies level-1 heading output.
    /// </summary>
    [Test]
    public void Heading_Level1_RendersHashPrefix()
    {
        var writer = new MarkdownWriter();

        var output = writer.Heading("Title", 1).Build();

        output.Should().Be("# Title\n");
    }

    /// <summary>
    /// Verifies level-3 heading output.
    /// </summary>
    [Test]
    public void Heading_Level3_RendersTripleHash()
    {
        var writer = new MarkdownWriter();

        var output = writer.Heading("Title", 3).Build();

        output.Should().Be("### Title\n");
    }

    /// <summary>
    /// Verifies level-6 heading output.
    /// </summary>
    [Test]
    public void Heading_Level6_RendersSixHashes()
    {
        var writer = new MarkdownWriter();

        var output = writer.Heading("Title", 6).Build();

        output.Should().Be("###### Title\n");
    }

    /// <summary>
    /// Verifies non-empty paragraph output.
    /// </summary>
    [Test]
    public void Paragraph_NonEmptyText_RendersText()
    {
        var writer = new MarkdownWriter();

        var output = writer.Paragraph("hello").Build();

        output.Should().Be("hello\n");
    }

    /// <summary>
    /// Verifies empty paragraph output.
    /// </summary>
    [Test]
    public void Paragraph_EmptyString_RendersEmptyParagraph()
    {
        var writer = new MarkdownWriter();

        var output = writer.Paragraph(string.Empty).Build();

        output.Should().Be("\n");
    }

    /// <summary>
    /// Verifies multi-column table header rendering.
    /// </summary>
    [Test]
    public void TableHeader_MultipleColumns_RendersCorrectSeparatorRow()
    {
        var writer = new MarkdownWriter();

        var output = writer.TableHeader("Name", "Value").Build();

        output.Should().Be("| Name | Value |\n| --- | --- |\n");
    }

    /// <summary>
    /// Verifies pipe escaping in table rows.
    /// </summary>
    [Test]
    public void TableRow_CellContainingPipe_EscapedInOutput()
    {
        var writer = new MarkdownWriter();

        var output = writer.TableRow(["a|b", "ok"]).Build();

        output.Should().Be("| a\\|b | ok |\n");
    }

    /// <summary>
    /// Verifies empty cell handling in table rows.
    /// </summary>
    [Test]
    public void TableRow_EmptyCell_RendersEmptyCell()
    {
        var writer = new MarkdownWriter();

        var output = writer.TableRow([string.Empty, "x"]).Build();

        output.Should().Be("|  | x |\n");
    }

    /// <summary>
    /// Verifies details open tag with open attribute.
    /// </summary>
    [Test]
    public void DetailsOpen_OpenTrue_ContainsOpenAttribute()
    {
        var writer = new MarkdownWriter();

        var output = writer.DetailsOpen("summary", true).Build();

        output.Should().Be("<details open><summary>summary</summary>\n");
    }

    /// <summary>
    /// Verifies details open tag without open attribute.
    /// </summary>
    [Test]
    public void DetailsOpen_OpenFalse_NoOpenAttribute()
    {
        var writer = new MarkdownWriter();

        var output = writer.DetailsOpen("summary", false).Build();

        output.Should().Be("<details><summary>summary</summary>\n");
    }

    /// <summary>
    /// Verifies details closing tag rendering.
    /// </summary>
    [Test]
    public void DetailsClose_AfterDetailsOpen_ProducesClosingTag()
    {
        var writer = new MarkdownWriter();

        var output = writer.DetailsOpen("summary").DetailsClose().Build();

        output.Should().Be("<details><summary>summary</summary>\n</details>\n");
    }

    /// <summary>
    /// Verifies fenced code block for single-line input.
    /// </summary>
    [Test]
    public void Code_SingleLine_RendersInFencedBlock()
    {
        var writer = new MarkdownWriter();

        var output = writer.Code("line", "json").Build();

        output.Should().Be("```json\nline\n```\n");
    }

    /// <summary>
    /// Verifies fenced code block preserves multi-line input.
    /// </summary>
    [Test]
    public void Code_MultiLine_RendersAllLinesInBlock()
    {
        var writer = new MarkdownWriter();

        var output = writer.Code("line1\nline2").Build();

        output.Should().Be("```\nline1\nline2\n```\n");
    }

    /// <summary>
    /// Verifies empty inline-code content.
    /// </summary>
    [Test]
    public void InlineCode_EmptyContent_RendersEmptyBackticks()
    {
        var output = MarkdownWriter.InlineCode(string.Empty);

        output.Should().Be("``");
    }

    /// <summary>
    /// Verifies raw non-empty content is appended unchanged.
    /// </summary>
    [Test]
    public void Raw_NonEmptyString_AppendedVerbatim()
    {
        var writer = new MarkdownWriter();

        var output = writer.Raw("abc").Build();

        output.Should().Be("abc\n");
    }

    /// <summary>
    /// Verifies raw empty content does not alter output.
    /// </summary>
    [Test]
    public void Raw_EmptyString_OutputUnchanged()
    {
        var writer = new MarkdownWriter();

        var output = writer.Raw(string.Empty).Build();

        output.Should().Be("\n");
    }

    /// <summary>
    /// Verifies blank lines between table rows are removed during build.
    /// </summary>
    [Test]
    public void Build_BlankLineBetweenTableRows_LineRemoved()
    {
        var writer = new MarkdownWriter();

        var output = writer
            .Raw("| A |\n\n| B |\n")
            .Build();

        output.Should().Be("| A |\n| B |\n");
    }

    /// <summary>
    /// Verifies indentation is removed from table rows.
    /// </summary>
    [Test]
    public void Build_IndentedTableRow_IndentationStripped()
    {
        var writer = new MarkdownWriter();

        var output = writer
            .Raw("Header\n    | Name | Value |\n")
            .Build();

        output.Should().Be("Header\n| Name | Value |\n");
    }

    /// <summary>
    /// Verifies runs of three blank lines are collapsed to one blank line.
    /// </summary>
    [Test]
    public void Build_ThreeConsecutiveBlankLines_CollapsedToOne()
    {
        var writer = new MarkdownWriter();

        var output = writer
            .Raw("a\n\n\n\nb")
            .Build();

        output.Should().Be("a\n\nb\n");
    }

    /// <summary>
    /// Verifies heading spacing inserts a blank line before headings when needed.
    /// </summary>
    [Test]
    public void Build_HeadingWithoutPrecedingBlankLine_BlankLineInsertedBefore()
    {
        var writer = new MarkdownWriter();

        var output = writer.Raw("text\n## H").Build();

        output.Should().Be("text\n\n## H\n");
    }

    /// <summary>
    /// Verifies heading spacing inserts a blank line after headings when needed.
    /// </summary>
    [Test]
    public void Build_HeadingWithoutFollowingBlankLine_BlankLineInsertedAfter()
    {
        var writer = new MarkdownWriter();

        var output = writer.Raw("## H\nvalue").Build();

        output.Should().Be("## H\n\nvalue\n");
    }

    /// <summary>
    /// Verifies heading spacing does not introduce duplicate blank lines.
    /// </summary>
    [Test]
    public void Build_HeadingAlreadySurrounded_NoExtraBlankLines()
    {
        var writer = new MarkdownWriter();

        var output = writer.Raw("text\n\n## H\n\nvalue").Build();

        output.Should().Be("text\n\n## H\n\nvalue\n");
    }
}
