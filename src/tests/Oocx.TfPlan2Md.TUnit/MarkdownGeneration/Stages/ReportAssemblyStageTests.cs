using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration.Stages;

/// <summary>
/// Tests for the explicit report-assembly stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ReportAssemblyStageTests
{
    private const string ModuleAlpha = "module.alpha";
    private const string ModuleZeta = "module.zeta";
    private static readonly DateTimeOffset GeneratedAtUtc = DateTimeOffset.Parse(
        "2026-03-06T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Test]
    public void ReportAssemblyStage_Build_GroupsModuleChangesAndModuleOnlyOutputs()
    {
        var stage = new ReportAssemblyStage();
        var rootChange = CreateChange("type.root", string.Empty);
        var childChange = CreateChange("type.child", ModuleZeta);
        var input = CreateInput(
            allChanges: [rootChange, childChange],
            displayChanges: [rootChange, childChange],
            allOutputs:
            [
                CreateOutput("root_output", string.Empty),
                CreateOutput("zeta_output", ModuleZeta),
                CreateOutput("alpha_only_output", ModuleAlpha)
            ]);

        var model = stage.Build(input);

        model.GlobalOutputs.Select(output => output.Name)
            .Should().ContainSingle().Which.Should().Be("root_output");
        model.ModuleChanges.Select(group => group.ModuleAddress)
            .Should().ContainInOrder(string.Empty, ModuleAlpha, ModuleZeta);
        model.ModuleChanges.Single(group => group.ModuleAddress == ModuleAlpha)
            .Changes.Should().BeEmpty();
        model.ModuleChanges.Single(group => group.ModuleAddress == ModuleAlpha)
            .Outputs.Select(output => output.Name).Should().ContainSingle().Which.Should().Be("alpha_only_output");
        model.ModuleChanges.Single(group => group.ModuleAddress == ModuleZeta)
            .Changes.Should().ContainSingle().Which.Address.Should().Be("type.child.example");
    }

    [Test]
    public void ReportAssemblyStage_Build_SortsRefactoringOperations()
    {
        var stage = new ReportAssemblyStage();
        var importReady = CreateChange(
            "type.import.ready",
            string.Empty,
            importId: "id-ready",
            isRefactoringAlreadyApplied: false);
        var importAlreadyApplied = CreateChange(
            "type.import.applied",
            string.Empty,
            importId: "id-applied",
            isRefactoringAlreadyApplied: true);
        var moveReady = CreateChange(
            "type.move.ready",
            string.Empty,
            movedFromAddress: "module.old.type.move.ready",
            isRefactoringAlreadyApplied: false);
        var moveAlreadyApplied = CreateChange(
            "type.move.applied",
            string.Empty,
            movedFromAddress: "module.old.type.move.applied",
            isRefactoringAlreadyApplied: true);

        var model = stage.Build(CreateInput(
            allChanges: [moveReady, importReady, moveAlreadyApplied, importAlreadyApplied],
            displayChanges: [importReady],
            allOutputs: []));

        model.RefactoringOperations
            .Select(operation => $"{operation.Operation}:{operation.Status}:{operation.Address}")
            .Should()
            .ContainInOrder(
                "Import:AlreadyApplied:type.import.applied.example",
                "Import:Ready:type.import.ready.example",
                "Move:AlreadyApplied:type.move.applied.example",
                "Move:Ready:type.move.ready.example");
    }

    private static ReportAssemblyInput CreateInput(
        IReadOnlyList<ResourceChangeModel> allChanges,
        IReadOnlyList<ResourceChangeModel> displayChanges,
        IReadOnlyList<OutputChangeModel> allOutputs)
    {
        return new ReportAssemblyInput(
            Plan: new TerraformPlan("1.0", "1.0", new List<ResourceChange>()),
            AllChanges: allChanges,
            DisplayChanges: displayChanges,
            AllOutputs: allOutputs,
            Summary: new SummaryModel
            {
                ToAdd = new ActionSummary(1, []),
                ToChange = new ActionSummary(0, []),
                ToDestroy = new ActionSummary(0, []),
                ToReplace = new ActionSummary(0, []),
                NoOp = new ActionSummary(0, []),
                Imports = new ActionSummary(0, []),
                Moves = new ActionSummary(0, []),
                Total = 1
            },
            CodeAnalysisReport: null,
            FilteredResourceCount: 0,
            EscapedReportTitle: "Report",
            Metadata: new ReportMetadata("1.2.3", "abcdef0", GeneratedAtUtc),
            HideMetadata: false,
            ShowUnchangedValues: false,
            IgnoreAzureIdCaseChanges: true,
            ShowSensitive: false,
            RenderTarget: Oocx.TfPlan2Md.RenderTargets.RenderTarget.AzureDevOps,
            DetailsDisplayMode: Oocx.TfPlan2Md.RenderTargets.DetailsDisplayMode.Auto);
    }

    private static ResourceChangeModel CreateChange(
        string type,
        string moduleAddress,
        string? importId = null,
        string? movedFromAddress = null,
        bool isRefactoringAlreadyApplied = false)
    {
        return new ResourceChangeModel
        {
            Address = $"{type}.example",
            ModuleAddress = moduleAddress,
            Type = type,
            Name = "example",
            ProviderName = "provider",
            Action = "create",
            ActionSymbol = ActionIcons.Add,
            AttributeChanges = [],
            ImportId = importId,
            MovedFromAddress = movedFromAddress,
            IsRefactoringAlreadyApplied = isRefactoringAlreadyApplied
        };
    }

    private static OutputChangeModel CreateOutput(string name, string moduleAddress)
    {
        return new OutputChangeModel
        {
            Name = name,
            Description = null,
            IsSensitive = false,
            Action = "create",
            ActionSymbol = ActionIcons.Add,
            Value = null,
            IsComputed = false,
            IsMasked = false,
            ModuleAddress = moduleAddress,
            ProviderName = "provider",
            IsLargeOutputValue = false,
            ReferencedAttributeName = null
        };
    }
}
