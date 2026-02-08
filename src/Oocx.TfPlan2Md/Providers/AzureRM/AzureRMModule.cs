using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Provider module for Azure Resource Manager (azurerm) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class AzureRMModule : IProviderModule
{
    private readonly LargeValueFormat _largeValueFormat;
    private readonly IPrincipalMapper _principalMapper;
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Optional mapper for tenant and management group display names.
    /// </summary>
    private readonly AzureEntityMapper? _entityMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureRMModule"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    /// <param name="principalMapper">Mapper for resolving principal names in role assignments.</param>
    /// <param name="scopeFormatter">Optional formatter for enriched Azure scope display.</param>
    /// <param name="entityMapper">Optional mapper for tenant and management group display names.</param>
    public AzureRMModule(
        LargeValueFormat largeValueFormat,
        IPrincipalMapper principalMapper,
        EnrichedAzureScopeFormatter? scopeFormatter = null,
        AzureEntityMapper? entityMapper = null)
    {
        _largeValueFormat = largeValueFormat;
        _principalMapper = principalMapper;
        _scopeFormatter = scopeFormatter;
        _entityMapper = entityMapper;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azurerm";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzureRM.Templates.";

    /// <summary>
    /// Registers AzureRM-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // AzureRM provider currently uses only core helpers
        // No provider-specific Scriban helpers needed
    }

    /// <summary>
    /// Registers AzureRM-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        AzureRmFactoryRegistration.Register(registry, _largeValueFormat, _principalMapper, _scopeFormatter);
    }

    /// <summary>
    /// Registers AzureRM-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        AzureRmValueFormatterRegistration.Register(registry, _scopeFormatter, _principalMapper, _entityMapper);
    }

    /// <summary>
    /// Registers AzureRM-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public void RegisterIconProviders(IconProviderRegistry registry)
    {
        AzureRmIconProviderRegistration.Register(registry);
    }
}
