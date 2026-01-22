using System.Text;
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
    /// Azure resource type string in format: Microsoft.{Service}/{ResourceType}@{ApiVersion}.
    /// </param>
    /// <returns>
    /// ScriptObject with properties: provider, service, resource_type, api_version.
    /// Returns empty values for invalid formats.
    /// </returns>
    /// <remarks>
    /// Extracts components from Azure resource type strings for display and documentation link generation.
    /// Example: "Microsoft.Automation/automationAccounts@2021-06-22" →
    /// { provider: "Microsoft.Automation", service: "Automation", resource_type: "automationAccounts", api_version: "2021-06-22" }.
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
    /// Input: "Microsoft.Automation/automationAccounts@2021-06-22".
    /// Output: "https://learn.microsoft.com/rest/api/automation/automation-accounts/".
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

    /// <summary>
    /// Renders azapi_resource body content as formatted markdown with proper handling of large values.
    /// </summary>
    /// <param name="bodyJson">The JSON body object to render.</param>
    /// <param name="heading">The heading text (e.g., "Body", "Body Changes").</param>
    /// <param name="mode">The rendering mode: "create", "update", or "delete".</param>
    /// <param name="beforeJson">The before state JSON (for update mode).</param>
    /// <param name="beforeSensitive">The before_sensitive structure (for update mode).</param>
    /// <param name="afterSensitive">The after_sensitive structure (for update mode).</param>
    /// <param name="showUnchanged">Whether to show unchanged properties (for update mode).</param>
    /// <param name="largeValueFormat">Format for rendering large values ("inline-diff" or "simple-diff").</param>
    /// <returns>Formatted markdown string for the body section.</returns>
    /// <remarks>
    /// This helper consolidates body rendering logic to keep the template concise.
    /// For create/delete modes, it flattens the body and separates small vs. large properties.
    /// For update mode, it compares before/after and shows only changed properties.
    /// Large properties are rendered outside of tables to avoid markdown parsing issues with newlines.
    /// Related feature: docs/features/040-azapi-resource-template/specification.md.
    /// </remarks>
    public static string RenderAzapiBody(
        object? bodyJson,
        string heading,
        string mode,
        object? beforeJson = null,
        object? beforeSensitive = null,
        object? afterSensitive = null,
        bool showUnchanged = false,
        string largeValueFormat = "inline-diff")
    {
        if (bodyJson is null)
        {
            return $"*{heading}: (empty)*\n";
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"#### {heading}");
        sb.AppendLine();

        if (mode == "update" && beforeJson is not null)
        {
            // Update mode: compare before/after and show changes
            var comparisons = CompareJsonProperties(beforeJson, bodyJson, beforeSensitive, afterSensitive, showUnchanged, false);
            var smallChanges = new ScriptArray();
            var largeChanges = new ScriptArray();

            foreach (var item in comparisons)
            {
                if (item is ScriptObject scriptObj && scriptObj["is_large"] is bool isLarge)
                {
                    if (isLarge)
                    {
                        largeChanges.Add(scriptObj);
                    }
                    else
                    {
                        smallChanges.Add(scriptObj);
                    }
                }
            }

            // Group properties by their nested object parent (for properties with >3 attributes)
            var groupedProps = GroupPropertiesByNestedObject(smallChanges);

            // Render main table with root-level and ungrouped properties
            if (groupedProps.MainProps.Count > 0)
            {
                sb.AppendLine("| Property | Before | After |");
                sb.AppendLine("|----------|--------|-------|");

                foreach (var item in groupedProps.MainProps)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var before = prop["before"];
                        var after = prop["after"];

                        // Remove "properties." prefix if present
                        path = RemovePropertiesPrefix(path);

                        var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), "azurerm");
                        var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), "azurerm");

                        sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
                    }
                }

                sb.AppendLine();
            }

            // Render separate tables for nested objects with >3 attributes
            foreach (var group in groupedProps.NestedGroups)
            {
                sb.AppendLine($"###### {heading} - `{group.Key}`");
                sb.AppendLine();
                sb.AppendLine("| Property | Before | After |");
                sb.AppendLine("|----------|--------|-------|");

                foreach (var item in group.Value)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var before = prop["before"];
                        var after = prop["after"];

                        // Remove the parent prefix and "properties." prefix
                        path = RemoveNestedPrefix(path, group.Key);

                        var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), "azurerm");
                        var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), "azurerm");

                        sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
                    }
                }

                sb.AppendLine();
            }

            if (largeChanges.Count > 0)
            {
                sb.AppendLine("<details>");
                sb.AppendLine("<summary>Large body property changes</summary>");
                sb.AppendLine();

                foreach (var item in largeChanges)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var before = prop["before"];
                        var after = prop["after"];

                        // Remove "properties." prefix
                        path = RemovePropertiesPrefix(path);

                        sb.AppendLine($"##### **{EscapeMarkdown(path)}:**");
                        sb.AppendLine();

                        var beforeStr = before?.ToString();
                        var afterStr = after?.ToString();
                        sb.AppendLine(FormatLargeValue(beforeStr, afterStr, largeValueFormat));
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            if (smallChanges.Count == 0 && largeChanges.Count == 0)
            {
                sb.AppendLine("*No body changes detected*");
                sb.AppendLine();
            }
        }
        else
        {
            // Create/delete mode: flatten and display body properties
            var flattened = FlattenJson(bodyJson, string.Empty);
            var smallProps = new ScriptArray();
            var largeProps = new ScriptArray();

            foreach (var item in flattened)
            {
                if (item is ScriptObject scriptObj && scriptObj["is_large"] is bool isLarge)
                {
                    if (isLarge)
                    {
                        largeProps.Add(scriptObj);
                    }
                    else
                    {
                        smallProps.Add(scriptObj);
                    }
                }
            }

            // Group properties by their nested object parent (for properties with >3 attributes)
            var groupedProps = GroupPropertiesByNestedObject(smallProps);

            // Render main table with root-level and ungrouped properties
            if (groupedProps.MainProps.Count > 0 || groupedProps.NestedGroups.Count == 0)
            {
                sb.AppendLine("| Property | Value |");
                sb.AppendLine("|----------|-------|");

                foreach (var item in groupedProps.MainProps)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var value = prop["value"];

                        // Remove "properties." prefix if present
                        path = RemovePropertiesPrefix(path);

                        var valueFormatted = FormatAttributeValueTable(path, value?.ToString(), "azurerm");
                        sb.AppendLine($"| {EscapeMarkdown(path)} | {valueFormatted} |");
                    }
                }

                sb.AppendLine();
            }

            // Render separate tables for nested objects with >3 attributes
            foreach (var group in groupedProps.NestedGroups)
            {
                sb.AppendLine($"###### {heading} - `{group.Key}`");
                sb.AppendLine();
                sb.AppendLine("| Property | Value |");
                sb.AppendLine("|----------|-------|");

                foreach (var item in group.Value)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var value = prop["value"];

                        // Remove the parent prefix and "properties." prefix
                        path = RemoveNestedPrefix(path, group.Key);

                        var valueFormatted = FormatAttributeValueTable(path, value?.ToString(), "azurerm");
                        sb.AppendLine($"| {EscapeMarkdown(path)} | {valueFormatted} |");
                    }
                }

                sb.AppendLine();
            }

            if (largeProps.Count > 0)
            {
                sb.AppendLine("<details>");
                sb.AppendLine("<summary>Large body properties</summary>");
                sb.AppendLine();

                foreach (var item in largeProps)
                {
                    if (item is ScriptObject prop)
                    {
                        var path = prop["path"]?.ToString() ?? string.Empty;
                        var value = prop["value"];

                        // Remove "properties." prefix
                        path = RemovePropertiesPrefix(path);

                        sb.AppendLine($"##### **{EscapeMarkdown(path)}:**");
                        sb.AppendLine();

                        var valueStr = value?.ToString();
                        sb.AppendLine(FormatLargeValue(null, valueStr, largeValueFormat));
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            if (smallProps.Count == 0 && largeProps.Count == 0)
            {
                sb.AppendLine($"*{heading}: (empty)*");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes the "properties." prefix from a property path if present.
    /// </summary>
    /// <param name="path">The property path.</param>
    /// <returns>Path with "properties." prefix removed.</returns>
    private static string RemovePropertiesPrefix(string path)
    {
        if (path.StartsWith("properties.", StringComparison.Ordinal))
        {
            return path.Substring("properties.".Length);
        }
        return path;
    }

    /// <summary>
    /// Removes the nested object prefix and "properties." prefix from a property path.
    /// </summary>
    /// <param name="path">The full property path.</param>
    /// <param name="parentPath">The parent path to remove.</param>
    /// <returns>Path with parent and "properties." prefixes removed.</returns>
    private static string RemoveNestedPrefix(string path, string parentPath)
    {
        // First remove "properties." if present
        path = RemovePropertiesPrefix(path);
        parentPath = RemovePropertiesPrefix(parentPath);

        // Then remove the parent path
        if (path.StartsWith(parentPath + ".", StringComparison.Ordinal))
        {
            return path.Substring(parentPath.Length + 1);
        }
        return path;
    }

    /// <summary>
    /// Groups properties by their nested object parent if the parent has more than 3 attributes.
    /// </summary>
    /// <param name="properties">The list of properties to group.</param>
    /// <returns>A tuple with main properties and nested groups.</returns>
    private static (ScriptArray MainProps, Dictionary<string, ScriptArray> NestedGroups) GroupPropertiesByNestedObject(ScriptArray properties)
    {
        var mainProps = new ScriptArray();
        var nestedGroups = new Dictionary<string, ScriptArray>();

        // First pass: identify nested objects with >3 attributes
        var nestedObjectCounts = new Dictionary<string, int>();

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;

                // Remove "properties." prefix for analysis
                path = RemovePropertiesPrefix(path);

                // Check if this is a nested property (has at least 2 segments)
                var segments = path.Split('.');
                if (segments.Length >= 2)
                {
                    // For deeply nested properties, only consider the first level
                    // (e.g., for "siteConfig.connectionStrings[0].name", parent is "siteConfig")
                    var firstLevelParent = segments[0];
                    if (firstLevelParent.Contains('['))
                    {
                        // Skip array indices
                        continue;
                    }

                    if (!nestedObjectCounts.TryGetValue(firstLevelParent, out var count))
                    {
                        count = 0;
                    }
                    nestedObjectCounts[firstLevelParent] = count + 1;
                }
            }
        }

        // Identify which nested objects should be grouped (>3 attributes)
        var nestedObjectsToGroup = nestedObjectCounts
            .Where(kvp => kvp.Value > 3)
            .Select(kvp => kvp.Key)
            .ToHashSet();

        // Second pass: assign properties to main or nested groups
        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var pathWithoutProperties = RemovePropertiesPrefix(path);
                var segments = pathWithoutProperties.Split('.');

                if (segments.Length >= 2)
                {
                    var firstLevelParent = segments[0];
                    if (!firstLevelParent.Contains('[') && nestedObjectsToGroup.Contains(firstLevelParent))
                    {
                        // This property belongs to a nested group
                        if (!nestedGroups.TryGetValue(firstLevelParent, out var groupArray))
                        {
                            groupArray = new ScriptArray();
                            nestedGroups[firstLevelParent] = groupArray;
                        }
                        groupArray.Add(item);
                        continue;
                    }
                }

                // Property stays in main table
                mainProps.Add(item);
            }
        }

        return (mainProps, nestedGroups);
    }
}
