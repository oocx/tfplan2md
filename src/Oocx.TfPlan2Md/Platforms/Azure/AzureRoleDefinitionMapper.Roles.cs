using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Loads Azure role definitions from the embedded JSON resource file.
/// </summary>
internal static class AzureRoleDefinitionsRegistry
{
    /// <summary>
    /// Loads Azure role definitions from the embedded JSON resource file.
    /// </summary>
    /// <returns>A frozen dictionary mapping role definition GUIDs to role names.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource cannot be loaded or parsed.</exception>
    internal static FrozenDictionary<string, string> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Oocx.TfPlan2Md.Platforms.Azure.AzureRoleDefinitions.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Failed to load embedded resource: {resourceName}");
        }

        var roles = JsonSerializer.Deserialize(stream, AzureRoleDefinitionsJsonContext.Default.DictionaryStringString);
        if (roles is null || roles.Count == 0)
        {
            throw new InvalidOperationException($"Failed to parse role definitions from {resourceName}");
        }

        return roles.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}

