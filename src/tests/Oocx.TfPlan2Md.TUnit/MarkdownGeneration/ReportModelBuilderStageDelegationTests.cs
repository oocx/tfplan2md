using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Verifies that <see cref="ReportModelBuilder"/> delegates to the extracted pipeline stages.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ReportModelBuilderStageDelegationTests
{
    private const string ProviderName = "provider";
    private const string ExampleName = "example";
    private const string TypeA = "type_a";
    private static readonly DateTimeOffset GeneratedAtUtc = DateTimeOffset.Parse(
        "2026-03-06T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Test]
    public void Build_UsesInjectedResourceChangeStage()
    {
        var stage = new StubResourceChangeStage(
        [
            new ResourceChangeModel
            {
                Address = "type_a.injected",
                ModuleAddress = string.Empty,
                Type = TypeA,
                Name = "injected",
                ProviderName = ProviderName,
                Action = TerraformActions.Create,
                ActionSymbol = ActionIcons.Add,
                AttributeChanges = []
            }
        ]);

        var model = new ReportModelBuilder(resourceChangeStage: stage, ignoreAzureIdCaseChanges: false)
            .Build(new TerraformPlan("1.0", "1.0", []));

        stage.Invocations.Should().Be(1);
        model.Changes.Should().ContainSingle().Which.Address.Should().Be("type_a.injected");
        model.Summary.ToAdd.Count.Should().Be(1);
    }

    [Test]
    public void Build_UsesInjectedAttributeFilteringStage()
    {
        var model = new ReportModelBuilder(
            resourceChangeStage: new StubResourceChangeStage(
            [
                new ResourceChangeModel
                {
                    Address = "azurerm_role_assignment.example",
                    ModuleAddress = string.Empty,
                    Type = "azurerm_role_assignment",
                    Name = ExampleName,
                    ProviderName = "azurerm",
                    Action = TerraformActions.Update,
                    ActionSymbol = ActionIcons.Update,
                    AttributeChanges =
                    [
                        new AttributeChangeModel
                        {
                            Name = "scope",
                            Before = "/subscriptions/ABC123/resourceGroups/my-rg",
                            After = "/subscriptions/abc123/resourceGroups/my-rg"
                        }
                    ]
                }
            ]),
            attributeFilteringStage: new StubAttributeFilteringStage(),
            ignoreAzureIdCaseChanges: false)
            .Build(new TerraformPlan("1.0", "1.0", []));

        model.Changes.Should().ContainSingle();
        model.Changes.Single().AttributeChanges.Should().BeEmpty();
    }

    [Test]
    public void Build_UsesInjectedSummaryEnrichmentStage()
    {
        var stage = new StubSummaryEnrichmentStage();

        var model = new ReportModelBuilder(
            resourceChangeStage: new StubResourceChangeStage(
            [
                new ResourceChangeModel
                {
                    Address = "type_a.example",
                    ModuleAddress = string.Empty,
                    Type = TypeA,
                    Name = ExampleName,
                    ProviderName = ProviderName,
                    Action = TerraformActions.Create,
                    ActionSymbol = ActionIcons.Add,
                    AttributeChanges = []
                }
            ]),
            summaryEnrichmentStage: stage,
            ignoreAzureIdCaseChanges: false)
            .Build(new TerraformPlan("1.0", "1.0", []));

        stage.Invocations.Should().Be(1);
        model.Summary.ToAdd.Count.Should().Be(99);
        model.Summary.Total.Should().Be(99);
    }

    [Test]
    public void Build_UsesInjectedDisplayFilteringStage()
    {
        var stage = new StubDisplayFilteringStage();

        var model = new ReportModelBuilder(
            resourceChangeStage: new StubResourceChangeStage(
            [
                new ResourceChangeModel
                {
                    Address = "type_a.example",
                    ModuleAddress = string.Empty,
                    Type = TypeA,
                    Name = ExampleName,
                    ProviderName = ProviderName,
                    Action = TerraformActions.Create,
                    ActionSymbol = ActionIcons.Add,
                    AttributeChanges = []
                }
            ]),
            displayFilteringStage: stage,
            ignoreAzureIdCaseChanges: false)
            .Build(new TerraformPlan("1.0", "1.0", []));

        stage.Invocations.Should().Be(1);
        model.FilteredResourceCount.Should().Be(999);
    }

    [Test]
    public void Build_UsesInjectedReportAssemblyStage()
    {
        var stage = new StubReportAssemblyStage();

        var model = new ReportModelBuilder(
            resourceChangeStage: new StubResourceChangeStage(
            [
                new ResourceChangeModel
                {
                    Address = "type_a.example",
                    ModuleAddress = string.Empty,
                    Type = TypeA,
                    Name = ExampleName,
                    ProviderName = ProviderName,
                    Action = TerraformActions.Create,
                    ActionSymbol = ActionIcons.Add,
                    AttributeChanges = []
                }
            ]),
            reportAssemblyStage: stage,
            ignoreAzureIdCaseChanges: false)
            .Build(new TerraformPlan("1.0", "1.0", []));

        stage.Invocations.Should().Be(1);
        model.TfPlan2MdVersion.Should().Be("assembly-stage");
        model.FilteredResourceCount.Should().Be(321);
    }

    private sealed class StubResourceChangeStage(IReadOnlyList<ResourceChangeModel> resourceChanges) : IResourceChangeStage
    {
        public int Invocations { get; private set; }

        public IReadOnlyList<ResourceChangeModel> Build(TerraformPlan plan)
        {
            _ = plan;
            Invocations++;
            return resourceChanges;
        }
    }

    private sealed class StubAttributeFilteringStage : IAttributeFilteringStage
    {
        public IReadOnlyList<ResourceChangeModel> Build(IReadOnlyList<ResourceChangeModel> resourceChanges)
        {
            return resourceChanges
                .Select(change => new ResourceChangeModel
                {
                    Address = change.Address,
                    ModuleAddress = change.ModuleAddress,
                    Type = change.Type,
                    Name = change.Name,
                    ProviderName = change.ProviderName,
                    Action = change.Action,
                    ActionSymbol = change.ActionSymbol,
                    AttributeChanges = [],
                    BeforeJson = change.BeforeJson,
                    AfterJson = change.AfterJson,
                    ReplacePaths = change.ReplacePaths,
                    Summary = change.Summary,
                    SummaryHtml = change.SummaryHtml,
                    ChangedAttributesSummary = change.ChangedAttributesSummary,
                    TagsBadges = change.TagsBadges,
                    ChildResourceGroups = change.ChildResourceGroups,
                    CodeAnalysisFindings = change.CodeAnalysisFindings,
                    ImportId = change.ImportId,
                    MovedFromAddress = change.MovedFromAddress,
                    IsRefactoringAlreadyApplied = change.IsRefactoringAlreadyApplied,
                    HasWholeResourceUnknownAfterApply = change.HasWholeResourceUnknownAfterApply,
                    BeforeSensitive = change.BeforeSensitive,
                    AfterSensitive = change.AfterSensitive,
                    AfterUnknown = change.AfterUnknown,
                    ConfigurationReferences = change.ConfigurationReferences,
                    ResourceChange = change.ResourceChange
                })
                .ToList();
        }
    }

    private sealed class StubSummaryEnrichmentStage : ISummaryEnrichmentStage
    {
        public int Invocations { get; private set; }

        public SummaryModel Build(IReadOnlyList<ResourceChangeModel> resourceChanges)
        {
            _ = resourceChanges;
            Invocations++;
            return new SummaryModel
            {
                ToAdd = new ActionSummary(99, []),
                ToChange = new ActionSummary(0, []),
                ToDestroy = new ActionSummary(0, []),
                ToReplace = new ActionSummary(0, []),
                NoOp = new ActionSummary(0, []),
                Total = 99
            };
        }
    }

    private sealed class StubDisplayFilteringStage : IDisplayFilteringStage
    {
        public int Invocations { get; private set; }

        public DisplayFilteringResult Build(IReadOnlyList<ResourceChangeModel> mergedChanges)
        {
            Invocations++;
            return new DisplayFilteringResult(mergedChanges, 999);
        }
    }

    private sealed class StubReportAssemblyStage : IReportAssemblyStage
    {
        public int Invocations { get; private set; }

        public ReportModel Build(ReportAssemblyInput input)
        {
            Invocations++;
            return new ReportModel
            {
                TerraformVersion = input.Plan.TerraformVersion,
                FormatVersion = input.Plan.FormatVersion,
                TfPlan2MdVersion = "assembly-stage",
                CommitHash = "abcdef0",
                GeneratedAtUtc = GeneratedAtUtc,
                HideMetadata = false,
                Timestamp = input.Plan.Timestamp,
                ReportTitle = input.EscapedReportTitle,
                Changes = input.DisplayChanges,
                ModuleChanges = [],
                Summary = input.Summary,
                CodeAnalysis = input.CodeAnalysisReport,
                ShowUnchangedValues = input.ShowUnchangedValues,
                IgnoreAzureIdCaseChanges = input.IgnoreAzureIdCaseChanges,
                ShowSensitive = input.ShowSensitive,
                RenderTarget = input.RenderTarget,
                DetailsDisplayMode = input.DetailsDisplayMode,
                RefactoringOperations = [],
                GlobalOutputs = [],
                FilteredResourceCount = 321
            };
        }
    }
}
