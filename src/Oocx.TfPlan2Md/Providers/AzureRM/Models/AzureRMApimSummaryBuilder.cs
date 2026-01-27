using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Builds summary HTML strings for API Management subresources.
/// Related feature: docs/features/051-display-enhancements/specification.md.
/// </summary>
internal static class AzureRMApimSummaryBuilder
{
    /// <summary>
    /// Attribute name for API Management name fields.
    /// </summary>
    private const string ApiManagementNameAttribute = "api_management_name";

    /// <summary>
    /// Attribute name for resource group names.
    /// </summary>
    private const string ResourceGroupNameAttribute = "resource_group_name";

    /// <summary>
    /// Builds a summary line for API Management subresources that include an API Management name.
    /// </summary>
    /// <param name="model">The resource change model to summarize.</param>
    /// <param name="state">The active Terraform state object.</param>
    /// <returns>Summary HTML string.</returns>
    internal static string BuildSubresourceSummaryHtml(ResourceChangeModel model, object? state)
    {
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        flatState.TryGetValue("name", out var nameValue);
        flatState.TryGetValue(ApiManagementNameAttribute, out var apiManagementName);
        flatState.TryGetValue(ResourceGroupNameAttribute, out var resourceGroup);
        flatState.TryGetValue("location", out var location);

        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        var detailParts = new List<string>();

        var primaryContext = !string.IsNullOrWhiteSpace(nameValue)
            ? FormatAttributeValueSummary("name", nameValue!, null)
            : null;

        if (!string.IsNullOrWhiteSpace(apiManagementName))
        {
            var apiManagementText = FormatAttributeValueSummary(ApiManagementNameAttribute, apiManagementName!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {apiManagementText}" : apiManagementText;
        }

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            var groupText = FormatAttributeValueSummary(ResourceGroupNameAttribute, resourceGroup!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} in {groupText}" : groupText;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationText = FormatAttributeValueSummary("location", location!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {locationText}" : locationText;
        }

        if (primaryContext != null)
        {
            detailParts.Add(primaryContext);
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }

    /// <summary>
    /// Builds a summary line for API Management API operation resources.
    /// </summary>
    /// <param name="model">The resource change model to summarize.</param>
    /// <param name="state">The active Terraform state object.</param>
    /// <returns>Summary HTML string.</returns>
    internal static string BuildApiOperationSummaryHtml(ResourceChangeModel model, object? state)
    {
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        flatState.TryGetValue("display_name", out var displayName);
        flatState.TryGetValue("operation_id", out var operationId);
        flatState.TryGetValue("api_name", out var apiName);
        flatState.TryGetValue(ApiManagementNameAttribute, out var apiManagementName);
        flatState.TryGetValue(ResourceGroupNameAttribute, out var resourceGroup);

        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            prefix = $"{prefix} {FormatAttributeValueSummary("display_name", displayName!, null)}";
        }

        var detailParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(operationId))
        {
            detailParts.Add(FormatAttributeValueSummary("operation_id", operationId!, null));
        }

        if (!string.IsNullOrWhiteSpace(apiName))
        {
            detailParts.Add(FormatAttributeValueSummary("api_name", apiName!, null));
        }

        if (!string.IsNullOrWhiteSpace(apiManagementName))
        {
            detailParts.Add(FormatAttributeValueSummary(ApiManagementNameAttribute, apiManagementName!, null));
        }

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            detailParts.Add($"in {FormatAttributeValueSummary(ResourceGroupNameAttribute, resourceGroup!, null)}");
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }
}
