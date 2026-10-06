using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Providers.MsGraph.Models;
using Oocx.TfPlan2Md.Providers.MsGraph.Renderers;

namespace Oocx.TfPlan2Md.Providers.MsGraph;

/// <summary>
/// Provider module for Microsoft Graph resources.
/// Related feature: docs/features/146-generic-resource-review-clarity/specification.md.
/// </summary>
internal sealed class MsGraphModule : IProvider, IResourceRendererProvider
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

    /// <inheritdoc />
    public void RegisterResourceRenderers(ResourceRendererRegistry registry)
    {
        registry.Register(new MsGraphResourceRenderer("msgraph_resource"));
        registry.Register(new MsGraphResourceRenderer("msgraph_update_resource"));
    }
}
