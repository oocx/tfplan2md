using System;
using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Builds summary HTML strings for Azure AD resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static class AzureAdSummaryBuilder
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
    /// Builds summary HTML for Azure AD user resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildUserSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = GetStringProperty(state, "display_name");
        var upn = GetStringProperty(state, "user_principal_name");
        var mail = GetStringProperty(state, "mail");

        var summaryText = string.Empty;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(upn))
        {
            var upnText = FormatSummaryValue(model, "user_principal_name", upn, iconProviderRegistry);
            upnText = $"({upnText})";
            summaryText = string.IsNullOrEmpty(summaryText) ? upnText : $"{summaryText} {upnText}";
        }

        if (!string.IsNullOrWhiteSpace(mail))
        {
            var mailText = FormatSummaryValue(model, "mail", mail, iconProviderRegistry);
            summaryText = string.IsNullOrEmpty(summaryText) ? mailText : $"{summaryText} {mailText}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD group resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal type resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildGroupSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = GetStringProperty(state, "display_name");
        var mailNickname = GetStringProperty(state, "mail_nickname");
        var description = GetStringProperty(state, "description");
        var members = GetStringArray(state, "members");

        var userCount = 0;
        var groupCount = 0;
        var spCount = 0;
        var unknownCount = 0;

        foreach (var memberId in members)
        {
            if (principalMapper.TryGetPrincipalType(memberId, out var principalType))
            {
                if (string.Equals(principalType, UserMemberType, StringComparison.OrdinalIgnoreCase))
                {
                    userCount++;
                }
                else if (string.Equals(principalType, GroupMemberType, StringComparison.OrdinalIgnoreCase))
                {
                    groupCount++;
                }
                else if (string.Equals(principalType, ServicePrincipalMemberType, StringComparison.OrdinalIgnoreCase))
                {
                    spCount++;
                }
                else
                {
                    unknownCount++;
                }
            }
            else
            {
                unknownCount++;
            }
        }

        var userIcon = ResolveMemberTypeIcon(model, UserMemberType, iconProviderRegistry);
        var groupIcon = ResolveMemberTypeIcon(model, GroupMemberType, iconProviderRegistry);
        var spIcon = ResolveMemberTypeIcon(model, ServicePrincipalMemberType, iconProviderRegistry);
        var unknownIcon = ResolveMemberTypeIcon(model, UnknownMemberType, iconProviderRegistry);

        var summaryCounts = BuildMemberCountSummary(
            userCount,
            userIcon,
            groupCount,
            groupIcon,
            spCount,
            spIcon,
            unknownCount,
            unknownIcon);

        var nameSummary = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            nameSummary = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(mailNickname))
        {
            var nicknameValue = FormatSummaryValue(model, "mail_nickname", mailNickname, iconProviderRegistry);
            nameSummary = $"{nameSummary} ({nicknameValue})";
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            nameSummary = $"{nameSummary} {EscapeMarkdown(description)}";
        }

        var summaryText = $"{nameSummary} | {FormatCodeSummary(summaryCounts)}";
        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD groups without member counts.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildGroupWithoutMembersSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = GetStringProperty(state, "display_name");
        var mailNickname = GetStringProperty(state, "mail_nickname");
        var description = GetStringProperty(state, "description");

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(mailNickname))
        {
            var nicknameText = FormatSummaryValue(model, "mail_nickname", mailNickname, iconProviderRegistry);
            nicknameText = $"({nicknameText})";
            summaryText = string.IsNullOrEmpty(summaryText) ? nicknameText : $"{summaryText} {nicknameText}";
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            summaryText = string.IsNullOrEmpty(summaryText)
                ? EscapeMarkdown(description)
                : $"{summaryText} {EscapeMarkdown(description)}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD group membership resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="principalMapper">Mapper used for principal resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildGroupMemberSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var groupId = GetStringProperty(state, "group_object_id") ?? string.Empty;
        var memberId = GetStringProperty(state, "member_object_id") ?? string.Empty;

        var groupName = principalMapper.GetName(groupId, GroupMemberType, model.Address) ?? groupId;
        var groupIsMapped = groupName != string.Empty && groupName != groupId;

        var groupSummary = groupIsMapped
            ? BuildPrincipalSummary(model, "group_name", groupName, groupId, iconProviderRegistry)
            : FormatCodeSummary(groupId);

        var summaryText = groupSummary;
        if (!string.IsNullOrEmpty(memberId))
        {
            var memberType = principalMapper.TryGetPrincipalType(memberId, out var resolvedType)
                ? resolvedType ?? string.Empty
                : string.Empty;
            var memberName = principalMapper.GetName(memberId, memberType, model.Address) ?? memberId;
            var memberIsMapped = memberName != string.Empty && memberName != memberId;

            var memberSummary = memberIsMapped
                ? BuildPrincipalSummary(model, "member_type", memberName, memberId, iconProviderRegistry, memberType)
                : FormatCodeSummary(memberId);

            summaryText = $"{summaryText} {MemberArrow} {memberSummary}";
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD service principal resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildServicePrincipalSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var displayName = GetStringProperty(state, "display_name");
        var appId = GetStringProperty(state, "application_id");
        var description = GetStringProperty(state, "description");

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, "display_name", displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(appId))
        {
            var appText = FormatSummaryValue(model, "application_id", appId, iconProviderRegistry);
            appText = $"({appText})";
            summaryText = string.IsNullOrEmpty(summaryText) ? appText : $"{summaryText} {appText}";
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            summaryText = string.IsNullOrEmpty(summaryText)
                ? EscapeMarkdown(description)
                : $"{summaryText} {EscapeMarkdown(description)}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds summary HTML for Azure AD invitation resources.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="state">The active JSON state.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <returns>Summary HTML string.</returns>
    private static string BuildInvitationSummaryHtml(
        ResourceChangeModel model,
        object? state,
        IconProviderRegistry? iconProviderRegistry)
    {
        var email = GetStringProperty(state, "user_email_address");
        var userType = GetStringProperty(state, "user_type");

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(email))
        {
            summaryText = FormatSummaryValue(model, "user_email_address", email, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(userType))
        {
            var typeText = $"({FormatCodeSummary(userType)})";
            summaryText = string.IsNullOrEmpty(summaryText) ? typeText : $"{summaryText} {typeText}";
        }

        if (string.IsNullOrEmpty(summaryText))
        {
            summaryText = FormatCodeSummary(model.Name);
        }

        return BuildSummaryHtml(model, summaryText);
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

    /// <summary>
    /// Builds a member count summary string that uses non-breaking spaces after icons.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="userCount">Count of user members.</param>
    /// <param name="userIcon">Icon for users.</param>
    /// <param name="groupCount">Count of group members.</param>
    /// <param name="groupIcon">Icon for groups.</param>
    /// <param name="servicePrincipalCount">Count of service principal members.</param>
    /// <param name="servicePrincipalIcon">Icon for service principals.</param>
    /// <param name="unknownCount">Count of unknown members.</param>
    /// <param name="unknownIcon">Icon for unknown members.</param>
    /// <returns>Formatted summary string.</returns>
    private static string BuildMemberCountSummary(
        int userCount,
        string userIcon,
        int groupCount,
        string groupIcon,
        int servicePrincipalCount,
        string servicePrincipalIcon,
        int unknownCount,
        string unknownIcon)
    {
        var summary = string.Concat(
            FormatMemberCountSegment(userCount, userIcon),
            FormatMemberCountSegment(groupCount, groupIcon),
            FormatMemberCountSegment(servicePrincipalCount, servicePrincipalIcon));

        if (unknownCount > 0)
        {
            summary = string.Concat(summary, FormatMemberCountSegment(unknownCount, unknownIcon));
        }

        return summary.TrimEnd(NonBreakingSpace[0]);
    }

    /// <summary>
    /// Formats a single count segment with a non-breaking space after the icon.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="count">The member count.</param>
    /// <param name="icon">The icon to render.</param>
    /// <returns>Formatted segment.</returns>
    private static string FormatMemberCountSegment(int count, string icon)
    {
        return string.IsNullOrWhiteSpace(icon)
            ? count.ToString()
            : $"{count} {icon}{NonBreakingSpace}";
    }

    /// <summary>
    /// Gets a string property from a JSON state object.
    /// </summary>
    /// <param name="state">The JSON state object.</param>
    /// <param name="propertyName">The property to retrieve.</param>
    /// <returns>The property value or null.</returns>
    private static string? GetStringProperty(object? state, string propertyName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => property.ToString()
        };
    }

    /// <summary>
    /// Gets string values from an array property on the JSON state object.
    /// </summary>
    /// <param name="state">The JSON state object.</param>
    /// <param name="propertyName">The array property to retrieve.</param>
    /// <returns>List of string values.</returns>
    private static IReadOnlyList<string> GetStringArray(object? state, string propertyName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<string>();
        }

        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    results.Add(value);
                }
            }
            else
            {
                var raw = item.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    results.Add(raw);
                }
            }
        }

        return results;
    }
}
