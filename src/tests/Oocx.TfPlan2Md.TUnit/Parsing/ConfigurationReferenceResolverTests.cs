using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Tests configuration reference index building for parent-child matching.
/// </summary>
public class ConfigurationReferenceResolverTests
{
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Ensures references are captured for root module resources.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_RootModule_ContainsReferences()
    {
        var json = File.ReadAllText("TestData/azuread-group-members-known-after-apply-plan.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("azuread_group_member.platform_admin_member", "group_object_id")).Should().BeTrue();
        index[("azuread_group_member.platform_admin_member", "group_object_id")]
            .Should()
            .Contain("azuread_group.platform_engineers.id");
    }

    /// <summary>
    /// Ensures missing configuration returns an empty index.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_NullConfiguration_ReturnsEmpty()
    {
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(null);

        index.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures nested module references are qualified with module prefixes.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_NestedModules_QualifiesAddresses()
    {
        var json = File.ReadAllText("TestData/configuration-with-nested-modules.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("module.security.azuread_group_member.member1", "group_object_id")).Should().BeTrue();
        index[("module.security.azuread_group_member.member1", "group_object_id")]
            .Should()
            .Contain("module.security.azuread_group.admins.id");
    }

    /// <summary>
    /// Ensures for_each resources are indexed by their base address.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_ForEach_UsesBaseAddress()
    {
        var json = File.ReadAllText("TestData/configuration-with-for-each.json");
        var plan = _parser.Parse(json);

        var index = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);

        index.ContainsKey(("azuread_group_member.members", "group_object_id")).Should().BeTrue();
    }

    /// <summary>
    /// Ensures reference index building completes quickly for large configurations.
    /// </summary>
    [Test]
    public void BuildReferenceIndex_LargeConfiguration_CompletesQuickly()
    {
        const int resourceCount = 1000;
        var configuration = BuildLargeConfiguration(resourceCount);

        var stopwatch = Stopwatch.StartNew();
        var index = ConfigurationReferenceResolver.BuildReferenceIndex(configuration);
        stopwatch.Stop();

        index.Should().HaveCount(resourceCount);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Builds a large configuration block for performance testing.
    /// </summary>
    /// <param name="resourceCount">The number of resources to include.</param>
    /// <returns>The configuration element.</returns>
    private static JsonElement BuildLargeConfiguration(int resourceCount)
    {
        var builder = new StringBuilder();
        builder.Append("{\"root_module\":{\"resources\":[");

        for (var index = 0; index < resourceCount; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"address\":\"custom_child.member");
            builder.Append(index);
            builder.Append("\",\"mode\":\"managed\",\"type\":\"custom_child\",\"name\":\"member");
            builder.Append(index);
            builder.Append("\",\"expressions\":{\"parent_id\":{\"references\":[\"custom_parent.team.id\"]}}}");
        }

        builder.Append("]}}");

        return JsonDocument.Parse(builder.ToString()).RootElement;
    }
}
