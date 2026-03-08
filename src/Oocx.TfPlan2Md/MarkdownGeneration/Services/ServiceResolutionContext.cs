namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Carries provider, resource, attribute, and value data for service resolution.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
/// <param name="ProviderName">The provider name associated with the value being resolved.</param>
/// <param name="ResourceType">The Terraform resource type (if available).</param>
/// <param name="AttributeName">The attribute name (if available).</param>
/// <param name="Value">The attribute value (if available).</param>
internal sealed record ServiceResolutionContext(
    string? ProviderName,
    string? ResourceType,
    string? AttributeName,
    string? Value);
