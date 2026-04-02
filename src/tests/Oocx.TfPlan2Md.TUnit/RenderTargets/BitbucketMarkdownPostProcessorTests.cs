using AwesomeAssertions;
using Oocx.TfPlan2Md.RenderTargets.Bitbucket;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.RenderTargets;

/// <summary>
/// Tests for <see cref="BitbucketMarkdownPostProcessor"/> to ensure Bitbucket-safe markdown rewrites preserve content.
/// </summary>
public class BitbucketMarkdownPostProcessorTests
{
    /// <summary>
    /// Verifies HTML details and summary wrappers are flattened to plain markdown headings.
    /// </summary>
    [Test]
    public async Task Process_WithDetailsBlock_FlattensSummaryAndRemovesHtmlContainer()
    {
        var markdown = "<details open><summary>### Resource Summary</summary><br>content</details>";

        var result = BitbucketMarkdownPostProcessor.Process(markdown);

        result.Should().Be("\n\n### Resource Summary\n\ncontent");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies inline code preserves decoded characters that markdown code spans render literally.
    /// </summary>
    [Test]
    public async Task Process_WithInlineCodeContainingAmpersandAndPipe_PreservesLiteralCharacters()
    {
        var markdown = "Value: <code>a&amp;b|c</code>";

        var result = BitbucketMarkdownPostProcessor.Process(markdown);

        result.Should().Be("Value: `a&b|c`");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies inline code chooses a long-enough fence when the content contains backticks.
    /// </summary>
    [Test]
    public async Task Process_WithInlineCodeContainingBackticks_UsesLongerFenceWithoutEscapingContent()
    {
        var markdown = "Value: <code>prefix `quoted` suffix</code>";

        var result = BitbucketMarkdownPostProcessor.Process(markdown);

        result.Should().Be("Value: ``prefix `quoted` suffix``");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies block code is converted to fenced markdown while preserving decoded newlines.
    /// </summary>
    [Test]
    public async Task Process_WithPreCodeBlock_ConvertsToMarkdownFence()
    {
        var markdown = "<pre><code>line 1&lt;br/&gt;line 2\nline 3</code></pre>";

        var result = BitbucketMarkdownPostProcessor.Process(markdown);

        result.Should().Be("```\nline 1\nline 2\nline 3\n```");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies unsupported inline HTML is rewritten to markdown-only equivalents.
    /// </summary>
    [Test]
    public async Task Process_WithBoldSpanAndBreak_RewritesMarkupToPlainMarkdown()
    {
        var markdown = "<span>Start</span> <b>bold</b><br/>next";

        var result = BitbucketMarkdownPostProcessor.Process(markdown);

        result.Should().Be("Start **bold** / next");

        await Task.CompletedTask;
    }
}
