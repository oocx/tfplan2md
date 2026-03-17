using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Loads Microsoft Graph app roles from the embedded JSON resource file.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
internal static class MicrosoftGraphAppRolesRegistry
{
    /// <summary>
    /// Loads Microsoft Graph app roles from the embedded JSON resource file.
    /// </summary>
    /// <returns>A frozen dictionary mapping app role GUIDs to permission names.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource cannot be loaded or parsed.</exception>
    internal static FrozenDictionary<string, string> Load()
    {
        var roles = JsonSerializer.Deserialize(
            EmbeddedJsonPayloads.MicrosoftGraphAppRoles,
            MicrosoftGraphAppRolesJsonContext.Default.DictionaryStringString);
        if (roles is null || roles.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse Microsoft Graph app roles from generated payload.");
        }

        return roles.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
