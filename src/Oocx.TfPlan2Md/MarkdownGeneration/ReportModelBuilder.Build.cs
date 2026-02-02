using System;
using System.Linq;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Builds a fully-populated report model from a parsed Terraform plan.
    /// </summary>
    /// <param name="plan">Terraform plan to transform into a report model.</param>
    /// <returns>A model containing change details, summaries, and optional custom title.</returns>
    public ReportModel Build(TerraformPlan plan)
    {
        // Build all resource change models first (for summary counting)
        var allChanges = plan.ResourceChanges
            .Select(BuildResourceChangeModel)
            .ToList();

        var codeAnalysisReport = BuildCodeAnalysisReport(allChanges);

        // Filter out no-op resources from the changes list passed to the template
        // No-op resources have no meaningful changes to display and including them
        // can cause the template to exceed Scriban's iteration limit of 1000
        var displayChanges = allChanges
            .Where(c => c.Action != NoOpAction || c.CodeAnalysisFindings.Count > 0 || c.ImportId is not null || c.MovedFromAddress is not null)
            .ToList();

        // SonarAnalyzer S3267: Cannot simplify with LINQ - this loop mutates existing objects
        // Justification: This loop modifies ModuleAddress property for null values, not filtering
#pragma warning disable S3267 // Loops should be simplified using the "Where" LINQ method
        foreach (var c in displayChanges)
        {
            if (c.ModuleAddress is null)
            {
                c.ModuleAddress = string.Empty;
            }
        }
#pragma warning restore S3267

        var toAdd = BuildActionSummary(allChanges.Where(c => c.Action == "create"));
        var toChange = BuildActionSummary(allChanges.Where(c => c.Action == "update"));
        var toDestroy = BuildActionSummary(allChanges.Where(c => c.Action == "delete"));
        var toReplace = BuildActionSummary(allChanges.Where(c => c.Action == "replace"));
        var noOp = BuildActionSummary(allChanges.Where(c => c.Action == "no-op"));

        var summary = new SummaryModel
        {
            ToAdd = toAdd,
            ToChange = toChange,
            ToDestroy = toDestroy,
            ToReplace = toReplace,
            NoOp = noOp,
            Total = toAdd.Count + toChange.Count + toDestroy.Count + toReplace.Count
        };

        // Group changes by module. Use empty string for root module. Sort so root comes first,
        // then modules in lexicographic order which ensures parents precede children (flat grouping).
        // Preserve the order of modules as they appear in the plan while ensuring the root
        // module is listed first. This keeps child modules next to their parent modules
        // (flat grouping but ordered by appearance).
        var moduleGroups = displayChanges
            .GroupBy(c => c.ModuleAddress ?? string.Empty)
            .Select(g => new
            {
                Key = g.Key,
                Changes = g.ToList(),
                FirstIndex = displayChanges.FindIndex(c => (c.ModuleAddress ?? string.Empty) == g.Key)
            })
            .OrderBy(g => g.Key == string.Empty ? 0 : 1)
            .ThenBy(g => g.FirstIndex)
            .Select(g => new ModuleChangeGroup
            {
                ModuleAddress = g.Key, // empty string represents root
                Changes = g.Changes
            })
            .ToList();

        var escapedReportTitle = _reportTitle is null ? null : EscapeMarkdownHeading(_reportTitle);
        var metadata = _metadataProvider.GetMetadata();
        var refactoringOperations = BuildRefactoringOperations(allChanges);

        return new ReportModel
        {
            TerraformVersion = plan.TerraformVersion,
            FormatVersion = plan.FormatVersion,
            TfPlan2MdVersion = metadata.Version,
            CommitHash = metadata.CommitHash,
            GeneratedAtUtc = metadata.GeneratedAtUtc,
            HideMetadata = _hideMetadata,
            Timestamp = plan.Timestamp,
            ReportTitle = escapedReportTitle,
            Changes = displayChanges,
            ModuleChanges = moduleGroups,
            Summary = summary,
            CodeAnalysis = codeAnalysisReport,
            ShowUnchangedValues = _showUnchangedValues,
            RenderTarget = renderTarget,
            RefactoringOperations = refactoringOperations
        };
    }

    /// <summary>
    /// Builds the list of refactoring operations (imports and moves) for the report summary.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="changes">Resource changes to inspect for refactoring metadata.</param>
    /// <returns>Sorted list of refactoring operations for rendering.</returns>
    private static List<Models.RefactoringOperationModel> BuildRefactoringOperations(
        IEnumerable<ResourceChangeModel> changes)
    {
        var operations = new List<Models.RefactoringOperationModel>();

        foreach (var change in changes)
        {
            var resourceName = ResolveRefactoringResourceName(change);

            if (change.ImportId is not null)
            {
                operations.Add(new Models.RefactoringOperationModel
                {
                    Operation = "Import",
                    Address = change.Address,
                    ResourceType = change.Type,
                    ResourceName = resourceName,
                    Details = change.ImportId,
                    Status = change.IsRefactoringAlreadyApplied ? "AlreadyApplied" : "Ready",
                    IsAlreadyApplied = change.IsRefactoringAlreadyApplied
                });
            }

            if (change.MovedFromAddress is not null)
            {
                operations.Add(new Models.RefactoringOperationModel
                {
                    Operation = "Move",
                    Address = change.Address,
                    ResourceType = change.Type,
                    ResourceName = resourceName,
                    Details = change.MovedFromAddress,
                    Status = change.IsRefactoringAlreadyApplied ? "AlreadyApplied" : "Ready",
                    IsAlreadyApplied = change.IsRefactoringAlreadyApplied
                });
            }
        }

        return operations
            .OrderBy(o => o.Operation == "Import" ? 0 : 1)
            .ThenBy(o => o.IsAlreadyApplied ? 0 : 1)
            .ThenBy(o => o.Address, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Resolves a human-friendly resource name for use in the Refactoring Summary table.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="change">Resource change containing before/after state.</param>
    /// <returns>The best available display name for the resource.</returns>
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

    private static ActionSummary BuildActionSummary(IEnumerable<ResourceChangeModel> changes)
    {
        var changeList = changes.ToList();

        var breakdown = changeList
            .GroupBy(c => c.Type)
            .Select(g => new ResourceTypeBreakdown(g.Key, g.Count()))
            .OrderBy(b => b.Type, StringComparer.Ordinal)
            .ToList();

        return new ActionSummary(changeList.Count, breakdown);
    }
}
