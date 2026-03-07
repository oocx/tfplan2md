using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies API Management subresource summary overrides that include API Management names.
/// Related feature: docs/features/051-display-enhancements/specification.md.
/// </summary>
internal sealed class AzureRMApimSubresourceFactory : IResourceViewModelFactory
{
    private readonly HashSet<string> _resourceTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRMApimSubresourceFactory"/> class.
    /// </summary>
    /// <param name="resourceTypes">The resource types this factory handles.</param>
    internal AzureRMApimSubresourceFactory(IEnumerable<string> resourceTypes)
    {
        _resourceTypes = new HashSet<string>(resourceTypes, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_resourceTypes.Contains(context.Model.Type))
        {
            return;
        }

        var state = ResolveActiveState(context.ResourceChange, context.Action);
        context.Model.SummaryHtml = AzureRMApimSummaryBuilder.BuildSubresourceSummaryHtml(context.Model, state);
    }

    /// <summary>
    /// Resolves the state object to use for summary generation based on the action.
    /// </summary>
    /// <param name="resourceChange">The resource change.</param>
    /// <param name="action">The normalized action string.</param>
    /// <returns>The state object to summarize.</returns>
    private static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }
}
