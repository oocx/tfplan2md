using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Registers AzureRM icon providers using embedded icon rules.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal static class AzureRmIconProviderRegistration
{
    /// <summary>
    /// Registers the AzureRM icon provider backed by the embedded JSON rule resource.
    /// </summary>
    /// <param name="registry">The icon provider registry to register with.</param>
    public static void Register(IconProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var resourceName = "Oocx.TfPlan2Md.Providers.Shared.Icons.azure-common-icons.json";
        registry.Register(new MatchPattern("(^azurerm$|.*/azurerm$)", null, null, null), new FileBasedIconProvider(resourceName));
    }
}
