using System.Collections.Generic;
using System.Linq;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers;

/// <summary>
/// Central registry for explicitly registering and managing Terraform provider modules.
/// </summary>
/// <remarks>
/// This registry enables explicit, AOT-compatible provider registration without reflection.
/// All provider modules must be registered manually in the application startup code.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal sealed class ProviderRegistry
{
    private readonly List<IProviderModule> _providers = new();

    /// <summary>
    /// Registers a provider module.
    /// </summary>
    /// <param name="provider">The provider module to register.</param>
    public void RegisterProvider(IProviderModule provider)
    {
        _providers.Add(provider);
    }

    /// <summary>
    /// Gets all registered provider modules.
    /// </summary>
    /// <returns>Read-only list of registered providers.</returns>
    public IReadOnlyList<IProviderModule> GetProviders()
    {
        return _providers.AsReadOnly();
    }

    /// <summary>
    /// Gets all template resource prefixes from registered providers.
    /// </summary>
    /// <returns>Enumerable of template resource prefixes.</returns>
    public IEnumerable<string> GetTemplateResourcePrefixes()
    {
        return _providers.Select(p => p.TemplateResourcePrefix);
    }

    /// <summary>
    /// Registers all provider-specific Scriban helper functions into the script object.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register functions into.</param>
    public void RegisterAllHelpers(ScriptObject scriptObject)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterHelpers(scriptObject);
        }
    }

    /// <summary>
    /// Registers all provider-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register into.</param>
    public void RegisterAllFactories(MarkdownGeneration.Models.IResourceViewModelFactoryRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterFactories(registry);
        }
    }
}
