using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Builds <see cref="NetworkSecurityGroupViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
internal static class NetworkSecurityGroupViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided network security group change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <returns>Populated <see cref="NetworkSecurityGroupViewModel"/>.</returns>
    public static NetworkSecurityGroupViewModel Build(ResourceChange change, string providerName)
    {
        var name = ExtractName(change.Change.After) ?? ExtractName(change.Change.Before);

        var beforeRules = ExtractRules(change.Change.Before);
        var afterRules = ExtractRules(change.Change.After);

        var beforeLookup = beforeRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        var afterLookup = afterRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
        var beforeNames = new HashSet<string>(beforeRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        var afterNames = new HashSet<string>(afterRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);

        // Added group: new rules (not in before) + after-version of modified rules (shown as ➕)
        var addedGroup = BuildAddedGroup(afterRules, beforeNames, beforeLookup, providerName);

        // Unchanged group: rules present in both with identical values (shown as ⏺️)
        var unchangedGroup = BuildUnchangedGroup(afterRules, beforeNames, beforeLookup, providerName);

        // Removed group: before-version of modified rules (shown as ❌) + rules only in before
        var removedGroup = BuildRemovedGroup(beforeRules, afterNames, afterLookup, providerName);

        var changeRows = new List<SecurityRuleChangeRowViewModel>(addedGroup.Count + unchangedGroup.Count + removedGroup.Count);
        changeRows.AddRange(addedGroup);
        changeRows.AddRange(unchangedGroup);
        changeRows.AddRange(removedGroup);

        return new NetworkSecurityGroupViewModel
        {
            Name = name,
            RuleChanges = changeRows,
            AfterRules = FormatRuleRows(afterRules, providerName),
            BeforeRules = FormatRuleRows(beforeRules, providerName)
        };
    }

    /// <summary>
    /// Extracts the network security group name from the provided state JSON.
    /// </summary>
    /// <param name="state">Terraform state object from the plan.</param>
    /// <returns>Name value when present; otherwise null.</returns>
    private static string? ExtractName(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return element.TryGetProperty("name", out var nameProperty) && nameProperty.ValueKind == JsonValueKind.String
            ? nameProperty.GetString()
            : null;
    }

    /// <summary>
    /// Builds raw security rule values from the Terraform state object.
    /// </summary>
    /// <param name="state">Terraform state object containing a security_rule array.</param>
    /// <returns>Collection of extracted rule values.</returns>
    private static IReadOnlyList<SecurityRuleValues> ExtractRules(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<SecurityRuleValues>();
        }

        if (!element.TryGetProperty("security_rule", out var rulesElement) || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SecurityRuleValues>();
        }

        var rules = new List<SecurityRuleValues>();
        foreach (var ruleElement in rulesElement.EnumerateArray())
        {
            if (ruleElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(ruleElement, "name");
            var priority = GetString(ruleElement, "priority");
            var priorityNumber = TryParsePriority(priority);
            var direction = GetString(ruleElement, "direction");
            var access = GetString(ruleElement, "access");
            var protocol = GetString(ruleElement, "protocol");
            var sourceAddresses = ComposeValue(ruleElement, "source_address_prefixes", "source_address_prefix");
            var destinationAddresses = ComposeValue(ruleElement, "destination_address_prefixes", "destination_address_prefix");
            var sourcePorts = ComposeValue(ruleElement, "source_port_ranges", "source_port_range");
            var destinationPorts = ComposeValue(ruleElement, "destination_port_ranges", "destination_port_range");
            var description = NormalizeDescription(GetString(ruleElement, "description"));

            rules.Add(new SecurityRuleValues(
                name,
                priorityNumber,
                priority,
                direction,
                access,
                protocol,
                sourceAddresses,
                sourcePorts,
                destinationAddresses,
                destinationPorts,
                description));
        }

        return rules;
    }

    /// <summary>
    /// Builds the added group: completely new rules plus the after-version of modified rules.
    /// Modified rules appear in both the added and removed groups (split-diff).
    /// </summary>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="beforeNames">Set of rule names in the before state.</param>
    /// <param name="beforeLookup">Before-state rules indexed by name.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered added rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildAddedGroup(
        IReadOnlyList<SecurityRuleValues> afterRules,
        HashSet<string> beforeNames,
        Dictionary<string, SecurityRuleValues> beforeLookup,
        string providerName)
    {
        return afterRules
            .Where(r => !beforeNames.Contains(r.Name) ||
                        (beforeLookup.TryGetValue(r.Name, out var before) && !RulesEqual(before, r)))
            .OrderBy(r => r.PriorityNumber ?? int.MaxValue)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => CreateAddedRow(r, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds the unchanged group: rules present in both states with identical values.
    /// </summary>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="beforeNames">Set of rule names in the before state.</param>
    /// <param name="beforeLookup">Before-state rules indexed by name.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered unchanged rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildUnchangedGroup(
        IReadOnlyList<SecurityRuleValues> afterRules,
        HashSet<string> beforeNames,
        Dictionary<string, SecurityRuleValues> beforeLookup,
        string providerName)
    {
        return afterRules
            .Where(r => beforeNames.Contains(r.Name) && beforeLookup.TryGetValue(r.Name, out var before) && RulesEqual(before, r))
            .OrderBy(r => r.PriorityNumber ?? int.MaxValue)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => CreateUnchangedRow(r, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds the removed group: the before-version of modified rules plus completely deleted rules.
    /// Modified rules appear in both the added and removed groups (split-diff).
    /// </summary>
    /// <param name="beforeRules">Rules from the before state.</param>
    /// <param name="afterNames">Set of rule names in the after state.</param>
    /// <param name="afterLookup">After-state rules indexed by name.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered removed rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildRemovedGroup(
        IReadOnlyList<SecurityRuleValues> beforeRules,
        HashSet<string> afterNames,
        Dictionary<string, SecurityRuleValues> afterLookup,
        string providerName)
    {
        return beforeRules
            .Where(r => !afterNames.Contains(r.Name) ||
                        (afterLookup.TryGetValue(r.Name, out var after) && !RulesEqual(r, after)))
            .OrderBy(r => r.PriorityNumber ?? int.MaxValue)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .Select(r => CreateRemovedRow(r, providerName))
            .ToList();
    }

    /// <summary>
    /// Formats rule values for create/delete tables.
    /// </summary>
    /// <param name="rules">Raw rule values.</param>
    /// <param name="providerName">Provider name for semantic formatting.</param>
    /// <returns>Formatted rule rows.</returns>
    private static List<SecurityRuleRowViewModel> FormatRuleRows(
        IReadOnlyList<SecurityRuleValues> rules,
        string providerName)
    {
        return rules
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(rule => new SecurityRuleRowViewModel
            {
                Name = FormatAttributeValueTable("name", rule.Name, providerName),
                Priority = FormatAttributeValueTable("priority", rule.Priority, providerName),
                Direction = FormatAttributeValueTable("direction", rule.Direction, providerName),
                Access = FormatAttributeValueTable("access", rule.Access, providerName),
                Protocol = FormatNsgProtocol(rule.Protocol),
                SourceAddresses = FormatNsgAddresses(rule.SourceAddresses, "source_address_prefix", providerName),
                SourcePorts = FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
                DestinationAddresses = FormatNsgAddresses(rule.DestinationAddresses, "destination_address_prefix", providerName),
                DestinationPorts = FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
                Description = FormatDescriptionCell(rule.Description, providerName)
            })
            .ToList();
    }

    /// <summary>
    /// Creates a formatted row for an added rule.
    /// </summary>
    /// <param name="rule">Rule values from the after state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Formatted change row.</returns>
    private static SecurityRuleChangeRowViewModel CreateAddedRow(SecurityRuleValues rule, string providerName)
    {
        return new SecurityRuleChangeRowViewModel
        {
            Change = ActionIcons.Add,
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = FormatNsgProtocol(rule.Protocol),
            SourceAddresses = FormatNsgAddresses(rule.SourceAddresses, "source_address_prefix", providerName),
            SourcePorts = FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = FormatNsgAddresses(rule.DestinationAddresses, "destination_address_prefix", providerName),
            DestinationPorts = FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = FormatDescriptionCell(rule.Description, providerName)
        };
    }

    /// <summary>
    /// Creates a formatted row for a removed rule.
    /// </summary>
    /// <param name="rule">Rule values from the before state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Formatted change row.</returns>
    private static SecurityRuleChangeRowViewModel CreateRemovedRow(SecurityRuleValues rule, string providerName)
    {
        return new SecurityRuleChangeRowViewModel
        {
            Change = ActionIcons.Delete,
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = FormatNsgProtocol(rule.Protocol),
            SourceAddresses = FormatNsgAddresses(rule.SourceAddresses, "source_address_prefix", providerName),
            SourcePorts = FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = FormatNsgAddresses(rule.DestinationAddresses, "destination_address_prefix", providerName),
            DestinationPorts = FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = FormatDescriptionCell(rule.Description, providerName)
        };
    }

    /// <summary>
    /// Creates a formatted row for an unchanged rule.
    /// </summary>
    /// <param name="rule">Rule values.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Formatted change row.</returns>
    private static SecurityRuleChangeRowViewModel CreateUnchangedRow(SecurityRuleValues rule, string providerName)
    {
        return new SecurityRuleChangeRowViewModel
        {
            Change = ActionIcons.Unchanged,
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = FormatNsgProtocol(rule.Protocol),
            SourceAddresses = FormatNsgAddresses(rule.SourceAddresses, "source_address_prefix", providerName),
            SourcePorts = FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = FormatNsgAddresses(rule.DestinationAddresses, "destination_address_prefix", providerName),
            DestinationPorts = FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = FormatDescriptionCell(rule.Description, providerName)
        };
    }

    /// <summary>
    /// Formats a protocol value with NSG-specific icons (🔗 for TCP/UDP/ICMP, ✳️ for wildcard).
    /// All routed protocols use the chain icon (🔗) consistent with the security rule extractor.
    /// Uses a regular space between icon and label to preserve baseline output.
    /// </summary>
    /// <param name="protocol">Raw protocol value (e.g. "Tcp", "Udp", "*").</param>
    /// <returns>Icon-prefixed protocol string.</returns>
    private static string FormatNsgProtocol(string protocol) => protocol.ToUpperInvariant() switch
    {
        "TCP" => FormatCodeTable("🔗" + NonBreakingSpace + "TCP"),
        "UDP" => FormatCodeTable("🔗" + NonBreakingSpace + "UDP"),
        "ICMP" => FormatCodeTable("🔗" + NonBreakingSpace + "ICMP"),
        "*" => FormatCodeTable("✳️"),
        _ => protocol
    };

    /// <summary>
    /// comma-separated addresses, each is formatted individually using the value table formatter.
    /// </summary>
    /// <param name="addresses">Single address or comma-separated list from <see cref="ComposeValue"/>.</param>
    /// <param name="attributeName">Attribute name for the icon registry lookup (e.g. "source_address_prefix").</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Icon-prefixed address string or joined list.</returns>
    private static string FormatNsgAddresses(string addresses, string attributeName, string providerName)
    {
        if (!addresses.Contains(',', StringComparison.Ordinal))
        {
            return FormatAttributeValueTable(attributeName, addresses, providerName);
        }

        var parts = addresses.Split(',');
        return string.Join(", ", parts.Select(p => FormatAttributeValueTable(attributeName, p.Trim(), providerName)));
    }

    /// <summary>
    /// Builds a value using plural list precedence, falling back to a singular value or a wildcard.
    /// </summary>
    /// <param name="rule">Rule JSON object.</param>
    /// <param name="pluralProperty">Property name for list values.</param>
    /// <param name="singularProperty">Property name for singular value.</param>
    /// <returns>Normalized value string.</returns>
    private static string ComposeValue(JsonElement rule, string pluralProperty, string singularProperty)
    {
        if (rule.TryGetProperty(pluralProperty, out var plural) && plural.ValueKind == JsonValueKind.Array && plural.GetArrayLength() > 0)
        {
            var values = plural.EnumerateArray()
                .Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            if (values.Count > 0)
            {
                return string.Join(", ", values);
            }
        }

        if (rule.TryGetProperty(singularProperty, out var singular) && singular.ValueKind != JsonValueKind.Null && singular.ValueKind != JsonValueKind.Undefined)
        {
            var text = singular.ValueKind == JsonValueKind.String ? singular.GetString() : singular.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text!;
            }
        }

        return "*";
    }

    /// <summary>
    /// Retrieves a property value as a string representation.
    /// </summary>
    /// <param name="element">JSON object containing the property.</param>
    /// <param name="propertyName">Property name to read.</param>
    /// <returns>String representation or empty string when missing.</returns>
    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null || property.ValueKind == JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => property.ToString()
        };
    }

    /// <summary>
    /// Normalizes description values to a dash placeholder when empty.
    /// </summary>
    /// <param name="value">Description value.</param>
    /// <returns>Original description or "-" when empty.</returns>
    private static string NormalizeDescription(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    /// <summary>
    /// Formats description values while preserving the plain dash placeholder for empty descriptions.
    /// </summary>
    /// <param name="description">Raw normalized description value.</param>
    /// <param name="providerName">Provider name for semantic formatting.</param>
    /// <returns>Formatted description cell value.</returns>
    private static string FormatDescriptionCell(string description, string providerName)
    {
        return description == "-" ? "-" : FormatAttributeValueTable("description", description, providerName);
    }

    /// <summary>
    /// Compares two rules using their raw values.
    /// </summary>
    /// <param name="before">Rule values before the change.</param>
    /// <param name="after">Rule values after the change.</param>
    /// <returns>True when all values match; otherwise false.</returns>
    private static bool RulesEqual(SecurityRuleValues before, SecurityRuleValues after)
    {
        return string.Equals(before.Name, after.Name, StringComparison.Ordinal)
               && string.Equals(before.Priority, after.Priority, StringComparison.Ordinal)
               && string.Equals(before.Direction, after.Direction, StringComparison.Ordinal)
               && string.Equals(before.Access, after.Access, StringComparison.Ordinal)
               && string.Equals(before.Protocol, after.Protocol, StringComparison.Ordinal)
               && string.Equals(before.SourceAddresses, after.SourceAddresses, StringComparison.Ordinal)
               && string.Equals(before.SourcePorts, after.SourcePorts, StringComparison.Ordinal)
               && string.Equals(before.DestinationAddresses, after.DestinationAddresses, StringComparison.Ordinal)
               && string.Equals(before.DestinationPorts, after.DestinationPorts, StringComparison.Ordinal)
               && string.Equals(before.Description, after.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// Attempts to parse the priority string into an integer for ordering.
    /// </summary>
    /// <param name="priority">Priority text from the plan.</param>
    /// <returns>Numeric priority when parsed; otherwise null.</returns>
    private static int? TryParsePriority(string priority)
    {
        if (int.TryParse(priority, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    /// <summary>
    /// Represents raw security rule values used during diff computation.
    /// </summary>
    private sealed record SecurityRuleValues(
        string Name,
        int? PriorityNumber,
        string Priority,
        string Direction,
        string Access,
        string Protocol,
        string SourceAddresses,
        string SourcePorts,
        string DestinationAddresses,
        string DestinationPorts,
        string Description);
}
