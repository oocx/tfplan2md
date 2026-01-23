using System;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Validates Terraform plan parse exception constructors and propagation.
/// Related feature: docs/features/012-terraform-plan-import/specification.md.
/// </summary>
public class TerraformPlanParseExceptionTests
{
    /// <summary>
    /// Ensures constructors preserve messages and inner exceptions for parsing failures.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Constructors_preserve_message_and_inner_exception()
    {
        var defaultException = new TerraformPlanParseException();
        var messageException = new TerraformPlanParseException("parse failure");
        var innerException = new InvalidOperationException("inner");
        var wrappedException = new TerraformPlanParseException("wrapped", innerException);

        await Assert.That(defaultException).IsNotNull();
        await Assert.That(messageException.Message).Contains("parse failure", StringComparison.Ordinal);
        await Assert.That(wrappedException.InnerException).IsSameReferenceAs(innerException);
    }
}
