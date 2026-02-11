using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

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
/// Lives in MarkdownGeneration.Services to avoid circular dependency between
/// MarkdownGeneration and Providers layers. Providers implement this interface.
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

    /// <summary>
    /// Registers provider-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register into.</param>
    void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        // Default no-op keeps existing provider modules compatible.
    }

    /// <summary>
    /// Registers provider-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register into.</param>
    void RegisterIconProviders(IconProviderRegistry registry)
    {
        // Default no-op keeps existing provider modules compatible.
    }

    /// <summary>
    /// Registers provider-specific parent-child resource relationships.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register into.</param>
    void RegisterParentChildRelationships(MarkdownGeneration.Models.IParentChildRelationshipRegistry registry)
    {
        // Default no-op keeps existing provider modules compatible.
    }

    /// <summary>
    /// Registers provider-specific resource model mappers for ScriptObject enrichment.
    /// </summary>
    /// <param name="registry">The resource model mapper registry to register into.</param>
    /// <remarks>
    /// Mappers enable providers to extend template rendering with typed view models
    /// (e.g., FirewallNetworkRuleCollectionViewModel) without creating compile-time
    /// dependencies from MarkdownGeneration to Providers.
    /// </remarks>
    void RegisterResourceModelMappers(ResourceModelMapperRegistry registry)
    {
        // Default no-op keeps existing provider modules compatible.
    }

    /// <summary>
    /// Registers provider-specific callbacks to be invoked after parent-child merging.
    /// </summary>
    /// <param name="builder">The report model builder to register callbacks with.</param>
    /// <remarks>
    /// Allows providers to perform post-merge processing like updating summaries
    /// without introducing dependencies from MarkdownGeneration to Providers.
    /// Related issue: docs/issues/070-parent-child-summary-member-counts/analysis.md.
    /// </remarks>
    void RegisterPostMergeCallbacks(ReportModelBuilder builder)
    {
        // Default no-op keeps existing provider modules compatible.
    }
}
