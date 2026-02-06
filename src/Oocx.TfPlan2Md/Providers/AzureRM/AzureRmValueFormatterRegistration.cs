using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureRM;

/// <summary>
/// Registers AzureRM value formatters.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal static class AzureRmValueFormatterRegistration
{
    /// <summary>
    /// Registers AzureRM value formatters in the provided registry.
    /// </summary>
    /// <param name="registry">The value formatter registry to register with.</param>
    public static void Register(ValueFormatterRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register(
            new MatchPattern("^azurerm$", null, null, "^/subscriptions/[^/]+/.*"),
            new AzureResourceIdFormatter());
    }
}
