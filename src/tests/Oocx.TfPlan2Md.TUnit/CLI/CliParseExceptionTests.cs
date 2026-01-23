using System;
using Oocx.TfPlan2Md.CLI;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CLI;

/// <summary>
/// Validates CLI parse exception construction and message propagation.
/// Related feature: docs/spec.md.
/// </summary>
public class CliParseExceptionTests
{
    /// <summary>
    /// Ensures constructors preserve provided messages and inner exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Constructors_preserve_message_and_inner_exception()
    {
        var defaultException = new CliParseException();
        var messageException = new CliParseException("bad arguments");
        var innerException = new InvalidOperationException("inner failure");
        var wrappedException = new CliParseException("wrapped", innerException);

        await Assert.That(defaultException).IsNotNull();
        await Assert.That(messageException.Message).Contains("bad arguments", StringComparison.Ordinal);
        await Assert.That(wrappedException.InnerException).IsSameReferenceAs(innerException);
    }
}
