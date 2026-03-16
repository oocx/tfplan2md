using System;
using System.Collections.Frozen;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Resolves Microsoft Graph app role GUIDs to permission names using built-in mappings.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
internal sealed class MicrosoftGraphAppRoleResolver : IAppRoleResolver
{
    /// <summary>
    /// Shared built-in resolver instance.
    /// </summary>
    private static readonly MicrosoftGraphAppRoleResolver BuiltInResolver = new(MicrosoftGraphAppRolesRegistry.Load());

    /// <summary>
    /// Immutable app role mappings loaded from the embedded JSON payload.
    /// </summary>
    private readonly FrozenDictionary<string, string> _appRoles;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftGraphAppRoleResolver"/> class.
    /// </summary>
    /// <param name="appRoles">App role GUID to permission name mappings.</param>
    private MicrosoftGraphAppRoleResolver(FrozenDictionary<string, string> appRoles)
    {
        _appRoles = appRoles;
    }

    /// <summary>
    /// Returns a shared resolver containing built-in Microsoft Graph app role definitions.
    /// </summary>
    /// <returns>The shared built-in resolver instance.</returns>
    internal static IAppRoleResolver CreateBuiltIn() => BuiltInResolver;

    /// <inheritdoc />
    public RoleDefinitionInfo GetAppRole(string? appRoleId)
    {
        if (string.IsNullOrWhiteSpace(appRoleId))
        {
            return new RoleDefinitionInfo(string.Empty, string.Empty, string.Empty);
        }

        if (_appRoles.TryGetValue(appRoleId, out var name))
        {
            return new RoleDefinitionInfo(name, appRoleId, $"{name} ({appRoleId})");
        }

        return new RoleDefinitionInfo(appRoleId, appRoleId, appRoleId);
    }

    /// <inheritdoc />
    public string GetAppRoleName(string? appRoleId) => GetAppRole(appRoleId).FullName;
}
