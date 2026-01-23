using System.Text.Json;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

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
    /// Flattens a JSON object into dot-notation key-value pairs for table rendering.
    /// </summary>
    /// <param name="jsonObject">The JSON object to flatten (typically from body attribute).</param>
    /// <param name="prefix">Property path prefix for nested recursion (default: empty).</param>
    /// <returns>
    /// List of objects with properties: path (string), value (object), is_large (bool).
    /// Empty objects are omitted. Null values are included with null value.
    /// </returns>
    /// <remarks>
    /// This is the core function for transforming azapi_resource body content into scannable tables.
    /// Uses dot notation for nested objects (e.g., properties.sku.name) and array indexing for arrays
    /// (e.g., tags[0].key). Values exceeding 200 characters are marked as large for separate rendering.
    /// </remarks>
    /// <example>
    /// Input: { "properties": { "sku": { "name": "Basic" }, "enabled": true } }
    /// Output: [
    ///   { path: "properties.sku.name", value: "Basic", is_large: false },
    ///   { path: "properties.enabled", value: true, is_large: false }
    /// ].
    /// </example>
    public static ScriptArray FlattenJson(object? jsonObject, string prefix = "")
    {
        var result = new ScriptArray();

        if (jsonObject is null)
        {
            return result;
        }

        // Handle different input types
        if (jsonObject is JsonElement jsonElement)
        {
            // Flatten JsonElement directly
            FlattenJsonElement(jsonElement, prefix, result);
            return result;
        }

        if (jsonObject is ScriptObject scriptObject)
        {
            // Flatten ScriptObject recursively
            FlattenScriptObject(scriptObject, prefix, result);
            return result;
        }

        if (jsonObject is string jsonString)
        {
            try
            {
                var element = JsonDocument.Parse(jsonString).RootElement;
                FlattenJsonElement(element, prefix, result);
                return result;
            }
            catch
            {
                // If parsing fails, treat as a single value
                result.Add(CreatePropertyObject(prefix, jsonString));
                return result;
            }
        }

        // For other types, treat as primitive values
        result.Add(CreatePropertyObject(prefix, jsonObject));
        return result;
    }

    /// <summary>
    /// Recursively flattens a ScriptObject into the result array.
    /// </summary>
    /// <param name="scriptObject">The ScriptObject to flatten.</param>
    /// <param name="prefix">Current property path prefix.</param>
    /// <param name="result">The result array to populate.</param>
    private static void FlattenScriptObject(ScriptObject scriptObject, string prefix, ScriptArray result)
    {
        foreach (var key in scriptObject.Keys)
        {
            var value = scriptObject[key];
            var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

            if (value is null)
            {
                result.Add(CreatePropertyObject(path, null));
            }
            else if (value is ScriptObject nestedScriptObject)
            {
                // Skip empty objects
                if (nestedScriptObject.Count > 0)
                {
                    FlattenScriptObject(nestedScriptObject, path, result);
                }
            }
            else if (value is ScriptArray scriptArray)
            {
                for (var i = 0; i < scriptArray.Count; i++)
                {
                    var arrayPath = $"{path}[{i}]";
                    var arrayItem = scriptArray[i];

                    if (arrayItem is ScriptObject nestedArrayObject)
                    {
                        FlattenScriptObject(nestedArrayObject, arrayPath, result);
                    }
                    else
                    {
                        result.Add(CreatePropertyObject(arrayPath, arrayItem));
                    }
                }
            }
            else
            {
                // Leaf value (string, number, boolean)
                result.Add(CreatePropertyObject(path, value));
            }
        }
    }

    /// <summary>
    /// Recursively flattens a JsonElement into the result array.
    /// </summary>
    /// <param name="element">The JsonElement to flatten.</param>
    /// <param name="prefix">Current property path prefix.</param>
    /// <param name="result">The result array to populate.</param>
    private static void FlattenJsonElement(JsonElement element, string prefix, ScriptArray result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                // Skip empty objects
                if (element.EnumerateObject().Any())
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        var path = string.IsNullOrEmpty(prefix)
                            ? property.Name
                            : $"{prefix}.{property.Name}";
                        FlattenJsonElement(property.Value, path, result);
                    }
                }
                break;

            case JsonValueKind.Array:
                var arrayItems = element.EnumerateArray().ToList();
                for (var i = 0; i < arrayItems.Count; i++)
                {
                    var path = $"{prefix}[{i}]";
                    FlattenJsonElement(arrayItems[i], path, result);
                }
                break;

            case JsonValueKind.Null:
                // Include null values with the path
                result.Add(CreatePropertyObject(prefix, null));
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                // Leaf values - add to result
                result.Add(CreatePropertyObject(prefix, ConvertJsonValue(element)));
                break;

            default:
                // Fallback for unexpected types
                result.Add(CreatePropertyObject(prefix, element.ToString()));
                break;
        }
    }

    /// <summary>
    /// Creates a property object for the flattened result.
    /// </summary>
    /// <param name="path">The dot-notation property path.</param>
    /// <param name="value">The property value.</param>
    /// <returns>ScriptObject with path, value, and is_large properties.</returns>
    private static ScriptObject CreatePropertyObject(string path, object? value)
    {
        var serializedValue = SerializeValue(value);
        var isLarge = serializedValue?.Length > LargeValueThreshold;

        return new ScriptObject
        {
            ["path"] = path,
            ["value"] = value,
            ["is_large"] = isLarge
        };
    }

    /// <summary>
    /// Serializes a value to string for length checking.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>String representation of the value.</returns>
    private static string? SerializeValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string str)
        {
            return str;
        }

        if (value is bool || value is long || value is int || value is double)
        {
            return value.ToString();
        }

        // For complex types, use ToString (AOT-compatible)
        return value.ToString();
    }

    /// <summary>
    /// Flattens a JSON object into a dictionary of path -> value mappings.
    /// </summary>
    /// <param name="jsonObject">The JSON object to flatten.</param>
    /// <returns>Dictionary mapping dot-notation paths to values.</returns>
    private static Dictionary<string, object?> FlattenJsonToDictionary(object? jsonObject)
    {
        var result = new Dictionary<string, object?>();

        if (jsonObject is null)
        {
            return result;
        }

        var flattened = FlattenJson(jsonObject);
        foreach (var item in flattened)
        {
            if (item is ScriptObject scriptObj)
            {
                var path = scriptObj["path"]?.ToString() ?? string.Empty;
                var value = scriptObj["value"];
                result[path] = value;
            }
        }

        return result;
    }
}
