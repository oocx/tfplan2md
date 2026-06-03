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
        var builtDrift = resourceChangeStage.Build(driftPlan, configurationReferenceIndex).ToList();

        // Reuse the same display filtering semantics used for normal resource changes so
        // no-op and fully suppressed drift entries do not render as false positives.
        return (_displayFilteringStage ?? CreateDisplayFilteringStage()).Build(builtDrift).DisplayChanges.ToList();
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

    /// <summary>
    /// Correlates <c>relevant_attributes</c> entries to replaced or destroyed resource cards, populating
    /// inline forced-replacement and depends-on annotations on each eligible <see cref="ResourceChangeModel"/>.
    /// Returns the subset of relevant attributes that could not be correlated to any changed resource,
    /// for rendering in the fallback <c>&lt;details&gt;</c> section.
    /// Only <c>replace</c> and <c>delete</c> actions receive annotations; in-place updates and drift entries
    /// are excluded per spec SC-5 and SC-6.
    /// Related feature: docs/features/660-inline-relevant-attributes/specification.md.
    /// </summary>
    /// <param name="allChanges">All resource change models (post-merge, excluding drift).</param>
    /// <param name="allRelevantAttributes">All relevant attributes built from the plan.</param>
    /// <returns>
    /// The uncorrelated subset of <paramref name="allRelevantAttributes"/> for the fallback section.
    /// Returns an empty list when all attributes were correlated, and returns the full list when
    /// no attributes could be correlated (e.g., no replace/delete changes, or no matching references).
    /// </returns>
    private static List<RelevantAttributeModel> BuildInlineRelevantAttributeAnnotations(
        IReadOnlyList<ResourceChangeModel> allChanges,
        List<RelevantAttributeModel> allRelevantAttributes)
    {
        if (allRelevantAttributes.Count == 0)
        {
            // Short-circuit: no relevant attributes to correlate; preserves pre-1.14 plan behaviour.
            return allRelevantAttributes;
        }

        // Build fast upstream lookup: resource address → list of RelevantAttributeModel (case-insensitive)
        var byUpstream = BuildUpstreamLookup(allRelevantAttributes);

        // Build replaced/destroyed address set for IsChangingInThisPlan detection
        var replacedOrDestroyed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rc in allChanges.Where(rc => rc.Action is TerraformActions.Replace or TerraformActions.Delete))
        {
            replacedOrDestroyed.Add(rc.Address);
        }

        // Track globally correlated relevant attribute models (by reference) to compute the fallback list
        var correlated = new HashSet<RelevantAttributeModel>(ReferenceEqualityComparer.Instance);

        foreach (var rc in allChanges)
        {
            if (rc.Action is not (TerraformActions.Replace or TerraformActions.Delete))
            {
                // Only replaced/destroyed resources receive inline annotations (spec SC-5, SC-6)
                continue;
            }

            var correlatedForResource = BuildCorrelatedSet(rc, byUpstream);
            if (correlatedForResource.Count == 0)
            {
                continue;
            }

            var (forcedAnnotations, forcedRaSet) = BuildForcedAnnotations(rc, correlatedForResource, byUpstream, replacedOrDestroyed);
            var dependsOnAnnotations = BuildDependsOnAnnotationList(correlatedForResource, forcedRaSet, replacedOrDestroyed);

            rc.ForcedReplacementAnnotations = forcedAnnotations;
            rc.DependsOnAnnotations = dependsOnAnnotations;

            foreach (var ra in correlatedForResource)
            {
                correlated.Add(ra);
            }
        }

        // Return only the uncorrelated entries for the fallback <details> section
        return allRelevantAttributes.Where(ra => !correlated.Contains(ra)).ToList();
    }

    /// <summary>
    /// Builds a case-insensitive dictionary mapping upstream resource addresses to their
    /// <see cref="RelevantAttributeModel"/> entries from the plan's <c>relevant_attributes</c> list.
    /// </summary>
    /// <param name="relevantAttributes">All relevant attribute models from the plan.</param>
    /// <returns>Lookup dictionary keyed by resource address.</returns>
    private static Dictionary<string, List<RelevantAttributeModel>> BuildUpstreamLookup(
        List<RelevantAttributeModel> relevantAttributes)
    {
        var byUpstream = new Dictionary<string, List<RelevantAttributeModel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var ra in relevantAttributes)
        {
            if (!byUpstream.TryGetValue(ra.Resource, out var list))
            {
                list = [];
                byUpstream[ra.Resource] = list;
            }

            list.Add(ra);
        }

        return byUpstream;
    }

    /// <summary>
    /// Finds all relevant attributes correlated to the given resource change via its
    /// <see cref="ResourceChangeModel.ConfigurationReferences"/>.
    /// </summary>
    /// <param name="rc">The resource change to correlate.</param>
    /// <param name="byUpstream">Upstream lookup built by <see cref="BuildUpstreamLookup"/>.</param>
    /// <returns>A set of correlated <see cref="RelevantAttributeModel"/> instances (by reference).</returns>
    private static HashSet<RelevantAttributeModel> BuildCorrelatedSet(
        ResourceChangeModel rc,
        Dictionary<string, List<RelevantAttributeModel>> byUpstream)
    {
        var correlatedForResource = new HashSet<RelevantAttributeModel>(ReferenceEqualityComparer.Instance);
        foreach (var refs in rc.ConfigurationReferences.Values)
        {
            foreach (var reference in refs)
            {
                if (TryResolveRelevantAttributesForReference(reference, byUpstream, out var raList))
                {
                    foreach (var ra in raList)
                    {
                        correlatedForResource.Add(ra);
                    }
                }
            }
        }

        return correlatedForResource;
    }

    /// <summary>
    /// Builds forced-replacement annotations for a resource from its <c>replace_paths</c>,
    /// correlating each top-level path segment to correlated upstream relevant attributes.
    /// </summary>
    /// <param name="rc">The resource being annotated.</param>
    /// <param name="correlated">Attributes already correlated to this resource.</param>
    /// <param name="byUpstream">Upstream lookup dictionary.</param>
    /// <param name="replacedOrDestroyed">Set of addresses being replaced/destroyed in this plan.</param>
    /// <returns>
    /// A tuple of the forced annotation list and the set of <see cref="RelevantAttributeModel"/>
    /// instances that were consumed as forced entries (for deduplication).
    /// </returns>
    private static (List<Models.ForcedReplacementAnnotation> Annotations, HashSet<RelevantAttributeModel> ForcedSet)
        BuildForcedAnnotations(
            ResourceChangeModel rc,
            HashSet<RelevantAttributeModel> correlated,
            Dictionary<string, List<RelevantAttributeModel>> byUpstream,
            HashSet<string> replacedOrDestroyed)
    {
        var forcedAnnotations = new List<Models.ForcedReplacementAnnotation>();
        var forcedRaSet = new HashSet<RelevantAttributeModel>(ReferenceEqualityComparer.Instance);

        foreach (var replacePath in rc.ReplacePaths ?? [])
        {
            if (replacePath.Count == 0)
            {
                continue;
            }

            // The top-level attribute name is always the first path segment (a string in valid Terraform plans)
            var topAttr = replacePath[0] as string;
            if (topAttr is null || !rc.ConfigurationReferences.TryGetValue(topAttr, out var topAttrRefs))
            {
                continue;
            }

            foreach (var reference in topAttrRefs)
            {
                if (!TryResolveRelevantAttributesForReference(reference, byUpstream, out var raList))
                {
                    continue;
                }

                foreach (var ra in raList)
                {
                    // Only entries correlated in BuildCorrelatedSet qualify; deduplicate by ra reference
                    if (!correlated.Contains(ra) || !forcedRaSet.Add(ra))
                    {
                        continue;
                    }

                    forcedAnnotations.Add(new Models.ForcedReplacementAnnotation
                    {
                        LocalAttribute = topAttr,
                        UpstreamResource = ra.Resource,
                        UpstreamAttributePath = ra.AttributePath,
                        IsChangingInThisPlan = replacedOrDestroyed.Contains(ra.Resource)
                    });
                }
            }
        }

        return (forcedAnnotations, forcedRaSet);
    }

    /// <summary>
    /// Builds depends-on annotations from the correlated set, excluding entries that were
    /// already promoted to forced-replacement annotations.
    /// </summary>
    /// <param name="correlated">All attributes correlated to this resource.</param>
    /// <param name="forcedRaSet">Attributes already consumed as forced entries.</param>
    /// <param name="replacedOrDestroyed">Set of addresses being replaced/destroyed in this plan.</param>
    /// <returns>A list of depends-on annotations for the remaining correlated attributes.</returns>
    private static List<Models.DependsOnAnnotation> BuildDependsOnAnnotationList(
        HashSet<RelevantAttributeModel> correlated,
        HashSet<RelevantAttributeModel> forcedRaSet,
        HashSet<string> replacedOrDestroyed)
    {
        return correlated
            .Where(ra => !forcedRaSet.Contains(ra))
            .OrderBy(ra => ra.Resource, StringComparer.OrdinalIgnoreCase)
            .ThenBy(ra => ra.AttributePath, StringComparer.OrdinalIgnoreCase)
            .Select(ra => new Models.DependsOnAnnotation
            {
                UpstreamResource = ra.Resource,
                UpstreamAttributePath = ra.AttributePath,
                IsChangingInThisPlan = replacedOrDestroyed.Contains(ra.Resource)
            })
            .ToList();
    }

    /// <summary>
    /// Resolves a configuration reference to the most specific upstream resource key present in
    /// <paramref name="byUpstream"/>, progressively stripping trailing path segments until a match is found.
    /// This supports managed resources, data sources, and module-prefixed references without relying on
    /// a fixed dot-segment heuristic.
    /// Related feature: docs/features/660-inline-relevant-attributes/architecture.md.
    /// </summary>
    /// <param name="reference">The raw reference string from <c>ConfigurationReferences</c>.</param>
    /// <param name="byUpstream">Lookup keyed by upstream resource address.</param>
    /// <param name="relevantAttributes">The resolved relevant attributes when a match is found.</param>
    /// <returns><c>true</c> when a matching upstream resource key is found; otherwise <c>false</c>.</returns>
    private static bool TryResolveRelevantAttributesForReference(
        string reference,
        Dictionary<string, List<RelevantAttributeModel>> byUpstream,
        out List<RelevantAttributeModel> relevantAttributes)
    {
        relevantAttributes = [];

        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var candidate = reference;
        while (!string.IsNullOrWhiteSpace(candidate))
        {
            if (byUpstream.TryGetValue(candidate, out var resolvedRelevantAttributes))
            {
                relevantAttributes = resolvedRelevantAttributes;
                return true;
            }

            var lastDot = candidate.LastIndexOf('.');
            if (lastDot < 0)
            {
                break;
            }

            candidate = candidate[..lastDot];
        }

        return false;
    }
}
