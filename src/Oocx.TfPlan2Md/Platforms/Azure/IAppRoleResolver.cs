namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Microsoft Graph app role identifiers into human-readable permission names.
/// Related feature: azuread_app_role_assignment support.
/// </summary>
internal interface IAppRoleResolver
{
    /// <summary>
    /// Resolves an app role GUID to its display name (e.g., "User.Read.All").
    /// </summary>
    /// <param name="appRoleId">The GUID of the app role to resolve.</param>
    /// <returns>
    /// The permission name followed by the GUID in parentheses when resolved;
    /// otherwise the raw GUID.
    /// </returns>
    string GetAppRoleName(string? appRoleId);

    /// <summary>
    /// Resolves an app role GUID to just the permission name without the GUID suffix.
    /// </summary>
    /// <param name="appRoleId">The GUID of the app role to resolve.</param>
    /// <returns>The permission name when found; otherwise <c>null</c>.</returns>
    string? GetPermissionName(string? appRoleId);
}
