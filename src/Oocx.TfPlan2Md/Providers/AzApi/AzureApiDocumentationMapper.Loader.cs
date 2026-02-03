using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Maps Azure resource types to their official REST API documentation URLs.
/// This partial class contains the loading logic for the mappings dictionary.
/// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
/// </summary>
public static partial class AzureApiDocumentationMapper
{
    private static readonly FrozenDictionary<string, string> Mappings = LoadMappings();

    /// <summary>
    /// Loads Azure API documentation mappings from the embedded JSON resource file.
    /// </summary>
    /// <returns>
    /// A frozen dictionary mapping resource type names (without version) to documentation URLs.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded resource cannot be loaded or parsed.
    /// </exception>
    /// <remarks>
    /// Uses a FrozenDictionary for optimal O(1) lookup performance. The dictionary uses
    /// case-insensitive string comparison to handle variations in resource type casing.
    /// 
    /// This method is called once during static initialization. If the JSON file is corrupted
    /// or missing, the application will fail fast at startup rather than during rendering.
    /// </remarks>
    private static FrozenDictionary<string, string> LoadMappings()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Oocx.TfPlan2Md.Providers.AzApi.Data.AzureApiDocumentationMappings.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to load embedded resource: {resourceName}");
        }

        var model = JsonSerializer.Deserialize(
            stream,
            AzureApiDocumentationMappingsJsonContext.Default.AzureApiDocumentationMappingsModel);

        if (model is null || model.Mappings is null || model.Mappings.Count == 0)
        {
            throw new InvalidOperationException($"Failed to parse Azure API documentation mappings from {resourceName}");
        }

        // Convert nested structure to flat dictionary: ResourceType -> URL
        var flatMappings = model.Mappings
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value?.Url))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Url,
                StringComparer.OrdinalIgnoreCase);

        return flatMappings.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
