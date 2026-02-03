namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Registry interface for resource view model factories, enabling providers to register their factories.
/// </summary>
/// <remarks>
/// This interface abstracts the factory registration mechanism, allowing provider modules
/// to register factories without depending on the internal implementation details.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal interface IResourceViewModelFactoryRegistry
{
    /// <summary>
    /// Registers a factory for a specific resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type (e.g., "azurerm_network_security_group").</param>
    /// <param name="factory">The factory instance to register.</param>
    void RegisterFactory(string resourceType, IResourceViewModelFactory factory);
}
