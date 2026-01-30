using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Reads SARIF runs to extract tool metadata and findings.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class SarifRunReader
{
    /// <summary>
    /// Parses a SARIF run entry and populates tool metadata and findings.
    /// </summary>
    /// <param name="run">The SARIF run element.</param>
    /// <param name="tools">The tool metadata list to populate.</param>
    /// <param name="findings">The findings list to populate.</param>
    internal static void ParseRun(JsonElement run, List<CodeAnalysisTool> tools, List<CodeAnalysisFinding> findings)
    {
        if (!TryGetDriver(run, out var driver))
        {
            return;
        }

        var toolName = SarifJsonReader.GetString(driver, "name");
        var toolVersion = SarifJsonReader.GetString(driver, "semanticVersion") ?? SarifJsonReader.GetString(driver, "version");

        if (!string.IsNullOrWhiteSpace(toolName))
        {
            tools.Add(new CodeAnalysisTool { Name = toolName, Version = toolVersion });
        }

        var rules = SarifRuleMetadata.ParseRules(driver);
        var helpByRuleId = SarifRuleMetadata.BuildHelpIndex(rules);

        if (!run.TryGetProperty("results", out var resultsElement) || resultsElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var result in resultsElement.EnumerateArray())
        {
            findings.Add(SarifResultReader.ParseFinding(result, toolName, rules, helpByRuleId));
        }
    }

    /// <summary>
    /// Attempts to get the SARIF tool driver element from a run.
    /// </summary>
    /// <param name="run">The SARIF run element.</param>
    /// <param name="driver">The extracted tool driver element.</param>
    /// <returns><c>true</c> when the driver element is available; otherwise <c>false</c>.</returns>
    private static bool TryGetDriver(JsonElement run, out JsonElement driver)
    {
        if (run.TryGetProperty("tool", out var toolElement) && toolElement.TryGetProperty("driver", out var driverElement))
        {
            driver = driverElement;
            return true;
        }

        driver = default;
        return false;
    }
}
