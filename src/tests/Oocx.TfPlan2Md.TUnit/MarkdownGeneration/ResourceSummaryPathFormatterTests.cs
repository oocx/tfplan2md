using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for formatting replacement path summaries.
/// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
/// </summary>
public class ResourceSummaryPathFormatterTests
{
    /// <summary>
    /// Verifies numeric segments are rendered as array indexes.
    /// </summary>
    [Test]
    public void FormatReplacePath_NumberSegments_RenderAsIndexes()
    {
        var path = new List<object> { "network_interface", 0, "name" };

        var result = ResourceSummaryPathFormatter.FormatReplacePath(path);

        result.Should().Be("network_interface[0]");
    }

    /// <summary>
    /// Verifies bracketed segments are appended directly.
    /// </summary>
    [Test]
    public void FormatReplacePath_BracketedSegments_AreAppended()
    {
        var path = new List<object> { "tags", "[0]", "value" };

        var result = ResourceSummaryPathFormatter.FormatReplacePath(path);

        result.Should().Be("tags[0].value");
    }

    /// <summary>
    /// Verifies invalid or empty paths return null.
    /// </summary>
    [Test]
    public void FormatReplacePath_EmptyPath_ReturnsNull()
    {
        var result = ResourceSummaryPathFormatter.FormatReplacePath([]);

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies name and context keys are filtered out.
    /// </summary>
    [Test]
    public void IsNameOrContextKey_FiltersExpectedKeys()
    {
        ResourceSummaryPathFormatter.IsNameOrContextKey("name").Should().BeTrue();
        ResourceSummaryPathFormatter.IsNameOrContextKey("Context").Should().BeTrue();
        ResourceSummaryPathFormatter.IsNameOrContextKey("other").Should().BeFalse();
    }
}
