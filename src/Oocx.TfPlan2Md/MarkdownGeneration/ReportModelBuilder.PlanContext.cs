using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
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
    /// Builds the plan status model from Terraform 1.14+ plan status fields.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    /// <param name="plan">The Terraform plan containing optional status fields.</param>
    /// <returns>A plan status model if any status field is present; otherwise null.</returns>
    private static PlanStatusModel? BuildPlanStatus(TerraformPlan plan)
    {
        if (plan.Applyable is null && plan.Complete is null && plan.Errored is null)
        {
            return null;
        }

        return new PlanStatusModel
        {
            Applyable = plan.Applyable,
            Complete = plan.Complete,
            Errored = plan.Errored
        };
    }

    /// <summary>
    /// Builds resource change models for drift detected outside Terraform.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    /// <param name="plan">The Terraform plan containing optional resource drift.</param>
    /// <param name="configurationReferenceIndex">Configuration reference index for drift resources.</param>
    /// <returns>A list of resource change models representing drift.</returns>
    private List<ResourceChangeModel> BuildResourceDrift(
        TerraformPlan plan,
        IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>> configurationReferenceIndex)
    {
        if (plan.ResourceDrift is null || plan.ResourceDrift.Count == 0)
        {
            return new List<ResourceChangeModel>();
        }

        var resourceChangeStage = _resourceChangeStage ?? CreateResourceChangeStage();

        // Simulate a plan with just drift entries to reuse the resource change stage
        var driftPlan = plan with { ResourceChanges = plan.ResourceDrift };
        return resourceChangeStage.Build(driftPlan, configurationReferenceIndex).ToList();
    }

    /// <summary>
    /// Builds relevant attribute models from Terraform 1.14+ relevant_attributes field.
    /// Related feature: docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md.
    /// </summary>
    /// <param name="plan">The Terraform plan containing optional relevant attributes.</param>
    /// <returns>A list of relevant attribute models.</returns>
    private static List<RelevantAttributeModel> BuildRelevantAttributes(TerraformPlan plan)
    {
        if (plan.RelevantAttributes is null || plan.RelevantAttributes.Count == 0)
        {
            return new List<RelevantAttributeModel>();
        }

        return plan.RelevantAttributes
            .Select(attr => new RelevantAttributeModel
            {
                Resource = attr.Resource,
                AttributePath = ResourceSummaryPathFormatter.FormatReplacePath(attr.Attribute) ?? string.Empty
            })
            .ToList();
    }
}
