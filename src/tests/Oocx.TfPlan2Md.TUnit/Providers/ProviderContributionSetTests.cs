using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Providers;

/// <summary>
/// Tests for the centralized provider-contribution model.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
public class ProviderContributionSetTests
{
    private const string SyntheticProviderName = "synthetic";

    [Test]
    public void CreateContributionSet_RegistersAllCapabilityTypesAtOnce()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new SyntheticProvider());
        var contributions = registry.CreateContributionSet();

        var factoryRegistry = new ResourceViewModelFactoryRegistry();
        contributions.RegisterFactories(factoryRegistry);
        factoryRegistry.TryGetFactory(SyntheticProvider.ResourceType, out var factory).Should().BeTrue();
        factory.Should().NotBeNull();

        var formatterRegistry = contributions.CreateValueFormatterRegistry();
        formatterRegistry.TryFormat(new ServiceResolutionContext(SyntheticProviderName, SyntheticProvider.ResourceType, "name", "value"))
            .Should().Be("formatted-value");

        var iconRegistry = contributions.CreateIconProviderRegistry();
        iconRegistry.TryGetIcon(new ServiceResolutionContext(SyntheticProviderName, SyntheticProvider.ResourceType, null, null))
            .Should().Be(":sparkles:");

        var relationshipRegistry = contributions.CreateParentChildRelationshipRegistry();
        relationshipRegistry.IsChildResourceType(SyntheticProvider.ChildType).Should().BeTrue();

        var filterRegistry = contributions.CreateAttributeChangeFilterRegistry();
        filterRegistry.ShouldSuppress(new AttributeChangeFilterContext(SyntheticProviderName, "ignored", "before", "after"))
            .Should().BeTrue();

        var rendererRegistry = contributions.CreateResourceRendererRegistry();
        rendererRegistry.GetRenderer(SyntheticProvider.ResourceType).Should().BeOfType<SyntheticRenderer>();
    }

    [Test]
    public void CreateContributionSet_CreatesNonNullRegistriesForRealProviders()
    {
        var registry = new ProviderRegistry();
        registry.RegisterProvider(new Oocx.TfPlan2Md.Providers.AzApi.AzApiModule());
        registry.RegisterProvider(new Oocx.TfPlan2Md.Providers.AzureAD.AzureADModule());
        registry.RegisterProvider(new Oocx.TfPlan2Md.Providers.AzureDevOps.AzureDevOpsModule());
        registry.RegisterProvider(new Oocx.TfPlan2Md.Providers.AzureRM.AzureRMModule(LargeValueFormat.InlineDiff, new NullPrincipalMapper()));

        var contributions = registry.CreateContributionSet();

        contributions.CreateValueFormatterRegistry().Should().NotBeNull();
        contributions.CreateIconProviderRegistry().Should().NotBeNull();
        contributions.CreateParentChildRelationshipRegistry().Should().NotBeNull();
        contributions.CreateAttributeChangeFilterRegistry().Should().NotBeNull();
        contributions.CreateResourceRendererRegistry().Should().NotBeNull();
    }

    private sealed class SyntheticProvider : IProvider, IValueFormatterProvider, IIconRegistrationProvider, IParentChildRelationshipProvider, IAttributeChangeFilterProvider, IResourceRendererProvider
    {
        internal const string ResourceType = "synthetic_resource";
        internal const string ChildType = "synthetic_child";

        public string ProviderName => SyntheticProviderName;

        public string TemplateResourcePrefix => "Synthetic.";

        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
            registry.RegisterFactory(ResourceType, new SyntheticFactory());
        }

        public void RegisterValueFormatters(ValueFormatterRegistry registry)
        {
            registry.Register(new MatchPattern(SyntheticProviderName, ResourceType, "name", ".*"), new SyntheticFormatter());
        }

        public void RegisterIconProviders(IconProviderRegistry registry)
        {
            registry.Register(new MatchPattern(SyntheticProviderName, ResourceType, null, null), new SyntheticIconProvider());
        }

        public void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
        {
            registry.Register(new ParentChildRelationship
            {
                ParentResourceType = ResourceType,
                ChildResourceType = ChildType,
                ChildReferenceAttribute = "parent_id",
                ParentIdAttribute = "id",
                ChildGroupLabel = "Children",
                TableColumns = [new ChildTableColumn("Name", "name")],
                RowExtractor = new SyntheticChildRowExtractor()
            });
        }

        public void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
        {
            registry.Register(new SyntheticFilter());
        }

        public void RegisterResourceRenderers(ResourceRendererRegistry registry)
        {
            registry.Register(new SyntheticRenderer());
        }
    }

    private sealed class SyntheticFactory : IResourceViewModelFactory;

    private sealed class SyntheticFormatter : IValueFormatter
    {
        public string? TryFormat(ServiceResolutionContext context)
        {
            return context.ProviderName == SyntheticProviderName ? "formatted-value" : null;
        }
    }

    private sealed class SyntheticIconProvider : IIconProvider
    {
        public string? TryGetIcon(ServiceResolutionContext context)
        {
            return context.ProviderName == SyntheticProviderName ? ":sparkles:" : null;
        }
    }

    private sealed class SyntheticFilter : IAttributeChangeFilter
    {
        public bool ShouldSuppress(AttributeChangeFilterContext context)
        {
            return context.ProviderName == SyntheticProviderName && context.AttributeName == "ignored";
        }
    }

    private sealed class SyntheticRenderer : IResourceRenderer
    {
        public string ResourceType => SyntheticProvider.ResourceType;

        public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
        {
            _ = writer;
            _ = change;
            _ = context;
        }
    }

    private sealed class SyntheticChildRowExtractor : IChildRowExtractor
    {
        public IReadOnlyDictionary<string, string> ExtractDiffRow(
            object? beforeState,
            object? afterState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry,
            LargeValueFormat largeValueFormat)
        {
            _ = beforeState;
            _ = largeValueFormat;
            return ExtractRow(afterState, providerName, valueFormatterRegistry, iconProviderRegistry);
        }

        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            _ = childState;
            _ = providerName;
            _ = valueFormatterRegistry;
            _ = iconProviderRegistry;
            return new Dictionary<string, string> { ["name"] = "child" };
        }
    }
}
