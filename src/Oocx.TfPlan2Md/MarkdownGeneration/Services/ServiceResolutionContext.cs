namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Carries provider, resource, attribute, and value data for service resolution.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class ServiceResolutionContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceResolutionContext"/> class.
    /// </summary>
    /// <param name="providerName">The provider name associated with the value being resolved.</param>
    /// <param name="resourceType">The Terraform resource type (if available).</param>
    /// <param name="attributeName">The attribute name (if available).</param>
    /// <param name="value">The attribute value (if available).</param>
    public ServiceResolutionContext(string? providerName, string? resourceType, string? attributeName, string? value)
    {
        ProviderName = providerName;
        ResourceType = resourceType;
        AttributeName = attributeName;
        Value = value;
    }

    /// <summary>
    /// Gets the provider name associated with the resolution request.
    /// </summary>
    /// <value>The provider name or null when not available.</value>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets the resource type associated with the resolution request.
    /// </summary>
    /// <value>The resource type or null when not available.</value>
    public string? ResourceType { get; }

    /// <summary>
    /// Gets the attribute name associated with the resolution request.
    /// </summary>
    /// <value>The attribute name or null when not available.</value>
    public string? AttributeName { get; }

    /// <summary>
    /// Gets the value associated with the resolution request.
    /// </summary>
    /// <value>The value or null when not available.</value>
    public string? Value { get; }
}
