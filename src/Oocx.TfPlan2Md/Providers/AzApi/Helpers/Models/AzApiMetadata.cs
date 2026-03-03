namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

/// <summary>
/// Extracted metadata used by AzAPI renderers.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
/// <param name="Type">Resource type, including API version suffix when present.</param>
/// <param name="Name">Resource name.</param>
/// <param name="ParentId">Human-readable parent scope summary.</param>
/// <param name="Location">Azure location.</param>
/// <param name="Tags">Resource tags.</param>
/// <param name="ResourceId">Resource ID used by azapi_update_resource.</param>
internal sealed record AzApiMetadata(
    string? Type,
    string? Name,
    string? ParentId,
    string? Location,
    IReadOnlyDictionary<string, string>? Tags,
    string? ResourceId);
