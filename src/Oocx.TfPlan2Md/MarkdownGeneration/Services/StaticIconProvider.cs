using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Returns a fixed icon for any matching resolution context.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class StaticIconProvider : IIconProvider
{
    /// <summary>
    /// The icon to return when the provider is matched.
    /// </summary>
    private readonly string _icon;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticIconProvider"/> class.
    /// </summary>
    /// <param name="icon">The icon to return for matching contexts.</param>
    public StaticIconProvider(string icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            throw new ArgumentException("Icon must be a non-empty string.", nameof(icon));
        }

        _icon = icon;
    }

    /// <summary>
    /// Returns the configured icon for any matching context.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>The configured icon.</returns>
    public string? TryGetIcon(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _icon;
    }
}
