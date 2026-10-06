using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Finds substantive values in Terraform state while ignoring caller-specified root housekeeping fields.
/// </summary>
internal static class MeaningfulStateWalker
{
    /// <summary>
    /// Determines whether JSON state contains at least one non-empty value.
    /// </summary>
    /// <param name="state">The prior resource state.</param>
    /// <param name="excludedRootPaths">Root property names that do not make the state substantive.</param>
    /// <returns><see langword="true"/> when at least one meaningful value is present.</returns>
    internal static bool HasMeaningfulState(object? state, IReadOnlySet<string>? excludedRootPaths = null)
    {
        return state is JsonElement value && HasMeaningfulValue(value, excludedRootPaths, isRoot: true);
    }

    /// <summary>
    /// Walks a JSON value recursively, applying housekeeping exclusions only at the root object.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="excludedRootPaths">Root property names to skip.</param>
    /// <param name="isRoot">Whether the current value is the resource-state root.</param>
    /// <returns><see langword="true"/> when a substantive scalar or collection member is found.</returns>
    private static bool HasMeaningfulValue(JsonElement value, IReadOnlySet<string>? excludedRootPaths, bool isRoot)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => HasMeaningfulObjectValue(value, excludedRootPaths, isRoot),
            JsonValueKind.Array => HasMeaningfulArrayValue(value, excludedRootPaths),
            JsonValueKind.String => !string.IsNullOrEmpty(value.GetString()),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks object properties, skipping excluded names only in the state root.
    /// </summary>
    /// <param name="value">The JSON object to inspect.</param>
    /// <param name="excludedRootPaths">Root property names to skip.</param>
    /// <param name="isRoot">Whether this object is the resource-state root.</param>
    /// <returns><see langword="true"/> when an included property contains a substantive value.</returns>
    private static bool HasMeaningfulObjectValue(JsonElement value, IReadOnlySet<string>? excludedRootPaths, bool isRoot)
    {
        foreach (var property in value.EnumerateObject())
        {
            if (isRoot && excludedRootPaths?.Contains(property.Name) == true)
            {
                continue;
            }

            if (HasMeaningfulValue(property.Value, excludedRootPaths, isRoot: false))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks array items while retaining the root exclusion policy for nested values.
    /// </summary>
    /// <param name="value">The JSON array to inspect.</param>
    /// <param name="excludedRootPaths">Root property names to skip.</param>
    /// <returns><see langword="true"/> when any item contains a substantive value.</returns>
    private static bool HasMeaningfulArrayValue(JsonElement value, IReadOnlySet<string>? excludedRootPaths)
    {
        return value.EnumerateArray().Any(item => HasMeaningfulValue(item, excludedRootPaths, isRoot: false));
    }
}
