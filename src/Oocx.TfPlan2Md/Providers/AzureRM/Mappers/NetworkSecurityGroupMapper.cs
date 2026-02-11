using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Mappers;

/// <summary>
/// Maps azurerm_network_security_group resources to ScriptObject with NetworkSecurityGroupViewModel.
/// </summary>
internal sealed class NetworkSecurityGroupMapper : IResourceModelMapper
{
    private readonly NetworkSecurityGroupFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkSecurityGroupMapper"/> class.
    /// </summary>
    /// <param name="factory">The factory for creating NetworkSecurityGroupViewModel instances.</param>
    public NetworkSecurityGroupMapper(NetworkSecurityGroupFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Determines whether this mapper applies to the resource.
    /// </summary>
    /// <param name="resource">The resource to evaluate.</param>
    /// <returns><c>true</c> if the resource is azurerm_network_security_group; otherwise, <c>false</c>.</returns>
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azurerm_network_security_group";
    }

    /// <summary>
    /// Enriches the ScriptObject with network_security_group property.
    /// </summary>
    /// <param name="resource">The resource change model.</param>
    /// <param name="scriptObject">The ScriptObject to enrich.</param>
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }

        var viewModel = _factory.CreateViewModel(resource.ResourceChange);
        scriptObject["network_security_group"] = MapNetworkSecurityGroup(viewModel);
    }

    private static ScriptObject MapNetworkSecurityGroup(NetworkSecurityGroupViewModel nsg)
    {
        var obj = new ScriptObject();
        obj["name"] = nsg.Name;

        // Rule changes for update scenarios
        var ruleChanges = new ScriptArray();
        foreach (var rule in nsg.RuleChanges)
        {
            var ruleObj = new ScriptObject();
            ruleObj["change"] = rule.Change;
            ruleObj["name"] = rule.Name;
            ruleObj["priority"] = rule.Priority;
            ruleObj["direction"] = rule.Direction;
            ruleObj["access"] = rule.Access;
            ruleObj["protocol"] = rule.Protocol;
            ruleObj["source_addresses"] = rule.SourceAddresses;
            ruleObj["source_ports"] = rule.SourcePorts;
            ruleObj["destination_addresses"] = rule.DestinationAddresses;
            ruleObj["destination_ports"] = rule.DestinationPorts;
            ruleObj["description"] = rule.Description;
            ruleChanges.Add(ruleObj);
        }

        obj["rule_changes"] = ruleChanges;

        //After rules for create scenarios
        var afterRules = new ScriptArray();
        foreach (var rule in nsg.AfterRules)
        {
            afterRules.Add(MapSecurityRuleRow(rule));
        }

        obj["after_rules"] = afterRules;

        // Before rules for delete scenarios
        var beforeRules = new ScriptArray();
        foreach (var rule in nsg.BeforeRules)
        {
            beforeRules.Add(MapSecurityRuleRow(rule));
        }

        obj["before_rules"] = beforeRules;

        return obj;
    }

    private static ScriptObject MapSecurityRuleRow(SecurityRuleRowViewModel rule)
    {
        var ruleObj = new ScriptObject();
        ruleObj["name"] = rule.Name;
        ruleObj["priority"] = rule.Priority;
        ruleObj["direction"] = rule.Direction;
        ruleObj["access"] = rule.Access;
        ruleObj["protocol"] = rule.Protocol;
        ruleObj["source_addresses"] = rule.SourceAddresses;
        ruleObj["source_ports"] = rule.SourcePorts;
        ruleObj["destination_addresses"] = rule.DestinationAddresses;
        ruleObj["destination_ports"] = rule.DestinationPorts;
        ruleObj["description"] = rule.Description;
        return ruleObj;
    }
}
