using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Builds summary HTML strings for Azure AD resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static partial class AzureAdSummaryBuilder
{
    /// <summary>
    /// Arrow separator used in group member summaries.
    /// </summary>
    private const string MemberArrow = "\u2192";

    /// <summary>
    /// Resource type for Azure AD users.
    /// </summary>
    private const string UserResourceType = "azuread_user";

    /// <summary>
    /// Resource type for Azure AD groups.
    /// </summary>
    private const string GroupResourceType = "azuread_group";

    /// <summary>
    /// Resource type for Azure AD groups without members.
    /// </summary>
    private const string GroupWithoutMembersResourceType = "azuread_group_without_members";

    /// <summary>
    /// Resource type for Azure AD group membership changes.
    /// </summary>
    private const string GroupMemberResourceType = "azuread_group_member";

    /// <summary>
    /// Resource type for Azure AD service principals.
    /// </summary>
    private const string ServicePrincipalResourceType = "azuread_service_principal";

    /// <summary>
    /// Resource type for Azure AD invitations.
    /// </summary>
    private const string InvitationResourceType = "azuread_invitation";

    /// <summary>
    /// Member type label for users.
    /// </summary>
    private const string UserMemberType = "User";

    /// <summary>
    /// Member type label for groups.
    /// </summary>
    private const string GroupMemberType = "Group";

    /// <summary>
    /// Member type label for service principals.
    /// </summary>
    private const string ServicePrincipalMemberType = "ServicePrincipal";

    /// <summary>
    /// Member type label for unknown principals.
    /// </summary>
    private const string UnknownMemberType = "Unknown";

    /// <summary>
    /// Builds the summary HTML string for the supplied Azure AD resource change.
    /// </summary>
    /// <param name="model">The resource change model being rendered.</param>
    /// <param name="resourceChange">The source Terraform resource change.</param>
    /// <param name="action">The normalized Terraform action.</param>
    /// <param name="principalMapper">Mapper used for principal name resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string matching the Azure AD templates.</returns>
    internal static string BuildSummaryHtml(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var state = ResolveActiveState(resourceChange, action);
        if (string.Equals(model.Type, UserResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildUserSummaryHtml(model, state, iconProviderRegistry);
        }

        if (string.Equals(model.Type, GroupResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildGroupSummaryHtml(model, state, principalMapper, iconProviderRegistry);
        }

        if (string.Equals(model.Type, GroupWithoutMembersResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildGroupWithoutMembersSummaryHtml(model, state, iconProviderRegistry);
        }

        if (string.Equals(model.Type, GroupMemberResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildGroupMemberSummaryHtml(model, state, principalMapper, iconProviderRegistry);
        }

        if (string.Equals(model.Type, ServicePrincipalResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildServicePrincipalSummaryHtml(model, state, iconProviderRegistry);
        }

        if (string.Equals(model.Type, InvitationResourceType, StringComparison.OrdinalIgnoreCase))
        {
            return BuildInvitationSummaryHtml(model, state, iconProviderRegistry);
        }

        return ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);
    }

    /// <summary>
    /// Resolves the JSON state object to use for summary generation based on the action.
    /// </summary>
    /// <param name="resourceChange">The resource change data.</param>
    /// <param name="action">The normalized Terraform action.</param>
    /// <returns>The resolved state object.</returns>
    private static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }


    /// <summary>
    /// Builds the summary HTML prefix and appends the detail text.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="detailText">The formatted detail text.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model, string detailText)
    {
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        return $"{prefix} \u2014 {detailText}";
    }

    /// <summary>
    /// Formats a summary value with registry icons when available.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="attributeName">The attribute name to resolve icons for.</param>
    /// <param name="value">The raw value to format.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Formatted summary value.</returns>
    private static string FormatSummaryValue(
        ResourceChangeModel model,
        string attributeName,
        string value,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var icon = ResolveIcon(model, attributeName, value, iconProviderRegistry);
        return string.IsNullOrWhiteSpace(icon)
            ? FormatCodeSummary(value)
            : FormatIconValueSummary($"{icon} {value}");
    }

    /// <summary>
    /// Formats principal summaries with icon support and ID suffixes.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="attributeName">The attribute name used for icon resolution.</param>
    /// <param name="displayName">The resolved display name.</param>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <param name="memberType">Optional member type for icon resolution.</param>
    /// <returns>Formatted principal summary string.</returns>
    private static string BuildPrincipalSummary(
        ResourceChangeModel model,
        string attributeName,
        string displayName,
        string principalId,
        IconProviderRegistry? iconProviderRegistry,
        string? memberType = null)
    {
        var iconValue = memberType is null ? displayName : memberType;
        var icon = ResolveIcon(model, attributeName, iconValue, iconProviderRegistry);
        var formatted = string.IsNullOrWhiteSpace(icon)
            ? FormatCodeSummary(displayName)
            : FormatIconValueSummary($"{icon} {displayName}");
        return $"{formatted} ({FormatCodeSummary(principalId)})";
    }

    /// <summary>
    /// Resolves an icon from the registry for the current resource context.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="attributeName">The attribute name associated with the value.</param>
    /// <param name="value">The value to resolve an icon for.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>The resolved icon or an empty string.</returns>
    private static string ResolveIcon(
        ResourceChangeModel model,
        string attributeName,
        string value,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (iconProviderRegistry is null)
        {
            return string.Empty;
        }

        var context = new ServiceResolutionContext(model.ProviderName, model.Type, attributeName, value);
        return iconProviderRegistry.TryGetIcon(context) ?? string.Empty;
    }

    /// <summary>
    /// Resolves member type icons using the group member resource type rules.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="memberType">The member type label.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>The resolved icon or an empty string.</returns>
    private static string ResolveMemberTypeIcon(
        ResourceChangeModel model,
        string memberType,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (iconProviderRegistry is null)
        {
            return string.Empty;
        }

        var context = new ServiceResolutionContext(model.ProviderName, GroupMemberResourceType, "member_type", memberType);
        return iconProviderRegistry.TryGetIcon(context) ?? string.Empty;
    }

}
