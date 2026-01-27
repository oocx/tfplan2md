namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Shared constants for AzAPI Scriban helper rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Script object key for flattened property paths.
    /// </summary>
    private const string AzApiPathKey = "path";

    /// <summary>
    /// Script object key for flattened property values.
    /// </summary>
    private const string AzApiValueKey = "value";

    /// <summary>
    /// Script object key for update-mode before values.
    /// </summary>
    private const string AzApiBeforeKey = "before";

    /// <summary>
    /// Script object key for update-mode after values.
    /// </summary>
    private const string AzApiAfterKey = "after";
}
