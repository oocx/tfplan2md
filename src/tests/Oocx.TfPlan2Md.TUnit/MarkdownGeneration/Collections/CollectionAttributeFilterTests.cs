using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// Tests the element-wise attribute filter for collection rows (TC-100 to TC-103).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class CollectionAttributeFilterTests
{
    private sealed class RecordingFilter(Func<AttributeChangeFilterContext, bool> decision) : IAttributeChangeFilter
    {
        public List<AttributeChangeFilterContext> Calls { get; } = [];

        public bool ShouldSuppress(AttributeChangeFilterContext context)
        {
            Calls.Add(context);
            return decision(context);
        }
    }

    private static CollectionElementModel El(int index, string value)
        => new(index, value, JsonValueKind.String, CollectionElementStatus.Unchanged);

    private static ResourceChangeModel Resource(string[] before, string[] after) => new()
    {
        Address = "example_resource.main",
        ModuleAddress = string.Empty,
        Type = "example_resource",
        Name = "main",
        ProviderName = "example",
        Action = "update",
        ActionSymbol = ActionIcons.Update,
        AttributeChanges = new List<AttributeChangeModel>
        {
            new()
            {
                Name = "ids",
                Before = "[]",
                After = "[]",
                Collection = new CollectionChangeModel
                {
                    BeforeState = CollectionState.Elements,
                    AfterState = CollectionState.Elements,
                    Before = before.Select((v, i) => El(i, v)).ToList(),
                    After = after.Select((v, i) => El(i, v)).ToList()
                }
            }
        }
    };

    private static IReadOnlyList<ResourceChangeModel> Filter(ResourceChangeModel resource, RecordingFilter filter)
    {
        var registry = new AttributeChangeFilterRegistry();
        registry.Register(filter);
        return new AttributeFilteringStage(true, registry, new ResourceSummaryBuilder()).Build([resource]);
    }

    [Test]
    public void AttributeFilter_AllDifferingPositionsSuppressed_RowRemoved()
    {
        var filter = new RecordingFilter(context => string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase));

        var result = Filter(Resource(["A", "same", "B"], ["a", "same", "b"]), filter);

        result[0].AttributeChanges.Should().BeEmpty();
    }

    [Test]
    public void AttributeFilter_OneNonSuppressedDifference_RowRenders()
    {
        var filter = new RecordingFilter(context => string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase));

        var result = Filter(Resource(["A", "x"], ["a", "y"]), filter);

        result[0].AttributeChanges.Should().ContainSingle();
    }

    [Test]
    public void AttributeFilter_DifferentLengths_RowRendersWithoutConsultingFilter()
    {
        var filter = new RecordingFilter(_ => true);

        var result = Filter(Resource(["A"], ["a", "b"]), filter);

        result[0].AttributeChanges.Should().ContainSingle();
        filter.Calls.Should().BeEmpty();
    }

    [Test]
    public void AttributeFilter_CollectionRow_CallsRegistryWithIndexedNameAndElementTexts()
    {
        var filter = new RecordingFilter(_ => true);

        Filter(Resource(["A", "same", "B"], ["a", "same", "b"]), filter);

        filter.Calls.Select(c => (c.AttributeName, c.BeforeValue, c.AfterValue)).Should().Equal(
            ("ids[0]", "A", "a"),
            ("ids[2]", "B", "b"));
    }

    private static string AzureId(string group) => $"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{group}/providers/Microsoft.Network/virtualNetworks/vnet";

    private static ReportModel BuildWithAzureRmFilter(Resource resource)
    {
        var filterRegistry = new AttributeChangeFilterRegistry();
        new AzureRMModule(LargeValueFormat.InlineDiff, new NullPrincipalMapper()).RegisterAttributeChangeFilters(filterRegistry);
        var plan = new Oocx.TfPlan2Md.Parsing.TerraformPlanParser().Parse(PlanJson([resource]));
        return new ReportModelBuilder(
            options: new ReportModelBuilderOptions(IgnoreAzureIdCaseChanges: true),
            services: new ReportModelBuilderServices(AttributeChangeFilterRegistry: filterRegistry)).Build(plan);
    }

    [Test]
    public void AzureRmCaseFilter_CaseOnlyAzureIdList_ProducesNoRow()
    {
        var resource = Res(
            ["update"],
            "{\"subnet_ids\":" + L(AzureId("RG-A"), AzureId("RG-B")) + ",\"name\":\"n\"}",
            "{\"subnet_ids\":" + L(AzureId("rg-a"), AzureId("rg-b")) + ",\"name\":\"n\"}",
            address: "azurerm_thing.main",
            type: "azurerm_thing",
            provider: "registry.terraform.io/hashicorp/azurerm");

        var model = BuildWithAzureRmFilter(resource);

        model.Changes.SelectMany(c => c.AttributeChanges).Should().BeEmpty();
    }

    [Test]
    public void AzureRmCaseFilter_PartiallyCaseOnlyAzureIdList_ShowsRowWithRemovedAndAdded()
    {
        var resource = Res(
            ["update"],
            "{\"subnet_ids\":" + L(AzureId("RG-A"), AzureId("rg-b")) + "}",
            "{\"subnet_ids\":" + L(AzureId("rg-a"), AzureId("rg-c")) + "}",
            address: "azurerm_thing.main",
            type: "azurerm_thing",
            provider: "registry.terraform.io/hashicorp/azurerm");

        var row = BuildWithAzureRmFilter(resource).Changes.Single().AttributeChanges.Single();

        row.Collection!.Before.Should().OnlyContain(e => e.Status == CollectionElementStatus.Removed);
        row.Collection.After.Should().OnlyContain(e => e.Status == CollectionElementStatus.Added);
    }
}
