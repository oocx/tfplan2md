namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Summary of changes in the Terraform plan.
/// </summary>
public class SummaryModel
{
    /// <summary>
    /// Gets the summary of resources to be added.
    /// </summary>
    public required ActionSummary ToAdd { get; init; }

    /// <summary>
    /// Gets the summary of resources to be changed in place.
    /// </summary>
    public required ActionSummary ToChange { get; init; }

    /// <summary>
    /// Gets the summary of resources to be destroyed.
    /// </summary>
    public required ActionSummary ToDestroy { get; init; }

    /// <summary>
    /// Gets the summary of resources to be replaced.
    /// </summary>
    public required ActionSummary ToReplace { get; init; }

    /// <summary>
    /// Gets the summary of resources with no changes.
    /// </summary>
    public required ActionSummary NoOp { get; init; }

    /// <summary>
    /// Gets the Total count of resources with changes, excluding no-op resources.
    /// Calculated as: ToAdd.Count + ToChange.Count + ToDestroy.Count + ToReplace.Count.
    /// </summary>
    public int Total { get; init; }
}
