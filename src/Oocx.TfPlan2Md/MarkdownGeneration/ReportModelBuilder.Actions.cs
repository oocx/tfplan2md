using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builder partial that populates Terraform 1.14+ action-invocation models on
/// resource changes and routes orphan actions into <see cref="OtherActionsModel"/>.
/// Runs after <c>MergeParentChildRelationships</c> so that children that were
/// hidden by the merge cannot accidentally become action targets.
/// Related feature: docs/features/122-terraform-1-15-support/adr-003-inline-action-rendering.md.
/// </summary>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Distributes parsed action invocations across the change models. Lifecycle
    /// actions whose <c>triggering_resource_address</c> matches a known resource
    /// change are appended to that resource's <see cref="ResourceChangeModel.Actions"/>
    /// list. Invoke-mode actions and lifecycle orphans are collected into a returned
    /// <see cref="OtherActionsModel"/> for the "🎬 Other Actions" report section.
    /// </summary>
    /// <param name="plan">Parsed Terraform plan.</param>
    /// <param name="allChanges">All resource change models (post-merge).</param>
    /// <returns>An <see cref="OtherActionsModel"/> when any orphan/invoke action exists; otherwise <c>null</c>.</returns>
    private static OtherActionsModel? BuildActionInvocations(TerraformPlan plan, IReadOnlyList<ResourceChangeModel> allChanges)
    {
        var hasImmediate = plan.ActionInvocations is { Count: > 0 };
        var hasDeferred = plan.DeferredActionInvocations is { Count: > 0 };
        if (!hasImmediate && !hasDeferred)
        {
            return null;
        }

        var byAddress = new Dictionary<string, ResourceChangeModel>(System.StringComparer.Ordinal);
        foreach (var change in allChanges)
        {
            // Last-write-wins is acceptable: parent-child merging guarantees a single
            // surviving address per logical resource. Duplicate keys from drift entries
            // are intentionally ignored — drift renders separately.
            byAddress[change.Address] = change;
        }

        var perResource = new Dictionary<string, List<ActionInvocationModel>>(System.StringComparer.Ordinal);
        var invokeActions = new List<ActionInvocationModel>();
        var orphanActions = new List<ActionInvocationModel>();

        if (plan.ActionInvocations is { } immediate)
        {
            DistributeActions(immediate, isDeferred: false, byAddress, perResource, invokeActions, orphanActions);
        }

        if (plan.DeferredActionInvocations is { } deferred)
        {
            DistributeActions(deferred, isDeferred: true, byAddress, perResource, invokeActions, orphanActions);
        }

        foreach (var (address, list) in perResource)
        {
            byAddress[address].Actions = list;
        }

        if (invokeActions.Count == 0 && orphanActions.Count == 0)
        {
            return null;
        }

        return new OtherActionsModel
        {
            InvokeActions = invokeActions,
            LifecycleOrphanActions = orphanActions
        };
    }

    /// <summary>
    /// Routes a single source list of action invocations into the appropriate buckets
    /// (per-resource attached, invoke-mode, or lifecycle orphan).
    /// </summary>
    /// <param name="source">The source action list (immediate or deferred).</param>
    /// <param name="isDeferred">Whether this source list represents deferred actions.</param>
    /// <param name="byAddress">Lookup of post-merge resource changes by address.</param>
    /// <param name="perResource">Accumulator of per-resource attached actions.</param>
    /// <param name="invokeActions">Accumulator of invoke-mode actions.</param>
    /// <param name="orphanActions">Accumulator of lifecycle orphan actions.</param>
    private static void DistributeActions(
        IReadOnlyList<ActionInvocation> source,
        bool isDeferred,
        Dictionary<string, ResourceChangeModel> byAddress,
        Dictionary<string, List<ActionInvocationModel>> perResource,
        List<ActionInvocationModel> invokeActions,
        List<ActionInvocationModel> orphanActions)
    {
        foreach (var invocation in source)
        {
            var model = new ActionInvocationModel { Invocation = invocation, IsDeferred = isDeferred };

            if (invocation.LifecycleActionTrigger is { TriggeringResourceAddress: { Length: > 0 } addr })
            {
                if (byAddress.ContainsKey(addr))
                {
                    if (!perResource.TryGetValue(addr, out var list))
                    {
                        list = new List<ActionInvocationModel>();
                        perResource[addr] = list;
                    }

                    list.Add(model);
                }
                else
                {
                    orphanActions.Add(model);
                }
            }
            else if (invocation.InvokeActionTrigger is not null)
            {
                invokeActions.Add(model);
            }
            else
            {
                // No trigger metadata at all — treat as orphan to avoid silent loss.
                orphanActions.Add(model);
            }
        }
    }
}
