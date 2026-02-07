using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies summary overrides for AzureRM private DNS A record resources.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
internal sealed class AzureRMPrivateDnsARecordFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Terraform resource type handled by this factory.
    /// </summary>
    private const string ResourceType = "azurerm_private_dns_a_record";

    /// <summary>
    /// Attribute name for the record name.
    /// </summary>
    private const string NameAttribute = "name";

    /// <summary>
    /// Attribute name for the private DNS zone name.
    /// </summary>
    private const string ZoneNameAttribute = "zone_name";

    /// <inheritdoc />
    public void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _ = attributeChanges;
        _ = principalMapper;
        _ = iconProviderRegistry;

        if (!string.Equals(model.Type, ResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var state = ResolveActiveState(resourceChange, action);
        if (!TryBuildFqdn(state, out var fqdn))
        {
            return;
        }

        model.Summary = FormatValue(fqdn, model.ProviderName);
        model.SummaryHtml = BuildSummaryHtml(model, fqdn);
    }

    /// <summary>
    /// Resolves the state object to use for summary generation based on the action.
    /// </summary>
    /// <param name="resourceChange">The resource change data.</param>
    /// <param name="action">The normalized Terraform action.</param>
    /// <returns>The resolved state object.</returns>
    private static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }

    /// <summary>
    /// Builds the summary HTML string using the fully qualified DNS name.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="fqdn">The fully qualified DNS name.</param>
    /// <returns>Summary HTML string for the resource.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model, string fqdn)
    {
        return $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(fqdn)}</b>";
    }

    /// <summary>
    /// Attempts to build the FQDN from the resource state.
    /// </summary>
    /// <param name="state">The active Terraform state.</param>
    /// <param name="fqdn">The combined name and zone when available.</param>
    /// <returns>True when both name and zone are present; otherwise false.</returns>
    private static bool TryBuildFqdn(object? state, out string fqdn)
    {
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);
        flatState.TryGetValue(NameAttribute, out var name);
        flatState.TryGetValue(ZoneNameAttribute, out var zoneName);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(zoneName))
        {
            fqdn = string.Empty;
            return false;
        }

        fqdn = $"{name}.{zoneName}";
        return true;
    }
}
