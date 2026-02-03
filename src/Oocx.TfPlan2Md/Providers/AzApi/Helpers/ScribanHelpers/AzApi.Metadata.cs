using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Platforms.Azure;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzApi;

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
    /// Extracts key azapi_resource attributes for display in templates.
    /// </summary>
    /// <param name="change">The ResourceChangeModel or mapped ScriptObject containing azapi_resource data.</param>
    /// <returns>
    /// ScriptObject with properties: name, type, parent_id, location, tags.
    /// Values are returned in raw form so templates can apply consistent semantic formatting.
    /// </returns>
    /// <remarks>
    /// Extracts standard azapi_resource attributes for table display.
    /// Formats parent_id into a human-readable scope summary when applicable, while leaving
    /// other values unformatted so templates can apply consistent semantic formatting.
    /// </remarks>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Keep metadata extraction flow readable while minimizing template complexity.")]
    public static ScriptObject ExtractAzapiMetadata(object? change)
    {
        var result = new ScriptObject();

        string? action;
        object? beforeState;
        object? afterState;

        if (change is ResourceChangeModel resourceChange)
        {
            action = resourceChange.Action;
            beforeState = resourceChange.BeforeJson;
            afterState = resourceChange.AfterJson;
        }
        else if (change is ScriptObject scriptChange)
        {
            action = scriptChange["action"]?.ToString();
            beforeState = scriptChange["before_json"];
            afterState = scriptChange["after_json"];
        }
        else
        {
            return result;
        }

        // Determine which state to extract from (after for create, before for delete)
        var state = action == "delete" ? beforeState : afterState;

        if (state is null)
        {
            return result;
        }

        // Convert to dictionary for easier access
        var stateDict = ToDictionary(state);

        // Extract type
        if (stateDict.TryGetValue("type", out var typeValue))
        {
            result["type"] = typeValue?.ToString();
        }

        // Extract name
        if (stateDict.TryGetValue("name", out var nameValue))
        {
            result["name"] = nameValue?.ToString();
        }

        // Extract parent_id and format
        if (stateDict.TryGetValue("parent_id", out var parentId) && parentId != null)
        {
            var parentIdStr = parentId.ToString();
            if (!string.IsNullOrEmpty(parentIdStr))
            {
                // Parse parent_id as Azure scope
                var scopeInfo = Platforms.Azure.AzureScopeParser.Parse(parentIdStr);
                result["parent_id"] = scopeInfo.Summary;
            }
        }

        // Extract location
        if (stateDict.TryGetValue("location", out var locationValue) && locationValue != null)
        {
            result["location"] = locationValue.ToString();
        }

        // Extract tags (will be handled separately by template)
        if (stateDict.TryGetValue("tags", out var tagsValue))
        {
            result["tags"] = tagsValue;
        }

        return result;
    }
}
