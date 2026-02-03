namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
#pragma warning disable CA1506 // Avoid excessive class coupling - ScribanHelpers naturally couples with many types for template rendering
public static partial class ScribanHelpers
{
    /// <summary>
    /// Large value threshold for property values (in characters).
    /// Values exceeding this length are marked as large and rendered in collapsible sections.
    /// </summary>
    private const int LargeValueThreshold = 200;
}
#pragma warning restore CA1506
