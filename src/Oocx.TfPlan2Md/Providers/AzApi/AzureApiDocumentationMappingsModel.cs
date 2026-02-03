using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Root model for Azure API documentation mappings JSON structure.
/// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
/// </summary>
internal sealed class AzureApiDocumentationMappingsModel
{
    /// <summary>
    /// Gets or sets the dictionary of resource type mappings.
    /// </summary>
    /// <remarks>
    /// Key: Azure resource type (e.g., "Microsoft.Compute/virtualMachines").
    /// Value: Mapping object containing URL.
    /// </remarks>
    [JsonPropertyName("mappings")]
    public Dictionary<string, ResourceTypeMapping> Mappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the metadata about the mappings file.
    /// </summary>
    [JsonPropertyName("metadata")]
    public MappingsMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Represents a single resource type mapping to its documentation URL.
/// </summary>
internal sealed class ResourceTypeMapping
{
    /// <summary>
    /// Gets or sets the official Microsoft Learn documentation URL for this resource type.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Metadata about the mappings data.
/// </summary>
internal sealed class MappingsMetadata
{
    /// <summary>
    /// Gets or sets the version of the mappings file.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last updated date in YYYY-MM-DD format.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source of the mappings.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}
