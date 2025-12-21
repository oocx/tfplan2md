namespace Oocx.TfPlan2Md.Azure;

public static partial class AzureRoleDefinitionMapper
{
    public static string GetRoleName(string? roleDefinitionId)
    {
        return GetRoleDefinition(roleDefinitionId, null).FullName;
    }

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

        var name = hasMapping
            ? mappedName
            : roleDefinitionName ?? (string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid) ?? string.Empty;

        var fullName = hasMapping
            ? $"{mappedName} ({roleGuid})"
            : roleDefinitionName ?? roleDefinitionId ?? string.Empty;

        var id = string.IsNullOrEmpty(roleGuid) ? roleDefinitionId : roleGuid;

        var safeName = name ?? string.Empty;
        var safeId = id ?? string.Empty;

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
