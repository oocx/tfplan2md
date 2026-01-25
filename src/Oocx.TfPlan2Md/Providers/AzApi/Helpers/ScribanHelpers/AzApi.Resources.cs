using Scriban.Runtime;

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
    /// Gets the official Azure REST API documentation URL for a resource type.
    /// </summary>
    /// <param name="resourceType">
    /// Azure resource type string (e.g., "Microsoft.Automation/automationAccounts@2021-06-22").
    /// </param>
    /// <returns>
    /// Official documentation URL from curated mappings, or null if no mapping exists.
    /// </returns>
    /// <remarks>
    /// Uses curated mappings from Microsoft Learn. Only returns URLs for known resource types.
    /// API version suffixes are ignored when looking up mappings.
    /// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Known resource type with version
    /// var url = AzureApiDocLink("Microsoft.Compute/virtualMachines@2023-03-01");
    /// // Returns: "https://learn.microsoft.com/rest/api/compute/virtual-machines"
    /// 
    /// // Unknown resource type
    /// var url = AzureApiDocLink("Microsoft.UnknownService/unknownResource");
    /// // Returns: null
    /// </code>
    /// </example>
    public static string? AzureApiDocLink(string? resourceType)
    {
        return AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);
    }
}
