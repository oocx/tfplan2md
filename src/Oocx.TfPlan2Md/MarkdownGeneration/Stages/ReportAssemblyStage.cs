using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Assembles the final <see cref="ReportModel"/> from pipeline outputs.
/// </summary>
/// <remarks>
/// Related features:
/// - docs/features/057-terraform-import-moved-blocks/specification.md.
/// - docs/features/097-terraform-outputs/specification.md.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1506:Avoid excessive class coupling",
    Justification = "The stage is the explicit assembly boundary and intentionally aggregates final report concerns.")]
internal sealed class ReportAssemblyStage : IReportAssemblyStage
{
    private const string ImportOperation = "Import";
    private const string MoveOperation = "Move";
    private const string AlreadyAppliedStatus = "AlreadyApplied";
    private const string ReadyStatus = "Ready";

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1506:Avoid excessive class coupling",
        Justification = "The assembly stage composes the full report model from multiple pipeline outputs by design.")]
    public ReportModel Build(ReportAssemblyInput input)
    {
        var globalOutputs = input.AllOutputs
            .Where(output => output.ModuleAddress == string.Empty)
            .OrderBy(output => output.Name, StringComparer.Ordinal)
            .ToList();

        var outputsByModule = input.AllOutputs
            .Where(output => output.ModuleAddress != string.Empty)
            .GroupBy(output => output.ModuleAddress)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OutputChangeModel>)group
                    .OrderBy(output => output.Name, StringComparer.Ordinal)
                    .ToList());

        var moduleGroups = BuildModuleGroups(input.DisplayChanges, outputsByModule);
        var refactoringOperations = BuildRefactoringOperations(input.AllChanges);
        var planStatus = BuildPlanStatusFromInput(input);
        var drift = input.Drift ?? new List<ResourceChangeModel>();
        var relevantAttributes = input.RelevantAttributes ?? new List<RelevantAttributeModel>();

        return new ReportModel
        {
            TerraformVersion = input.Plan.TerraformVersion,
            FormatVersion = input.Plan.FormatVersion,
            TfPlan2MdVersion = input.Metadata.Version,
            CommitHash = input.Metadata.CommitHash,
            GeneratedAtUtc = input.Metadata.GeneratedAtUtc,
            HideMetadata = input.HideMetadata,
            Timestamp = input.Plan.Timestamp,
            ReportTitle = input.EscapedReportTitle,
            Changes = input.DisplayChanges,
            ModuleChanges = moduleGroups,
            Summary = input.Summary,
            CodeAnalysis = input.CodeAnalysisReport,
            ShowUnchangedValues = input.ShowUnchangedValues,
            IgnoreAzureIdCaseChanges = input.IgnoreAzureIdCaseChanges,
            ShowSensitive = input.ShowSensitive,
            RenderTarget = input.RenderTarget,
            DetailsDisplayMode = input.DetailsDisplayMode,
            RefactoringOperations = refactoringOperations,
            GlobalOutputs = globalOutputs,
            FilteredResourceCount = input.FilteredResourceCount,
            PlanStatus = planStatus,
            Drift = drift,
            RelevantAttributes = relevantAttributes
        };
    }

    private static PlanStatusModel? BuildPlanStatusFromInput(ReportAssemblyInput input)
    {
        if (input.PlanStatus is null)
        {
            return null;
        }

        return new PlanStatusModel
        {
            Applyable = input.PlanStatus.Applyable,
            Complete = input.PlanStatus.Complete,
            Errored = input.PlanStatus.Errored
        };
    }

    private static List<ModuleChangeGroup> BuildModuleGroups(
        IReadOnlyList<ResourceChangeModel> displayChanges,
        Dictionary<string, IReadOnlyList<OutputChangeModel>> outputsByModule)
    {
        var firstIndexByModule = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < displayChanges.Count; i++)
        {
            var key = displayChanges[i].ModuleAddress ?? string.Empty;
            firstIndexByModule.TryAdd(key, i);
        }

        var moduleGroups = displayChanges
            .GroupBy(change => change.ModuleAddress ?? string.Empty)
            .Select(group => new
            {
                Key = group.Key,
                Changes = group.ToList(),
                FirstIndex = firstIndexByModule[group.Key]
            })
            .OrderBy(group => group.Key == string.Empty ? 0 : 1)
            .ThenBy(group => group.FirstIndex)
            .Select(group => new ModuleChangeGroup
            {
                ModuleAddress = group.Key,
                Changes = group.Changes,
                Outputs = outputsByModule.TryGetValue(group.Key, out var outputs)
                    ? outputs
                    : Array.Empty<OutputChangeModel>()
            })
            .ToList();

        foreach (var (moduleAddress, outputs) in outputsByModule)
        {
            if (!moduleGroups.Exists(module => module.ModuleAddress == moduleAddress))
            {
                var insertIndex = moduleGroups.Count;
                for (var i = 0; i < moduleGroups.Count; i++)
                {
                    if (string.Compare(moduleAddress, moduleGroups[i].ModuleAddress, StringComparison.Ordinal) < 0)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                moduleGroups.Insert(insertIndex, new ModuleChangeGroup
                {
                    ModuleAddress = moduleAddress,
                    Changes = Array.Empty<ResourceChangeModel>(),
                    Outputs = outputs
                });
            }
        }

        return moduleGroups;
    }

    private static List<RefactoringOperationModel> BuildRefactoringOperations(
        IEnumerable<ResourceChangeModel> changes)
    {
        var operations = new List<RefactoringOperationModel>();

        foreach (var change in changes)
        {
            var resourceName = ResolveRefactoringResourceName(change);

            if (change.ImportId is not null)
            {
                operations.Add(new RefactoringOperationModel
                {
                    Operation = ImportOperation,
                    Address = change.Address,
                    ResourceType = change.Type,
                    ResourceName = resourceName,
                    Details = change.ImportId,
                    Status = change.IsRefactoringAlreadyApplied ? AlreadyAppliedStatus : ReadyStatus,
                    IsAlreadyApplied = change.IsRefactoringAlreadyApplied
                });
            }

            if (change.MovedFromAddress is not null)
            {
                operations.Add(new RefactoringOperationModel
                {
                    Operation = MoveOperation,
                    Address = change.Address,
                    ResourceType = change.Type,
                    ResourceName = resourceName,
                    Details = change.MovedFromAddress,
                    Status = change.IsRefactoringAlreadyApplied ? AlreadyAppliedStatus : ReadyStatus,
                    IsAlreadyApplied = change.IsRefactoringAlreadyApplied
                });
            }
        }

        return operations
            .OrderBy(operation => operation.Operation == ImportOperation ? 0 : 1)
            .ThenBy(operation => operation.IsAlreadyApplied ? 0 : 1)
            .ThenBy(operation => operation.Address, StringComparer.Ordinal)
            .ToList();
    }

    private static string ResolveRefactoringResourceName(ResourceChangeModel change)
    {
        var state = change.AfterJson ?? change.BeforeJson;
        var flatState = Helpers.JsonFlattener.ConvertToFlatDictionary(state);

        static string? GetValue(Dictionary<string, string?> values, string key)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        var fromState = GetValue(flatState, "name")
            ?? GetValue(flatState, "display_name")
            ?? GetValue(flatState, "body.displayName")
            ?? GetValue(flatState, "displayName")
            ?? GetValue(flatState, "url");

        if (fromState is not null)
        {
            return fromState;
        }

        return !string.IsNullOrWhiteSpace(change.Name)
            ? change.Name
            : change.Address;
    }
}
