using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Formats app_role_id GUID values with resolved Microsoft Graph permission names.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
internal sealed class AppRoleIdFormatter : IValueFormatter
{
    /// <summary>
    /// Resolver used to map app role GUIDs to permission names.
    /// </summary>
    private readonly IAppRoleResolver _appRoleResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRoleIdFormatter"/> class.
    /// </summary>
    /// <param name="appRoleResolver">Optional resolver for app role GUIDs.</param>
    internal AppRoleIdFormatter(IAppRoleResolver? appRoleResolver = null)
    {
        _appRoleResolver = appRoleResolver ?? MicrosoftGraphAppRoleResolver.CreateBuiltIn();
    }

    /// <summary>
    /// Attempts to format app role ID values into human-readable form.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>A formatted app role string when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var roleInfo = _appRoleResolver.GetAppRole(context.Value);
        if (string.IsNullOrWhiteSpace(roleInfo.Name) || roleInfo.Name == roleInfo.Id)
        {
            return null;
        }

        var roleText = $"🛡️{MarkdownHelpers.NonBreakingSpace}{roleInfo.FullName}";
        return MarkdownHelpers.FormatCodeTable(roleText);
    }
}
