using System.Text;
using System.Text.Json;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Large value threshold for property values (in characters).
    /// Values exceeding this length are marked as large and rendered in collapsible sections.
    /// </summary>
    private const int LargeValueThreshold = 200;

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
    /// ]
    /// </example>
    public static ScriptArray FlattenJson(object? jsonObject, string prefix = "")
    {
        var result = new ScriptArray();

        if (jsonObject is null)
        {
            return result;
        }

        // Convert to JsonElement if needed
        JsonElement element;
        if (jsonObject is JsonElement jsonElement)
        {
            element = jsonElement;
        }
        else if (jsonObject is string jsonString)
        {
            try
            {
                element = JsonDocument.Parse(jsonString).RootElement;
            }
            catch
            {
                // If parsing fails, treat as a single value
                result.Add(CreatePropertyObject(prefix, jsonString));
                return result;
            }
        }
        else
        {
            // For non-JsonElement types that aren't strings, treat as primitive values
            // This avoids AOT-incompatible JsonSerializer.Serialize calls
            result.Add(CreatePropertyObject(prefix, jsonObject));
            return result;
        }

        // Flatten based on element type
        FlattenJsonElement(element, prefix, result);

        return result;
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
        var isLarge = serializedValue != null && serializedValue.Length > LargeValueThreshold;

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
    /// Parses an Azure resource type string into its components.
    /// </summary>
    /// <param name="resourceType">
    /// Azure resource type string in format: Microsoft.{Service}/{ResourceType}@{ApiVersion}
    /// </param>
    /// <returns>
    /// ScriptObject with properties: provider, service, resource_type, api_version.
    /// Returns empty values for invalid formats.
    /// </returns>
    /// <remarks>
    /// Extracts components from Azure resource type strings for display and documentation link generation.
    /// Example: "Microsoft.Automation/automationAccounts@2021-06-22" →
    /// { provider: "Microsoft.Automation", service: "Automation", resource_type: "automationAccounts", api_version: "2021-06-22" }
    /// </remarks>
    public static ScriptObject ParseAzureResourceType(string? resourceType)
    {
        var result = new ScriptObject
        {
            ["provider"] = string.Empty,
            ["service"] = string.Empty,
            ["resource_type"] = string.Empty,
            ["api_version"] = string.Empty
        };

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return result;
        }

        // Split by @ to separate API version
        var parts = resourceType.Split('@', 2);
        var typeAndVersion = parts[0];
        var apiVersion = parts.Length > 1 ? parts[1] : string.Empty;

        // Split by / to separate provider and resource type
        var typeParts = typeAndVersion.Split('/', 2);
        if (typeParts.Length < 2)
        {
            return result;
        }

        var provider = typeParts[0]; // e.g., "Microsoft.Automation"
        var resourceTypeName = typeParts[1]; // e.g., "automationAccounts"

        // Extract service name from provider (e.g., "Automation" from "Microsoft.Automation")
        var service = string.Empty;
        if (provider.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase))
        {
            service = provider.Substring("Microsoft.".Length);
        }

        result["provider"] = provider;
        result["service"] = service;
        result["resource_type"] = resourceTypeName;
        result["api_version"] = apiVersion;

        return result;
    }

    /// <summary>
    /// Constructs a best-effort Azure REST API documentation URL from a resource type string.
    /// </summary>
    /// <param name="resourceType">
    /// Azure resource type string (e.g., "Microsoft.Automation/automationAccounts@2021-06-22").
    /// </param>
    /// <returns>
    /// Documentation URL or null for non-Microsoft providers. Links use a heuristic pattern
    /// and may not always be accurate (best-effort approach).
    /// </returns>
    /// <remarks>
    /// Generates documentation links by converting service names to lowercase and resource types
    /// to kebab-case. The pattern follows: https://learn.microsoft.com/rest/api/{service}/{resource}/
    /// This is a best-effort heuristic as Azure documentation URLs don't follow a perfectly
    /// predictable pattern across all services.
    /// </remarks>
    /// <example>
    /// Input: "Microsoft.Automation/automationAccounts@2021-06-22"
    /// Output: "https://learn.microsoft.com/rest/api/automation/automation-accounts/"
    /// </example>
    public static string? AzureApiDocLink(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return null;
        }

        var parsed = ParseAzureResourceType(resourceType);
        var service = parsed["service"] as string;
        var resourceTypeName = parsed["resource_type"] as string;

        // Only generate links for Microsoft resource types
        if (string.IsNullOrEmpty(service) || string.IsNullOrEmpty(resourceTypeName))
        {
            return null;
        }

        // Convert service to lowercase
        var serviceLower = service.ToLowerInvariant();

        // Convert resource type to kebab-case
        var resourceKebab = ConvertToKebabCase(resourceTypeName);

        // Construct URL: https://learn.microsoft.com/rest/api/{service}/{resource}/
        return $"https://learn.microsoft.com/rest/api/{serviceLower}/{resourceKebab}/";
    }

    /// <summary>
    /// Converts a PascalCase or camelCase string to kebab-case.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>Kebab-case string (e.g., "automationAccounts" → "automation-accounts").</returns>
    private static string ConvertToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

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
        var allPaths = beforeFlattened.Keys.Union(afterFlattened.Keys).OrderBy(p => p).ToList();

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

    /// <summary>
    /// Extracts and formats key azapi_resource attributes for display in templates.
    /// </summary>
    /// <param name="change">The ResourceChangeModel containing azapi_resource data.</param>
    /// <returns>
    /// ScriptObject with formatted properties: name, type, parent_id, location, tags.
    /// Values are formatted with appropriate emoji and inline code formatting.
    /// </returns>
    /// <remarks>
    /// Extracts standard azapi_resource attributes and formats them for table display.
    /// Uses globe emoji for location, formats parent_id as "Resource Group `{name}`" when
    /// applicable, and wraps names in inline code. Handles missing optional attributes gracefully.
    /// </remarks>
    public static ScriptObject ExtractAzapiMetadata(object? change)
    {
        var result = new ScriptObject();

        if (change is not ResourceChangeModel resourceChange)
        {
            return result;
        }

        // Determine which state to extract from (after for create, before for delete)
        var state = resourceChange.Action == "delete" ? resourceChange.BeforeJson : resourceChange.AfterJson;

        if (state is null)
        {
            return result;
        }

        // Convert to dictionary for easier access
        var stateDict = ToDictionary(state);

        // Extract type
        if (stateDict.TryGetValue("type", out var typeValue))
        {
            result["type"] = $"`{typeValue}`";
        }

        // Extract name with inline code
        if (stateDict.TryGetValue("name", out var nameValue))
        {
            result["name"] = $"`{nameValue}`";
        }

        // Extract parent_id and format
        if (stateDict.TryGetValue("parent_id", out var parentId) && parentId != null)
        {
            var parentIdStr = parentId.ToString();
            if (!string.IsNullOrEmpty(parentIdStr))
            {
                // Parse parent_id as Azure scope
                var scopeInfo = Oocx.TfPlan2Md.Azure.AzureScopeParser.Parse(parentIdStr);
                result["parent_id"] = scopeInfo.Summary;
            }
        }

        // Extract location with globe emoji
        if (stateDict.TryGetValue("location", out var locationValue) && locationValue != null)
        {
            result["location"] = $"🌍 `{locationValue}`";
        }

        // Extract tags (will be handled separately by template)
        if (stateDict.TryGetValue("tags", out var tagsValue))
        {
            result["tags"] = tagsValue;
        }

        return result;
    }
}
