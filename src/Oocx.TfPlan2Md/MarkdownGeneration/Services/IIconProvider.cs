namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Provides icons for attribute values based on provider-aware context.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal interface IIconProvider
{
    /// <summary>
    /// Attempts to resolve an icon for the given resolution context.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>An icon string when handled; otherwise null to allow fallback.</returns>
    string? TryGetIcon(ServiceResolutionContext context);
}
