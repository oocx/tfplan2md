using Scriban.Runtime;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Maps provider-specific resource view models to ScriptObject properties for template rendering.
/// </summary>
/// <remarks>
/// Providers implement this interface to enrich the ScriptObject with typed view models
/// (e.g., FirewallNetworkRuleCollectionViewModel, NetworkSecurityGroupViewModel) without
/// creating compile-time dependencies from MarkdownGeneration to Providers.
/// Related refactoring: Eliminates architecture boundary violations from typed properties on ResourceChangeModel.
/// </remarks>
internal interface IResourceModelMapper
{
    /// <summary>
    /// Determines whether this mapper can process the given resource.
    /// </summary>
    /// <param name="resource">The resource change model to evaluate.</param>
    /// <returns><c>true</c> if this mapper handles the resource type; otherwise, <c>false</c>.</returns>
    bool CanMap(ResourceChangeModel resource);

    /// <summary>
    /// Enriches the ScriptObject with provider-specific view model properties.
    /// </summary>
    /// <param name="resource">The resource change model containing the raw state data.</param>
    /// <param name="scriptObject">The ScriptObject to enrich with mapped properties.</param>
    /// <remarks>
    /// Implementations should add properties to the scriptObject that templates can access
    /// (e.g., scriptObject["firewall_network_rule_collection"] = mappedViewModel).
    /// </remarks>
    void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject);
}
