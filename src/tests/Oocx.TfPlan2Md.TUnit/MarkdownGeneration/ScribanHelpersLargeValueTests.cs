using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;
using TUnit.Core;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ScribanHelpersLargeValueTests
{
    [Test]
    public void IsLargeValue_WithNewlines_ReturnsTrue()
    {
        IsLargeValue("line1\nline2").Should().BeTrue();
        IsLargeValue("line1\r\nline2").Should().BeTrue();
    }

    [Test]
    public void IsLargeValue_LongSingleLine_ReturnsTrue()
    {
        var input = new string('a', 101);
        IsLargeValue(input).Should().BeTrue();
    }

    [Test]
    public void IsLargeValue_ShortOrEmpty_ReturnsFalse()
    {
        IsLargeValue("short value").Should().BeFalse();
        IsLargeValue(new string('a', 100)).Should().BeFalse();
        IsLargeValue(string.Empty).Should().BeFalse();
        IsLargeValue(null).Should().BeFalse();
    }

    [Test]
    public void IsLargeValue_AzureResourceIdWithAzProvider_ReturnsFalse()
    {
        const string providerName = "registry.terraform.io/hashicorp/azurerm";
        var longId = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-with-a-very-long-name-to-force-length-over-threshold/providers/Microsoft.KeyVault/vaults/kv";

        IsLargeValue(longId, providerName).Should().BeFalse();
    }

    [Test]
    public void FormatLargeValue_Create_ShowsSingleCodeBlock()
    {
        var result = FormatLargeValue(null, "value", "simple-diff");

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Test]
    public void FormatLargeValue_Delete_ShowsSingleCodeBlock()
    {
        var result = FormatLargeValue("value", null, "simple-diff");

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Test]
    public void FormatLargeValue_Update_UsesDiffFence()
    {
        var result = FormatLargeValue("old", "new", "simple-diff");

        result.Should().StartWith("```diff\n");
        result.Should().Contain("- old");
        result.Should().Contain("+ new");
        result.Should().EndWith("```");
    }

    [Test]
    public void FormatLargeValue_InlineDiff_WithCommonLines_RendersPreWithStyles()
    {
        var before = "common\nold";
        var after = "common\nnew";

        var result = FormatLargeValue(before, after, "inline-diff");

        result.Should().StartWith("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
        result.Should().Contain("common\n");
        result.Should().Contain("background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: 0;");
        result.Should().Contain("background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: 0;");
        result.Should().Contain("background-color: #ffc0c0;");
        result.Should().Contain("background-color: #acf2bd;");
        result.Should().EndWith("</code></pre>");
    }

    [Test]
    public void FormatLargeValue_InlineDiff_NoCommonLines_ShowsBeforeAfterBlocks()
    {
        var before = "foo";
        var after = "bar";

        var result = FormatLargeValue(before, after, "inline-diff");

        result.Should().StartWith("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
        result.Should().Contain("foo");
        result.Should().Contain("bar");
        result.Should().Contain("background-color: #fff5f5");
        result.Should().Contain("background-color: #f0fff4");
        result.Should().EndWith("</code></pre>");
        result.Should().NotContain("**Before:**");
    }

    [Test]
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

        var summary = LargeAttributesSummary(attrs);

        summary.Should().Be("Large values: policy (3 lines, 2 changes), data (1 line, 0 changes)");
    }

    [Test]
    public void LargeAttributesSummary_WhenAttributesAreNull_ReturnsEmpty()
    {
        var summary = LargeAttributesSummary(null);

        summary.Should().Be(string.Empty);
    }

    [Test]
    public void LargeAttributesSummary_WhenAttributesAreString_ReturnsEmpty()
    {
        var summary = LargeAttributesSummary("not-a-list");

        summary.Should().Be(string.Empty);
    }

    [Test]
    public void LargeAttributesSummary_MapsVariousAttributeShapes()
    {
        var scriptObject = new ScriptObject
        {
            ["name"] = "script",
            ["before"] = "line1",
            ["after"] = "line2"
        };

        var model = new AttributeChangeModel
        {
            Name = "model",
            Before = "before",
            After = "after"
        };

        var roleAttribute = new RoleAssignmentAttributeViewModel
        {
            Name = "role",
            Before = "one",
            After = "two"
        };

        IReadOnlyDictionary<string, object?> readOnlyDictionary = new Dictionary<string, object?>
        {
            ["name"] = "readonly",
            ["before"] = "first",
            ["after"] = "second"
        };

        var dictionary = new Dictionary<string, object?>
        {
            ["name"] = "dictionary",
            ["before"] = "alpha",
            ["after"] = "beta"
        };

        var attrs = new List<object?>
        {
            null,
            scriptObject,
            model,
            roleAttribute,
            readOnlyDictionary,
            dictionary
        };

        var summary = LargeAttributesSummary(attrs);

        summary.Should().Contain("script (2 lines, 2 changes)");
        summary.Should().Contain("model (2 lines, 2 changes)");
        summary.Should().Contain("role (2 lines, 2 changes)");
        summary.Should().Contain("readonly (2 lines, 2 changes)");
        summary.Should().Contain("dictionary (2 lines, 2 changes)");
    }
}
