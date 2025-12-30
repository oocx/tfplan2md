using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds <see cref="NetworkSecurityGroupViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md
/// </summary>
internal static class NetworkSecurityGroupViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided network security group change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="NetworkSecurityGroupViewModel"/>.</returns>
    public static NetworkSecurityGroupViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
    {
        var name = ExtractName(change.Change.After) ?? ExtractName(change.Change.Before);

        var beforeRules = ExtractRules(change.Change.Before);
        var afterRules = ExtractRules(change.Change.After);

        var added = BuildAdded(afterRules, beforeRules, providerName);
        var removed = BuildRemoved(beforeRules, afterRules, providerName);
        var modified = BuildModified(beforeRules, afterRules, providerName, largeValueFormat);
        var unchanged = BuildUnchanged(beforeRules, afterRules, providerName);

        var changeRows = new List<SecurityRuleChangeRowViewModel>();
        changeRows.AddRange(added);
        changeRows.AddRange(modified);
        changeRows.AddRange(removed);
        changeRows.AddRange(unchanged);

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
    /// Builds change rows for rules that only exist in the after state.
    /// </summary>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="beforeRules">Rules from the before state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered added rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildAdded(
        IReadOnlyList<SecurityRuleValues> afterRules,
        IReadOnlyList<SecurityRuleValues> beforeRules,
        string providerName)
    {
        var beforeNames = new HashSet<string>(beforeRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        return afterRules
            .Where(rule => !beforeNames.Contains(rule.Name))
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(rule => CreateAddedRow(rule, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that only exist in the before state.
    /// </summary>
    /// <param name="beforeRules">Rules from the before state.</param>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered removed rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildRemoved(
        IReadOnlyList<SecurityRuleValues> beforeRules,
        IReadOnlyList<SecurityRuleValues> afterRules,
        string providerName)
    {
        var afterNames = new HashSet<string>(afterRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        return beforeRules
            .Where(rule => !afterNames.Contains(rule.Name))
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(rule => CreateRemovedRow(rule, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that exist in both states but differ.
    /// </summary>
    /// <param name="beforeRules">Rules from the before state.</param>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <returns>Ordered modified rule rows.</returns>
    private static IReadOnlyList<SecurityRuleChangeRowViewModel> BuildModified(
        IReadOnlyList<SecurityRuleValues> beforeRules,
        IReadOnlyList<SecurityRuleValues> afterRules,
        string providerName,
        LargeValueFormat largeValueFormat)
    {
        var beforeLookup = beforeRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        return afterRules
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && !RulesEqual(before!, after))
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(after => CreateDiffRow(beforeLookup[after.Name], after, providerName, largeValueFormat))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that remain unchanged between states.
    /// </summary>
    /// <param name="beforeRules">Rules from the before state.</param>
    /// <param name="afterRules">Rules from the after state.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <returns>Ordered unchanged rule rows.</returns>
    private static List<SecurityRuleChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<SecurityRuleValues> beforeRules,
        IReadOnlyList<SecurityRuleValues> afterRules,
        string providerName)
    {
        var beforeLookup = beforeRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        return afterRules
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && RulesEqual(before!, after))
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(after => CreateUnchangedRow(after, providerName))
            .ToList();
    }

    /// <summary>
    /// Formats rule values for create/delete tables.
    /// </summary>
    /// <param name="rules">Raw rule values.</param>
    /// <param name="providerName">Provider name for semantic formatting.</param>
    /// <returns>Formatted rule rows.</returns>
    private static IReadOnlyList<SecurityRuleRowViewModel> FormatRuleRows(
        IReadOnlyList<SecurityRuleValues> rules,
        string providerName)
    {
        return rules
            .OrderBy(rule => rule.PriorityNumber ?? int.MaxValue)
            .ThenBy(rule => rule.Name, StringComparer.Ordinal)
            .Select(rule => new SecurityRuleRowViewModel
            {
                Name = ScribanHelpers.FormatAttributeValueTable("name", rule.Name, providerName),
                Priority = ScribanHelpers.FormatAttributeValueTable("priority", rule.Priority, providerName),
                Direction = ScribanHelpers.FormatAttributeValueTable("direction", rule.Direction, providerName),
                Access = ScribanHelpers.FormatAttributeValueTable("access", rule.Access, providerName),
                Protocol = ScribanHelpers.FormatAttributeValueTable("protocol", rule.Protocol, providerName),
                SourceAddresses = ScribanHelpers.FormatAttributeValueTable("source_addresses", rule.SourceAddresses, providerName),
                SourcePorts = ScribanHelpers.FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
                DestinationAddresses = ScribanHelpers.FormatAttributeValueTable("destination_addresses", rule.DestinationAddresses, providerName),
                DestinationPorts = ScribanHelpers.FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
                Description = ScribanHelpers.FormatAttributeValueTable("description", rule.Description, providerName)
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
            Change = "➕",
            Name = ScribanHelpers.FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = ScribanHelpers.FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = ScribanHelpers.FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = ScribanHelpers.FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = ScribanHelpers.FormatAttributeValueTable("protocol", rule.Protocol, providerName),
            SourceAddresses = ScribanHelpers.FormatAttributeValueTable("source_addresses", rule.SourceAddresses, providerName),
            SourcePorts = ScribanHelpers.FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = ScribanHelpers.FormatAttributeValueTable("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = ScribanHelpers.FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = ScribanHelpers.FormatAttributeValueTable("description", rule.Description, providerName)
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
            Change = "❌",
            Name = ScribanHelpers.FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = ScribanHelpers.FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = ScribanHelpers.FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = ScribanHelpers.FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = ScribanHelpers.FormatAttributeValueTable("protocol", rule.Protocol, providerName),
            SourceAddresses = ScribanHelpers.FormatAttributeValueTable("source_addresses", rule.SourceAddresses, providerName),
            SourcePorts = ScribanHelpers.FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = ScribanHelpers.FormatAttributeValueTable("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = ScribanHelpers.FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = ScribanHelpers.FormatAttributeValueTable("description", rule.Description, providerName)
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
            Change = "⏺️",
            Name = ScribanHelpers.FormatAttributeValueTable("name", rule.Name, providerName),
            Priority = ScribanHelpers.FormatAttributeValueTable("priority", rule.Priority, providerName),
            Direction = ScribanHelpers.FormatAttributeValueTable("direction", rule.Direction, providerName),
            Access = ScribanHelpers.FormatAttributeValueTable("access", rule.Access, providerName),
            Protocol = ScribanHelpers.FormatAttributeValueTable("protocol", rule.Protocol, providerName),
            SourceAddresses = ScribanHelpers.FormatAttributeValueTable("source_addresses", rule.SourceAddresses, providerName),
            SourcePorts = ScribanHelpers.FormatAttributeValueTable("source_ports", rule.SourcePorts, providerName),
            DestinationAddresses = ScribanHelpers.FormatAttributeValueTable("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = ScribanHelpers.FormatAttributeValueTable("destination_ports", rule.DestinationPorts, providerName),
            Description = ScribanHelpers.FormatAttributeValueTable("description", rule.Description, providerName)
        };
    }

    /// <summary>
    /// Creates a formatted diff row for a modified rule.
    /// </summary>
    /// <param name="before">Rule values before the change.</param>
    /// <param name="after">Rule values after the change.</param>
    /// <param name="providerName">Provider name for formatting.</param>
    /// <param name="largeValueFormat">Preferred diff format.</param>
    /// <returns>Formatted diff row.</returns>
    private static SecurityRuleChangeRowViewModel CreateDiffRow(
        SecurityRuleValues before,
        SecurityRuleValues after,
        string providerName,
        LargeValueFormat largeValueFormat)
    {
        var format = largeValueFormat.ToString();

        return new SecurityRuleChangeRowViewModel
        {
            Change = "🔄",
            Name = ScribanHelpers.FormatAttributeValueTable("name", after.Name, providerName),
            Priority = ScribanHelpers.FormatDiff(before.Priority, after.Priority, format),
            Direction = ScribanHelpers.FormatDiff(before.Direction, after.Direction, format),
            Access = ScribanHelpers.FormatDiff(before.Access, after.Access, format),
            Protocol = ScribanHelpers.FormatDiff(before.Protocol, after.Protocol, format),
            SourceAddresses = ScribanHelpers.FormatDiff(before.SourceAddresses, after.SourceAddresses, format),
            SourcePorts = ScribanHelpers.FormatDiff(before.SourcePorts, after.SourcePorts, format),
            DestinationAddresses = ScribanHelpers.FormatDiff(before.DestinationAddresses, after.DestinationAddresses, format),
            DestinationPorts = ScribanHelpers.FormatDiff(before.DestinationPorts, after.DestinationPorts, format),
            Description = ScribanHelpers.FormatDiff(before.Description, after.Description, format)
        };
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
