using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Builds <see cref="FirewallApplicationRuleCollectionViewModel"/> instances from Terraform plan data.
/// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Factory naturally couples with view models, parsing types, and formatting helpers")]
internal static class FirewallApplicationRuleCollectionViewModelFactory
{
    /// <summary>
    /// Creates a view model for the provided firewall application rule collection change.
    /// </summary>
    /// <param name="change">The resource change containing before/after state.</param>
    /// <param name="providerName">The provider name for semantic formatting.</param>
    /// <param name="largeValueFormat">Preferred large value format for diff rendering.</param>
    /// <returns>Populated <see cref="FirewallApplicationRuleCollectionViewModel"/>.</returns>
    public static FirewallApplicationRuleCollectionViewModel Build(ResourceChange change, string providerName, LargeValueFormat largeValueFormat)
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

        var changeRows = new List<FirewallApplicationRuleChangeRowViewModel>();
        changeRows.AddRange(added);
        changeRows.AddRange(modified);
        changeRows.AddRange(removed);
        changeRows.AddRange(unchanged);

        return new FirewallApplicationRuleCollectionViewModel
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
    /// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
    /// </summary>
    /// <param name="model">Firewall application rule collection view model containing rule changes.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Summary string or empty when not applicable.</returns>
    internal static string BuildChangedAttributesSummary(FirewallApplicationRuleCollectionViewModel model, string action)
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
    /// Builds raw firewall application rule values from the Terraform state object.
    /// </summary>
    private static IReadOnlyList<ApplicationRuleValues> ExtractRules(object? state)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<ApplicationRuleValues>();
        }

        if (!element.TryGetProperty("rule", out var rulesElement) || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ApplicationRuleValues>();
        }

        var rules = new List<ApplicationRuleValues>();
        foreach (var ruleElement in rulesElement.EnumerateArray())
        {
            if (ruleElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = GetString(ruleElement, "name");
            var protocols = GetProtocolList(ruleElement, "protocols");
            var sourceAddresses = GetStringList(ruleElement, "source_addresses");
            var sourceIpGroups = GetStringList(ruleElement, "source_ip_groups");
            var targetFqdns = GetStringList(ruleElement, "target_fqdns");
            var fqdnTags = GetStringList(ruleElement, "fqdn_tags");
            var description = GetString(ruleElement, "description");

            rules.Add(new ApplicationRuleValues(
                name,
                protocols,
                sourceAddresses,
                sourceIpGroups,
                targetFqdns,
                fqdnTags,
                description));
        }

        return rules;
    }

    /// <summary>
    /// Builds change rows for rules that only exist in the after state.
    /// </summary>
    private static List<FirewallApplicationRuleChangeRowViewModel> BuildAdded(
        IReadOnlyList<ApplicationRuleValues> afterRules,
        IReadOnlyList<ApplicationRuleValues> beforeRules,
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
    private static List<FirewallApplicationRuleChangeRowViewModel> BuildRemoved(
        IReadOnlyList<ApplicationRuleValues> beforeRules,
        IReadOnlyList<ApplicationRuleValues> afterRules,
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
    private static List<FirewallApplicationRuleChangeRowViewModel> BuildModified(
        IReadOnlyList<ApplicationRuleValues> beforeRules,
        IReadOnlyList<ApplicationRuleValues> afterRules,
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
    private static List<FirewallApplicationRuleChangeRowViewModel> BuildUnchanged(
        IReadOnlyList<ApplicationRuleValues> beforeRules,
        IReadOnlyList<ApplicationRuleValues> afterRules,
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
    private static List<FirewallApplicationRuleRowViewModel> FormatRuleRows(
        IReadOnlyList<ApplicationRuleValues> rules,
        string providerName)
    {
        return rules
            .Select(rule => new FirewallApplicationRuleRowViewModel
            {
                Name = FormatAttributeValueTable("name", rule.Name, providerName),
                Protocols = FormatList(rule.Protocols),
                SourceAddresses = FormatList(rule.SourceAddresses),
                SourceIpGroups = FormatList(rule.SourceIpGroups),
                TargetFqdns = FormatList(rule.TargetFqdns),
                FqdnTags = FormatList(rule.FqdnTags),
                Description = $"`{EscapeMarkdown(rule.Description)}`"
            })
            .ToList();
    }

    /// <summary>
    /// Creates a formatted row for an added rule.
    /// </summary>
    private static FirewallApplicationRuleChangeRowViewModel CreateAddedRow(ApplicationRuleValues rule, string providerName)
    {
        return new FirewallApplicationRuleChangeRowViewModel
        {
            Change = "➕",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList(rule.Protocols),
            SourceAddresses = FormatList(rule.SourceAddresses),
            SourceIpGroups = FormatList(rule.SourceIpGroups),
            TargetFqdns = FormatList(rule.TargetFqdns),
            FqdnTags = FormatList(rule.FqdnTags),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted row for a removed rule.
    /// </summary>
    private static FirewallApplicationRuleChangeRowViewModel CreateRemovedRow(ApplicationRuleValues rule, string providerName)
    {
        return new FirewallApplicationRuleChangeRowViewModel
        {
            Change = "❌",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList(rule.Protocols),
            SourceAddresses = FormatList(rule.SourceAddresses),
            SourceIpGroups = FormatList(rule.SourceIpGroups),
            TargetFqdns = FormatList(rule.TargetFqdns),
            FqdnTags = FormatList(rule.FqdnTags),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted row for an unchanged rule.
    /// </summary>
    private static FirewallApplicationRuleChangeRowViewModel CreateUnchangedRow(ApplicationRuleValues rule, string providerName)
    {
        return new FirewallApplicationRuleChangeRowViewModel
        {
            Change = "⏺️",
            Name = FormatAttributeValueTable("name", rule.Name, providerName),
            Protocols = FormatList(rule.Protocols),
            SourceAddresses = FormatList(rule.SourceAddresses),
            SourceIpGroups = FormatList(rule.SourceIpGroups),
            TargetFqdns = FormatList(rule.TargetFqdns),
            FqdnTags = FormatList(rule.FqdnTags),
            Description = $"`{EscapeMarkdown(rule.Description)}`"
        };
    }

    /// <summary>
    /// Creates a formatted diff row for a modified rule.
    /// </summary>
    private static FirewallApplicationRuleChangeRowViewModel CreateDiffRow(
        ApplicationRuleValues before,
        ApplicationRuleValues after,
        string providerName,
        LargeValueFormat largeValueFormat)
    {
        var format = largeValueFormat.ToString();

        return new FirewallApplicationRuleChangeRowViewModel
        {
            Change = "🔄",
            Name = FormatAttributeValueTable("name", after.Name, providerName),
            Protocols = FormatListDiff(before.Protocols, after.Protocols, format),
            SourceAddresses = FormatListDiff(before.SourceAddresses, after.SourceAddresses, format),
            SourceIpGroups = FormatListDiff(before.SourceIpGroups, after.SourceIpGroups, format),
            TargetFqdns = FormatListDiff(before.TargetFqdns, after.TargetFqdns, format),
            FqdnTags = FormatListDiff(before.FqdnTags, after.FqdnTags, format),
            Description = FormatDiff(before.Description, after.Description, format)
        };
    }

    /// <summary>
    /// Formats a list of values with truncation for long lists.
    /// </summary>
    private static string FormatList(IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        const int maxItems = 5;
        if (values.Count > maxItems)
        {
            var displayItems = values.Take(3).Select(EscapeMarkdown);
            var remaining = values.Count - 3;
            return $"`{string.Join(", ", displayItems)}, ... +{remaining} more`";
        }

        return $"`{string.Join(", ", values.Select(EscapeMarkdown))}`";
    }

    /// <summary>
    /// Formats a diff between two lists.
    /// </summary>
    private static string FormatListDiff(IReadOnlyList<string> before, IReadOnlyList<string> after, string format)
    {
        var beforeStr = string.Join(", ", before);
        var afterStr = string.Join(", ", after);

        if (string.Equals(beforeStr, afterStr, StringComparison.Ordinal))
        {
            return $"`{EscapeMarkdown(afterStr)}`";
        }

        return FormatDiff(beforeStr, afterStr, format);
    }

    /// <summary>
    /// Formats a summary entry for a single firewall application rule change.
    /// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
    /// </summary>
    /// <param name="change">The rule change row view model.</param>
    /// <returns>Formatted summary entry string.</returns>
    private static string FormatSummaryEntry(FirewallApplicationRuleChangeRowViewModel change)
    {
        var ruleName = TrimMarkdownCode(change.Name);
        return $"{change.Change}{NonBreakingSpace}{FormatCodeSummary(ruleName)}";
    }

    /// <summary>
    /// Removes surrounding markdown code ticks for summary-friendly HTML rendering.
    /// Related feature: docs/features/060-azurerm-firewall-application-rule-template/specification.md.
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
    /// Handles both string and number values.
    /// </summary>
    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            _ => string.Empty
        };
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
    /// Retrieves protocol list from the state, handling the object format {type: "Https", port: 443}.
    /// </summary>
    private static IReadOnlyList<string> GetProtocolList(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                // Handle object format: {"type": "Https", "port": 443}
                var type = GetString(item, "type");
                var port = GetString(item, "port");

                if (!string.IsNullOrEmpty(type))
                {
                    var protocol = !string.IsNullOrEmpty(port) ? $"{type}:{port}" : type;
                    list.Add(protocol);
                }
            }
            else if (item.ValueKind == JsonValueKind.String)
            {
                // Handle string format (legacy or alternative)
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
    private static bool RulesEqual(ApplicationRuleValues before, ApplicationRuleValues after)
    {
        return string.Equals(before.Name, after.Name, StringComparison.Ordinal)
               && string.Equals(before.Description, after.Description, StringComparison.Ordinal)
               && ListsEqual(before.Protocols, after.Protocols)
               && ListsEqual(before.SourceAddresses, after.SourceAddresses)
               && ListsEqual(before.SourceIpGroups, after.SourceIpGroups)
               && ListsEqual(before.TargetFqdns, after.TargetFqdns)
               && ListsEqual(before.FqdnTags, after.FqdnTags);
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
    /// Represents raw firewall application rule values used during diff computation.
    /// </summary>
    private sealed record ApplicationRuleValues(
        string Name,
        IReadOnlyList<string> Protocols,
        IReadOnlyList<string> SourceAddresses,
        IReadOnlyList<string> SourceIpGroups,
        IReadOnlyList<string> TargetFqdns,
        IReadOnlyList<string> FqdnTags,
        string Description);
}
