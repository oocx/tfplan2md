using System;
using System.Collections.Frozen;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Microsoft Graph app role GUIDs to human-readable permission names
/// using the built-in well-known roles registry.
/// Related feature: azuread_app_role_assignment support.
/// </summary>
internal sealed class MicrosoftGraphAppRoleResolver : IAppRoleResolver
{
    /// <summary>
    /// Shared singleton instance using only the built-in role mappings.
    /// </summary>
    private static readonly MicrosoftGraphAppRoleResolver Instance = new(
        MicrosoftGraphAppRolesRegistry.Load());

    /// <summary>
    /// Immutable built-in app role definitions loaded from the embedded payload.
    /// </summary>
    private readonly FrozenDictionary<string, string> _builtInRoles;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftGraphAppRoleResolver"/> class.
    /// </summary>
    /// <param name="builtInRoles">The immutable built-in role lookup.</param>
    private MicrosoftGraphAppRoleResolver(FrozenDictionary<string, string> builtInRoles)
    {
        _builtInRoles = builtInRoles;
    }

    /// <summary>
    /// Returns the shared resolver containing only built-in app role definitions.
    /// </summary>
    /// <returns>The shared built-in-only resolver instance.</returns>
    internal static IAppRoleResolver CreateBuiltIn()
    {
        return Instance;
    }

    /// <inheritdoc />
    public string GetAppRoleName(string? appRoleId)
    {
        if (string.IsNullOrWhiteSpace(appRoleId))
        {
            return string.Empty;
        }

        if (_builtInRoles.TryGetValue(appRoleId, out var permissionName))
        {
            return $"{permissionName} ({appRoleId})";
        }

        return appRoleId;
    }

    /// <inheritdoc />
    public string? GetPermissionName(string? appRoleId)
    {
        if (string.IsNullOrWhiteSpace(appRoleId))
        {
            return null;
        }

        return _builtInRoles.TryGetValue(appRoleId, out var permissionName)
            ? permissionName
            : null;
    }
}
