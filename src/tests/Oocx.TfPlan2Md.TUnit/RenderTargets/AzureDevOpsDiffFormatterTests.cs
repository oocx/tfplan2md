using AwesomeAssertions;
using Oocx.TfPlan2Md.RenderTargets.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.RenderTargets;

/// <summary>
/// Tests for <see cref="AzureDevOpsDiffFormatter"/> to validate inline diff rendering.
/// </summary>
public class AzureDevOpsDiffFormatterTests
{
    [Test]
    public async Task FormatDiff_WhenValuesAreNull_ReturnsEmpty()
    {
        var formatter = new AzureDevOpsDiffFormatter();

        var result = formatter.FormatDiff(null, null);

        result.Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesAreEqual_ReturnsEscapedInlineCode()
    {
        var formatter = new AzureDevOpsDiffFormatter();

        var result = formatter.FormatDiff("value*", "value*");

        result.Should().Be("<code>value\\*</code>");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesDiffer_UsesHtmlInlineDiff()
    {
        var formatter = new AzureDevOpsDiffFormatter();

        var result = formatter.FormatDiff("foo", "bar");

        // Should generate HTML with styled spans and character-level highlighting
        result.Should().Contain("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">");
        result.Should().Contain("background-color: #fff5f5"); // Removed line background
        result.Should().Contain("background-color: #f0fff4"); // Added line background
        result.Should().Contain("background-color: #ffc0c0"); // Removed char background (all chars different)
        result.Should().Contain("background-color: #acf2bd"); // Added char background (all chars different)
        result.Should().Contain("- "); // Has minus prefix
        result.Should().Contain("+ "); // Has plus prefix
        result.Should().Contain("foo");
        result.Should().Contain("bar");
        result.Should().Contain("<br>");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesDifferWithSpecialChars_UsesHtmlAndHighlightsChanges()
    {
        var formatter = new AzureDevOpsDiffFormatter();

        var result = formatter.FormatDiff("a|b", "a|c");

        // Should generate HTML with styled spans and character-level highlighting
        result.Should().Contain("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">");
        result.Should().Contain("background-color: #fff5f5"); // Removed line background
        result.Should().Contain("background-color: #f0fff4"); // Added line background
        // Character-level diff highlighting (b vs c)
        result.Should().Contain("background-color: #ffc0c0"); // Removed char background
        result.Should().Contain("background-color: #acf2bd"); // Added char background
        result.Should().Contain("- a|");
        result.Should().Contain("+ a|");
        result.Should().Contain("<br>");

        await Task.CompletedTask;
    }
}
