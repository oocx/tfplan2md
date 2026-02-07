using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Represents a mapping entry with a stable identifier and a display name.
/// </summary>
/// <param name="Id">The identifier to match (GUID or management group ID).</param>
/// <param name="DisplayName">The human-friendly display name for the identifier.</param>
/// <remarks>
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </remarks>
internal sealed record MappingEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName);
