using System.Linq;
using Oocx.TfPlan2Md.Parsing;

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

        // Filter out no-op resources from the changes list passed to the template
        // No-op resources have no meaningful changes to display and including them
        // can cause the template to exceed Scriban's iteration limit of 1000
        var displayChanges = allChanges
            .Where(c => c.Action != "no-op")
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

        var escapedReportTitle = _reportTitle is null ? null : ScribanHelpers.EscapeMarkdownHeading(_reportTitle);
        var metadata = _metadataProvider.GetMetadata();

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
            ShowUnchangedValues = _showUnchangedValues,
            RenderTarget = renderTarget
        };
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
