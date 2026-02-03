using System.Text.Json;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// JSON conversion helpers for Scriban compatibility.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Converts a JsonElement to a Scriban-compatible object (ScriptObject or ScriptArray).
    /// </summary>
    /// <param name="element">The JsonElement to convert.</param>
    /// <returns>A ScriptObject, ScriptArray, or primitive value.</returns>
    public static object? ConvertToScriptObject(JsonElement element)
    {
        return ConvertJsonValue(element);
    }

    /// <summary>
    /// Converts a JsonElement object into a Scriban ScriptObject recursively.
    /// </summary>
    /// <param name="element">Element to convert.</param>
    /// <returns>ScriptObject containing converted properties.</returns>
    private static ScriptObject JsonElementToScriptObject(JsonElement element)
    {
        var scriptObj = new ScriptObject();

        if (element.ValueKind != JsonValueKind.Object)
        {
            return scriptObj;
        }

        foreach (var property in element.EnumerateObject())
        {
            scriptObj[property.Name] = ConvertJsonValue(property.Value);
        }

        return scriptObj;
    }

    /// <summary>
    /// Converts supported objects into a dictionary representation for attribute collection.
    /// </summary>
    /// <param name="obj">Source object (JsonElement or ScriptObject).</param>
    /// <returns>Dictionary of property names to values.</returns>
    internal static Dictionary<string, object?> ToDictionary(object? obj)
    {
        var result = new Dictionary<string, object?>();

        if (obj is null)
        {
            return result;
        }

        if (obj is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonValue(property.Value);
            }

            return result;
        }

        if (obj is ScriptObject scriptObject)
        {
            foreach (var key in scriptObject.Keys)
            {
                result[key] = scriptObject[key];
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether a value is effectively null, handling JsonElement null values.
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <returns>True when null or JsonElement null.</returns>
    private static bool IsNullValue(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Null;
        }

        return false;
    }

    /// <summary>
    /// Converts a JsonElement to the closest Scriban-compatible primitive or nested object.
    /// </summary>
    /// <param name="element">Element to convert.</param>
    /// <returns>Converted value.</returns>
    internal static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonElementToScriptObject(element),
            JsonValueKind.Array => ConvertJsonArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    /// <summary>
    /// Converts a JsonElement array to a Scriban ScriptArray recursively.
    /// </summary>
    /// <param name="element">Array element.</param>
    /// <returns>ScriptArray with converted items.</returns>
    private static ScriptArray ConvertJsonArray(JsonElement element)
    {
        var array = new ScriptArray();
        foreach (var item in element.EnumerateArray())
        {
            array.Add(ConvertJsonValue(item));
        }
        return array;
    }
}
