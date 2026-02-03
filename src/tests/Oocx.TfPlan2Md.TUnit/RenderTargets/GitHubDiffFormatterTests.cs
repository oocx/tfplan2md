using AwesomeAssertions;
using Oocx.TfPlan2Md.RenderTargets.GitHub;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.RenderTargets;

/// <summary>
/// Tests for <see cref="GitHubDiffFormatter"/> to ensure GitHub-specific diff output remains stable.
/// </summary>
public class GitHubDiffFormatterTests
{
    [Test]
    public async Task FormatDiff_WhenBothValuesAreNull_ReturnsEmpty()
    {
        var formatter = new GitHubDiffFormatter();

        var result = formatter.FormatDiff(null, null);

        result.Should().Be(string.Empty);

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesAreEqual_ReturnsEscapedInlineCode()
    {
        var formatter = new GitHubDiffFormatter();

        var result = formatter.FormatDiff("value*", "value*");

        result.Should().Be("<code>value\\*</code>");

        await Task.CompletedTask;
    }

    [Test]
    public async Task FormatDiff_WhenValuesDiffer_UsesSimpleDiffWithEscaping()
    {
        var formatter = new GitHubDiffFormatter();

        var result = formatter.FormatDiff("a|b", "a|c");

        result.Should().Be("- `a\\|b`<br>+ `a\\|c`");

        await Task.CompletedTask;
    }
}
