using System;
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
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(context.Model.Type, _resourceType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var state = ResourceChangeHelpers.ResolveActiveState(context.ResourceChange, context.Action);
        context.Model.SummaryHtml = AzureRMApimSummaryBuilder.BuildApiOperationSummaryHtml(context.Model, state);
    }
}
