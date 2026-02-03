using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Flattens JSON objects into key-value dictionaries for attribute processing.
/// </summary>
/// <remarks>
/// Extracted from ReportModelBuilder to reduce class coupling.
/// </remarks>
internal static class JsonFlattener
{
    /// <summary>
    /// Converts a JSON object or element to a flat dictionary with dotted key paths.
    /// </summary>
    /// <param name="obj">The object to flatten (typically a JsonElement).</param>
    /// <param name="prefix">The key prefix for nested properties.</param>
    /// <returns>A flat dictionary where keys represent JSON paths.</returns>
    public static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "")
    {
        Dictionary<string, string?> result = [];
        if (obj is null)
        {
            return result;
        }

        if (obj is JsonElement element)
        {
            FlattenJsonElement(element, prefix, result);
        }

        return result;
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, result);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    FlattenJsonElement(item, key, result);
                    index++;
                }
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;
            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;
            case JsonValueKind.Null:
                result[prefix] = null;
                break;
        }
    }
}
