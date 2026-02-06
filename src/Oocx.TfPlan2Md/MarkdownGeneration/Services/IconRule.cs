using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Represents a single icon rule loaded from JSON configuration.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class IconRule
{
    /// <summary>
    /// Gets or sets the provider regex pattern, or null to match all providers.
    /// </summary>
    /// <value>The provider pattern.</value>
    [JsonPropertyName("providerPattern")]
    public string? ProviderPattern { get; set; }

    /// <summary>
    /// Gets or sets the resource type regex pattern, or null to match all resource types.
    /// </summary>
    /// <value>The resource type pattern.</value>
    [JsonPropertyName("resourceTypePattern")]
    public string? ResourceTypePattern { get; set; }

    /// <summary>
    /// Gets or sets the attribute name regex pattern, or null to match all attributes.
    /// </summary>
    /// <value>The attribute name pattern.</value>
    [JsonPropertyName("attributeNamePattern")]
    public string? AttributeNamePattern { get; set; }

    /// <summary>
    /// Gets or sets the value regex pattern, or null to match all values.
    /// </summary>
    /// <value>The value pattern.</value>
    [JsonPropertyName("valuePattern")]
    public string? ValuePattern { get; set; }

    /// <summary>
    /// Gets or sets the icon value to return when the rule matches.
    /// </summary>
    /// <value>The icon value.</value>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
