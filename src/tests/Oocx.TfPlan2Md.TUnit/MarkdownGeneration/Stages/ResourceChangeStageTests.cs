using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
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
    /// Creates a stage instance with default test dependencies.
    /// </summary>
    /// <returns>A configured resource-change stage.</returns>
    private static ResourceChangeStage CreateStage()
    {
        return new ResourceChangeStage(
            new ResourceSummaryBuilder(),
            showSensitive: false,
            showUnchangedValues: false,
            new ResourceViewModelFactoryRegistry(),
            new NullPrincipalMapper(),
            iconProviderRegistry: null);
    }
}
