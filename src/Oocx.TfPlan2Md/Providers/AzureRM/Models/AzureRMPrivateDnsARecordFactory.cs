using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Non-breaking space used to keep icons attached to labels.
    /// </summary>
    private const string NonBreakingSpace = "\u00A0";

    /// <summary>
    /// Icon used for record name display.
    /// </summary>
    private const string RecordNameIcon = "🆔";

    /// <summary>
    /// Icon used for record value display.
    /// </summary>
    private const string RecordValueIcon = "🌐";

    /// <summary>
    /// Attribute name for record values.
    /// </summary>
    private const string RecordsAttribute = "records";

    /// <summary>
    /// Maximum number of record values to include in the summary.
    /// </summary>
    private const int MaxRecordValues = 3;

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
        if (!TryBuildSummaryData(state, out var recordName, out var fqdn, out var recordValues))
        {
            return;
        }

        var recordNameToken = FormatCodeTable($"{RecordNameIcon}{NonBreakingSpace}{recordName}");
        var fqdnToken = FormatCodeTable(fqdn);
        var recordTokens = BuildRecordValueTokens(recordValues);
        var recordSuffix = recordTokens.Count > 0 ? $" {string.Join(" ", recordTokens)}" : string.Empty;

        model.Summary = $"{recordNameToken} — {fqdnToken}{recordSuffix}";
        model.SummaryHtml = BuildSummaryHtml(model, recordName, fqdn, recordValues);
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
    /// Builds the summary HTML string using the record name, FQDN, and record values.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="recordName">The DNS record name.</param>
    /// <param name="fqdn">The fully qualified DNS name.</param>
    /// <param name="recordValues">The record values to include.</param>
    /// <returns>Summary HTML string for the resource.</returns>
    private static string BuildSummaryHtml(
        ResourceChangeModel model,
        string recordName,
        string fqdn,
        IReadOnlyList<string> recordValues)
    {
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary($"{RecordNameIcon}{NonBreakingSpace}{recordName}")}</b>";
        var parts = new List<string> { FormatCodeSummary(fqdn) };
        parts.AddRange(recordValues.Select(value => FormatCodeSummary($"{RecordValueIcon}{NonBreakingSpace}{value}")));

        return $"{prefix} — {string.Join(" ", parts)}";
    }

    /// <summary>
    /// Attempts to build summary data from the resource state.
    /// </summary>
    /// <param name="state">The active Terraform state.</param>
    /// <param name="recordName">The resolved record name when available.</param>
    /// <param name="fqdn">The combined name and zone when available.</param>
    /// <param name="recordValues">The resolved record values.</param>
    /// <returns>True when both name and zone are present; otherwise false.</returns>
    private static bool TryBuildSummaryData(
        object? state,
        out string recordName,
        out string fqdn,
        out IReadOnlyList<string> recordValues)
    {
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);
        flatState.TryGetValue(NameAttribute, out var name);
        flatState.TryGetValue(ZoneNameAttribute, out var zoneName);
        recordValues = BuildRecordValues(flatState);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(zoneName))
        {
            recordName = string.Empty;
            fqdn = string.Empty;
            return false;
        }

        recordName = name;
        fqdn = $"{name}.{zoneName}";
        return true;
    }

    /// <summary>
    /// Builds the list of record values to include in the summary.
    /// </summary>
    /// <param name="flatState">The flattened Terraform state.</param>
    /// <returns>Record values to include in the summary.</returns>
    private static List<string> BuildRecordValues(Dictionary<string, string?> flatState)
    {
        var values = new List<string>();
        for (var index = 0; index < MaxRecordValues; index++)
        {
            if (!flatState.TryGetValue($"{RecordsAttribute}[{index}]", out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    /// <summary>
    /// Builds formatted record value tokens for summary rendering.
    /// </summary>
    /// <param name="recordValues">Record values to format.</param>
    /// <returns>Formatted record value tokens.</returns>
    private static IReadOnlyList<string> BuildRecordValueTokens(IReadOnlyList<string> recordValues)
    {
        if (recordValues.Count == 0)
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>(recordValues.Count);
        foreach (var value in recordValues)
        {
            tokens.Add(FormatCodeTable($"{RecordValueIcon}{NonBreakingSpace}{value}"));
        }

        return tokens;
    }
}
