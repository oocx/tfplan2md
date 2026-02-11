using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Mappers;

/// <summary>
/// Maps azurerm_firewall_network_rule_collection resources to ScriptObject with FirewallNetworkRuleCollectionViewModel.
/// </summary>
internal sealed class FirewallNetworkRuleCollectionMapper : IResourceModelMapper
{
    private readonly FirewallNetworkRuleCollectionFactory _factory;

    /// <summary>
    /// The Scriban key used for source addresses fields.
    /// </summary>
    private const string SourceAddressesKey = "source_addresses";

    /// <summary>
    /// The Scriban key used for destination addresses fields.
    /// </summary>
    private const string DestinationAddressesKey = "destination_addresses";

    /// <summary>
    /// The Scriban key used for destination ports fields.
    /// </summary>
    private const string DestinationPortsKey = "destination_ports";

    /// <summary>
    /// The Scriban key used for description fields.
    /// </summary>
    private const string DescriptionKey = "description";

    /// <summary>
    /// Initializes a new instance of the <see cref="FirewallNetworkRuleCollectionMapper"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating FirewallNetworkRuleCollectionViewModel instances.</param>
    public FirewallNetworkRuleCollectionMapper(FirewallNetworkRuleCollectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Determines whether this mapper applies to the resource.
    /// </summary>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><c>true</c> if the resource is azurerm_firewall_network_rule_collection; otherwise, <c>false</c>.</returns>
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azurerm_firewall_network_rule_collection";
    }

    /// <summary>
    /// Enriches the ScriptObject with firewall_network_rule_collection property.
    /// </summary>
    /// <param name="resource">The resource change model.</param>
    /// <param name="scriptObject">The ScriptObject to enrich.</param>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }

        var (viewModel, changedAttributesSummary) = _factory.CreateViewModel(resource.ResourceChange, resource.Action);
        scriptObject["firewall_network_rule_collection"] = MapFirewallNetworkRuleCollection(viewModel);

        // Update changed attributes summary if provided
        if (changedAttributesSummary != null)
        {
            scriptObject["changed_attributes_summary"] = changedAttributesSummary;
        }
    }

    /// <summary>
    /// Maps a FirewallNetworkRuleCollectionViewModel to a ScriptObject.
    /// </summary>
    /// <param name="fwrc">The view model to map.</param>
    /// <returns>A ScriptObject containing the mapped data.</returns>
    private static ScriptObject MapFirewallNetworkRuleCollection(FirewallNetworkRuleCollectionViewModel fwrc)
    {
        var obj = new ScriptObject
        {
            ["name"] = fwrc.Name,
            ["priority"] = fwrc.Priority,
            ["action"] = fwrc.Action
        };

        // Rule changes for update scenarios
        var ruleChanges = new ScriptArray();
        foreach (var rule in fwrc.RuleChanges)
        {
            var ruleObj = new ScriptObject
            {
                ["change"] = rule.Change,
                ["name"] = rule.Name,
                ["protocols"] = rule.Protocols,
                [SourceAddressesKey] = rule.SourceAddresses,
                [DestinationAddressesKey] = rule.DestinationAddresses,
                [DestinationPortsKey] = rule.DestinationPorts,
                [DescriptionKey] = rule.Description
            };
            ruleChanges.Add(ruleObj);
        }

        obj["rule_changes"] = ruleChanges;

        // After rules for create scenarios
        var afterRules = new ScriptArray();
        foreach (var rule in fwrc.AfterRules)
        {
            afterRules.Add(MapFirewallRuleRow(rule));
        }

        obj["after_rules"] = afterRules;

        // Before rules for delete scenarios
        var beforeRules = new ScriptArray();
        foreach (var rule in fwrc.BeforeRules)
        {
            beforeRules.Add(MapFirewallRuleRow(rule));
        }

        obj["before_rules"] = beforeRules;

        return obj;
    }

    /// <summary>
    /// Maps a FirewallRuleRowViewModel to a ScriptObject.
    /// </summary>
    /// <param name="rule">The rule view model to map.</param>
    /// <returns>A ScriptObject containing the mapped rule data.</returns>
    private static ScriptObject MapFirewallRuleRow(FirewallRuleRowViewModel rule)
    {
        return new ScriptObject
        {
            ["name"] = rule.Name,
            ["protocols"] = rule.Protocols,
            [SourceAddressesKey] = rule.SourceAddresses,
            [DestinationAddressesKey] = rule.DestinationAddresses,
            [DestinationPortsKey] = rule.DestinationPorts,
            [DescriptionKey] = rule.Description
        };
    }
}
