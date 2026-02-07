using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Provider module for Azure API (azapi) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class AzApiModule : IProviderModule
{
    /// <summary>
    /// Optional formatter for enriched Azure scope display.
    /// </summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzApiModule"/> class.
    /// </summary>
    /// <param name="scopeFormatter">Optional formatter for enriched Azure scope display.</param>
    public AzApiModule(EnrichedAzureScopeFormatter? scopeFormatter = null)
    {
        _scopeFormatter = scopeFormatter;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azapi";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzApi.Templates.";

    /// <summary>
    /// Registers AzApi-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        Oocx.TfPlan2Md.Providers.AzApi.ScribanHelpers.RegisterAzApiHelpers(scriptObject);
    }

    /// <summary>
    /// Registers AzApi-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(MarkdownGeneration.Models.IResourceViewModelFactoryRegistry registry)
    {
        // AzApi provider currently uses generic resource handling
        // No custom factories needed at this time
    }

    /// <summary>
    /// Registers AzApi-specific value formatters.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public void RegisterValueFormatters(ValueFormatterRegistry registry)
    {
        registry.Register(
            new MatchPattern("(^azapi$|.*/azapi$)", null, null, "^/subscriptions/[^/]+/.*"),
            new AzureResourceIdFormatter(_scopeFormatter));
    }

    /// <summary>
    /// Registers AzApi-specific icon providers.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public void RegisterIconProviders(IconProviderRegistry registry)
    {
        var resourceName = "Oocx.TfPlan2Md.Providers.Shared.Icons.azure-common-icons.json";
        registry.Register(new MatchPattern("(^azapi$|.*/azapi$)", null, null, null), new FileBasedIconProvider(resourceName));
    }
}
