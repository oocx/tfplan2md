namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Represents the plan status fields from Terraform 1.14+ plans
/// (applyable, complete, errored).
/// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
/// </summary>
internal class PlanStatusModel
{
    /// <summary>
    /// Gets a value indicating whether the plan is applyable.
    /// When <c>false</c>, Terraform refused to apply this plan.
    /// </summary>
    public bool? Applyable { get; init; }

    /// <summary>
    /// Gets a value indicating whether the plan is complete.
    /// When <c>false</c>, the plan does not represent the full intended state.
    /// </summary>
    public bool? Complete { get; init; }

    /// <summary>
    /// Gets a value indicating whether the plan failed to compute fully.
    /// When <c>true</c>, the plan encountered errors during computation.
    /// </summary>
    public bool? Errored { get; init; }
}
