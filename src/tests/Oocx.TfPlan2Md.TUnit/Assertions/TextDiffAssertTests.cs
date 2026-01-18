using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Assertions;

/// <summary>
/// Verifies that <see cref="TextDiffAssert"/> produces actionable diagnostics on mismatches.
/// Related feature: docs/features/030-terraform-show-approximation/
/// </summary>
/// <remarks>
/// In TUnit, we test that TextDiffAssert throws TUnitAssertionException instead of XunitException.
/// The key is verifying the error message quality, not the specific exception type.
/// </remarks>
public sealed class TextDiffAssertTests
{
    /// <summary>
    /// Ensures the failure message includes the first differing line and column.
    /// </summary>
    [Test]
    public async Task EqualIgnoringLeadingWhitespace_WhenDifferent_ReportsLineAndColumn()
    {
        var expected = "a\n  b\nxyz\n";
        var actual = "a\n  b\nxyZ\n";

        Exception? caughtException = null;
        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        await Assert.That(caughtException).IsNotNull();
        var message = caughtException!.Message;
        await Assert.That(message).Contains("First difference: line 3, column 3");
        await Assert.That(message).Contains("Context (escaped, expected vs actual):");
        await Assert.That(message).Contains("L0003");
    }

    /// <summary>
    /// Ensures the failure message clearly indicates when the actual output has extra lines.
    /// </summary>
    [Test]
    public async Task EqualIgnoringLeadingWhitespace_WhenActualHasExtraLines_ReportsMismatch()
    {
        var expected = "a\n";
        var actual = "a\nextra\n";

        Exception? caughtException = null;
        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        await Assert.That(caughtException).IsNotNull();
        var message = caughtException!.Message;
        await Assert.That(message).Contains("Expected lines: 2");
        await Assert.That(message).Contains("Actual lines:   3");
        await Assert.That(message).Contains("First difference: line 2");
    }

    /// <summary>
    /// Ensures trailing whitespace differences are explained via codepoint output.
    /// </summary>
    [Test]
    public async Task EqualIgnoringLeadingWhitespace_WhenTrailingSpaceDiffers_ShowsCodepoints()
    {
        var expected = "abc \n";
        var actual = "abc\n";

        Exception? caughtException = null;
        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        await Assert.That(caughtException).IsNotNull();
        var message = caughtException!.Message;
        await Assert.That(message).Contains("Codepoint diagnostics at first difference:");
        await Assert.That(message).Contains("U+0020");
        await Assert.That(message).Contains("<EOL>");
    }
}
