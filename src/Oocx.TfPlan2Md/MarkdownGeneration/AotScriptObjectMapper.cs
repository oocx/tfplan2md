using System;
using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provides explicit mapping of ReportModel to ScriptObject for NativeAOT compatibility.
/// Reflection-based Scriban Import does not work reliably under AOT trimming.
/// Related feature: docs/features/037-aot-trimmed-image/specification.md.
/// </summary>
internal static class AotScriptObjectMapper
{
    /// <summary>
    /// Maps a ReportModel to a ScriptObject without using reflection.
    /// </summary>
    /// <param name="model">The report model to map.</param>
    /// <returns>A ScriptObject containing all report data accessible by templates.</returns>
    internal static ScriptObject MapReportModel(ReportModel model)
    {
        var scriptObject = new ScriptObject();

        // Top-level scalar properties
        scriptObject["terraform_version"] = model.TerraformVersion;
        scriptObject["format_version"] = model.FormatVersion;
        scriptObject["tf_plan2_md_version"] = model.TfPlan2MdVersion;
        scriptObject["commit_hash"] = model.CommitHash;
        scriptObject["hide_metadata"] = model.HideMetadata;
        scriptObject["timestamp"] = model.Timestamp;
        scriptObject["report_title"] = model.ReportTitle;
        scriptObject["show_unchanged_values"] = model.ShowUnchangedValues;
        scriptObject["large_value_format"] = model.RenderTarget == RenderTargets.RenderTarget.GitHub ? "simple-diff" : "inline-diff";

        // Generated timestamp as nested object with DateTime for Scriban date functions
        var generatedAtUtcObj = new ScriptObject();
        generatedAtUtcObj.Add("date_time", model.GeneratedAtUtc.UtcDateTime);
        scriptObject["generated_at_utc"] = generatedAtUtcObj;

        // Summary
        scriptObject["summary"] = MapSummary(model.Summary);

        // Changes and module changes
        scriptObject["changes"] = MapChanges(model.Changes);
        scriptObject["module_changes"] = MapModuleChanges(model.ModuleChanges);

        return scriptObject;
    }

    /// <summary>
    /// Maps a ResourceChangeModel to a ScriptObject for resource-specific template rendering.
    /// Includes large_value_format from the provided parameter.
    /// </summary>
    /// <param name="change">The resource change to map.</param>
    /// <param name="renderTarget">The target platform for rendering.</param>
    /// <returns>A ScriptObject containing the change data.</returns>
    internal static ScriptObject MapResourceChangeWithFormat(ResourceChangeModel change, RenderTargets.RenderTarget renderTarget)
    {
        var changeObject = MapResourceChange(change);

        // Add large_value_format to change context for template access
        var formatString = renderTarget == RenderTargets.RenderTarget.GitHub ? "simple-diff" : "inline-diff";
        changeObject["large_value_format"] = formatString;

        return changeObject;
    }

    private static ScriptObject MapSummary(SummaryModel summary)
    {
        var obj = new ScriptObject();
        obj["to_add"] = MapActionSummary(summary.ToAdd);
        obj["to_change"] = MapActionSummary(summary.ToChange);
        obj["to_destroy"] = MapActionSummary(summary.ToDestroy);
        obj["to_replace"] = MapActionSummary(summary.ToReplace);
        obj["no_op"] = MapActionSummary(summary.NoOp);
        obj["total"] = summary.Total;
        return obj;
    }

    private static ScriptObject MapActionSummary(ActionSummary action)
    {
        var obj = new ScriptObject();
        obj["count"] = action.Count;

        var breakdown = new ScriptArray();
        foreach (var b in action.Breakdown)
        {
            var bObj = new ScriptObject();
            bObj["type"] = b.Type;
            bObj["count"] = b.Count;
            breakdown.Add(bObj);
        }

        obj["breakdown"] = breakdown;
        return obj;
    }

    private static ScriptArray MapChanges(IReadOnlyList<ResourceChangeModel> changes)
    {
        var arr = new ScriptArray();
        foreach (var change in changes)
        {
            arr.Add(MapResourceChange(change));
        }

        return arr;
    }

