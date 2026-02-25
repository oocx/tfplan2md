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
            nameSummary = string.IsNullOrWhiteSpace(nameSummary)
                ? EscapeMarkdown(description)
                : $"{nameSummary} - {EscapeMarkdown(description)}";
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
                : $"{summaryText} - {EscapeMarkdown(description)}";
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
        var instanceKey = ExtractInstanceKey(model.Address);

        var groupSummary = BuildGroupSummary(model, groupId, instanceKey, principalMapper, iconProviderRegistry);
        var summaryText = groupSummary;

        if (!string.IsNullOrEmpty(memberId))
        {
            summaryText = $"{summaryText} {MemberArrow} {BuildMemberSummary(model, memberId, principalMapper, iconProviderRegistry)}";
        }
        else if (JsonStateReader.PropertyExists(state, "member_object_id"))
        {
            // member_object_id is present in the plan but null — the attribute is "known after apply".
            // Try to find the source resource reference for better context.
            // Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
            var memberRef = FindResourceReference(model.AttributeReferences, "member_object_id", instanceKey);
            summaryText = $"{summaryText} {MemberArrow} {FormatCodeSummary(memberRef ?? "(known after apply)")}";
        }

        return BuildSummaryHtml(model, summaryText);
    }

    private static string BuildGroupSummary(
        ResourceChangeModel model,
        string groupId,
        string? instanceKey,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            // group_object_id is unknown at plan time — use configuration reference for context.
            // Related issue: docs/issues/575-azuread-group-member-empty-summary/analysis.md.
            var groupRef = FindResourceReference(model.AttributeReferences, "group_object_id", instanceKey);
            return FormatCodeSummary(groupRef ?? "(known after apply)");
        }

        var groupName = principalMapper.GetName(groupId, GroupMemberType, model.Address) ?? groupId;
        var groupIsMapped = groupName != string.Empty && groupName != groupId;
        return groupIsMapped
            ? BuildPrincipalSummary(model, "group_name", groupName, groupId, iconProviderRegistry)
            : FormatCodeSummary(groupId);
    }

    private static string BuildMemberSummary(
        ResourceChangeModel model,
        string memberId,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        var memberType = principalMapper.TryGetPrincipalType(memberId, out var resolvedType)
            ? resolvedType ?? string.Empty
            : string.Empty;
        var memberName = principalMapper.GetName(memberId, memberType, model.Address) ?? memberId;
        var memberIsMapped = memberName != string.Empty && memberName != memberId;
        return memberIsMapped
            ? BuildPrincipalSummary(model, "member_type", memberName, memberId, iconProviderRegistry, memberType)
            : FormatCodeSummary(memberId);
    }

    /// <summary>
    /// Extracts the for_each or count instance key from a resource address.
    /// For example, <c>azuread_group_member.members[0]</c> → <c>"0"</c>,
    /// <c>azuread_group_member.user_groups["team - user@example.de"]</c> → <c>"team - user@example.de"</c>.
    /// Returns null when the address has no instance key.
    /// </summary>
    private static string? ExtractInstanceKey(string address)
    {
        if (!address.EndsWith(']'))
        {
            return null;
        }

        var bracketIndex = address.LastIndexOf('[');
        if (bracketIndex < 0)
        {
            return null;
        }

        var raw = address[(bracketIndex + 1)..^1];
        // Strip surrounding quotes from string keys like ["team-example - user@example.de"]
        if (raw.Length >= 2 && raw.StartsWith('"') && raw.EndsWith('"'))
        {
            raw = raw[1..^1];
        }

        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    /// <summary>
    /// Finds the most useful reference string for a computed (null) attribute using
    /// the configuration-level expression references stored on the model.
    /// </summary>
    /// <remarks>
    /// Preference order:
    /// 1. A static resource reference (e.g., <c>azuread_group.team</c>) — optionally combined
    ///    with a numeric instance key to give <c>azuread_group.team[0]</c>.
    /// 2. A string instance key from a for_each map (e.g., <c>"team-example - user@example.de"</c>)
    ///    when only dynamic <c>each.value.*</c> references exist.
    /// 3. Null — caller falls back to "(known after apply)".
    /// </remarks>
    /// <param name="attributeReferences">The attribute references from the model.</param>
    /// <param name="attributeName">The attribute to look up (e.g., "group_object_id").</param>
    /// <param name="instanceKey">The for_each/count instance key extracted from the resource address.</param>
    /// <returns>A display string, or null when no useful reference is available.</returns>
    private static string? FindResourceReference(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? attributeReferences,
        string attributeName,
        string? instanceKey)
    {
        if (attributeReferences is null || !attributeReferences.TryGetValue(attributeName, out var refs))
        {
            // No configuration references at all — fall back to instance key (string only) for context
            return instanceKey is not null && !IsNumericKey(instanceKey)
                ? $"\"{instanceKey}\""
                : null;
        }

        // Look for a static resource-level reference (type.name), filtering out dynamic references
        // like each.*, var.*, local.*, data.*, path.*, count.*, self.*
        var staticRef = refs.FirstOrDefault(IsStaticResourceReference);

        if (staticRef is not null)
        {
            // If the instance key is numeric (count-based), combine: resourceRef[index]
            // This handles cases like member_object_id = azuread_user.users[count.index].object_id
            // where instance 0 means azuread_user.users[0].
            if (instanceKey is not null && IsNumericKey(instanceKey))
            {
                return $"{staticRef}[{instanceKey}]";
            }

            return staticRef;
        }

        // Only dynamic references (each.value.*, etc.) — use the string instance key as context
        // since it often contains meaningful information (e.g., the group name and user email).
        return instanceKey is not null && !IsNumericKey(instanceKey)
            ? $"\"{instanceKey}\""
            : null;
    }

    /// <summary>
    /// Returns true when the reference is a static resource reference (type.name or module path)
    /// rather than a dynamic expression like each.value.*, var.*, etc.
    /// </summary>
    private static bool IsStaticResourceReference(string reference)
    {
        // Dynamic prefixes are never useful as display labels
        if (reference.StartsWith("each.", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("var.", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("local.", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("path.", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("count.", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("self.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = reference.Split('.');
        // Acceptable formats: type.name (2 parts) or module.mod.type.name (4 parts)
        // Strip any trailing [index] from the last segment before counting
        return parts.Length == 2 || (parts.Length == 4 && parts[0].Equals("module", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns true when the instance key string is a numeric (count-based) index.
    /// </summary>
    private static bool IsNumericKey(string key) =>
        int.TryParse(key, out _);

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
