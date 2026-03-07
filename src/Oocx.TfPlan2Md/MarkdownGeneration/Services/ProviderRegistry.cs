using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Central registry for explicitly registering and managing Terraform provider modules.
/// </summary>
/// <remarks>
/// This registry enables explicit, AOT-compatible provider registration without reflection.
/// All provider modules must be registered manually in the application startup code.
/// Providers can implement the narrow IProvider interface plus any optional capability interfaces
/// (IValueFormatterProvider, IIconRegistrationProvider, etc.) to contribute functionality.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal sealed class ProviderRegistry
{
    private readonly List<IProvider> _providers = new();

    /// <summary>
    /// Registers a provider.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    public void RegisterProvider(IProvider provider)
    {
        _providers.Add(provider);
    }

    /// <summary>
    /// Gets all registered provider modules.
    /// </summary>
    /// <returns>Read-only list of registered providers.</returns>
    public IReadOnlyList<IProvider> GetProviders()
    {
        return _providers.AsReadOnly();
    }

    /// <summary>
    /// Creates a centralized provider contribution set from all registered providers.
    /// </summary>
    /// <returns>A provider contribution set aggregating all optional provider capabilities.</returns>
    public ProviderContributionSet CreateContributionSet()
    {
        return new ProviderContributionSet(_providers.AsReadOnly());
    }

    /// <summary>
    /// Gets all template resource prefixes from registered providers.
    /// </summary>
    /// <returns>Enumerable of template resource prefixes.</returns>
    public IEnumerable<string> GetTemplateResourcePrefixes()
    {
        return _providers.Select(p => p.TemplateResourcePrefix);
    }
}
