using System;
using System.Collections.Generic;

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
}
