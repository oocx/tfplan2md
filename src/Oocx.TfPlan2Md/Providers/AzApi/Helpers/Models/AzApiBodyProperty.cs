namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

/// <summary>
/// Represents a flattened AzAPI body property.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
/// <param name="Path">Flattened path (dot notation).</param>
/// <param name="Value">Raw value.</param>
/// <param name="IsLarge">Whether the serialized value exceeds the large-value threshold.</param>
internal sealed record AzApiBodyProperty(string Path, object? Value, bool IsLarge);
