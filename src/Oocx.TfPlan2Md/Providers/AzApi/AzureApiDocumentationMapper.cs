namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Maps Azure resource types to their official REST API documentation URLs.
/// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
/// </summary>
/// <remarks>
/// This mapper loads curated mappings from an embedded JSON file and provides O(1) lookups
/// using a FrozenDictionary. The mappings are version-agnostic; API version suffixes
/// (e.g., "@2023-03-01") are automatically stripped before lookup.
/// 
/// Mappings are derived from official Microsoft Learn REST API documentation and are
/// maintained via the update script: scripts/update-azure-api-mappings.py.
/// </remarks>
public static partial class AzureApiDocumentationMapper
{
    /// <summary>
    /// Gets the official REST API documentation URL for the specified Azure resource type.
    /// </summary>
    /// <param name="resourceType">
    /// Azure resource type string (e.g., "Microsoft.Compute/virtualMachines" or
    /// "Microsoft.Compute/virtualMachines@2023-03-01"). API version suffix is automatically
    /// stripped before lookup.
    /// </param>
    /// <returns>
    /// The official Microsoft Learn documentation URL if a mapping exists; otherwise null.
    /// </returns>
    /// <remarks>
    /// Only returns URLs for resource types that have been explicitly mapped. No heuristic
    /// URL construction is performed. This ensures all returned links are valid and accurate.
    /// 
    /// Lookups are case-insensitive to handle variations in resource type casing.
    /// </remarks>
    /// <example>
    /// <code>
    /// // With API version
    /// var url = GetDocumentationUrl("Microsoft.Compute/virtualMachines@2023-03-01");
    /// // Returns: "https://learn.microsoft.com/rest/api/compute/virtual-machines"
    /// 
    /// // Without API version
    /// var url = GetDocumentationUrl("Microsoft.Storage/storageAccounts");
    /// // Returns: "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"
    /// 
    /// // Unknown resource type
    /// var url = GetDocumentationUrl("Microsoft.UnknownService/unknownResource");
    /// // Returns: null
    /// </code>
    /// </example>
    public static string? GetDocumentationUrl(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return null;
        }

        // Strip API version suffix (e.g., "@2023-03-01")
        var typeWithoutVersion = resourceType.Split('@', 2)[0];

        return Mappings.TryGetValue(typeWithoutVersion, out var url) ? url : null;
    }
}
