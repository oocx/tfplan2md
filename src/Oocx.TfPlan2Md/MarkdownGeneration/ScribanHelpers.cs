using System;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Azure;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Custom Scriban helper functions for template rendering.
/// </summary>
public static class ScribanHelpers
{
    /// <summary>
    /// Registers all custom helper functions with the given ScriptObject.
    /// </summary>
    public static void RegisterHelpers(ScriptObject scriptObject, IPrincipalMapper principalMapper)
    {
        scriptObject.Import("format_diff", new Func<string?, string?, string>(FormatDiff));
        scriptObject.Import("diff_array", new Func<object?, object?, string, ScriptObject>(DiffArray));
        scriptObject.Import("escape_markdown", new Func<string?, string>(EscapeMarkdown));
        scriptObject.Import("azure_role_name", new Func<string?, string>(AzureRoleDefinitionMapper.GetRoleName));
        scriptObject.Import("azure_scope", new Func<string?, string>(AzureScopeParser.ParseScope));
        scriptObject.Import("azure_principal_name", new Func<string?, string>(p => ResolvePrincipalName(p, principalMapper)));
        scriptObject.Import("azure_scope_info", new Func<string?, ScriptObject>(GetScopeInfo));
        scriptObject.Import("azure_role_info", new Func<string?, string?, ScriptObject>(GetRoleInfo));
        scriptObject.Import("azure_principal_info", new Func<string?, string?, ScriptObject>((id, type) => GetPrincipalInfo(id, type, principalMapper)));
        scriptObject.Import("collect_attributes", new Func<object?, object?, ScriptArray>(CollectAttributes));
    }

    /// <summary>
    /// Escapes markdown-sensitive characters to keep generated tables and headings valid.
    /// Related feature: docs/features/markdown-quality-validation/specification.md
    /// </summary>
    /// <param name="input">The raw value to escape.</param>
    /// <returns>A markdown-safe string with newlines replaced by &lt;br/&gt;.</returns>
    public static string EscapeMarkdown(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var value = input;

        value = value.Replace("\\", "\\\\");
        value = value.Replace("|", "\\|");
        value = value.Replace("*", "\\*");
        value = value.Replace("_", "\\_");
        value = value.Replace("[", "\\[");
        value = value.Replace("]", "\\]");
        value = value.Replace("(", "\\(");
        value = value.Replace(")", "\\)");
        value = value.Replace("#", "\\#");
        value = value.Replace("`", "\\`");
        value = value.Replace("<", "\\<");
        value = value.Replace(">", "\\>");
        value = value.Replace("&", "&amp;");

        value = value.Replace("\r\n", "<br/>");
        value = value.Replace("\n", "<br/>");
        value = value.Replace("\r", "<br/>");

        return value;
    }

    private static ScriptObject GetScopeInfo(string? scope)
    {
        var info = AzureScopeParser.Parse(scope);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["type"] = info.Type,
            ["subscription_id"] = info.SubscriptionId ?? string.Empty,
            ["resource_group"] = info.ResourceGroup ?? string.Empty,
            ["level"] = info.Level.ToString(),
            ["summary"] = info.Summary,
            ["summary_label"] = info.SummaryLabel,
            ["summary_name"] = info.SummaryName,
            ["details"] = info.Details
        };
    }

    private static ScriptObject GetRoleInfo(string? roleDefinitionId, string? roleDefinitionName)
    {
        var info = AzureRoleDefinitionMapper.GetRoleDefinition(roleDefinitionId, roleDefinitionName);

        return new ScriptObject
        {
            ["name"] = info.Name,
            ["id"] = info.Id,
            ["full_name"] = info.FullName
        };
    }

    private static ScriptObject GetPrincipalInfo(string? principalId, string? principalType, IPrincipalMapper principalMapper)
    {
        var id = principalId ?? string.Empty;
        var type = principalType ?? string.Empty;
        var name = principalMapper.GetName(id, type) ?? id;
        var fullName = BuildPrincipalFullName(name, id, type);

        return new ScriptObject
        {
            ["name"] = name,
            ["id"] = id,
            ["type"] = type,
            ["full_name"] = fullName
        };
    }

    private static ScriptArray CollectAttributes(object? before, object? after)
    {
        var beforeDict = ToDictionary(before);
        var afterDict = ToDictionary(after);
        var keys = beforeDict.Keys.Union(afterDict.Keys).ToList();

        var attributes = new ScriptArray();
        foreach (var key in keys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            if (IsNullValue(beforeValue) && IsNullValue(afterValue))
            {
                continue;
            }

            attributes.Add(key);
        }

        return attributes;
    }

    /// <summary>
    /// Formats a before/after pair into a single diff-style string.
    /// </summary>
    /// <param name="before">The original value.</param>
    /// <param name="after">The updated value.</param>
    /// <returns>The unchanged value when equal; otherwise "- before&lt;br&gt;+ after".</returns>
    public static string FormatDiff(string? before, string? after)
    {
        var beforeValue = before ?? string.Empty;
        var afterValue = after ?? string.Empty;

        if (string.Equals(beforeValue, afterValue, StringComparison.Ordinal))
        {
            return afterValue;
        }

        return $"- {beforeValue}<br>+ {afterValue}";
    }

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

    private static Dictionary<string, ScriptObject> ExtractArrayItems(
        object? array,
        string keyProperty,
        string arrayName)
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
                    throw new ScribanHelperException(
                        $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
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
                        throw new ScribanHelperException(
                            $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
                    }

                    var key = keyValue.ToString()!;
                    result[key] = scriptObj;
                }
                else if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    if (!element.TryGetProperty(keyProperty, out var keyElement))
                    {
                        throw new ScribanHelperException(
                            $"Item at index {index} in '{arrayName}' array is missing required key property '{keyProperty}'.");
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
    /// Converts a JsonElement to a Scriban-compatible object (ScriptObject or ScriptArray).
    /// </summary>
    /// <param name="element">The JsonElement to convert.</param>
    /// <returns>A ScriptObject, ScriptArray, or primitive value.</returns>
    public static object? ConvertToScriptObject(JsonElement element)
    {
        return ConvertJsonValue(element);
    }

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

    private static string ResolvePrincipalName(string? principalId, IPrincipalMapper principalMapper)
    {
        if (principalId is null)
        {
            return string.Empty;
        }

        return principalMapper.GetPrincipalName(principalId);
    }

    private static string BuildPrincipalFullName(string name, string? principalId, string? principalType)
    {
        var typePart = string.IsNullOrWhiteSpace(principalType) ? string.Empty : $" ({principalType})";
        var idPart = string.IsNullOrWhiteSpace(principalId) ? string.Empty : $" [{principalId}]";
        return $"{name}{typePart}{idPart}".Trim();
    }

    private static Dictionary<string, object?> ToDictionary(object? obj)
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

    private static object? ConvertJsonValue(JsonElement element)
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

    private static ScriptArray ConvertJsonArray(JsonElement element)
    {
        var array = new ScriptArray();
        foreach (var item in element.EnumerateArray())
        {
            array.Add(ConvertJsonValue(item));
        }
        return array;
    }

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

/// <summary>
/// Exception thrown when a Scriban helper function encounters an error.
/// </summary>
public class ScribanHelperException : Exception
{
    public ScribanHelperException(string message) : base(message)
    {
    }

    public ScribanHelperException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
