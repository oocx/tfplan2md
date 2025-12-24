using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersLargeValueTests
{
    [Fact]
    public void IsLargeValue_WithNewlines_ReturnsTrue()
    {
        ScribanHelpers.IsLargeValue("line1\nline2").Should().BeTrue();
        ScribanHelpers.IsLargeValue("line1\r\nline2").Should().BeTrue();
    }

    [Fact]
    public void IsLargeValue_LongSingleLine_ReturnsTrue()
    {
        var input = new string('a', 101);
        ScribanHelpers.IsLargeValue(input).Should().BeTrue();
    }

    [Fact]
    public void IsLargeValue_ShortOrEmpty_ReturnsFalse()
    {
        ScribanHelpers.IsLargeValue("short value").Should().BeFalse();
        ScribanHelpers.IsLargeValue(new string('a', 100)).Should().BeFalse();
        ScribanHelpers.IsLargeValue(string.Empty).Should().BeFalse();
        ScribanHelpers.IsLargeValue(null).Should().BeFalse();
    }
}
