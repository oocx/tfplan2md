using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Builds <see cref="FirewallNetworkRuleCollectionViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
internal static class FirewallNetworkRuleCollectionViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided firewall network rule collection change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="FirewallNetworkRuleCollectionViewModel"/>.</returns>
    public static FirewallNetworkRuleCollectionViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
    {
        var name = ExtractString(change.Change.After, "name") ?? ExtractString(change.Change.Before, "name");
        var priority = ExtractString(change.Change.After, "priority") ?? ExtractString(change.Change.Before, "priority");
        var action = ExtractString(change.Change.After, "action") ?? ExtractString(change.Change.Before, "action");

        var formattedPriority = !string.IsNullOrEmpty(priority) ? priority : null;
        var formattedAction = !string.IsNullOrEmpty(action)
            ? FormatAttributeValueTable("access", action, providerName)
            : null;

        var beforeRules = ExtractRules(change.Change.Before);
        var afterRules = ExtractRules(change.Change.After);

        var added = BuildAdded(afterRules, beforeRules, providerName);
        var removed = BuildRemoved(beforeRules, afterRules, providerName);
        var modified = BuildModified(beforeRules, afterRules, providerName, largeValueFormat);
        var unchanged = BuildUnchanged(beforeRules, afterRules, providerName);

        var changeRows = new List<FirewallRuleChangeRowViewModel>();
        changeRows.AddRange(added);
        changeRows.AddRange(modified);
        changeRows.AddRange(removed);
        changeRows.AddRange(unchanged);

        return new FirewallNetworkRuleCollectionViewModel
        {
            Name = name,
            Priority = formattedPriority,
            Action = formattedAction,
            RuleChanges = changeRows,
            AfterRules = FormatRuleRows(afterRules, providerName),
            BeforeRules = FormatRuleRows(beforeRules, providerName)
        };
    }

    /// <summary>
    /// Builds a changed-attributes summary using semantic rule changes for update actions.
    /// Related issue: docs/issues/049-firewall-summary-array-shift/analysis.md.
    /// </summary>
    /// <param name="model">Firewall rule collection view model containing rule changes.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Summary string or empty when not applicable.</returns>
    internal static string BuildChangedAttributesSummary(FirewallNetworkRuleCollectionViewModel model, string action)
    {
        if (!string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var changes = model.RuleChanges
            .Where(change => !string.Equals(change.Change, "⏺️", StringComparison.Ordinal))
            .ToList();

        if (changes.Count == 0)
        {
            return string.Empty;
        }

        var displayed = changes
            .Take(3)
            .Select(FormatSummaryEntry)
            .ToList();

        var remaining = changes.Count - displayed.Count;
        var nameList = string.Join(", ", displayed);

        if (remaining > 0)
        {
            nameList += $", +{remaining} more";
        }

        return $"{changes.Count}🔧{NonBreakingSpace}{nameList}";
    }

    /// <summary>
    /// Extracts a string property from the state object.
    /// Handles both string and number values.
    /// </summary>
    private static string? ExtractString(object? state, string propertyName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
    }

    /// <summary>
    /// Builds raw firewall rule values from the Terraform state object.
    /// </summary>
    private static IReadOnlyList<FirewallRuleValues> ExtractRules(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<FirewallRuleValues>();
        }

        if (!element.TryGetProperty("rule", out var rulesElement) || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<FirewallRuleValues>();
        }

        var rules = new List<FirewallRuleValues>();
        foreach (var ruleElement in rulesElement.EnumerateArray())
        {
            if (ruleElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(ruleElement, "name");
            var protocols = GetStringList(ruleElement, "protocols");
            var sourceAddresses = GetStringList(ruleElement, "source_addresses");
            var destinationAddresses = GetStringList(ruleElement, "destination_addresses");
            var destinationPorts = GetStringList(ruleElement, "destination_ports");
            var description = GetString(ruleElement, "description");

            rules.Add(new FirewallRuleValues(
                name,
                protocols,
                sourceAddresses,
                destinationAddresses,
                destinationPorts,
                description));
        }

        return rules;
    }

    /// <summary>
    /// Builds change rows for rules that only exist in the after state.
    /// </summary>
    private static List<FirewallRuleChangeRowViewModel> BuildAdded(
        IReadOnlyList<FirewallRuleValues> afterRules,
        IReadOnlyList<FirewallRuleValues> beforeRules,
        string providerName)
    {
        var beforeNames = new HashSet<string>(beforeRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        return afterRules
            .Where(rule => !beforeNames.Contains(rule.Name))
            .Select(rule => CreateAddedRow(rule, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that only exist in the before state.
    /// </summary>
    private static List<FirewallRuleChangeRowViewModel> BuildRemoved(
        IReadOnlyList<FirewallRuleValues> beforeRules,
        IReadOnlyList<FirewallRuleValues> afterRules,
        string providerName)
    {
        var afterNames = new HashSet<string>(afterRules.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        return beforeRules
            .Where(rule => !afterNames.Contains(rule.Name))
            .Select(rule => CreateRemovedRow(rule, providerName))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that exist in both states but differ.
    /// </summary>
    private static List<FirewallRuleChangeRowViewModel> BuildModified(
        IReadOnlyList<FirewallRuleValues> beforeRules,
        IReadOnlyList<FirewallRuleValues> afterRules,
        string providerName,
        LargeValueFormat largeValueFormat)
    {
        var beforeLookup = beforeRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        return afterRules
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && !RulesEqual(before!, after))
            .Select(after => CreateDiffRow(beforeLookup[after.Name], after, providerName, largeValueFormat))
            .ToList();
    }

    /// <summary>
    /// Builds change rows for rules that remain unchanged between states.
    /// </summary>
    private static List<FirewallRuleChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<FirewallRuleValues> beforeRules,
        IReadOnlyList<FirewallRuleValues> afterRules,
        string providerName)
    {
        var beforeLookup = beforeRules.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        return afterRules
            .Where(after => beforeLookup.TryGetValue(after.Name, out var before) && RulesEqual(before!, after))
            .Select(after => CreateUnchangedRow(after, providerName))
            .ToList();
    }

    /// <summary>
    /// Formats rule values for create/delete tables.
    /// </summary>
    private static List<FirewallRuleRowViewModel> FormatRuleRows(
        IReadOnlyList<FirewallRuleValues> rules,
        string providerName)
    {
        return rules
            .Select(rule => new FirewallRuleRowViewModel
            {
                Name = FormatAttributeValueTable("name", rule.Name, providerName),
                Protocols = FormatList("protocol", rule.Protocols, providerName),
                SourceAddresses = FormatList("source_addresses", rule.SourceAddresses, providerName),
                DestinationAddresses = FormatList("destination_addresses", rule.DestinationAddresses, providerName),
                DestinationPorts = FormatList("destination_ports", rule.DestinationPorts, providerName),
                Description = $"`{EscapeMarkdown(rule.Description)}`"
            })
            .ToList();
    }

    /// <summary>
    /// Creates a formatted row for an added rule.
    /// </summary>
    private static FirewallRuleChangeRowViewModel CreateAddedRow(FirewallRuleValues rule, string providerName)
    {
        return new FirewallRuleChangeRowViewModel
        {
            Change = "➕",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList("protocol", rule.Protocols, providerName),
            SourceAddresses = FormatList("source_addresses", rule.SourceAddresses, providerName),
            DestinationAddresses = FormatList("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = FormatList("destination_ports", rule.DestinationPorts, providerName),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted row for a removed rule.
    /// </summary>
    private static FirewallRuleChangeRowViewModel CreateRemovedRow(FirewallRuleValues rule, string providerName)
    {
        return new FirewallRuleChangeRowViewModel
        {
            Change = "❌",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList("protocol", rule.Protocols, providerName),
            SourceAddresses = FormatList("source_addresses", rule.SourceAddresses, providerName),
            DestinationAddresses = FormatList("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = FormatList("destination_ports", rule.DestinationPorts, providerName),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted row for an unchanged rule.
    /// </summary>
    private static FirewallRuleChangeRowViewModel CreateUnchangedRow(FirewallRuleValues rule, string providerName)
    {
        return new FirewallRuleChangeRowViewModel
        {
            Change = "⏺️",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList("protocol", rule.Protocols, providerName),
            SourceAddresses = FormatList("source_addresses", rule.SourceAddresses, providerName),
            DestinationAddresses = FormatList("destination_addresses", rule.DestinationAddresses, providerName),
            DestinationPorts = FormatList("destination_ports", rule.DestinationPorts, providerName),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted diff row for a modified rule.
    /// </summary>
    private static FirewallRuleChangeRowViewModel CreateDiffRow(
        FirewallRuleValues before,
        FirewallRuleValues after,
        string providerName,
        LargeValueFormat largeValueFormat)
    {
        var format = largeValueFormat.ToString();

        return new FirewallRuleChangeRowViewModel
        {
            Change = "🔄",
            Name = FormatAttributeValueTable("name", after.Name, providerName),
            Protocols = FormatListDiff("protocol", before.Protocols, after.Protocols, providerName, format),
            SourceAddresses = FormatListDiff("source_addresses", before.SourceAddresses, after.SourceAddresses, providerName, format),
            DestinationAddresses = FormatListDiff("destination_addresses", before.DestinationAddresses, after.DestinationAddresses, providerName, format),
            DestinationPorts = FormatListDiff("destination_ports", before.DestinationPorts, after.DestinationPorts, providerName, format),
            Description = FormatDiff(before.Description, after.Description, format)
        };
    }

    /// <summary>
    /// Formats a list of values with semantic icons.
    /// </summary>
    private static string FormatList(string attributeName, IReadOnlyList<string> values, string providerName)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        var formatted = values
            .Select(v => FormatAttributeValueTable(attributeName, v, providerName))
            .ToList();

        return string.Join(", ", formatted);
    }

    /// <summary>
    /// Formats a diff between two lists.
    /// </summary>
    private static string FormatListDiff(string attributeName, IReadOnlyList<string> before, IReadOnlyList<string> after, string providerName, string format)
    {
        var beforeFormatted = before
            .Select(v => FormatAttributeValuePlain(attributeName, v, providerName))
            .ToList();

        var afterFormatted = after
            .Select(v => FormatAttributeValuePlain(attributeName, v, providerName))
            .ToList();

        var beforeStr = string.Join(", ", beforeFormatted);
        var afterStr = string.Join(", ", afterFormatted);

        if (string.Equals(beforeStr, afterStr, StringComparison.Ordinal))
        {
            return afterStr;
        }

        return FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Formats a summary entry for a single firewall rule change.
    /// Related issue: docs/issues/049-firewall-summary-array-shift/analysis.md.
    /// </summary>
    /// <param name="change">The rule change row view model.</param>
    /// <returns>Formatted summary entry string.</returns>
    private static string FormatSummaryEntry(FirewallRuleChangeRowViewModel change)
    {
        var ruleName = TrimMarkdownCode(change.Name);
        return $"{change.Change}{NonBreakingSpace}{FormatCodeSummary(ruleName)}";
    }

    /// <summary>
    /// Removes surrounding markdown code ticks for summary-friendly HTML rendering.
    /// Related issue: docs/issues/049-firewall-summary-array-shift/analysis.md.
    /// </summary>
    /// <param name="value">The formatted markdown code value.</param>
    /// <returns>Value without surrounding backticks.</returns>
    private static string TrimMarkdownCode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length >= 2 && value.StartsWith('`') && value.EndsWith('`')
            ? value[1..^1]
            : value;
    }

    /// <summary>
    /// Retrieves a string property value.
    /// </summary>
    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Retrieves a string list property value.
    /// </summary>
    private static IReadOnlyList<string> GetStringList(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    list.Add(value);
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Compares two rules using their raw values.
    /// </summary>
    private static bool RulesEqual(FirewallRuleValues before, FirewallRuleValues after)
    {
        return string.Equals(before.Name, after.Name, StringComparison.Ordinal)
               && string.Equals(before.Description, after.Description, StringComparison.Ordinal)
               && ListsEqual(before.Protocols, after.Protocols)
               && ListsEqual(before.SourceAddresses, after.SourceAddresses)
               && ListsEqual(before.DestinationAddresses, after.DestinationAddresses)
               && ListsEqual(before.DestinationPorts, after.DestinationPorts);
    }

    /// <summary>
    /// Compares two string lists for equality.
    /// </summary>
    private static bool ListsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Represents raw firewall rule values used during diff computation.
    /// </summary>
    private sealed record FirewallRuleValues(
        string Name,
        IReadOnlyList<string> Protocols,
        IReadOnlyList<string> SourceAddresses,
        IReadOnlyList<string> DestinationAddresses,
        IReadOnlyList<string> DestinationPorts,
        string Description);
}
