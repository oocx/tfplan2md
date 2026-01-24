using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Providers.AzureDevOps.Models;
using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Provider module for Azure DevOps (azuredevops) resources.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </summary>
internal sealed class AzureDevOpsModule : IProviderModule
{
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureDevOpsModule"/> class.
    /// </summary>
    /// <param name="largeValueFormat">Format for rendering large values (inline-diff or simple-diff).</param>
    public AzureDevOpsModule(LargeValueFormat largeValueFormat)
    {
        _largeValueFormat = largeValueFormat;
    }

    /// <summary>
    /// Gets the unique name of this Terraform provider.
    /// </summary>
    public string ProviderName => "azuredevops";

    /// <summary>
    /// Gets the embedded resource prefix for this provider's templates.
    /// </summary>
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.AzureDevOps.Templates.";

    /// <summary>
    /// Registers AzureDevOps-specific Scriban helper functions.
    /// </summary>
    /// <param name="scriptObject">The Scriban script object to register helpers with.</param>
    public void RegisterHelpers(ScriptObject scriptObject)
    {
        // AzureDevOps provider currently uses only core helpers
        // No provider-specific Scriban helpers needed
    }

    /// <summary>
    /// Registers AzureDevOps-specific resource view model factories.
    /// </summary>
    /// <param name="registry">The factory registry to register with.</param>
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        registry.RegisterFactory("azuredevops_variable_group", new VariableGroupFactory(_largeValueFormat));
    }
}
