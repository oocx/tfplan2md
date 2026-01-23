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
