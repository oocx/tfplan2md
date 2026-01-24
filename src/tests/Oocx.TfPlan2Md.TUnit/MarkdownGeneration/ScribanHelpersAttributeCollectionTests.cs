using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for attribute collection helper exported to Scriban templates.
/// </summary>
public class ScribanHelpersAttributeCollectionTests
{
    [SuppressMessage("Major Code Smell", "S3011", Justification = "Validating helper exported to Scriban via reflection only available through private method.")]
    [Test]
    public async Task CollectAttributes_MergesKeysAndSkipsNulls()
    {
        var collectMethod = typeof(ScribanHelpers).GetMethod("CollectAttributes", BindingFlags.Static | BindingFlags.NonPublic);
        collectMethod.Should().NotBeNull();

        var before = new ScriptObject
        {
            ["name"] = "before",
            ["keep"] = "value"
        };

        var after = new ScriptObject
        {
            ["keep"] = "value",
            ["added"] = "after",
            ["null"] = null
        };

        var result = (ScriptArray?)collectMethod!.Invoke(null, new object?[] { before, after });

        result.Should().NotBeNull();
        result!.Should().Contain("name");
        result.Should().Contain("keep");
        result.Should().Contain("added");
        result.Should().NotContain("null");

        await Task.CompletedTask;
    }
}