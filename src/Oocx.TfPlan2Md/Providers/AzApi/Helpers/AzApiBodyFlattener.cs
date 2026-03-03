using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Flattens AzAPI JSON bodies and state objects into path/value entries.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "AzApi body flattening must support multiple Terraform JSON representations for migration parity.")]
internal static class AzApiBodyFlattener
{
    private const int LargeValueThreshold = 200;

    /// <summary>
    /// Flattens the input object into dot-notation properties.
    /// </summary>
    /// <param name="value">Body object, JSON element, or JSON string.</param>
    /// <param name="prefix">Path prefix used during recursion.</param>
    /// <returns>Flattened properties preserving discovery order.</returns>
    internal static IReadOnlyList<AzApiBodyProperty> Flatten(object? value, string prefix = "")
    {
        List<AzApiBodyProperty> result = [];
        FlattenCore(value, prefix, result);
        return result;
    }

    /// <summary>
    /// Flattens a value into a path dictionary.
    /// </summary>
    /// <param name="value">Input value.</param>
    /// <returns>Path/value dictionary.</returns>
    internal static IReadOnlyDictionary<string, object?> FlattenToDictionary(object? value)
    {
        var flattened = Flatten(value);
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in flattened)
        {
            map[property.Path] = property.Value;
        }

        return map;
    }

    /// <summary>
    /// Collects paths for empty objects and arrays so renderers can preserve
    /// sensitivity placeholders for intentionally empty containers.
    /// </summary>
    /// <param name="value">Body object, JSON element, or JSON string.</param>
    /// <param name="prefix">Path prefix used during recursion.</param>
    /// <returns>Flattened paths that point to empty objects/arrays.</returns>
    internal static IReadOnlyList<string> CollectEmptyContainerPaths(object? value, string prefix = "")
    {
        List<string> result = [];
        CollectEmptyContainerPathsCore(value, prefix, result);
        return result;
    }

    /// <summary>
    /// Converts an arbitrary state object into a shallow dictionary.
    /// </summary>
    /// <param name="value">State object.</param>
    /// <returns>Dictionary of top-level properties.</returns>
    internal static IReadOnlyDictionary<string, object?> ToDictionary(object? value)
    {
        if (value is null)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        if (value is JsonElement element)
        {
            return ToDictionary(element);
        }

        if (value is IReadOnlyDictionary<string, object?> typedReadonly)
        {
            return new Dictionary<string, object?>(typedReadonly, StringComparer.Ordinal);
        }

        if (value is IDictionary<string, object?> typed)
        {
            return new Dictionary<string, object?>(typed, StringComparer.Ordinal);
        }

        if (value is IDictionary<string, object> generic)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var item in generic)
            {
                map[item.Key] = item.Value;
            }

            return map;
        }

        if (value is string jsonText)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonText);
                return ToDictionary(document.RootElement);
            }
            catch
            {
                return new Dictionary<string, object?>(StringComparer.Ordinal);
            }
        }

        return new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            map[property.Name] = ConvertJsonValue(property.Value);
        }

        return map;
    }

    private static void FlattenCore(object? value, string prefix, List<AzApiBodyProperty> result)
    {
        if (value is null)
        {
            result.Add(new AzApiBodyProperty(prefix, null, IsLargeValue(null)));
            return;
        }

        if (value is string text)
        {
            if (TryParseJson(text, out var root))
            {
                FlattenCore(root, prefix, result);
                return;
            }

            result.Add(new AzApiBodyProperty(prefix, text, IsLargeValue(text)));
            return;
        }

        if (value is JsonElement element)
        {
            FlattenJsonElement(element, prefix, result);
            return;
        }

        if (TryGetDictionary(value, out var dict))
        {
            if (dict.Count == 0)
            {
                return;
            }

            foreach (var (key, nestedValue) in dict)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                FlattenCore(nestedValue, path, result);
            }

            return;
        }

        if (TryGetList(value, out var list))
        {
            for (var index = 0; index < list.Count; index++)
            {
                var path = $"{prefix}[{index}]";
                FlattenCore(list[index], path, result);
            }

            return;
        }

        result.Add(new AzApiBodyProperty(prefix, value, IsLargeValue(value)));
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Empty-container traversal must support all supported Terraform carrier shapes (json string, JsonElement, dictionaries, lists).")]
    [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Container-path extraction intentionally branches on runtime value shapes for deterministic parity behavior.")]
    private static void CollectEmptyContainerPathsCore(object? value, string prefix, List<string> result)
    {
        if (value is null)
        {
            return;
        }

        if (value is string text)
        {
            if (TryParseJson(text, out var root))
            {
                CollectEmptyContainerPathsCore(root, prefix, result);
            }

            return;
        }

        if (value is JsonElement element)
        {
            CollectEmptyContainerPathsFromElement(element, prefix, result);
            return;
        }

        if (TryGetDictionary(value, out var dict))
        {
            if (dict.Count == 0)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    result.Add(prefix);
                }

                return;
            }

            foreach (var (key, nestedValue) in dict)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                CollectEmptyContainerPathsCore(nestedValue, path, result);
            }

            return;
        }

        if (TryGetList(value, out var list))
        {
            if (list.Count == 0)
            {
                if (!string.IsNullOrEmpty(prefix))
                {
                    result.Add(prefix);
                }

                return;
            }

            for (var index = 0; index < list.Count; index++)
            {
                CollectEmptyContainerPathsCore(list[index], $"{prefix}[{index}]", result);
            }
        }
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "JsonElement traversal explicitly handles object/array empty-container cases to preserve sensitivity placeholders.")]
    [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "ValueKind branching keeps behavior explicit and avoids hidden recursion side effects.")]
    private static void CollectEmptyContainerPathsFromElement(JsonElement element, string prefix, List<string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = element.EnumerateObject().ToList();
                if (properties.Count == 0)
                {
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        result.Add(prefix);
                    }

                    return;
                }

                foreach (var property in properties)
                {
                    var path = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    CollectEmptyContainerPathsFromElement(property.Value, path, result);
                }

                break;

            case JsonValueKind.Array:
                var items = element.EnumerateArray().ToList();
                if (items.Count == 0)
                {
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        result.Add(prefix);
                    }

                    return;
                }

                for (var index = 0; index < items.Count; index++)
                {
                    CollectEmptyContainerPathsFromElement(items[index], $"{prefix}[{index}]", result);
                }

                break;
        }
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, List<AzApiBodyProperty> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (!element.EnumerateObject().Any())
                {
                    return;
                }

                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, path, result);
                }

                break;

            case JsonValueKind.Array:
                var array = element.EnumerateArray().ToList();
                for (var index = 0; index < array.Count; index++)
                {
                    FlattenJsonElement(array[index], $"{prefix}[{index}]", result);
                }

                break;

            default:
                var converted = ConvertJsonValue(element);
                result.Add(new AzApiBodyProperty(prefix, converted, IsLargeValue(converted)));
                break;
        }
    }

    private static bool TryParseJson(string text, out JsonElement root)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            root = document.RootElement.Clone();
            return true;
        }
        catch
        {
            root = default;
            return false;
        }
    }

    private static bool TryGetDictionary(object value, out IReadOnlyDictionary<string, object?> dictionary)
    {
        if (value is IReadOnlyDictionary<string, object?> typedReadonly)
        {
            dictionary = typedReadonly;
            return true;
        }

        if (value is IDictionary<string, object?> typed)
        {
            dictionary = new Dictionary<string, object?>(typed, StringComparer.Ordinal);
            return true;
        }

        if (value is IDictionary<string, object> generic)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var item in generic)
            {
                map[item.Key] = item.Value;
            }

            dictionary = map;
            return true;
        }

        dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
        return false;
    }

    private static bool TryGetList(object value, out IReadOnlyList<object?> list)
    {
        if (value is IReadOnlyList<object?> typedReadonly)
        {
            list = typedReadonly;
            return true;
        }

        if (value is IEnumerable<object?> enumerable)
        {
            list = enumerable.ToList();
            return true;
        }

        if (value is IEnumerable<object> enumerableObject)
        {
            list = enumerableObject.Cast<object?>().ToList();
            return true;
        }

        if (value is IEnumerable<JsonElement> jsonElements)
        {
            list = jsonElements.Cast<object?>().ToList();
            return true;
        }

        list = [];
        return false;
    }

    private static bool IsLargeValue(object? value)
    {
        return SerializeValue(value)?.Length > LargeValueThreshold;
    }

    private static string? SerializeValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text => text,
            JsonElement element => element.ToString(),
            _ => value.ToString()
        };
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "JSON conversion handles all primitive/container kinds explicitly for deterministic output.")]
    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertJsonValue(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}
