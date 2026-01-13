
namespace Oocx.TfPlan2Md.Tests.Assertions;

/// <summary>
/// Verifies that <see cref="TextDiffAssert"/> produces actionable diagnostics on mismatches.
/// Related feature: docs/features/030-terraform-show-approximation/
/// </summary>
[TestClass]
public sealed class TextDiffAssertTests
{
    /// <summary>
    /// Ensures the failure message includes the first differing line and column.
    /// </summary>
    [TestMethod]
    public void EqualIgnoringLeadingWhitespace_WhenDifferent_ReportsLineAndColumn()
    {
        var expected = "a\n  b\nxyz\n";
        var actual = "a\n  b\nxyZ\n";

        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
            Assert.Fail("Expected AssertFailedException to be thrown");
        }
        catch (AssertFailedException exception)
        {
            Assert.IsTrue(exception.Message.Contains("First difference: line 3, column 3"));
            Assert.IsTrue(exception.Message.Contains("Context (escaped, expected vs actual):"));
            Assert.IsTrue(exception.Message.Contains("L0003"));
        }
    }

    /// <summary>
    /// Ensures the failure message clearly indicates when the actual output has extra lines.
    /// </summary>
    [TestMethod]
    public void EqualIgnoringLeadingWhitespace_WhenActualHasExtraLines_ReportsMismatch()
    {
        var expected = "a\n";
        var actual = "a\nextra\n";

        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
            Assert.Fail("Expected AssertFailedException to be thrown");
        }
        catch (AssertFailedException exception)
        {
            Assert.IsTrue(exception.Message.Contains("Expected lines: 2"));
            Assert.IsTrue(exception.Message.Contains("Actual lines:   3"));
            Assert.IsTrue(exception.Message.Contains("First difference: line 2"));
        }
    }

    /// <summary>
    /// Ensures trailing whitespace differences are explained via codepoint output.
    /// </summary>
    [TestMethod]
    public void EqualIgnoringLeadingWhitespace_WhenTrailingSpaceDiffers_ShowsCodepoints()
    {
        var expected = "abc \n";
        var actual = "abc\n";

        try
        {
            TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual);
            Assert.Fail("Expected AssertFailedException to be thrown");
        }
        catch (AssertFailedException exception)
        {
            Assert.IsTrue(exception.Message.Contains("Codepoint diagnostics at first difference:"));
            Assert.IsTrue(exception.Message.Contains("U+0020"));
            Assert.IsTrue(exception.Message.Contains("<EOL>"));
        }
    }
}
