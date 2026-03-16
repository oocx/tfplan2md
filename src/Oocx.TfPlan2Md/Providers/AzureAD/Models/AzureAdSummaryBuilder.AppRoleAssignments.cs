using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// App role assignment summary builder for Azure AD resources.
/// Related feature: docs/features/116-azuread-app-role-assignment/specification.md.
/// </summary>
internal static partial class AzureAdSummaryBuilder
{
    /// <summary>
    /// Resource type for Azure AD app role assignments.
    /// </summary>
    private const string AppRoleAssignmentResourceType = "azuread_app_role_assignment";

    /// <summary>
    /// Builds summary HTML for Azure AD app role assignment resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="appRoleResolver">Resolver for app role GUIDs.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildAppRoleAssignmentSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IAppRoleResolver? appRoleResolver)
    {
        var appRoleId = JsonStateReader.GetStringProperty(state, "app_role_id");
        var principalObjectId = JsonStateReader.GetStringProperty(state, "principal_object_id");
        var resourceObjectId = JsonStateReader.GetStringProperty(state, "resource_object_id");
        var principalDisplayName = JsonStateReader.GetStringProperty(state, "principal_display_name");
        var resourceDisplayName = JsonStateReader.GetStringProperty(state, "resource_display_name");

        // Resolve app role name
        var roleName = ResolveAppRoleName(appRoleId, appRoleResolver);

        // Resolve principal name: 1. IPrincipalMapper → 2. computed principal_display_name → 3. raw GUID
        var principalName = ResolvePrincipalName(principalObjectId, principalMapper, principalDisplayName);

        // Resolve resource name: 1. IPrincipalMapper → 2. computed resource_display_name → 3. raw GUID
        var resourceName = ResolvePrincipalName(resourceObjectId, principalMapper, resourceDisplayName);

        // Build summary text: {role} → {principal} on {resource}
        var roleText = FormatCodeSummary(roleName);
        var principalText = FormatCodeSummary(principalName);
        var resourceText = FormatCodeSummary(resourceName);

        var summaryText = $"{roleText} \u2192 {principalText} on {resourceText}";

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Resolves an app role ID to a display name using the resolver.
    /// </summary>
    /// <param name="appRoleId">The app role GUID.</param>
    /// <param name="appRoleResolver">The app role resolver.</param>
    /// <returns>The resolved permission name or the raw GUID.</returns>
    private static string ResolveAppRoleName(string? appRoleId, IAppRoleResolver? appRoleResolver)
    {
        if (string.IsNullOrWhiteSpace(appRoleId))
        {
            return string.Empty;
        }

        if (appRoleResolver is null)
        {
            return appRoleId;
        }

        var roleInfo = appRoleResolver.GetAppRole(appRoleId);
        return roleInfo.Name;
    }

    /// <summary>
    /// Resolves a principal/resource object ID to a display name with fallback.
    /// </summary>
    /// <param name="objectId">The object ID GUID.</param>
    /// <param name="principalMapper">The principal mapper.</param>
    /// <param name="computedDisplayName">Computed display name fallback from Terraform state.</param>
    /// <returns>The resolved display name or the raw GUID.</returns>
    private static string ResolvePrincipalName(string? objectId, IPrincipalMapper principalMapper, string? computedDisplayName)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return string.IsNullOrWhiteSpace(computedDisplayName)
                ? string.Empty
                : computedDisplayName;
        }

        // Try IPrincipalMapper first
        var name = principalMapper.GetName(objectId);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Fall back to computed display name from Terraform state
        if (!string.IsNullOrWhiteSpace(computedDisplayName))
        {
            return computedDisplayName;
        }

        // Fall back to raw GUID
        return objectId;
    }
}
