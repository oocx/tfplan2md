namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Maps Azure role definition IDs to their human-readable role names.
/// Related feature: docs/features/025-azure-role-definition-mapping/specification.md.
/// </summary>
public static partial class AzureRoleDefinitionMapper
{
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
    public static RoleDefinitionInfo GetRoleDefinition(string? roleDefinitionId, string? roleDefinitionName)
    {
        if (string.IsNullOrWhiteSpace(roleDefinitionId))
        {
            var fallbackName = roleDefinitionName ?? string.Empty;
            return new RoleDefinitionInfo(fallbackName, string.Empty, fallbackName);
        }

        var roleGuid = ExtractGuid(roleDefinitionId);
        var mappedName = string.Empty;
        var hasMapping = !string.IsNullOrEmpty(roleGuid) && Roles.TryGetValue(roleGuid, out mappedName);

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
