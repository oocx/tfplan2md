using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Stages;

/// <summary>
/// Tests for the explicit resource-change pipeline stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ResourceChangeStageTests
{
    /// <summary>
    /// Verifies the stage creates one resource model per plan resource.
    /// </summary>
    [Test]
    public void ResourceChangeStage_Build_ProducesOneModelPerPlanResource()
    {
        var stage = CreateStage();
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new(
                    "type_a.first",
                    null,
                    "managed",
                    "type_a",
                    "first",
                    "provider",
                    new Change(["create"])),
                new(
                    "type_b.second",
                    null,
                    "managed",
                    "type_b",
                    "second",
                    "provider",
                    new Change(["update"])),
                new(
                    "type_c.third",
                    null,
                    "managed",
                    "type_c",
                    "third",
                    "provider",
                    new Change(["delete"]))
            });

        var models = stage.Build(plan);

        models.Should().HaveCount(3);
        models.Select(model => model.Address).Should().Equal("type_a.first", "type_b.second", "type_c.third");
    }

    /// <summary>
    /// TC-113-3: Build() with null ResourceChanges should return an empty list, not throw.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void ResourceChangeStage_Build_NullResourceChanges_ReturnsEmptyList()
    {
        var stage = CreateStage();
        // Simulate what System.Text.Json does when "resource_changes" is absent or null:
        // it sets the property to null even though it is declared non-nullable.
        var plan = new TerraformPlan("1.0", "1.0", null!);

        var models = stage.Build(plan);

        models.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies mapped Azure DevOps descriptors remain in the attribute table even when their raw values exceed the large-value threshold.
    /// Related issue: https://github.com/oocx/tfplan2md/issues/667.
    /// </summary>
    [Test]
    public void ResourceChangeStage_Build_MappedLongAzureDevOpsDescriptors_AreNotLarge()
    {
        const string groupDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw";
        const string memberDescriptor = "aad.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5LTI0MDI5ODY0MTMtMjE3OTQwODYxNi0zLTM4Mzg1ODYwMTUtMzIyMTk1OTc5OC0xMjM0NTY3ODkw";
        var formatterRegistry = CreateAzureDevOpsFormatterRegistry(groupDescriptor, memberDescriptor);
        var stage = CreateStage(formatterRegistry);
        using var document = JsonDocument.Parse($$"""{"group":"{{groupDescriptor}}","members":["{{memberDescriptor}}"]}""");
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            [new ResourceChange(
                "azuredevops_group_membership.example",
                null,
                "managed",
                "azuredevops_group_membership",
                "example",
                "registry.terraform.io/microsoft/azuredevops",
                new Change(["create"], after: document.RootElement.Clone()))]);

        var attributes = stage.Build(plan).Single().AttributeChanges;

        attributes.Should().ContainSingle(attribute => attribute.Name == "group" && !attribute.IsLarge);
        attributes.Should().ContainSingle(attribute => attribute.Name == "members[0]" && !attribute.IsLarge);
    }

    /// <summary>
    /// Creates a stage instance with default test dependencies.
    /// </summary>
    /// <returns>A configured resource-change stage.</returns>
    private static ResourceChangeStage CreateStage(ValueFormatterRegistry? valueFormatterRegistry = null)
    {
        return new ResourceChangeStage(
            new ResourceSummaryBuilder(),
            showSensitive: false,
            showUnchangedValues: false,
            new ResourceViewModelFactoryRegistry(),
            new NullPrincipalMapper(),
            iconProviderRegistry: null,
            valueFormatterRegistry: valueFormatterRegistry);
    }

    private static ValueFormatterRegistry CreateAzureDevOpsFormatterRegistry(string groupDescriptor, string memberDescriptor)
    {
        var module = new AzureDevOpsModule(
            azdoUserMapper: new AzdoUserMapper(
                new Dictionary<string, string> { [memberDescriptor] = "Build Service" }.ToFrozenDictionary(),
                diagnostics: null),
            azdoGroupMapper: new AzdoGroupMapper(
                new Dictionary<string, string> { [groupDescriptor] = "Readers" }.ToFrozenDictionary(),
                diagnostics: null));
        var registry = new ValueFormatterRegistry();
        module.RegisterValueFormatters(registry);
        return registry;
    }
}
