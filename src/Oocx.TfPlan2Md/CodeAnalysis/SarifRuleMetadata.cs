using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Represents SARIF rule metadata needed for findings mapping.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
/// <param name="Id">The rule identifier.</param>
/// <param name="HelpUri">The help URI for the rule.</param>
internal sealed record SarifRuleMetadata(string? Id, string? HelpUri)
{
    /// <summary>
    /// Parses SARIF rules from the tool driver element.
    /// </summary>
    /// <param name="driver">The SARIF tool driver element.</param>
    /// <returns>The list of rule metadata.</returns>
    internal static List<SarifRuleMetadata> ParseRules(JsonElement driver)
    {
        if (!driver.TryGetProperty("rules", out var rulesElement) || rulesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rules = new List<SarifRuleMetadata>();
        foreach (var rule in rulesElement.EnumerateArray())
        {
            rules.Add(new SarifRuleMetadata(SarifJsonReader.GetString(rule, "id"), SarifJsonReader.GetString(rule, "helpUri")));
        }

        return rules;
    }

    /// <summary>
    /// Builds a lookup of rule IDs to help URIs.
    /// </summary>
    /// <param name="rules">The rule metadata list.</param>
    /// <returns>A dictionary of rule IDs to help URIs.</returns>
    internal static Dictionary<string, string?> BuildHelpIndex(List<SarifRuleMetadata> rules)
    {
        var lookup = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                continue;
            }

            lookup.TryAdd(rule.Id, rule.HelpUri);
        }

        return lookup;
    }
}
