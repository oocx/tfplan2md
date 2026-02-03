using AwesomeAssertions;
using Scriban.Runtime;
using TUnit.Core;
using AzApiHelpers = Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Unit tests for AzAPI prefix grouping logic.
/// Related feature: docs/features/050-azapi-attribute-grouping/test-plan.md.
/// </summary>
public class ScribanHelpersAzApiGroupingTests
{
    /// <summary>
    /// TC-02: Verifies the fixed threshold requires at least three attributes per group.
    /// </summary>
    [Test]
    public async Task IdentifyGroupedPrefixes_ThresholdRequiresThreeAttributes()
    {
        var belowThreshold = CreateProperties("alpha.one", "alpha.two");
        var belowResult = AzApiHelpers.IdentifyGroupedPrefixes(belowThreshold);

        belowResult.Should().BeEmpty();

        var atThreshold = CreateProperties("alpha.one", "alpha.two", "alpha.three");
        var atThresholdResult = AzApiHelpers.IdentifyGroupedPrefixes(atThreshold);

        atThresholdResult.Should().ContainSingle();
        atThresholdResult[0].Path.Should().Be("alpha");
        atThresholdResult[0].Kind.Should().Be(AzApiHelpers.AzApiGroupedPrefixKind.Prefix);
        atThresholdResult[0].MemberIndexes.Should().Equal(0, 1, 2);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-01: Verifies the longest common prefix wins when both parent and child qualify.
    /// </summary>
    [Test]
    public async Task IdentifyGroupedPrefixes_LongestPrefixWinsForNestedGroups()
    {
        var properties = CreateProperties(
            "foo.bar.one",
            "foo.bar.two",
            "foo.bar.three",
            "foo.baz.one");

        var result = AzApiHelpers.IdentifyGroupedPrefixes(properties);

        result.Should().ContainSingle();
        result[0].Path.Should().Be("foo.bar");
        result[0].Kind.Should().Be(AzApiHelpers.AzApiGroupedPrefixKind.Prefix);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-03: Verifies array paths are identified as array groups.
    /// </summary>
    [Test]
    public async Task IdentifyGroupedPrefixes_ArrayGroupsAreDetected()
    {
        var properties = CreateProperties(
            "tags[0].key",
            "tags[0].value",
            "tags[1].key");

        var result = AzApiHelpers.IdentifyGroupedPrefixes(properties);

        result.Should().ContainSingle();
        result[0].Path.Should().Be("tags");
        result[0].Kind.Should().Be(AzApiHelpers.AzApiGroupedPrefixKind.Array);
        result[0].MemberIndexes.Should().Equal(0, 1, 2);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-04: Verifies non-array prefixes can group attributes that include arrays below the array threshold.
    /// </summary>
    [Test]
    public async Task IdentifyGroupedPrefixes_NonArrayGroupingWorksWhenArrayBelowThreshold()
    {
        var properties = CreateProperties(
            "cors.allowedOrigins[0]",
            "cors.allowedOrigins[1]",
            "cors.supportCredentials");

        var result = AzApiHelpers.IdentifyGroupedPrefixes(properties);

        result.Should().ContainSingle();
        result[0].Path.Should().Be("cors");
        result[0].Kind.Should().Be(AzApiHelpers.AzApiGroupedPrefixKind.Prefix);

        await Task.CompletedTask;
    }

    /// <summary>
    /// TC-11: Verifies group ordering follows the first occurrence of attributes in the input.
    /// </summary>
    [Test]
    public async Task IdentifyGroupedPrefixes_PreservesFirstOccurrenceOrdering()
    {
        var properties = CreateProperties(
            "z.one",
            "z.two",
            "z.three",
            "a.one",
            "a.two",
            "a.three");

        var result = AzApiHelpers.IdentifyGroupedPrefixes(properties);

        result.Should().HaveCount(2);
        result[0].Path.Should().Be("z");
        result[1].Path.Should().Be("a");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Builds a ScriptArray of property objects for grouping tests.
    /// </summary>
    /// <param name="paths">The property paths to include.</param>
    /// <returns>A ScriptArray containing each path as a ScriptObject entry.</returns>
    private static ScriptArray CreateProperties(params string[] paths)
    {
        var properties = new ScriptArray();

        foreach (var path in paths)
        {
            properties.Add(new ScriptObject
            {
                ["path"] = path
            });
        }

        return properties;
    }
}
