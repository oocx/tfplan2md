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
    /// Resource type for Azure AD directory role assignments.
    /// </summary>
    private const string DirectoryRoleAssignmentResourceType = "azuread_directory_role_assignment";

    /// <summary>
    /// Resource type for Azure AD delegated permission grants.
    /// </summary>
    private const string DelegatedPermissionGrantResourceType = "azuread_service_principal_delegated_permission_grant";

    /// <summary>
    /// Builds summary HTML for Azure AD app role assignment resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="appRoleResolver">Resolver for app role GUIDs.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildAppRoleAssignmentSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IAppRoleResolver? appRoleResolver,
        IconProviderRegistry? iconProviderRegistry)
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

        // Build summary text: {principal} → {role} → {resource}
        var principalSummary = BuildResolvedPrincipalSummary(
            model, "principal_object_id", principalObjectId ?? string.Empty, principalName, iconProviderRegistry);
        var roleSummary = FormatSummaryValue(model, "app_role_id", roleName, iconProviderRegistry);
        var resourceSummary = BuildResolvedPrincipalSummary(
            model, "resource_object_id", resourceObjectId ?? string.Empty, resourceName, iconProviderRegistry);

        var summaryText = $"{principalSummary} {MemberArrow} {roleSummary} {MemberArrow} {resourceSummary}";

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD directory role assignment resources.
    /// Shows the principal and the directory role.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildDirectoryRoleAssignmentSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var principalId = JsonStateReader.GetStringProperty(state, "principal_object_id") ?? string.Empty;
        var roleTemplateId = JsonStateReader.GetStringProperty(state, "role_definition_id") ?? string.Empty;

        var principalName = ResolvePrincipalName(principalId, principalMapper, null);
        var principalSummary = BuildResolvedPrincipalSummary(
            model, "principal_object_id", principalId, principalName, iconProviderRegistry);

        var roleSummary = FormatSummaryValue(model, "role_definition_id", roleTemplateId, iconProviderRegistry);

        var summaryText = $"{principalSummary} {MemberArrow} {roleSummary}";
        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD delegated permission grant resources.
    /// Shows the service principal and the granted claim values.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildDelegatedPermissionGrantSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var servicePrincipalId = JsonStateReader.GetStringProperty(state, "service_principal_object_id") ?? string.Empty;
        var resourceId = JsonStateReader.GetStringProperty(state, "resource_object_id") ?? string.Empty;
        var claimValues = JsonStateReader.GetStringArray(state, "claim_values");

        var spName = ResolvePrincipalName(servicePrincipalId, principalMapper, null);
        var spSummary = BuildResolvedPrincipalSummary(
            model, "service_principal_object_id", servicePrincipalId, spName, iconProviderRegistry);

        var claimText = claimValues.Count > 0
            ? FormatSummaryValue(model, "claim_values", string.Join(", ", claimValues), iconProviderRegistry)
            : FormatSummaryValue(model, "claim_values", "(no claims)", iconProviderRegistry);

        var resourceName = ResolvePrincipalName(resourceId, principalMapper, null);
        var resourceSummary = BuildResolvedPrincipalSummary(
            model, "resource_object_id", resourceId, resourceName, iconProviderRegistry);

        var summaryText = $"{spSummary} {MemberArrow} {claimText} {MemberArrow} {resourceSummary}";
        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds a resolved principal/resource summary with icon support.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="attributeName">The attribute name for icon resolution.</param>
    /// <param name="objectId">The object ID GUID.</param>
    /// <param name="resolvedName">The resolved display name.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted summary string.</returns>
    private static string BuildResolvedPrincipalSummary(
        ResourceChangeModel model,
        string attributeName,
        string objectId,
        string resolvedName,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(resolvedName) || string.IsNullOrWhiteSpace(objectId))
        {
            return FormatSummaryValue(model, attributeName, string.IsNullOrWhiteSpace(objectId) ? "(unknown)" : objectId, iconProviderRegistry);
        }

        var isMapped = !string.Equals(resolvedName, objectId, System.StringComparison.OrdinalIgnoreCase);

        return isMapped
            ? BuildPrincipalSummary(model, attributeName, resolvedName, objectId, iconProviderRegistry)
            : FormatSummaryValue(model, attributeName, objectId, iconProviderRegistry);
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
