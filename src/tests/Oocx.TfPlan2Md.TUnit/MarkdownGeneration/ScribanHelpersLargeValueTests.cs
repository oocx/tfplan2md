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
    private const string SimpleDiffFormat = "simple-diff";
    private const string ValueText = "value";
    private const string BeforeText = "before";
    private const string AfterText = "after";

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
        var result = FormatLargeValue(null, ValueText, SimpleDiffFormat);

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Test]
    public void FormatLargeValue_Create_JsonContent_UsesJsonFenceAndPrettyPrint()
    {
        var result = FormatLargeValue(null, "{\"a\":1,\"b\":[1,2]}", SimpleDiffFormat);

        result.Should().StartWith("```json\n");
        result.Should().Contain("\n  \"a\": 1");
        result.Should().Contain("\n  \"b\": [");
        result.Should().EndWith("```");
    }

    [Test]
    public void FormatLargeValue_Create_XmlContent_UsesXmlFenceAndPrettyPrint()
    {
        var result = FormatLargeValue(null, "<root><child>value</child></root>", SimpleDiffFormat);

        result.Should().StartWith("```xml\n");
        result.Should().Contain("\n  <child>value</child>");
        result.Should().EndWith("```");
    }

    [Test]
    public void FormatLargeValue_Create_AlreadyFormattedJson_PreservesFormatting()
    {
        var formatted = "{\n  \"a\": 1\n}";

        var result = FormatLargeValue(null, formatted, SimpleDiffFormat);

        result.Should().Be($"```json\n{formatted}\n```");
    }

    [Test]
    public void FormatLargeValue_Delete_ShowsSingleCodeBlock()
    {
        var result = FormatLargeValue(ValueText, null, SimpleDiffFormat);

        result.Should().StartWith("```\n");
        result.Should().Contain("value");
        result.Should().EndWith("```");
        result.Should().NotContain("- ");
        result.Should().NotContain("+ ");
    }

    [Test]
    public void FormatLargeValue_Update_UsesDiffFence()
    {
        var result = FormatLargeValue("old", "new", SimpleDiffFormat);

        result.Should().StartWith("```diff\n");
        result.Should().Contain("- old");
        result.Should().Contain("+ new");
        result.Should().EndWith("```");
    }

    [Test]
    public void FormatLargeValue_Update_JsonSimpleDiff_UsesPrettyPrintedLines()
    {
        var result = FormatLargeValue("{\"a\":1}", "{\"a\":2}", SimpleDiffFormat);

        result.Should().StartWith("```diff\n");
        result.Should().Contain("-   \"a\": 1");
        result.Should().Contain("+   \"a\": 2");
    }

    [Test]
    public void FormatLargeValue_Update_JsonInlineDiff_UsesPrettyPrintedLines()
    {
        var result = FormatLargeValue("{\"a\":1}", "{\"a\":2}", "inline-diff");

        result.Should().StartWith("<pre style=\"font-family: monospace; line-height: 1.5;\"><code>");
        result.Should().Contain("&quot;a&quot;:");
        result.Should().Contain("background-color: #ffc0c0");
        result.Should().Contain("background-color: #acf2bd");
        result.Should().EndWith("</code></pre>");
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
                [BeforeText] = "a\nb",
                [AfterText] = "a\nc"
            },
            new ScriptObject
            {
                ["name"] = "data",
                [BeforeText] = "x",
                [AfterText] = "x"
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
            [BeforeText] = "line1",
            [AfterText] = "line2"
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
            [BeforeText] = "first",
            [AfterText] = "second"
        };

        var dictionary = new Dictionary<string, object?>
        {
            ["name"] = "dictionary",
            [BeforeText] = "alpha",
            [AfterText] = "beta"
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
