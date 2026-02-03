using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for JSON conversion helpers used by Scriban templates.
/// </summary>
public class ScribanHelpersJsonConversionTests
{
    /// <summary>
    /// Ensures JSON objects are converted into ScriptObject entries.
    /// </summary>
    [Test]
    public async Task ConvertToScriptObject_WithJsonObject_MapsProperties()
    {
        var element = JsonDocument.Parse("""
            {
                "name": "demo",
                "count": 1,
                "enabled": true
            }
            """).RootElement;

        var result = ConvertToScriptObject(element) as ScriptObject;

        result.Should().NotBeNull();
        result!["name"].Should().Be("demo");
        result["count"].Should().Be(1L);
        result["enabled"].Should().Be(true);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures JSON arrays are converted into ScriptArray values.
    /// </summary>
    [Test]
    public async Task ConvertToScriptObject_WithJsonArray_MapsItems()
    {
        var element = JsonDocument.Parse("""
            [
                "value",
                1.5,
                { "nested": "item" }
            ]
            """).RootElement;

        var result = ConvertToScriptObject(element) as ScriptArray;

        result.Should().NotBeNull();
        result![0].Should().Be("value");
        result[1].Should().Be(1.5d);
        var nested = result[2] as ScriptObject;
        nested?["nested"].Should().Be("item");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures dictionary conversion ignores non-object JsonElements.
    /// </summary>
    [Test]
    public async Task ToDictionary_WithNonObjectJson_ReturnsEmpty()
    {
        var element = JsonDocument.Parse("""
            [1, 2]
            """).RootElement;

        var result = ToDictionary(element);

        result.Should().BeEmpty();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ensures dictionary conversion handles ScriptObject inputs.
    /// </summary>
    [Test]
    public async Task ToDictionary_WithScriptObject_ReturnsValues()
    {
        var scriptObject = new ScriptObject
        {
            ["name"] = "demo",
            ["count"] = 2
        };

        var result = ToDictionary(scriptObject);

        result.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["name"] = "demo",
            ["count"] = 2
        });

        await Task.CompletedTask;
    }
}
