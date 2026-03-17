namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Microsoft Graph app role GUIDs to human-readable permission names.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
internal interface IAppRoleResolver
{
    /// <summary>
    /// Resolves an app role GUID to its role information.
    /// </summary>
    /// <param name="appRoleId">The app role GUID to resolve.</param>
    /// <returns>Role information with name, ID, and full formatted name.</returns>
    RoleDefinitionInfo GetAppRole(string? appRoleId);

    /// <summary>
    /// Resolves an app role GUID to its formatted display name.
    /// </summary>
    /// <param name="appRoleId">The app role GUID to resolve.</param>
    /// <returns>The full formatted role name.</returns>
    string GetAppRoleName(string? appRoleId);
}
