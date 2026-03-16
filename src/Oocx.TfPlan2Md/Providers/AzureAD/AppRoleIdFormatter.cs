using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureAD;

/// <summary>
/// Formats Azure AD app role identifiers with mapped Graph permission names.
/// Resolves GUIDs to human-readable permission names like "User.Read.All".
/// </summary>
/// <remarks>
/// Related feature: azuread_app_role_assignment support.
/// </remarks>
internal sealed class AppRoleIdFormatter : IValueFormatter
{
    /// <summary>
    /// Resolver used to map app role GUIDs to permission names.
    /// </summary>
    private readonly IAppRoleResolver _appRoleResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRoleIdFormatter"/> class.
    /// </summary>
    /// <param name="appRoleResolver">Optional resolver for app role permission names.</param>
    internal AppRoleIdFormatter(IAppRoleResolver? appRoleResolver = null)
    {
        _appRoleResolver = appRoleResolver ?? MicrosoftGraphAppRoleResolver.CreateBuiltIn();
    }

    /// <summary>
    /// Attempts to format app role identifiers into mapped permission names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted permission name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var permissionName = _appRoleResolver.GetPermissionName(context.Value);
        if (string.IsNullOrWhiteSpace(permissionName))
        {
            return null;
        }

        var roleText = $"🔑{MarkdownHelpers.NonBreakingSpace}{permissionName} ({context.Value})";
        return MarkdownHelpers.FormatCodeTable(roleText);
    }
}
