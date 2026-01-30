using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for Scriban array diff helpers.
/// Related feature: docs/features/024-visual-report-enhancements/specification.md.
/// </summary>
public class ScribanHelpersDiffArrayTests
{
    /// <summary>
    /// Verifies null inputs return empty diff buckets.
    /// </summary>
    [Test]
    public void DiffArray_NullArrays_ReturnsEmptyBuckets()
    {
        var result = ScribanHelpers.DiffArray(null, null, "name");

        ((ScriptArray)result["added"]).Should().BeEmpty();
        ((ScriptArray)result["removed"]).Should().BeEmpty();
        ((ScriptArray)result["modified"]).Should().BeEmpty();
        ((ScriptArray)result["unchanged"]).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies JSON arrays produce added, removed, and unchanged buckets.
    /// </summary>
    [Test]
    public void DiffArray_JsonArrays_ProducesExpectedBuckets()
    {
        using var beforeDoc = JsonDocument.Parse("""
        [
          { "name": "a", "value": 1 },
          { "name": "b", "value": 2 }
        ]
        """);
        using var afterDoc = JsonDocument.Parse("""
        [
          { "name": "b", "value": 2 },
          { "name": "c", "value": 3 }
        ]
        """);

        var result = ScribanHelpers.DiffArray(beforeDoc.RootElement, afterDoc.RootElement, "name");

        ((ScriptArray)result["added"]).Should().HaveCount(1);
        ((ScriptArray)result["removed"]).Should().HaveCount(1);
        ((ScriptArray)result["unchanged"]).Should().HaveCount(1);
        ((ScriptArray)result["modified"]).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies modified items are detected for script arrays.
    /// </summary>
    [Test]
    public void DiffArray_ScriptArrays_DetectsModifiedItems()
    {
        var before = new ScriptArray
        {
            new ScriptObject
            {
                ["name"] = "item",
                ["value"] = "before"
            }
        };
        var after = new ScriptArray
        {
            new ScriptObject
            {
                ["name"] = "item",
                ["value"] = "after"
            }
        };

        var result = ScribanHelpers.DiffArray(before, after, "name");

        ((ScriptArray)result["modified"]).Should().ContainSingle();
    }

    /// <summary>
    /// Verifies nested arrays are compared recursively for equality.
    /// </summary>
    [Test]
    public void DiffArray_NestedArrays_DetectsUnchanged()
    {
        var before = new ScriptArray
        {
            new ScriptObject
            {
                ["name"] = "item",
                ["values"] = new ScriptArray { 1, 2 }
            }
        };
        var after = new ScriptArray
        {
            new ScriptObject
            {
                ["name"] = "item",
                ["values"] = new ScriptArray { 1, 2 }
            }
        };

        var result = ScribanHelpers.DiffArray(before, after, "name");

        ((ScriptArray)result["unchanged"]).Should().ContainSingle();
        ((ScriptArray)result["modified"]).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies JsonElement items inside ScriptArray are supported.
    /// </summary>
    [Test]
    public void DiffArray_ScriptArrayWithJsonElements_ParsesKeys()
    {
        using var document = JsonDocument.Parse("""
        [
          { "name": "item", "value": 1 }
        ]
        """);
        var element = document.RootElement.EnumerateArray().First();
        var scriptArray = new ScriptArray { element };

        var result = ScribanHelpers.DiffArray(scriptArray, null, "name");

        ((ScriptArray)result["removed"]).Should().ContainSingle();
    }

    /// <summary>
    /// Verifies missing keys in JSON arrays throw helper exceptions.
    /// </summary>
    [Test]
    public void DiffArray_JsonArrayMissingKey_ThrowsException()
    {
        using var document = JsonDocument.Parse("""
        [
          { "value": 1 }
        ]
        """);

        var act = () => ScribanHelpers.DiffArray(document.RootElement, null, "name");

        act.Should().Throw<ScribanHelperException>();
    }

    /// <summary>
    /// Verifies missing key properties throw a helper exception.
    /// </summary>
    [Test]
    public void DiffArray_MissingKey_ThrowsException()
    {
        var before = new ScriptArray { new ScriptObject() };

        var act = () => ScribanHelpers.DiffArray(before, null, "name");

        act.Should().Throw<ScribanHelperException>();
    }
}