    private static ScriptArray MapModuleChanges(IReadOnlyList<ModuleChangeGroup> moduleChanges)
    {
        var arr = new ScriptArray();
        foreach (var group in moduleChanges)
        {
            var obj = new ScriptObject();
            obj["module_address"] = group.ModuleAddress;
            obj["changes"] = MapChanges(group.Changes);
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptObject MapResourceChange(ResourceChangeModel change)
    {
        var obj = new ScriptObject();
        obj["address"] = change.Address;
        obj["module_address"] = change.ModuleAddress;
        obj["type"] = change.Type;
        obj["name"] = change.Name;
        obj["provider_name"] = change.ProviderName;
        obj["action"] = change.Action;
        obj["action_symbol"] = change.ActionSymbol;
        obj["summary"] = change.Summary;
        obj["summary_html"] = change.SummaryHtml;
        obj["changed_attributes_summary"] = change.ChangedAttributesSummary;
        obj["tags_badges"] = change.TagsBadges;

        // JSON values
        obj["before_json"] = change.BeforeJson is JsonElement jsonBefore
            ? ConvertToScriptObject(jsonBefore)
            : null;
        obj["after_json"] = change.AfterJson is JsonElement jsonAfter
            ? ConvertToScriptObject(jsonAfter)
            : null;

        // Replace paths
        if (change.ReplacePaths != null)
        {
            var replacePaths = new ScriptArray();
            foreach (var path in change.ReplacePaths)
            {
                var pathArr = new ScriptArray();
                foreach (var segment in path)
                {
                    pathArr.Add(segment?.ToString());
                }

                replacePaths.Add(pathArr);
            }

            obj["replace_paths"] = replacePaths;
        }

        // Attribute changes
        var attrChanges = new ScriptArray();
        foreach (var attr in change.AttributeChanges)
        {
            attrChanges.Add(MapAttributeChange(attr));
        }

        obj["attribute_changes"] = attrChanges;

        // View models for specialized templates
        if (change.NetworkSecurityGroup != null)
        {
            obj["network_security_group"] = MapNetworkSecurityGroup(change.NetworkSecurityGroup);
        }

        if (change.FirewallNetworkRuleCollection != null)
        {
            obj["firewall_network_rule_collection"] = MapFirewallNetworkRuleCollection(change.FirewallNetworkRuleCollection);
        }

        if (change.RoleAssignment != null)
        {
            obj["role_assignment"] = MapRoleAssignment(change.RoleAssignment);
        }

        if (change.VariableGroup != null)
        {
            obj["variable_group"] = MapVariableGroup(change.VariableGroup);
        }

        return obj;
    }

    private static ScriptObject MapAttributeChange(AttributeChangeModel attr)
    {
        var obj = new ScriptObject();
        obj["name"] = attr.Name;
        obj["before"] = attr.Before;
        obj["after"] = attr.After;
        obj["is_sensitive"] = attr.IsSensitive;
        obj["is_large"] = attr.IsLarge;
        return obj;
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

        // After rules for create scenarios
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

    private static ScriptObject MapFirewallNetworkRuleCollection(FirewallNetworkRuleCollectionViewModel fwrc)
    {
        var obj = new ScriptObject();
        obj["name"] = fwrc.Name;
        obj["priority"] = fwrc.Priority;
        obj["action"] = fwrc.Action;

        // Rule changes for update scenarios
        var ruleChanges = new ScriptArray();
        foreach (var rule in fwrc.RuleChanges)
        {
            var ruleObj = new ScriptObject();
            ruleObj["change"] = rule.Change;
            ruleObj["name"] = rule.Name;
            ruleObj["protocols"] = rule.Protocols;
            ruleObj["source_addresses"] = rule.SourceAddresses;
            ruleObj["destination_addresses"] = rule.DestinationAddresses;
            ruleObj["destination_ports"] = rule.DestinationPorts;
            ruleObj["description"] = rule.Description;
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

    private static ScriptObject MapFirewallRuleRow(FirewallRuleRowViewModel rule)
    {
        var ruleObj = new ScriptObject();
        ruleObj["name"] = rule.Name;
        ruleObj["protocols"] = rule.Protocols;
        ruleObj["source_addresses"] = rule.SourceAddresses;
        ruleObj["destination_addresses"] = rule.DestinationAddresses;
        ruleObj["destination_ports"] = rule.DestinationPorts;
        ruleObj["description"] = rule.Description;
        return ruleObj;
    }

    private static ScriptObject MapRoleAssignment(RoleAssignmentViewModel ra)
    {
        var obj = new ScriptObject();
        obj["resource_name"] = ra.ResourceName;
        obj["description"] = ra.Description;
        obj["summary_text"] = ra.SummaryText;

        // Small attributes for table display
        var smallAttributes = new ScriptArray();
        foreach (var attr in ra.SmallAttributes)
        {
            smallAttributes.Add(MapRoleAssignmentAttribute(attr));
        }

        obj["small_attributes"] = smallAttributes;

        // Large attributes for collapsible display
        var largeAttributes = new ScriptArray();
        foreach (var attr in ra.LargeAttributes)
        {
            largeAttributes.Add(MapRoleAssignmentAttribute(attr));
        }

        obj["large_attributes"] = largeAttributes;

        return obj;
    }

    private static ScriptObject MapRoleAssignmentAttribute(RoleAssignmentAttributeViewModel attr)
    {
        var obj = new ScriptObject();
        obj["name"] = attr.Name;
        obj["before"] = attr.Before;
        obj["after"] = attr.After;
        return obj;
    }

    private static ScriptObject MapVariableGroup(VariableGroupViewModel vg)
    {
        var obj = new ScriptObject();
        obj["name"] = vg.Name;
        obj["description"] = vg.Description;

        // Variable changes for update scenarios
        var variableChanges = new ScriptArray();
        foreach (var variable in vg.VariableChanges)
        {
            variableChanges.Add(MapVariableChangeRow(variable));
        }

        obj["variable_changes"] = variableChanges;

        // After variables for create scenarios
        var afterVariables = new ScriptArray();
        foreach (var variable in vg.AfterVariables)
        {
            afterVariables.Add(MapVariableRow(variable));
        }

        obj["after_variables"] = afterVariables;

        // Before variables for delete scenarios
        var beforeVariables = new ScriptArray();
        foreach (var variable in vg.BeforeVariables)
        {
            beforeVariables.Add(MapVariableRow(variable));
        }

        obj["before_variables"] = beforeVariables;

        // Key Vault blocks
        var keyVaultBlocks = new ScriptArray();
        foreach (var kv in vg.KeyVaultBlocks)
        {
            keyVaultBlocks.Add(MapKeyVaultRow(kv));
        }

        obj["key_vault_blocks"] = keyVaultBlocks;

        return obj;
    }

    private static ScriptObject MapVariableChangeRow(VariableChangeRowViewModel variable)
    {
        var obj = new ScriptObject();
        obj["change"] = variable.Change;
        obj["name"] = variable.Name;
        obj["value"] = variable.Value;
        obj["enabled"] = variable.Enabled;
        obj["content_type"] = variable.ContentType;
        obj["expires"] = variable.Expires;
        obj["is_large_value"] = variable.IsLargeValue;
        return obj;
    }

    private static ScriptObject MapVariableRow(VariableRowViewModel variable)
    {
        var obj = new ScriptObject();
        obj["name"] = variable.Name;
        obj["value"] = variable.Value;
        obj["enabled"] = variable.Enabled;
        obj["content_type"] = variable.ContentType;
        obj["expires"] = variable.Expires;
        obj["is_large_value"] = variable.IsLargeValue;
        return obj;
    }

    private static ScriptObject MapKeyVaultRow(KeyVaultRowViewModel kv)
    {
        var obj = new ScriptObject();
        obj["name"] = kv.Name;
        obj["service_endpoint_id"] = kv.ServiceEndpointId;
        obj["search_depth"] = kv.SearchDepth;
        return obj;
    }
}
