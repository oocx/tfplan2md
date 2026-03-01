using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Represents a Terraform provider module that registers provider-specific functionality.
/// </summary>
/// <remarks>
/// Provider modules encapsulate all provider-specific logic including:
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
    /// Registers provider-specific attribute change filters.
    /// </summary>
    /// <param name="registry">The attribute change filter registry to register into.</param>
    /// <remarks>
    /// Filters allow providers to suppress attribute change rows based on their own criteria
    /// (e.g., Azure resource ID casing-only changes). The default no-op keeps all existing
    /// provider modules source-compatible without changes.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </remarks>
    void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
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
    /// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
    /// </remarks>
    void RegisterPostMergeCallbacks(ReportModelBuilder builder)
    {
        // Default no-op keeps existing provider modules compatible.
    }

    /// <summary>
    /// Registers provider-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">Resource renderer registry used by the pure C# markdown pipeline.</param>
    void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        // Default no-op keeps existing provider modules compatible.
    }
}
