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

        result.Should().Be("<code>value*</code>");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesDiffer_WrapsInlineDiffContent()
    {
        var formatter = new AzureDevOpsDiffFormatter();

        var result = formatter.FormatDiff("foo", "bar");

        result.Should().StartWith("<code style=\"display:block; white-space:normal; padding:0; margin:0;\">");
        result.Should().Contain("- <span");
        result.Should().Contain("foo</span>");
        result.Should().Contain("+ <span");
        result.Should().Contain("bar</span>");
        result.Should().EndWith("</code>");

        await Task.CompletedTask;
    }
}
