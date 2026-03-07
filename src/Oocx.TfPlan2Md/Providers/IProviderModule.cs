using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Represents a core Terraform provider with factory registration.
/// </summary>
/// <remarks>
/// This is the narrower, focused provider interface that all providers must implement.
/// Additional capabilities are expressed through separate capability interfaces.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IProvider
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
}

/// <summary>
/// Represents a Terraform provider that provides custom value formatters.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register value formatters.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IValueFormatterProvider
{
    /// <summary>
    /// Registers provider-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register into.</param>
    void RegisterValueFormatters(ValueFormatterRegistry registry);
}

/// <summary>
/// Represents a Terraform provider that provides custom icon definitions.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register icon providers.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IIconRegistrationProvider
{
    /// <summary>
    /// Registers provider-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register into.</param>
    void RegisterIconProviders(IconProviderRegistry registry);
}

/// <summary>
/// Represents a Terraform provider that defines parent-child resource relationships.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register parent-child relationships.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IParentChildRelationshipProvider
{
    /// <summary>
    /// Registers provider-specific parent-child resource relationships.
    /// </summary>
    /// <param name="registry">The parent-child relationship registry to register into.</param>
    void RegisterParentChildRelationships(MarkdownGeneration.Models.IParentChildRelationshipRegistry registry);
}

/// <summary>
/// Represents a Terraform provider that provides attribute change filters.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register attribute change filters.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </remarks>
internal interface IAttributeChangeFilterProvider
{
    /// <summary>
    /// Registers provider-specific attribute change filters.
    /// </summary>
    /// <param name="registry">The attribute change filter registry to register into.</param>
    void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry);
}

/// <summary>
/// Represents a Terraform provider that provides post-merge callbacks.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register post-merge callbacks.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IPostMergeCallbackProvider
{
    /// <summary>
    /// Registers provider-specific callbacks to be invoked after parent-child merging.
    /// </summary>
    /// <param name="builder">The report model builder to register callbacks with.</param>
    void RegisterPostMergeCallbacks(ReportModelBuilder builder);
}

/// <summary>
/// Represents a Terraform provider that provides custom resource renderers.
/// </summary>
/// <remarks>
/// Providers can optionally implement this interface to register resource renderers.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </remarks>
internal interface IResourceRendererProvider
{
    /// <summary>
    /// Registers provider-specific C# resource renderers.
    /// </summary>
    /// <param name="registry">Resource renderer registry used by the pure C# markdown pipeline.</param>
    void RegisterResourceRenderers(ResourceRendererRegistry registry);
}
