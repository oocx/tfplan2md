using System.Text;
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
}
