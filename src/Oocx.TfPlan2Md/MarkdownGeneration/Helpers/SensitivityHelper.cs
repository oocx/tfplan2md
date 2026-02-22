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
        // Check root boolean sensitivity first: when Terraform marks the entire resource
        // as sensitive, the flattened dictionary contains {"": "true"}.
        if ((beforeSensitive.TryGetValue("", out var rootBefore) && rootBefore == "true")
            || (afterSensitive.TryGetValue("", out var rootAfter) && rootAfter == "true"))
        {
            return true;
        }

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
    /// <returns>An enumerable of unique paths from most specific to least specific, though strict ordering
    /// within a level is not guaranteed for intermediate paths.</returns>
    /// <remarks>
    /// Examples:
    /// - Input: "variable[0].secret_value" → Output: ["variable[0].secret_value", "variable[0]", "variable"].
    /// - Input: "a[0].b[1]" → Output: ["a[0].b[1]", "a[0].b", "a[0]", "a"].
    /// - Input: "secrets[0]" → Output: ["secrets[0]", "secrets"].
    /// - Input: "simple_attr" → Output: ["simple_attr"].
    /// All paths are unique — no duplicates are yielded even for deeply nested indexed keys.
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </remarks>
    internal static IEnumerable<string> GetHierarchicalPaths(string key)
    {
        var seen = new HashSet<string>();

        // Always check the key itself first
        yield return key;
        seen.Add(key);

        // Split by '.' to get path segments
        var parts = key.Split('.');

        // For each parent level (from most specific to least specific)
        for (var i = parts.Length - 1; i > 0; i--)
        {
            var parentPath = string.Join('.', parts.Take(i));

            // If the last removed segment had an array index, also yield the parent path
            // with that segment but without the index. For example, when processing
            // "a[0].b[1]" and removing "b[1]", we first yield "a[0].b" (strip index from
            // the removed segment and append to parent), then yield "a[0]" (the parent itself).
            var removedSegment = parts[i];
            if (removedSegment.Contains('['))
            {
                var segmentBase = removedSegment[..removedSegment.IndexOf('[')];
                var pathWithBase = $"{parentPath}.{segmentBase}";
                if (seen.Add(pathWithBase))
                {
                    yield return pathWithBase;
                }
            }

            // If the parent path ends with an array-indexed segment (e.g., "a[0]"),
            // also check without the index (e.g., "a"). Only strip when the path ends
            // with ']' to avoid incorrectly stripping indices from middle segments.
            if (parentPath.EndsWith(']'))
            {
                var arrayName = parentPath[..parentPath.LastIndexOf('[')];
                if (seen.Add(arrayName))
                {
                    yield return arrayName;
                }
            }

            if (seen.Add(parentPath))
            {
                yield return parentPath;
            }
        }

        // Handle top-level array keys without dots (e.g., "secrets[0]" → "secrets")
        // The loop above only runs when parts.Length > 1, so single-segment array keys
        // need this additional check.
        if (parts.Length == 1 && key.Contains('['))
        {
            var baseName = key[..key.IndexOf('[')];
            if (seen.Add(baseName))
            {
                yield return baseName;
            }
        }
    }
}
