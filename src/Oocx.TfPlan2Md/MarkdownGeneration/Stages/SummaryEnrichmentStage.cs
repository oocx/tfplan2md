using System;
using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Default implementation of the summary-enrichment pipeline stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal sealed class SummaryEnrichmentStage : ISummaryEnrichmentStage
{
    /// <summary>
    /// Terraform action value for create operations.
    /// </summary>
    private const string CreateAction = TerraformActions.Create;

    /// <summary>
    /// Terraform action value for update operations.
    /// </summary>
    private const string UpdateAction = TerraformActions.Update;

    /// <summary>
    /// Terraform action value for unknown operations.
    /// </summary>
    private const string UnknownAction = TerraformActions.Unknown;

    /// <summary>
    /// Terraform action value for delete operations.
    /// </summary>
    private const string DeleteAction = TerraformActions.Delete;

    /// <summary>
    /// Terraform action value for forget operations.
    /// </summary>
    private const string ForgetAction = TerraformActions.Forget;

    /// <summary>
    /// Terraform action value for replace operations.
    /// </summary>
    private const string ReplaceAction = TerraformActions.Replace;

    /// <summary>
    /// Terraform action value for no-op operations.
    /// </summary>
    private const string NoOpAction = TerraformActions.NoOp;

    /// <inheritdoc />
    public SummaryModel Build(IReadOnlyList<ResourceChangeModel> resourceChanges)
    {
        var toAdd = BuildActionSummary(resourceChanges.Where(change => change.Action == CreateAction));
        var toChange = BuildActionSummary(resourceChanges.Where(change => change.Action is UpdateAction or UnknownAction));
        var toDestroy = BuildActionSummary(resourceChanges.Where(change => change.Action is DeleteAction or ForgetAction));
        var toReplace = BuildActionSummary(resourceChanges.Where(change => change.Action == ReplaceAction));
        var noOp = BuildActionSummary(resourceChanges.Where(change => change.Action == NoOpAction));

        // Imports and moves are orthogonal to the action buckets above: a single resource
        // may appear in both an action row (e.g., Add) and the Imports/Moves row when the
        // change carries import or move metadata.
        var imports = BuildActionSummary(resourceChanges.Where(change => change.ImportId is not null));
        var moves = BuildActionSummary(resourceChanges.Where(change => change.MovedFromAddress is not null));

        return new SummaryModel
        {
            ToAdd = toAdd,
            ToChange = toChange,
            ToDestroy = toDestroy,
            ToReplace = toReplace,
            NoOp = noOp,
            Imports = imports,
            Moves = moves,
            Total = toAdd.Count + toChange.Count + toDestroy.Count + toReplace.Count
        };
    }

    /// <summary>
    /// Builds the action summary for one Terraform action bucket.
    /// </summary>
    /// <param name="changes">The resource changes in the bucket.</param>
    /// <returns>The action summary with count and type breakdown.</returns>
    private static ActionSummary BuildActionSummary(IEnumerable<ResourceChangeModel> changes)
    {
        var changeList = changes.ToList();
        var breakdown = changeList
            .GroupBy(change => change.Type)
            .Select(group => new ResourceTypeBreakdown(group.Key, group.Count()))
            .OrderBy(item => item.Type, StringComparer.Ordinal)
            .ToList();

        return new ActionSummary(changeList.Count, breakdown);
    }
}
