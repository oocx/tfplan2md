using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.RenderTargets;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for details display mode helpers used by templates.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public class ScribanHelpersDetailsDisplayTests
{
    [Test]
    public void GetDetailsOpenAttr_OpenMode_ReturnsOpen()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Open);

        // Assert
        result.Should().Be(" open");
    }

    [Test]
    public void GetDetailsOpenAttr_ClosedMode_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Closed);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_NoFindings_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_WithFindings_ReturnsOpen()
    {
        // Arrange
        var change = new ScriptObject();
        var findings = new ScriptArray { new ScriptObject { ["severity"] = "critical" } };
        change["code_analysis_findings"] = findings;

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(" open");
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_EmptyFindings_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();
        var findings = new ScriptArray();
        change["code_analysis_findings"] = findings;

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_ClosedMode_ReturnsClosed()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Closed);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_AutoMode_ReturnsClosed()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_OpenMode_ReturnsOpen()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Open);

        // Assert
        result.Should().Be(" open");
    }
}
