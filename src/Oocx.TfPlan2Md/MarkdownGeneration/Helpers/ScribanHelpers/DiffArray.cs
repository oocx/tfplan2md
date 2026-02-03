using System.Text.Json;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Array diff helper exposed to Scriban templates.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Computes semantic differences between two arrays using a key property.
    /// </summary>
    /// <param name="beforeArray">Array from current state.</param>
    /// <param name="afterArray">Array from planned state.</param>
    /// <param name="keyProperty">Property name to identify items (e.g., "name").</param>
    /// <returns>Object with added, removed, modified, and unchanged collections.</returns>
    /// <exception cref="ScribanHelperException">Thrown when an item lacks the key property.</exception>
    public static ScriptObject DiffArray(object? beforeArray, object? afterArray, string keyProperty)
    {
        var beforeItems = ExtractArrayItems(beforeArray, keyProperty, "before");
        var afterItems = ExtractArrayItems(afterArray, keyProperty, "after");

        var beforeKeys = beforeItems.Keys.ToHashSet();
        var afterKeys = afterItems.Keys.ToHashSet();

        var addedKeys = afterKeys.Except(beforeKeys);
        var removedKeys = beforeKeys.Except(afterKeys);
        var commonKeys = beforeKeys.Intersect(afterKeys);

        var added = new ScriptArray();
        var removed = new ScriptArray();
        var modified = new ScriptArray();
        var unchanged = new ScriptArray();

        foreach (var key in addedKeys)
        {
            added.Add(afterItems[key]);
        }

        foreach (var key in removedKeys)
        {
            removed.Add(beforeItems[key]);
        }

        foreach (var key in commonKeys)
        {
            var beforeItem = beforeItems[key];
            var afterItem = afterItems[key];

            if (AreItemsEqual(beforeItem, afterItem))
            {
                unchanged.Add(afterItem);
            }
            else
            {
                var modifiedItem = new ScriptObject
                {
                    ["before"] = beforeItem,
                    ["after"] = afterItem
                };
                modified.Add(modifiedItem);
            }
        }

        var result = new ScriptObject
        {
            ["added"] = added,
            ["removed"] = removed,
            ["modified"] = modified,
            ["unchanged"] = unchanged
        };

        return result;
    }

    /// <summary>
    /// Extracts keyed items from either a JsonElement array or a Scriban ScriptArray.
    /// </summary>
    /// <param name="array">Incoming array from template context.</param>
    /// <param name="keyProperty">Property name that acts as the item key.</param>
    /// <param name="arrayName">Array label used for error reporting.</param>
    /// <returns>Dictionary keyed by property value with ScriptObject entries.</returns>
    /// <exception cref="ScribanHelperException">Thrown when an item is missing the required key.</exception>
    private static Dictionary<string, ScriptObject> ExtractArrayItems(object? array, string keyProperty, string arrayName)
    {
        var result = new Dictionary<string, ScriptObject>();

        if (array is null)
        {
            return result;
        }

        if (array is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            var index = 0;
            foreach (var element in jsonElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    index++;
                    continue;
                }

                if (!element.TryGetProperty(keyProperty, out var keyElement))
                {
                    throw new ScribanHelperException($"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                }

                var key = keyElement.ToString();
                var scriptObj = JsonElementToScriptObject(element);
                result[key] = scriptObj;
                index++;
            }
        }
        else if (array is ScriptArray scriptArray)
        {
            var index = 0;
            foreach (var item in scriptArray)
            {
                if (item is ScriptObject scriptObj)
                {
                    if (!scriptObj.TryGetValue(keyProperty, out var keyValue) || keyValue is null)
                    {
                        throw new ScribanHelperException($"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                    }

                    var key = keyValue.ToString()!;
                    result[key] = scriptObj;
                }
                else if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    if (!element.TryGetProperty(keyProperty, out var keyElement))
                    {
                        throw new ScribanHelperException($"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                    }

                    var key = keyElement.ToString();
                    var converted = JsonElementToScriptObject(element);
                    result[key] = converted;
                }

                index++;
            }
        }

        return result;
    }

    /// <summary>
    /// Compares two ScriptObjects for deep equality across their keys and values.
    /// </summary>
    /// <param name="before">Original object.</param>
    /// <param name="after">Updated object.</param>
    /// <returns>True when objects are equivalent.</returns>
    private static bool AreItemsEqual(ScriptObject before, ScriptObject after)
    {
        var beforeKeys = before.Keys.ToHashSet();
        var afterKeys = after.Keys.ToHashSet();

        if (!beforeKeys.SetEquals(afterKeys))
        {
            return false;
        }

        foreach (var key in beforeKeys)
        {
            var beforeValue = before[key];
            var afterValue = after[key];

            if (!AreValuesEqual(beforeValue, afterValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Recursively compares two values (primitive, ScriptObject, or ScriptArray) for equality.
    /// </summary>
    /// <param name="before">Original value.</param>
    /// <param name="after">Updated value.</param>
    /// <returns>True when values are equal.</returns>
    private static bool AreValuesEqual(object? before, object? after)
    {
        if (before is null && after is null)
        {
            return true;
        }

        if (before is null || after is null)
        {
            return false;
        }

        if (before is ScriptObject beforeObj && after is ScriptObject afterObj)
        {
            return AreItemsEqual(beforeObj, afterObj);
        }

        if (before is ScriptArray beforeArr && after is ScriptArray afterArr)
        {
            if (beforeArr.Count != afterArr.Count)
            {
                return false;
            }

            for (var i = 0; i < beforeArr.Count; i++)
            {
                if (!AreValuesEqual(beforeArr[i], afterArr[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return before.Equals(after);
    }
}
