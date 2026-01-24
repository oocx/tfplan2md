using System.Text.Json;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Compares before and after JSON objects and returns only changed properties with their values.
    /// </summary>
    /// <param name="beforeJson">The before state JSON object.</param>
    /// <param name="afterJson">The after state JSON object.</param>
    /// <param name="beforeSensitive">The before_sensitive structure indicating sensitive properties.</param>
    /// <param name="afterSensitive">The after_sensitive structure indicating sensitive properties.</param>
    /// <param name="showUnchanged">When true, returns all properties; when false, returns only changed properties.</param>
    /// <param name="showSensitive">Reserved for future use (sensitive value masking handled by template).</param>
    /// <returns>
    /// List of property comparison objects with properties: path, before, after, is_large, is_sensitive, is_changed.
    /// </returns>
    /// <remarks>
    /// This is the core comparison function for azapi_resource update operations. It flattens both
    /// before and after JSON, compares them property-by-property, and identifies added, removed, and
    /// modified properties. Sensitivity is determined by navigating the before_sensitive and after_sensitive
    /// structures in parallel with the value structures.
    /// The showSensitive parameter is included for API consistency but the actual masking of sensitive
    /// values is handled by the template layer.
    /// </remarks>
    public static ScriptArray CompareJsonProperties(
        object? beforeJson,
        object? afterJson,
        object? beforeSensitive,
        object? afterSensitive,
        bool showUnchanged,
#pragma warning disable IDE0060 // Remove unused parameter - included for API consistency
        bool showSensitive)
#pragma warning restore IDE0060
    {
        var result = new ScriptArray();

        // Flatten both before and after JSON
        var beforeFlattened = FlattenJsonToDictionary(beforeJson);
        var afterFlattened = FlattenJsonToDictionary(afterJson);

        // Flatten sensitivity structures
        var beforeSensitiveFlattened = FlattenSensitivity(beforeSensitive);
        var afterSensitiveFlattened = FlattenSensitivity(afterSensitive);

        // Get all unique property paths
        var allPaths = beforeFlattened.Keys.Union(afterFlattened.Keys).Order().ToList();

        foreach (var path in allPaths)
        {
            var beforeValue = beforeFlattened.GetValueOrDefault(path);
            var afterValue = afterFlattened.GetValueOrDefault(path);

            // Check if property is sensitive
            var isSensitive = beforeSensitiveFlattened.Contains(path) || afterSensitiveFlattened.Contains(path);

            // Determine if value changed
            var isChanged = !ValuesEqual(beforeValue, afterValue);

            // Skip unchanged properties if showUnchanged is false
            if (!isChanged && !showUnchanged)
            {
                continue;
            }

            // Determine if values are large
            var beforeLarge = beforeValue != null && SerializeValue(beforeValue)?.Length > LargeValueThreshold;
            var afterLarge = afterValue != null && SerializeValue(afterValue)?.Length > LargeValueThreshold;
            var isLarge = beforeLarge || afterLarge;

            // Create comparison object
            var comparison = new ScriptObject
            {
                ["path"] = path,
                ["before"] = beforeValue,
                ["after"] = afterValue,
                ["is_large"] = isLarge,
                ["is_sensitive"] = isSensitive,
                ["is_changed"] = isChanged
            };

            result.Add(comparison);
        }

        return result;
    }

    /// <summary>
    /// Flattens a sensitivity structure to extract paths of sensitive properties.
    /// </summary>
    /// <param name="sensitiveObject">The sensitivity structure (e.g., before_sensitive or after_sensitive).</param>
    /// <returns>Set of paths that are marked as sensitive.</returns>
    private static HashSet<string> FlattenSensitivity(object? sensitiveObject)
    {
        var result = new HashSet<string>();

        if (sensitiveObject is null)
        {
            return result;
        }

        // Convert to JsonElement if needed
        JsonElement element;
        if (sensitiveObject is JsonElement jsonElement)
        {
            element = jsonElement;
        }
        else if (sensitiveObject is string jsonString)
        {
            try
            {
                element = JsonDocument.Parse(jsonString).RootElement;
            }
            catch
            {
                return result;
            }
        }
        else
        {
            return result;
        }

        // Recursively traverse sensitivity structure
        TraverseSensitivity(element, string.Empty, result);

        return result;
    }

    /// <summary>
    /// Recursively traverses a sensitivity structure to identify sensitive paths.
    /// </summary>
    /// <param name="element">The current JsonElement.</param>
    /// <param name="prefix">The current path prefix.</param>
    /// <param name="sensitivePaths">The set to populate with sensitive paths.</param>
    private static void TraverseSensitivity(JsonElement element, string prefix, HashSet<string> sensitivePaths)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    // If the value is true, this path is sensitive
                    if (property.Value.ValueKind == JsonValueKind.True)
                    {
                        sensitivePaths.Add(path);
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Object || property.Value.ValueKind == JsonValueKind.Array)
                    {
                        // Recurse into nested structures
                        TraverseSensitivity(property.Value, path, sensitivePaths);
                    }
                }
                break;

            case JsonValueKind.Array:
                var arrayItems = element.EnumerateArray().ToList();
                for (var i = 0; i < arrayItems.Count; i++)
                {
                    var path = $"{prefix}[{i}]";
                    if (arrayItems[i].ValueKind == JsonValueKind.True)
                    {
                        sensitivePaths.Add(path);
                    }
                    else if (arrayItems[i].ValueKind == JsonValueKind.Object || arrayItems[i].ValueKind == JsonValueKind.Array)
                    {
                        TraverseSensitivity(arrayItems[i], path, sensitivePaths);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Checks if two values are equal for comparison purposes.
    /// </summary>
    /// <param name="before">The before value.</param>
    /// <param name="after">The after value.</param>
    /// <returns>True if values are equal, false otherwise.</returns>
    private static bool ValuesEqual(object? before, object? after)
    {
        if (before is null && after is null)
        {
            return true;
        }

        if (before is null || after is null)
        {
            return false;
        }

        // Handle numeric comparisons (int/long/double)
        if (IsNumeric(before) && IsNumeric(after))
        {
            return Convert.ToDouble(before) == Convert.ToDouble(after);
        }

        // Default: use Equals
        return before.Equals(after);
    }

    /// <summary>
    /// Checks if a value is numeric.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if numeric, false otherwise.</returns>
    private static bool IsNumeric(object value)
    {
        return value is int || value is long || value is double || value is float || value is decimal;
    }
}
