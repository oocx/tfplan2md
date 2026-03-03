using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Extracts AzAPI resource metadata from Terraform before/after state payloads.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
internal static class AzApiMetadataExtractor
{
    /// <summary>
    /// Extracts metadata for a resource change.
    /// </summary>
    /// <param name="change">Resource change model.</param>
    /// <returns>Extracted metadata.</returns>
    internal static AzApiMetadata Extract(ResourceChangeModel change)
    {
        var state = ResolveState(change);
        var dictionary = AzApiBodyFlattener.ToDictionary(state);

        var type = ReadString(dictionary, "type");
        var name = ReadString(dictionary, "name");
        var parentIdRaw = ReadString(dictionary, "parent_id");
        var location = ReadString(dictionary, "location");
        var resourceId = ReadString(dictionary, "resource_id");

        var parentId = string.IsNullOrWhiteSpace(parentIdRaw)
            ? null
            : AzureScopeParser.Parse(parentIdRaw).Summary;

        var tags = ExtractTags(dictionary);

        return new AzApiMetadata(type, name, parentId, location, tags, resourceId);
    }

    /// <summary>
    /// Tries reading a top-level property from a state object.
    /// </summary>
    /// <param name="state">State object.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="value">Resolved value when present.</param>
    /// <returns><c>true</c> when a value is found.</returns>
    internal static bool TryGetProperty(object? state, string propertyName, out object? value)
    {
        var dictionary = AzApiBodyFlattener.ToDictionary(state);
        return dictionary.TryGetValue(propertyName, out value);
    }

    private static object? ResolveState(ResourceChangeModel change)
    {
        if (string.Equals(change.Action, "delete", StringComparison.Ordinal))
        {
            return change.BeforeJson;
        }

        return change.AfterJson ?? change.BeforeJson;
    }

    private static string? ReadString(IReadOnlyDictionary<string, object?> dictionary, string propertyName)
    {
        if (!dictionary.TryGetValue(propertyName, out var value) || value is null)
        {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static Dictionary<string, string>? ExtractTags(IReadOnlyDictionary<string, object?> dictionary)
    {
        if (!dictionary.TryGetValue("tags", out var tagsValue) || tagsValue is null)
        {
            return null;
        }

        var tagsMap = AzApiBodyFlattener.ToDictionary(tagsValue);
        if (tagsMap.Count == 0)
        {
            return null;
        }

        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in tagsMap)
        {
            normalized[key] = value?.ToString() ?? string.Empty;
        }

        return normalized;
    }
}
