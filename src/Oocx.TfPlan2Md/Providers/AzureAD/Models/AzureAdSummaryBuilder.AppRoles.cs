using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// App role and directory role assignment summary builders for Azure AD resources.
/// Related feature: azuread_app_role_assignment support.
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
    /// Attribute name for the app role ID.
    /// </summary>
    private const string AppRoleIdAttribute = "app_role_id";

    /// <summary>
    /// Attribute name for the principal object ID.
    /// </summary>
    private const string PrincipalObjectIdAttribute = "principal_object_id";

    /// <summary>
    /// Attribute name for the resource object ID.
    /// </summary>
    private const string ResourceObjectIdAttribute = "resource_object_id";

    /// <summary>
    /// Attribute name for the directory role template ID.
    /// </summary>
    private const string RoleTemplateIdAttribute = "role_definition_id";

    /// <summary>
    /// Attribute name for claim values in delegated permission grants.
    /// </summary>
    private const string ClaimValuesAttribute = "claim_values";

    /// <summary>
    /// Shared app role resolver for summary building.
    /// </summary>
    private static readonly IAppRoleResolver AppRoleResolver = MicrosoftGraphAppRoleResolver.CreateBuiltIn();

    /// <summary>
    /// Builds summary HTML for Azure AD app role assignment resources.
    /// Shows the principal, the granted permission, and the target resource.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildAppRoleAssignmentSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var principalId = JsonStateReader.GetStringProperty(state, PrincipalObjectIdAttribute) ?? string.Empty;
        var appRoleId = JsonStateReader.GetStringProperty(state, AppRoleIdAttribute) ?? string.Empty;
        var resourceId = JsonStateReader.GetStringProperty(state, ResourceObjectIdAttribute) ?? string.Empty;

        var principalSummary = BuildResolvedPrincipalSummary(
            model, PrincipalObjectIdAttribute, principalId, principalMapper, iconProviderRegistry);

        var permissionName = AppRoleResolver.GetPermissionName(appRoleId);
        var roleSummary = !string.IsNullOrWhiteSpace(permissionName)
            ? FormatSummaryValue(model, AppRoleIdAttribute, $"{permissionName} ({appRoleId})", iconProviderRegistry)
            : FormatCodeSummary(appRoleId);

        var resourceSummary = BuildResolvedPrincipalSummary(
            model, ResourceObjectIdAttribute, resourceId, principalMapper, iconProviderRegistry);

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
        var principalId = JsonStateReader.GetStringProperty(state, PrincipalObjectIdAttribute) ?? string.Empty;
        var roleTemplateId = JsonStateReader.GetStringProperty(state, RoleTemplateIdAttribute) ?? string.Empty;

        var principalSummary = BuildResolvedPrincipalSummary(
            model, PrincipalObjectIdAttribute, principalId, principalMapper, iconProviderRegistry);

        var roleSummary = FormatCodeSummary(roleTemplateId);

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
        var resourceId = JsonStateReader.GetStringProperty(state, ResourceObjectIdAttribute) ?? string.Empty;
        var claimValues = JsonStateReader.GetStringArray(state, ClaimValuesAttribute);

        var spSummary = BuildResolvedPrincipalSummary(
            model, "service_principal_object_id", servicePrincipalId, principalMapper, iconProviderRegistry);

        var claimText = claimValues.Count > 0
            ? FormatCodeSummary(string.Join(", ", claimValues))
            : FormatCodeSummary("(no claims)");

        var resourceSummary = BuildResolvedPrincipalSummary(
            model, ResourceObjectIdAttribute, resourceId, principalMapper, iconProviderRegistry);

        var summaryText = $"{spSummary} {MemberArrow} {claimText} {MemberArrow} {resourceSummary}";
        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds a resolved principal summary with optional icon and ID suffix.
    /// Reuses principal mapping to resolve GUIDs to display names.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="attributeName">The attribute name for icon resolution.</param>
    /// <param name="principalId">The principal GUID to resolve.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted principal summary string.</returns>
    private static string BuildResolvedPrincipalSummary(
        ResourceChangeModel model,
        string attributeName,
        string principalId,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return FormatCodeSummary("(unknown)");
        }

        var displayName = principalMapper.GetName(principalId, null, model.Address);
        var isMapped = !string.IsNullOrWhiteSpace(displayName)
            && !string.Equals(displayName, principalId, System.StringComparison.OrdinalIgnoreCase);

        return isMapped
            ? BuildPrincipalSummary(model, attributeName, displayName!, principalId, iconProviderRegistry)
            : FormatCodeSummary(principalId);
    }
}
