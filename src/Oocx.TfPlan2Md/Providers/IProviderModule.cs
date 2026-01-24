using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers;

/// <summary>
/// Represents a Terraform provider module that registers provider-specific functionality.
/// </summary>
/// <remarks>
/// Provider modules encapsulate all provider-specific logic including:
/// - Scriban helper function registration
/// - Resource view model factory registration
/// - Template discovery prefixes
/// 
/// This interface enables explicit, AOT-compatible provider registration without reflection.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal interface IProviderModule
{
    /// <summary>
    /// Gets the unique name of the Terraform provider (e.g., "azurerm", "azapi", "azuredevops").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    /// <example>
    /// For azurerm provider: "Oocx.TfPlan2Md.Providers.AzureRM.Templates.".
    /// </example>
    string TemplateResourcePrefix { get; }

    /// <summary>
    /// Registers provider-specific Scriban helper functions into the template context.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register functions into.</param>
    void RegisterHelpers(ScriptObject scriptObject);

    /// <summary>
    /// Registers provider-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register into.</param>
    void RegisterFactories(MarkdownGeneration.Models.IResourceViewModelFactoryRegistry registry);
}
