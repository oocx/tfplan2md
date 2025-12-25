using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;

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

    [Fact]
    public void FormatLargeValue_InlineDiff_WithCommonLines_RendersPreWithStyles()
    {
        var before = "common\nold";
        var after = "common\nnew";

        var result = ScribanHelpers.FormatLargeValue(before, after, "inline-diff");

        result.Should().StartWith("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
        result.Should().Contain("common\n");
        result.Should().Contain("background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;");
        result.Should().Contain("background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: -4px;");
        result.Should().Contain("background-color: #ffc0c0;");
        result.Should().Contain("background-color: #acf2bd;");
        result.Should().EndWith("</code></pre>");
    }

    [Fact]
    public void FormatLargeValue_InlineDiff_NoCommonLines_ShowsBeforeAfterBlocks()
    {
        var before = "foo";
        var after = "bar";

        var result = ScribanHelpers.FormatLargeValue(before, after, "inline-diff");

        result.Should().StartWith("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
        result.Should().Contain("foo");
        result.Should().Contain("bar");
        result.Should().Contain("background-color: #fff5f5");
        result.Should().Contain("background-color: #f0fff4");
        result.Should().EndWith("</code></pre>");
        result.Should().NotContain("**Before:**");
    }

    [Fact]
    public void LargeAttributesSummary_ComputesCounts()
    {
        var attrs = new ScriptArray
        {
            new ScriptObject
            {
                ["name"] = "policy",
                ["before"] = "a\nb",
                ["after"] = "a\nc"
            },
            new ScriptObject
            {
                ["name"] = "data",
                ["before"] = "x",
                ["after"] = "x"
            }
        };

        var summary = ScribanHelpers.LargeAttributesSummary(attrs);

        summary.Should().Be("Large values: policy (3 lines, 2 changed), data (1 line, 0 changed)");
    }
}
