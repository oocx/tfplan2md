using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Provides centralized sensitivity detection for Terraform plan attribute paths.
/// </summary>
/// <remarks>
/// Extracted from <c>ReportModelBuilder.ResourceChanges</c> to enable direct unit testing and
/// reuse across provider-specific renderers (e.g., AzApi body rendering).
/// Terraform marks sensitive values via <c>before_sensitive</c> / <c>after_sensitive</c> metadata
/// which is flattened into key-value dictionaries by <see cref="JsonFlattener"/>.
/// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
/// </remarks>
internal static class SensitivityHelper
{
    /// <summary>
    /// Checks if an attribute is marked as sensitive by examining the attribute path and all parent paths.
    /// </summary>
    /// <param name="key">The attribute path (e.g., "variable[0].secret_value").</param>
    /// <param name="beforeSensitive">Dictionary of sensitive attributes from before state.</param>
    /// <param name="afterSensitive">Dictionary of sensitive attributes from after state.</param>
    /// <returns>True if the attribute or any parent path is marked sensitive.</returns>
    /// <remarks>
    /// Terraform marks entire arrays/objects as sensitive in the plan JSON. This method checks hierarchically:
    /// - For "variable[0].secret_value", checks: "variable[0].secret_value", "variable[0]", "variable"
    /// - For "repository[0].secrets[1].value", checks all parent paths up to the root
    /// This prevents sensitive data disclosure when Terraform marks a parent container as sensitive.
    /// Related issue: docs/issues/093-sensitive-attribute-disclosure/analysis.md.
    /// </remarks>
    internal static bool IsSensitiveAttribute(
        string key,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        // Check all hierarchical paths (key itself, then parent paths)
        foreach (var pathToCheck in GetHierarchicalPaths(key))
        {
            if ((beforeSensitive.TryGetValue(pathToCheck, out var bv) && bv == "true")
                || (afterSensitive.TryGetValue(pathToCheck, out var av) && av == "true"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generates all hierarchical paths for a given attribute key to support parent-level sensitivity checking.
    /// </summary>
    /// <param name="key">The attribute path (e.g., "variable[0].secret_value").</param>
    /// <returns>An enumerable of all paths from most specific to least specific.</returns>
    /// <remarks>
    /// Examples:
    /// - Input: "variable[0].secret_value" → Output: ["variable[0].secret_value", "variable[0]", "variable"].
    /// - Input: "repository[0].secrets[1].value" → Output: ["repository[0].secrets[1].value", "repository[0].secrets[1]", "repository[0].secrets", "repository[0]", "repository"].
    /// - Input: "simple_attr" → Output: ["simple_attr"].
    /// </remarks>
    internal static IEnumerable<string> GetHierarchicalPaths(string key)
    {
        // Always check the key itself first
        yield return key;

        // Split by '.' to get path segments
        var parts = key.Split('.');

        // For each parent level (from most specific to least specific)
        for (var i = parts.Length - 1; i > 0; i--)
        {
            var parentPath = string.Join('.', parts.Take(i));

            // If the parent path contains array indices, also check without the index
            // e.g., "variable[0]" should also check "variable"
            if (parentPath.Contains('['))
            {
                var arrayName = parentPath[..parentPath.IndexOf('[')];
                yield return arrayName;
            }

            yield return parentPath;
        }
    }
}
