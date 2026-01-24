using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Registry that maps resource types to their corresponding view model factories.
/// </summary>
/// <remarks>
/// This registry pattern reduces ReportModelBuilder's class coupling by eliminating
/// direct dependencies on specific factory classes.
/// Related feature: docs/features/046-code-quality-metrics-enforcement/specification.md.
/// </remarks>
internal sealed class ResourceViewModelFactoryRegistry : IResourceViewModelFactoryRegistry
{
    private readonly Dictionary<string, IResourceViewModelFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceViewModelFactoryRegistry"/> class.
    /// </summary>
    /// <param name="largeValueFormat">The format to use for large values.</param>
    /// <param name="principalMapper">The mapper for resolving principal names.</param>
#pragma warning disable IDE0060 // Remove unused parameter - kept for future extensibility (AzureDevOps migration)
    public ResourceViewModelFactoryRegistry(LargeValueFormat largeValueFormat, IPrincipalMapper principalMapper)
#pragma warning restore IDE0060
    {
        // Register core/non-provider-specific factories
        _factories["azuredevops_variable_group"] = new VariableGroupFactory(largeValueFormat);

        // Note: AzureRM and AzApi provider-specific factories are registered via ProviderRegistry
        // Related feature: docs/features/047-provider-code-separation/specification.md
    }

    /// <summary>
    /// Tries to get a factory for the specified resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_network_security_group").</param>
    /// <param name="factory">The factory if one is registered for this type; otherwise, null.</param>
    /// <returns>True if a factory was found; otherwise, false.</returns>
    public bool TryGetFactory(string resourceType, out IResourceViewModelFactory? factory)
    {
        return _factories.TryGetValue(resourceType, out factory);
    }

    /// <summary>
    /// Registers a factory for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_network_security_group").</param>
    /// <param name="factory">The factory instance to register.</param>
    public void RegisterFactory(string resourceType, IResourceViewModelFactory factory)
    {
        _factories[resourceType] = factory;
    }

    /// <summary>
    /// Adapter for <see cref="VariableGroupViewModelFactory"/>.
    /// </summary>
    private sealed class VariableGroupFactory : IResourceViewModelFactory
    {
        private readonly LargeValueFormat _largeValueFormat;

        public VariableGroupFactory(LargeValueFormat largeValueFormat)
        {
            _largeValueFormat = largeValueFormat;
        }

        public void ApplyViewModel(
            ResourceChangeModel model,
            Parsing.ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
        {
            model.VariableGroup = VariableGroupViewModelFactory.Build(
                resourceChange,
                resourceChange.ProviderName,
                _largeValueFormat);
        }
    }
}
