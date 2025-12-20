namespace Oocx.TfPlan2Md.Azure;

public static partial class AzureRoleDefinitionMapper
{
    public static string GetRoleName(string? roleDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(roleDefinitionId))
        {
            return roleDefinitionId ?? string.Empty;
        }

        var roleGuid = ExtractGuid(roleDefinitionId);
        if (string.IsNullOrEmpty(roleGuid))
        {
            return roleDefinitionId;
        }

        if (Roles.TryGetValue(roleGuid, out var name))
        {
            return $"{name} ({roleGuid})";
        }

        return roleDefinitionId;
    }

    private static string ExtractGuid(string roleDefinitionId)
    {
        var lastSlashIndex = roleDefinitionId.LastIndexOf('/');
        return lastSlashIndex >= 0 && lastSlashIndex < roleDefinitionId.Length - 1
            ? roleDefinitionId[(lastSlashIndex + 1)..]
            : roleDefinitionId;
    }
}
