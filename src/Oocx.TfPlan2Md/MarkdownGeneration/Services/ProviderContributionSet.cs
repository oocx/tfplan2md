using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Aggregates provider capabilities into a single object graph consumed by composition code.
/// </summary>
/// <remarks>
/// This type centralizes provider capability consumption so callers do not need to manually
/// coordinate each capability-specific registry.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal sealed class ProviderContributionSet(IReadOnlyList<IProvider> providers)
{
    /// <summary>
    /// Gets the registered providers contributing to this set.
    /// </summary>
    public IReadOnlyList<IProvider> Providers { get; } = providers;

    /// <summary>
    /// Registers all provider-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to populate.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        foreach (var provider in Providers)
        {
            provider.RegisterFactories(registry);
        }
    }

    /// <summary>
    /// Creates a populated value formatter registry from all provider contributions.
    /// </summary>
    /// <returns>The populated value formatter registry.</returns>
    public ValueFormatterRegistry CreateValueFormatterRegistry()
    {
        var registry = new ValueFormatterRegistry();

        foreach (var provider in Providers)
        {
            if (provider is IValueFormatterProvider valueFormatterProvider)
            {
                valueFormatterProvider.RegisterValueFormatters(registry);
            }
        }

        return registry;
    }

    /// <summary>
    /// Creates a populated icon provider registry from all provider contributions.
    /// </summary>
    /// <returns>The populated icon provider registry.</returns>
    public IconProviderRegistry CreateIconProviderRegistry()
    {
        var registry = new IconProviderRegistry();

        foreach (var provider in Providers)
        {
            if (provider is IIconRegistrationProvider iconProvider)
            {
                iconProvider.RegisterIconProviders(registry);
            }
        }

        return registry;
    }

    /// <summary>
    /// Creates a populated parent-child relationship registry from all provider contributions.
    /// </summary>
    /// <returns>The populated parent-child relationship registry.</returns>
    public ParentChildRelationshipRegistry CreateParentChildRelationshipRegistry()
    {
        var registry = new ParentChildRelationshipRegistry();

        foreach (var provider in Providers)
        {
            if (provider is IParentChildRelationshipProvider relationshipProvider)
            {
                relationshipProvider.RegisterParentChildRelationships(registry);
            }
        }

        return registry;
    }

    /// <summary>
    /// Creates a populated attribute change filter registry from all provider contributions.
    /// </summary>
    /// <returns>The populated attribute change filter registry.</returns>
    public AttributeChangeFilterRegistry CreateAttributeChangeFilterRegistry()
    {
        var registry = new AttributeChangeFilterRegistry();

        foreach (var provider in Providers)
        {
            if (provider is IAttributeChangeFilterProvider filterProvider)
            {
                filterProvider.RegisterAttributeChangeFilters(registry);
            }
        }

        return registry;
    }

    /// <summary>
    /// Creates a populated resource renderer registry from all provider contributions.
    /// </summary>
    /// <returns>The populated resource renderer registry.</returns>
    public ResourceRendererRegistry CreateResourceRendererRegistry()
    {
        var registry = new ResourceRendererRegistry();

        foreach (var provider in Providers)
        {
            if (provider is IResourceRendererProvider rendererProvider)
            {
                rendererProvider.RegisterResourceRenderers(registry);
            }
        }

        return registry;
    }

    /// <summary>
    /// Registers post-merge callbacks contributed by providers.
    /// </summary>
    /// <param name="builder">The report model builder receiving callbacks.</param>
    public void RegisterPostMergeCallbacks(ReportModelBuilder builder)
    {
        foreach (var provider in Providers)
        {
            if (provider is IPostMergeCallbackProvider callbackProvider)
            {
                callbackProvider.RegisterPostMergeCallbacks(builder);
            }
        }
    }
}
