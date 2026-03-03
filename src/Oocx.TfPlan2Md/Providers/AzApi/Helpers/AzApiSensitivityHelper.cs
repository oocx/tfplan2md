using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Utilities for flattening and resolving AzAPI sensitivity paths.
/// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Sensitivity traversal supports multiple state shapes and JSON carriers used by Terraform plans.")]
internal static class AzApiSensitivityHelper
{
    /// <summary>
    /// Flattens a Terraform sensitivity structure into path markers.
    /// </summary>
    /// <param name="value">Sensitivity structure.</param>
    /// <returns>Set of sensitive flattened paths.</returns>
    internal static HashSet<string> Flatten(object? value)
    {
        HashSet<string> sensitivePaths = [];

        if (value is null)
        {
            return sensitivePaths;
        }

        Traverse(value, string.Empty, sensitivePaths);
        return sensitivePaths;
    }

    /// <summary>
    /// Checks whether a flattened path should be treated as sensitive.
    /// </summary>
    /// <param name="path">Flattened property path.</param>
    /// <param name="sensitivePaths">Sensitive path markers.</param>
    /// <returns><c>true</c> when the value must be masked.</returns>
    internal static bool IsPathSensitive(string path, HashSet<string> sensitivePaths)
    {
        if (sensitivePaths.Count == 0)
        {
            return false;
        }

        if (sensitivePaths.Contains(path) || sensitivePaths.Contains(string.Empty))
        {
            return true;
        }

        var dotIndex = path.LastIndexOf('.');
        while (dotIndex > 0)
        {
            var parentPath = path[..dotIndex];
            if (sensitivePaths.Contains(parentPath))
            {
                return true;
            }

            dotIndex = parentPath.LastIndexOf('.');
        }

        return false;
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Traversal branches are required to handle all supported sensitivity node types.")]
    private static void Traverse(object value, string prefix, HashSet<string> sensitivePaths)
    {
        if (value is bool boolean)
        {
            if (boolean)
            {
                sensitivePaths.Add(prefix);
            }

            return;
        }

        if (value is JsonElement element)
        {
            TraverseJsonElement(element, prefix, sensitivePaths);
            return;
        }

        if (value is IReadOnlyDictionary<string, object?> readonlyMap)
        {
            foreach (var (key, nestedValue) in readonlyMap)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                if (nestedValue is not null)
                {
                    Traverse(nestedValue, path, sensitivePaths);
                }
            }

            return;
        }

        if (value is IDictionary<string, object?> map)
        {
            foreach (var (key, nestedValue) in map)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                if (nestedValue is not null)
                {
                    Traverse(nestedValue, path, sensitivePaths);
                }
            }

            return;
        }

        if (value is IEnumerable<object?> list)
        {
            var index = 0;
            foreach (var item in list)
            {
                var path = $"{prefix}[{index}]";
                if (item is not null)
                {
                    Traverse(item, path, sensitivePaths);
                }

                index++;
            }

            return;
        }

        if (value is string jsonText)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonText);
                TraverseJsonElement(document.RootElement, prefix, sensitivePaths);
            }
            catch
            {
                // Ignore malformed sensitivity payloads.
            }
        }
    }

    private static void TraverseJsonElement(JsonElement element, string prefix, HashSet<string> sensitivePaths)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                sensitivePaths.Add(prefix);
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var path = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    TraverseJsonElement(property.Value, path, sensitivePaths);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    TraverseJsonElement(item, $"{prefix}[{index}]", sensitivePaths);
                    index++;
                }

                break;
        }
    }
}
