using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Reads SARIF result entries into code analysis findings.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class SarifResultReader
{
    /// <summary>
    /// Parses a SARIF result into a code analysis finding.
    /// </summary>
    /// <param name="result">The SARIF result element.</param>
    /// <param name="toolName">The tool name for the current run.</param>
    /// <param name="rules">The rule metadata list for the run.</param>
    /// <param name="helpByRuleId">Lookup of rule IDs to help URIs.</param>
    /// <returns>The parsed code analysis finding.</returns>
    internal static CodeAnalysisFinding ParseFinding(
        JsonElement result,
        string? toolName,
        List<SarifRuleMetadata> rules,
        Dictionary<string, string?> helpByRuleId)
    {
        var ruleIndex = SarifJsonReader.GetOptionalInt(result, "ruleIndex");
        var ruleId = SarifJsonReader.GetString(result, "ruleId") ?? GetRuleIdFromIndex(rules, ruleIndex);
        var helpUri = SarifJsonReader.GetString(result, "helpUri") ?? GetHelpUriFromRules(ruleId, ruleIndex, rules, helpByRuleId);

        return new CodeAnalysisFinding
        {
            ToolName = toolName,
            RuleId = ruleId,
            Message = GetMessageText(result),
            Level = SarifJsonReader.GetString(result, "level"),
            SecuritySeverity = SarifJsonReader.GetOptionalDoubleFromProperties(result, "security-severity"),
            Rank = SarifJsonReader.GetOptionalDouble(result, "rank") ?? SarifJsonReader.GetOptionalDoubleFromProperties(result, "rank"),
            HelpUri = helpUri,
            Locations = ParseLocations(result)
        };
    }

    /// <summary>
    /// Extracts logical locations from a SARIF result.
    /// </summary>
    /// <param name="result">The SARIF result element.</param>
    /// <returns>The list of logical locations.</returns>
    private static List<CodeAnalysisLocation> ParseLocations(JsonElement result)
    {
        if (!result.TryGetProperty("locations", out var locationsElement) || locationsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var locations = new List<CodeAnalysisLocation>();
        foreach (var location in locationsElement.EnumerateArray())
        {
            if (!location.TryGetProperty("logicalLocations", out var logicalLocations) || logicalLocations.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var logicalLocation in logicalLocations.EnumerateArray())
            {
                var fullyQualifiedName = SarifJsonReader.GetString(logicalLocation, "fullyQualifiedName");
                if (string.IsNullOrWhiteSpace(fullyQualifiedName))
                {
                    continue;
                }

                locations.Add(new CodeAnalysisLocation { FullyQualifiedName = fullyQualifiedName });
            }
        }

        return locations;
    }

    /// <summary>
    /// Reads the SARIF message text, falling back to markdown when needed.
    /// </summary>
    /// <param name="result">The SARIF result element.</param>
    /// <returns>The message text, or an empty string when unavailable.</returns>
    private static string GetMessageText(JsonElement result)
    {
        if (!result.TryGetProperty("message", out var messageElement) || messageElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var text = SarifJsonReader.GetString(messageElement, "text") ?? SarifJsonReader.GetString(messageElement, "markdown");
        return text ?? string.Empty;
    }

    /// <summary>
    /// Gets a rule ID from a rule index if available.
    /// </summary>
    /// <param name="rules">The rule metadata list.</param>
    /// <param name="ruleIndex">The rule index.</param>
    /// <returns>The rule ID, or <c>null</c> when unavailable.</returns>
    private static string? GetRuleIdFromIndex(List<SarifRuleMetadata> rules, int? ruleIndex)
    {
        if (ruleIndex is null || ruleIndex < 0 || ruleIndex >= rules.Count)
        {
            return null;
        }

        return rules[ruleIndex.Value].Id;
    }

    /// <summary>
    /// Gets the help URI for a rule using rule metadata lookups.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="ruleIndex">The rule index.</param>
    /// <param name="rules">The rule metadata list.</param>
    /// <param name="helpByRuleId">Lookup of rule IDs to help URIs.</param>
    /// <returns>The help URI, or <c>null</c> when unavailable.</returns>
    private static string? GetHelpUriFromRules(
        string? ruleId,
        int? ruleIndex,
        List<SarifRuleMetadata> rules,
        Dictionary<string, string?> helpByRuleId)
    {
        if (!string.IsNullOrWhiteSpace(ruleId) && helpByRuleId.TryGetValue(ruleId, out var helpUri))
        {
            return helpUri;
        }

        if (ruleIndex is null || ruleIndex < 0 || ruleIndex >= rules.Count)
        {
            return null;
        }

        return rules[ruleIndex.Value].HelpUri;
    }
}
