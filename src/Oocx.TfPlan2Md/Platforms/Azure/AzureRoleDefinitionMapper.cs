using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Maps Azure role definition IDs to their human-readable role names.
/// Related feature: docs/features/025-azure-role-definition-mapping/specification.md.
/// </summary>
public static partial class AzureRoleDefinitionMapper
{
    /// <summary>
    /// Stores custom role definitions loaded from the mapping file.
    /// </summary>
    private static FrozenDictionary<string, string> _customRoles = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Merges custom role definitions into the lookup table.
    /// </summary>
    /// <param name="roles">The custom role definitions to apply.</param>
    /// <remarks>
    /// Related feature: docs/features/063-azure-display-enhancements/specification.md.
    /// </remarks>
    internal static void MergeCustomRoles(IReadOnlyList<MappingEntry> roles)
    {
        if (roles.Count == 0)
        {
            _customRoles = FrozenDictionary<string, string>.Empty;
            return;
        }

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role.Id))
            {
                continue;
            }

            var key = ExtractGuid(role.Id);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            lookup[key] = role.DisplayName ?? string.Empty;
        }

        _customRoles = lookup.Count == 0
            ? FrozenDictionary<string, string>.Empty
            : lookup.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retrieves the full display name for an Azure role definition.
    /// </summary>
    /// <param name="roleDefinitionId">The role definition ID to look up.</param>
    /// <returns>The full formatted role name.</returns>
    public static string GetRoleName(string? roleDefinitionId)
    {
        return GetRoleDefinition(roleDefinitionId, null).FullName;
    }

    /// <summary>
    /// Retrieves detailed role definition information from an ID or name.
    /// </summary>
    /// <param name="roleDefinitionId">The role definition ID to look up.</param>
    /// <param name="roleDefinitionName">Optional fallback role name if ID lookup fails.</param>
    /// <returns>A RoleDefinitionInfo object containing the role's name, ID, and full display name.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Role resolution handles multiple nullable inputs and mapping fallbacks.")]
    public static RoleDefinitionInfo GetRoleDefinition(string? roleDefinitionId, string? roleDefinitionName)
    {
        if (string.IsNullOrWhiteSpace(roleDefinitionId))
        {
            var fallbackName = roleDefinitionName ?? string.Empty;
            return new RoleDefinitionInfo(fallbackName, string.Empty, fallbackName);
        }

        var roleGuid = ExtractGuid(roleDefinitionId);
        var mappedName = string.Empty;
        var hasCustomMapping = !string.IsNullOrEmpty(roleGuid) && _customRoles.TryGetValue(roleGuid, out mappedName);
        var hasBuiltInMapping = !hasCustomMapping
            && !string.IsNullOrEmpty(roleGuid)
            && Roles.TryGetValue(roleGuid, out mappedName);
        var hasMapping = hasCustomMapping || hasBuiltInMapping;

        // SonarAnalyzer S2583 & S3358: Defensive null-coalescing and nested ternaries are intentional
        // Justification: Complex control flow with multiple nullable inputs requires defensive fallbacks
        // even if some code paths appear statically unreachable. This ensures robustness.
#pragma warning disable S2583 // Conditionally executed code should be reachable
#pragma warning disable S3358 // Extract nested ternary operation
        var name = hasMapping
            ? mappedName
            : roleDefinitionName ?? (string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid) ?? string.Empty;

        var fullName = hasMapping
            ? $"{mappedName} ({roleGuid})"
            : roleDefinitionName ?? roleDefinitionId ?? string.Empty;

        var id = string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid;

        var safeName = name ?? string.Empty;
        var safeId = id ?? string.Empty;
#pragma warning restore S3358
#pragma warning restore S2583

        return new RoleDefinitionInfo(safeName, safeId, fullName);
    }

    private static string ExtractGuid(string roleDefinitionId)
    {
        var lastSlashIndex = roleDefinitionId.LastIndexOf('/');
        return lastSlashIndex >= 0 && lastSlashIndex < roleDefinitionId.Length - 1
            ? roleDefinitionId[(lastSlashIndex + 1)..]
            : roleDefinitionId;
    }
}
