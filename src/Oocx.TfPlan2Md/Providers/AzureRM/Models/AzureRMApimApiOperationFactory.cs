using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies API Management API operation summary overrides.
/// Related feature: docs/features/051-display-enhancements/specification.md.
/// </summary>
internal sealed class AzureRMApimApiOperationFactory : IResourceViewModelFactory
{
    private readonly string _resourceType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRMApimApiOperationFactory"/> class.
    /// </summary>
    /// <param name="resourceType">The resource type this factory handles.</param>
    internal AzureRMApimApiOperationFactory(string resourceType = "azurerm_api_management_api_operation")
    {
        _resourceType = resourceType;
    }

    /// <inheritdoc />
    public void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        _ = attributeChanges;

        if (!string.Equals(model.Type, _resourceType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var state = ResolveActiveState(resourceChange, action);
        model.SummaryHtml = AzureRMApimSummaryBuilder.BuildApiOperationSummaryHtml(model, state);
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
