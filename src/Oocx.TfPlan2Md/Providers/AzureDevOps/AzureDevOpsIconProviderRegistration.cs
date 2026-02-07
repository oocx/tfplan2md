using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Registers Azure DevOps icon providers using embedded icon rules.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal static class AzureDevOpsIconProviderRegistration
{
    /// <summary>
    /// Registers the Azure DevOps icon provider backed by the embedded JSON rule resource.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public static void Register(IconProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var resourceName = "Oocx.TfPlan2Md.Providers.AzureDevOps.Icons.azuredevops-icons.json";
        registry.Register(new MatchPattern("(^azuredevops$|.*/azuredevops$)", null, null, null), new FileBasedIconProvider(resourceName));
    }
}
