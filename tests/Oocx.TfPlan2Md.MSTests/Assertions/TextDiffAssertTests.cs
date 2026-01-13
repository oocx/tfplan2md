
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

        var exception = Assert.ThrowsAny<XunitException>(() => TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual));

        Assert.Contains("First difference: line 3, column 3", exception.Message);
        Assert.Contains("Context (escaped, expected vs actual):", exception.Message);
        Assert.Contains("L0003", exception.Message);
    }

    /// <summary>
    /// Ensures the failure message clearly indicates when the actual output has extra lines.
    /// </summary>
    [TestMethod]
    public void EqualIgnoringLeadingWhitespace_WhenActualHasExtraLines_ReportsMismatch()
    {
        var expected = "a\n";
        var actual = "a\nextra\n";

        var exception = Assert.ThrowsAny<XunitException>(() => TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual));

        Assert.Contains("Expected lines: 2", exception.Message);
        Assert.Contains("Actual lines:   3", exception.Message);
        Assert.Contains("First difference: line 2", exception.Message);
    }

    /// <summary>
    /// Ensures trailing whitespace differences are explained via codepoint output.
    /// </summary>
    [TestMethod]
    public void EqualIgnoringLeadingWhitespace_WhenTrailingSpaceDiffers_ShowsCodepoints()
    {
        var expected = "abc \n";
        var actual = "abc\n";

        var exception = Assert.ThrowsAny<XunitException>(() => TextDiffAssert.EqualIgnoringLeadingWhitespace(expected, actual));

        Assert.Contains("Codepoint diagnostics at first difference:", exception.Message);
        Assert.Contains("U+0020", exception.Message);
        Assert.Contains("<EOL>", exception.Message);
    }
}
