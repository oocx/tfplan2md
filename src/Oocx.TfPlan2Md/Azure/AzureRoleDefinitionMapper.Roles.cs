using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Oocx.TfPlan2Md.Azure;

/// <summary>
/// Maps Azure role definition IDs to their human-readable role names.
/// This partial class contains the role definitions lookup dictionary loaded from embedded JSON data.
/// Related feature: docs/features/025-azure-role-definition-mapping/specification.md.
/// </summary>
public static partial class AzureRoleDefinitionMapper
{
    private static readonly FrozenDictionary<string, string> Roles = LoadRoleDefinitions();

    /// <summary>
    /// Loads Azure role definitions from the embedded JSON resource file.
    /// </summary>
    /// <returns>A frozen dictionary mapping role definition GUIDs to role names.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource cannot be loaded or parsed.</exception>
    private static FrozenDictionary<string, string> LoadRoleDefinitions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Oocx.TfPlan2Md.Azure.AzureRoleDefinitions.json";

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

