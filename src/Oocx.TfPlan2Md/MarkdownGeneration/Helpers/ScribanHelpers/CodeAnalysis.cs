using System;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Code analysis helpers for Scriban templates.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Gets the severity indicator emoji for an attribute based on code analysis findings.
    /// Returns the highest severity indicator if multiple findings exist for the same attribute.
    /// Related feature: docs/features/056-static-analysis-integration/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to look up.</param>
    /// <param name="findings">The code analysis findings array from the template context.</param>
    /// <returns>The severity emoji (🚨, ⚠️, ℹ️) with a space prefix if a finding exists, or empty string otherwise.</returns>
    public static string GetAttributeFindingIndicator(string? attributeName, ScriptArray? findings)
    {
        if (string.IsNullOrEmpty(attributeName) || findings is null || findings.Count == 0)
        {
            return string.Empty;
        }

        var icon = FindHighestSeverityIcon(attributeName, findings);
        return icon != null ? $" {icon}" : string.Empty;
    }

    /// <summary>
    /// Finds the highest severity icon for the given attribute name.
    /// </summary>
    private static string? FindHighestSeverityIcon(string attributeName, ScriptArray findings)
    {
        var highestRank = -1;
        string? highestIcon = null;

        foreach (var item in findings)
        {
            if (item is not ScriptObject finding)
            {
                continue;
            }

            var attrPath = GetFindingAttributePath(finding);
            if (!AttributeMatches(attributeName, attrPath))
            {
                continue;
            }

            var rank = GetFindingSeverityRank(finding);
            if (rank > highestRank)
            {
                highestRank = rank;
                highestIcon = GetFindingSeverityIcon(finding);
            }
        }

        return highestIcon;
    }

    /// <summary>
    /// Gets the attribute_path property from a finding.
    /// </summary>
    private static string? GetFindingAttributePath(ScriptObject finding) =>
        finding.TryGetValue("attribute_path", out var pathValue) ? pathValue?.ToString() : null;

    /// <summary>
    /// Gets the severity_rank property from a finding.
    /// </summary>
    private static int GetFindingSeverityRank(ScriptObject finding) =>
        finding.TryGetValue("severity_rank", out var rankValue) && rankValue is int r ? r : 0;

    /// <summary>
    /// Gets the severity_icon property from a finding.
    /// </summary>
    private static string? GetFindingSeverityIcon(ScriptObject finding) =>
        finding.TryGetValue("severity_icon", out var iconValue) ? iconValue?.ToString() : null;

    /// <summary>
    /// Checks if the finding's attribute path matches the given attribute name.
    /// Matches exact name or prefix (e.g., "tags.environment" matches "tags").
    /// </summary>
    private static bool AttributeMatches(string attributeName, string? attrPath)
    {
        if (string.IsNullOrEmpty(attrPath))
        {
            return false;
        }

        return string.Equals(attrPath, attributeName, StringComparison.OrdinalIgnoreCase)
            || attrPath.StartsWith(attributeName + ".", StringComparison.OrdinalIgnoreCase)
            || attrPath.StartsWith(attributeName + "[", StringComparison.OrdinalIgnoreCase);
    }
}
