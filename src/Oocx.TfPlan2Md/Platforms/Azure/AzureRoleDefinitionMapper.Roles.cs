using System;
using System.Collections.Frozen;
using System.Collections.Generic;
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
        var roles = JsonSerializer.Deserialize(
            EmbeddedJsonPayloads.AzureRoleDefinitions,
            AzureRoleDefinitionsJsonContext.Default.DictionaryStringString);
        if (roles is null || roles.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse role definitions from generated payload.");
        }

        return roles.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}

