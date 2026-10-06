using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.MsGraph.Models;

namespace Oocx.TfPlan2Md.Providers.MsGraph;

/// <summary>
/// Provider module for Microsoft Graph resources.
/// Related feature: docs/features/146-generic-resource-review-clarity/specification.md.
/// </summary>
internal sealed class MsGraphModule : IProvider
{
    /// <inheritdoc />
    public string ProviderName => "msgraph";

    /// <inheritdoc />
    public string TemplateResourcePrefix => "Oocx.TfPlan2Md.Providers.MsGraph.Templates.";

    /// <inheritdoc />
    public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
    {
        var factory = new MsGraphResourceViewModelFactory();
        registry.RegisterFactory("msgraph_resource", factory);
        registry.RegisterFactory("msgraph_update_resource", factory);
    }
}
