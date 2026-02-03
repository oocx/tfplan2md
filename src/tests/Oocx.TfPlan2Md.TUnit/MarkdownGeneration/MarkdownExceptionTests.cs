using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Covers Markdown-specific exception helpers to improve diagnostic reliability.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
public class MarkdownExceptionTests
{
    /// <summary>
    /// Verifies MarkdownRenderException constructors capture messages and inner exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Markdown_render_exception_constructors_preserve_state()
    {
        var defaultException = new MarkdownRenderException();
        var messageException = new MarkdownRenderException("render failed");
        var innerException = new InvalidOperationException("inner");
        var wrappedException = new MarkdownRenderException("wrapped", innerException);

        await Assert.That(defaultException).IsNotNull();
        await Assert.That(messageException.Message).Contains("render failed", StringComparison.Ordinal);
        await Assert.That(wrappedException.InnerException).IsSameReferenceAs(innerException);
    }

    /// <summary>
    /// Verifies ScribanHelperException constructors capture messages and inner exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Scriban_helper_exception_constructors_preserve_state()
    {
        var defaultException = new ScribanHelperException();
        var messageException = new ScribanHelperException("helper failed");
        var innerException = new InvalidOperationException("inner");
        var wrappedException = new ScribanHelperException("wrapped", innerException);

        await Assert.That(defaultException).IsNotNull();
        await Assert.That(messageException.Message).Contains("helper failed", StringComparison.Ordinal);
        await Assert.That(wrappedException.InnerException).IsSameReferenceAs(innerException);
    }
}
