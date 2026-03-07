using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Resolves and executes icon providers in specificity order.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class IconProviderRegistry
{
    /// <summary>
    /// The underlying pattern-matching registry for icon providers.
    /// </summary>
    private readonly PatternMatchingRegistry<IIconProvider> _registry = new();

    /// <summary>
    /// Registers an icon provider with its match pattern.
    /// </summary>
    /// <param name="pattern">The match pattern that determines applicability.</param>
    /// <param name="provider">The provider to register.</param>
    public void Register(MatchPattern pattern, IIconProvider provider)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(provider);

        _registry.Register(pattern, provider);
    }

    /// <summary>
    /// Attempts to resolve an icon by returning the first non-null result from registered providers.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>An icon string when handled; otherwise null.</returns>
    public string? TryGetIcon(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _registry.TryResolveFirst(context, static (provider, ctx) => provider.TryGetIcon(ctx));
    }
}
