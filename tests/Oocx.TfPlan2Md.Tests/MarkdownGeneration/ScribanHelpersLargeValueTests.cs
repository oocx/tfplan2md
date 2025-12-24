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

    [Fact]
    public void FormatLargeValue_Create_ShowsSingleCodeBlock()
    {
        var result = ScribanHelpers.FormatLargeValue(null, "value", "standard-diff");

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Fact]
    public void FormatLargeValue_Delete_ShowsSingleCodeBlock()
    {
        var result = ScribanHelpers.FormatLargeValue("value", null, "standard-diff");

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Fact]
    public void FormatLargeValue_Update_UsesDiffFence()
    {
        var result = ScribanHelpers.FormatLargeValue("old", "new", "standard-diff");

        result.Should().StartWith("```diff\n");
        result.Should().Contain("- old");
        result.Should().Contain("+ new");
        result.Should().EndWith("```");
    }
}
