using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Group-focused summary builders for Azure AD resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static partial class AzureAdSummaryBuilder
{
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
        var displayName = JsonStateReader.GetStringProperty(state, "display_name");
        var mailNickname = JsonStateReader.GetStringProperty(state, "mail_nickname");
        var description = JsonStateReader.GetStringProperty(state, "description");
        var members = JsonStateReader.GetStringArray(state, "members");

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
        var displayName = JsonStateReader.GetStringProperty(state, "display_name");
        var mailNickname = JsonStateReader.GetStringProperty(state, "mail_nickname");
        var description = JsonStateReader.GetStringProperty(state, "description");

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
        var groupId = JsonStateReader.GetStringProperty(state, "group_object_id") ?? string.Empty;
        var memberId = JsonStateReader.GetStringProperty(state, "member_object_id") ?? string.Empty;

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
    /// Builds a member count summary string that uses non-breaking spaces after icons.
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
}
