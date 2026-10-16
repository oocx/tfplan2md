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
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;
using static Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections.CollectionPlanTestSupport;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Collections;

/// <summary>
/// The element-wise case-only filter with masked entries and order changes (TC-101, TC-103, TC-132 order variant, TC-133).
/// Related feature: docs/features/148-compact-collection-diffs/test-plan.md.
/// </summary>
public class MaskedCollectionFilterTests
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

    private static readonly Func<AttributeChangeFilterContext, bool> CaseOnly =
        context => string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase);

    private static CollectionElementModel Visible(int index, string value, CollectionElementStatus status = CollectionElementStatus.Unchanged)
        => new(index, value, JsonValueKind.String, status);

    private static CollectionElementModel Masked(int index, CollectionElementStatus status)
        => new(index, null, JsonValueKind.Undefined, status, IsMasked: true);

    private static IReadOnlyList<ResourceChangeModel> Filter(
        IReadOnlyList<CollectionElementModel> before,
        IReadOnlyList<CollectionElementModel> after,
        RecordingFilter filter,
        bool orderChanged = false)
    {
        var resource = new ResourceChangeModel
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
                        Before = before,
                        After = after,
                        OrderChanged = orderChanged
                    }
                }
            }
        };
        var registry = new AttributeChangeFilterRegistry();
        registry.Register(filter);
        return new AttributeFilteringStage(true, registry, new ResourceSummaryBuilder()).Build([resource]);
    }

    [Test]
    public void AttributeFilter_ChangedMaskedEntryBesideCaseOnlyDifference_RowRenders()
    {
        var filter = new RecordingFilter(CaseOnly);

        var result = Filter(
            [Visible(0, "A"), Masked(1, CollectionElementStatus.Removed)],
            [Visible(0, "a"), Masked(1, CollectionElementStatus.Added)],
            filter);

        result[0].AttributeChanges.Should().ContainSingle();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void AttributeFilter_MaskedEntryOppositeVisibleEntry_RowRenders(bool maskedBefore)
    {
        var filter = new RecordingFilter(_ => true);
        IReadOnlyList<CollectionElementModel> masked = [Masked(0, CollectionElementStatus.Removed)];
        IReadOnlyList<CollectionElementModel> visible = [Visible(0, "a", CollectionElementStatus.Added)];

        var result = maskedBefore ? Filter(masked, visible, filter) : Filter(visible, masked, filter);

        result[0].AttributeChanges.Should().ContainSingle();
        filter.Calls.Should().BeEmpty();
    }

    [Test]
    public void AttributeFilter_UnchangedMaskedPairBesideCaseOnlyDifference_RowSuppressedAndMaskedPositionNeverPassedToRegistry()
    {
        var filter = new RecordingFilter(CaseOnly);

        var result = Filter(
            [Visible(0, "A"), Masked(1, CollectionElementStatus.Unchanged), Visible(2, "same")],
            [Visible(0, "a"), Masked(1, CollectionElementStatus.Unchanged), Visible(2, "same")],
            filter);

        result[0].AttributeChanges.Should().BeEmpty();
        filter.Calls.Select(c => (c.AttributeName, c.BeforeValue, c.AfterValue)).Should().Equal(("ids[0]", "A", "a"));
    }

    [Test]
    public void AttributeFilter_OnlyUnchangedMaskedPairs_RowNotSuppressed()
    {
        var filter = new RecordingFilter(_ => true);

        var result = Filter([Masked(0, CollectionElementStatus.Unchanged)], [Masked(0, CollectionElementStatus.Unchanged)], filter);

        result[0].AttributeChanges.Should().ContainSingle();
    }

    [Test]
    public void AttributeFilter_CaseOnlyPositionsWithOrderChange_RowRenders()
    {
        var filter = new RecordingFilter(CaseOnly);

        var result = Filter([Visible(0, "A"), Visible(1, "a")], [Visible(0, "a"), Visible(1, "A")], filter, orderChanged: true);

        result[0].AttributeChanges.Should().ContainSingle();
    }

    // ---- full pipeline with the AzureRM case-only filter ----

    private static string AzureId(string group) => $"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{group}/providers/Microsoft.Network/virtualNetworks/vnet";

    private static Resource Ids(string[] before, string[] after, string? beforeSensitive = null, string? afterSensitive = null)
        => Res(
            ["update"],
            "{\"subnet_ids\":" + L(before) + ",\"name\":\"n\"}",
            "{\"subnet_ids\":" + L(after) + ",\"name\":\"n\"}",
            address: "azurerm_thing.main",
            type: "azurerm_thing",
            beforeSensitive: beforeSensitive,
            afterSensitive: afterSensitive,
            provider: "registry.terraform.io/hashicorp/azurerm");

    private static string Flags(params bool[] flags) => "{\"subnet_ids\":[" + string.Join(',', flags.Select(f => f ? "true" : "false")) + "]}";

    private static ReportModel BuildWithAzureRmFilter(Resource resource)
    {
        var filterRegistry = new AttributeChangeFilterRegistry();
        new AzureRMModule(LargeValueFormat.InlineDiff, new NullPrincipalMapper()).RegisterAttributeChangeFilters(filterRegistry);
        var plan = new Oocx.TfPlan2Md.Parsing.TerraformPlanParser().Parse(PlanJson([resource]));
        return new ReportModelBuilder(
            options: new ReportModelBuilderOptions(IgnoreAzureIdCaseChanges: true),
            services: new ReportModelBuilderServices(AttributeChangeFilterRegistry: filterRegistry)).Build(plan);
    }

    private static string RenderWithAzureRmFilter(Resource resource) => new MarkdownRenderer().Render(BuildWithAzureRmFilter(resource));

    [Test]
    public void AzureRmCaseFilter_MaskedAzureIdsDifferingOnlyInCase_RowShownWithoutValues()
    {
        var resource = Ids([AzureId("SECRET-RG")], [AzureId("secret-rg")], Flags(true), Flags(true));

        var markdown = RenderWithAzureRmFilter(resource);

        markdown.Should().Contain("| subnet_ids | ➖ `(sensitive)` | ➕ `(sensitive)` |");
        markdown.Should().NotContainEquivalentOf("secret-rg");
    }

    [Test]
    public void AzureRmCaseFilter_CaseOnlyVisibleWithChangedMaskedEntry_RowShown()
    {
        var resource = Ids([AzureId("RG-A"), AzureId("SECRET-ONE")], [AzureId("rg-a"), AzureId("SECRET-TWO")], Flags(false, true), Flags(false, true));

        BuildWithAzureRmFilter(resource).Changes.Single().AttributeChanges.Should().ContainSingle();
    }

    [Test]
    public void AzureRmCaseFilter_MaskedOppositeVisibleCaseVariant_RowShown()
    {
        var resource = Ids([AzureId("RG-A")], [AzureId("rg-a")], Flags(true), Flags(false));

        var markdown = RenderWithAzureRmFilter(resource);

        markdown.Should().Contain("| subnet_ids | ➖ `(sensitive)` |");
    }

    [Test]
    [Arguments("SECRET-ONE")]
    [Arguments("/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-a/providers/Microsoft.Network/virtualNetworks/vnet")]
    [Arguments("")]
    public void AzureRmCaseFilter_UnchangedMaskedEntryBesideCaseOnlyDifference_RowHiddenWhateverTheSecret(string secret)
    {
        var resource = Ids([AzureId("RG-A"), secret], [AzureId("rg-a"), secret], Flags(false, true), Flags(false, true));

        BuildWithAzureRmFilter(resource).Changes.SelectMany(c => c.AttributeChanges).Should().BeEmpty();
    }

    [Test]
    public void AzureRmCaseFilter_SameLengthCaseOnlyReorder_RowShown()
    {
        var resource = Ids([AzureId("RG-A"), AzureId("rg-a")], [AzureId("rg-a"), AzureId("RG-A")]);

        var row = BuildWithAzureRmFilter(resource).Changes.Single().AttributeChanges.Single();

        row.Collection!.OrderChanged.Should().BeTrue();
    }
}
