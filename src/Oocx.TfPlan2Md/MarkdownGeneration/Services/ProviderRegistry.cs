using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

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

    /// <summary>
    /// Registers all provider-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register into.</param>
    public void RegisterAllValueFormatters(ValueFormatterRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterValueFormatters(registry);
        }
    }

    /// <summary>
    /// Registers all provider-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register into.</param>
    public void RegisterAllIconProviders(IconProviderRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterIconProviders(registry);
        }
    }

    /// <summary>
    /// Registers all provider-specific parent-child resource relationships.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register into.</param>
    public void RegisterAllParentChildRelationships(MarkdownGeneration.Models.IParentChildRelationshipRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterParentChildRelationships(registry);
        }
    }

    /// <summary>
    /// Registers all provider-specific post-merge callbacks with the report model builder.
    /// </summary>
    /// <param name="builder">The report model builder to register callbacks with.</param>
    /// <remarks>
    /// Invoked during builder construction to allow providers to hook into the build pipeline.
    /// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
    /// </remarks>
    public void RegisterAllPostMergeCallbacks(ReportModelBuilder builder)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterPostMergeCallbacks(builder);
        }
    }

    /// <summary>
    /// Registers all provider-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">The resource renderer registry to register into.</param>
    public void RegisterAllResourceRenderers(ResourceRendererRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterResourceRenderers(registry);
        }
    }

    /// <summary>
    /// Registers all provider-specific attribute change filters.
    /// </summary>
    /// <param name="registry">The attribute change filter registry to register into.</param>
    /// <remarks>
    /// Mirrors the existing <see cref="RegisterAllValueFormatters"/> pattern.
    /// Each provider may register zero or more filters; providers without overrides use
    /// the default no-op implementation on <see cref="IProviderModule"/>.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </remarks>
    public void RegisterAllAttributeChangeFilters(AttributeChangeFilterRegistry registry)
    {
        foreach (var provider in _providers)
        {
            provider.RegisterAttributeChangeFilters(registry);
        }
    }
}
