using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.MarkdownHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Group-focused summary builders for Azure AD resources.
/// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
/// </summary>
internal static partial class AzureAdSummaryBuilder
{
    private const string DisplayNameAttribute = "display_name";
    private const string MailNicknameAttribute = "mail_nickname";
    private const string DescriptionAttribute = "description";
    private const string GroupObjectIdAttribute = "group_object_id";
    private const string MemberObjectIdAttribute = "member_object_id";
    private const string KnownAfterApplyPrefix = "(known after apply";

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
        var displayName = JsonStateReader.GetStringProperty(state, DisplayNameAttribute);
        var mailNickname = JsonStateReader.GetStringProperty(state, MailNicknameAttribute);
        var description = JsonStateReader.GetStringProperty(state, DescriptionAttribute);
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
            [
                new MemberCountSummarySegment(userCount, userIcon),
                new MemberCountSummarySegment(groupCount, groupIcon),
                new MemberCountSummarySegment(spCount, spIcon),
                new MemberCountSummarySegment(unknownCount, unknownIcon),
            ]);

        var nameSummary = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            nameSummary = FormatSummaryValue(model, DisplayNameAttribute, displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(mailNickname))
        {
            var nicknameValue = FormatSummaryValue(model, MailNicknameAttribute, mailNickname, iconProviderRegistry);
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
        var displayName = JsonStateReader.GetStringProperty(state, DisplayNameAttribute);
        var mailNickname = JsonStateReader.GetStringProperty(state, MailNicknameAttribute);
        var description = JsonStateReader.GetStringProperty(state, DescriptionAttribute);

        var summaryText = string.Empty;
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            summaryText = FormatSummaryValue(model, DisplayNameAttribute, displayName, iconProviderRegistry);
        }

        if (!string.IsNullOrWhiteSpace(mailNickname))
        {
            var nicknameText = FormatSummaryValue(model, MailNicknameAttribute, mailNickname, iconProviderRegistry);
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
        var groupId = JsonStateReader.GetStringProperty(state, GroupObjectIdAttribute) ?? string.Empty;
        var memberId = JsonStateReader.GetStringProperty(state, MemberObjectIdAttribute) ?? string.Empty;

        var groupReference = TryGetStaticReference(model, GroupObjectIdAttribute);
        var memberReference = TryGetStaticReference(model, MemberObjectIdAttribute);
        var (stringInstanceKey, numericInstanceKey) = ExtractInstanceKey(model.Address);

        if (!string.IsNullOrWhiteSpace(groupReference)
            && !string.IsNullOrWhiteSpace(numericInstanceKey))
        {
            groupReference = $"{groupReference}[{numericInstanceKey}]";
        }

        var hasComputedGroupId = HasComputedAttribute(model, GroupObjectIdAttribute);
        var hasComputedMemberId = HasComputedAttribute(model, MemberObjectIdAttribute);
        var groupFallback = ResolveComputedFallback(groupReference, stringInstanceKey);
        var memberFallback = ResolveComputedFallback(memberReference, stringInstanceKey);

        var groupName = string.IsNullOrWhiteSpace(groupId) && hasComputedGroupId
            ? groupFallback
            : principalMapper.GetName(groupId, GroupMemberType, model.Address) ?? groupId;
        var groupIsMapped = !string.IsNullOrWhiteSpace(groupId)
            && groupName != string.Empty
            && groupName != groupId;

        var groupSummary = groupIsMapped
            ? BuildPrincipalSummary(model, "group_name", groupName, groupId, iconProviderRegistry)
            : FormatCodeSummary(groupName);

        var summaryText = groupSummary;
        if (!string.IsNullOrEmpty(memberId) || hasComputedMemberId)
        {
            var memberSummary = BuildMemberSummary(
                model,
                principalMapper,
                iconProviderRegistry,
                memberId,
                memberFallback);

            summaryText = $"{summaryText} {MemberArrow} {memberSummary}";
        }

        return BuildSummaryHtml(model, summaryText);
    }

    /// <summary>
    /// Builds member summary text for group membership summary lines.
    /// </summary>
    /// <param name="model">The resource change model.</param>
    /// <param name="principalMapper">Mapper used for principal resolution.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    /// <param name="memberId">Resolved member ID from state.</param>
    /// <param name="memberFallback">Fallback display label for computed IDs.</param>
    /// <returns>Formatted member summary text.</returns>
    private static string BuildMemberSummary(
        ResourceChangeModel model,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry,
        string memberId,
        string memberFallback)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return FormatCodeSummary(memberFallback);
        }

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
    /// Resolves a static resource-level reference from model configuration references.
    /// </summary>
    /// <param name="model">The resource model containing configuration references.</param>
    /// <param name="attributeName">Top-level attribute name.</param>
    /// <returns>Static resource-level reference when available; otherwise <see langword="null"/>.</returns>
    private static string? TryGetStaticReference(ResourceChangeModel model, string attributeName)
    {
        if (!model.ConfigurationReferences.TryGetValue(attributeName, out var references)
            || references.Count == 0)
        {
            return null;
        }

        return ReferenceSelector.SelectResourceLevelReference(references);
    }

    /// <summary>
    /// Resolves fallback text for computed group/member IDs.
    /// </summary>
    /// <param name="preferredReference">Preferred reference value.</param>
    /// <param name="stringInstanceKey">String instance key from resource address.</param>
    /// <returns>Fallback label for display.</returns>
    private static string ResolveComputedFallback(string? preferredReference, string? stringInstanceKey)
    {
        if (!string.IsNullOrWhiteSpace(preferredReference))
        {
            return preferredReference;
        }

        if (!string.IsNullOrWhiteSpace(stringInstanceKey))
        {
            return stringInstanceKey;
        }

        return "(known after apply)";
    }

    /// <summary>
    /// Extracts optional string/numeric instance keys from a resource address.
    /// </summary>
    /// <param name="address">Terraform resource address.</param>
    /// <returns>Tuple containing string instance key and numeric instance key.</returns>
    private static (string? StringKey, string? NumericKey) ExtractInstanceKey(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || !address.EndsWith(']'))
        {
            return (null, null);
        }

        var openBracketIndex = address.LastIndexOf('[');
        if (openBracketIndex < 0 || openBracketIndex + 2 > address.Length)
        {
            return (null, null);
        }

        var rawKey = address[(openBracketIndex + 1)..^1];
        if (rawKey.Length >= 2 && rawKey[0] == '"' && rawKey[^1] == '"')
        {
            return (rawKey, null);
        }

        return int.TryParse(rawKey, out _)
            ? (null, rawKey)
            : (null, null);
    }

    /// <summary>
    /// Builds a member count summary string that uses non-breaking spaces after icons.
    /// </summary>
    /// <param name="segments">Summary segments to concatenate.</param>
    /// <returns>Formatted summary string.</returns>
    private static string BuildMemberCountSummary(IReadOnlyList<MemberCountSummarySegment> segments)
    {
        var summary = string.Empty;
        foreach (var segment in segments)
        {
            summary = string.Concat(summary, FormatMemberCountSegment(segment.Count, segment.Icon));
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

    /// <summary>
    /// Determines whether an attribute is rendered as a computed known-after-apply value.
    /// </summary>
    /// <param name="model">Resource model containing flattened attribute changes.</param>
    /// <param name="attributeName">Top-level attribute name to inspect.</param>
    /// <returns><see langword="true"/> when the attribute is known-after-apply; otherwise <see langword="false"/>.</returns>
    private static bool HasComputedAttribute(ResourceChangeModel model, string attributeName)
    {
        foreach (var attribute in model.AttributeChanges)
        {
            if (!string.Equals(attribute.Name, attributeName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(attribute.After)
                && attribute.After.StartsWith(KnownAfterApplyPrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Represents one member-count summary segment.
    /// </summary>
    /// <param name="Count">Number of members for the segment.</param>
    /// <param name="Icon">Display icon for the segment.</param>
    private readonly record struct MemberCountSummarySegment(int Count, string Icon);
}
