using System;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Platforms.Azure;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Rebuilds Azure AD group summaries after parent-child merging to include all members.
/// </summary>
/// <remarks>
/// Related issue: docs/issues/069-parent-child-summary-count-mismatch/analysis.md.
/// </remarks>
internal sealed class AzureAdGroupSummaryRebuilder : IParentSummaryRebuilder
{
    private const string UserMemberType = "User";
    private const string GroupMemberType = "Group";
    private const string ServicePrincipalMemberType = "ServicePrincipal";
    private const string UnknownMemberType = "Unknown";
    private const string NonBreakingSpace = "\u00A0";

    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAdGroupSummaryRebuilder"/> class.
    /// </summary>
    /// <param name="principalMapper">Mapper for resolving principal types.</param>
    public AzureAdGroupSummaryRebuilder(IPrincipalMapper principalMapper)
    {
        _principalMapper = principalMapper;
    }

    /// <summary>
    /// Determines if this rebuilder can handle the parent resource.
    /// </summary>
    /// <param name="parent">The parent resource to check.</param>
    /// <returns><c>true</c> if this is an Azure AD group resource.</returns>
    public bool CanRebuild(ResourceChangeModel parent)
    {
        return string.Equals(parent.Type, "azuread_group", StringComparison.OrdinalIgnoreCase)
               && parent.ChildResourceGroups.Count > 0;
    }

    /// <summary>
    /// Rebuilds the Azure AD group summary to include all members from child resources.
    /// </summary>
    /// <param name="context">The rebuild context.</param>
    public void RebuildSummary(ParentSummaryRebuildContext context)
    {
        var parent = context.Parent;
        var memberGroup = parent.ChildResourceGroups.FirstOrDefault(g =>
            string.Equals(g.Label, "Members", StringComparison.OrdinalIgnoreCase));

        if (memberGroup == null || memberGroup.Rows.Count == 0)
        {
            return;
        }

        var userCount = 0;
        var groupCount = 0;
        var spCount = 0;
        var unknownCount = 0;

#pragma warning disable S3267 // SonarQube false positive: Counting by category, not transforming
        foreach (var row in memberGroup.Rows)
        {
            if (row.MemberId == null)
            {
                unknownCount++;
                continue;
            }

            if (_principalMapper.TryGetPrincipalType(row.MemberId, out var principalType))
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
#pragma warning restore S3267

        var userIcon = ResolveMemberTypeIcon(parent, UserMemberType, context.IconProviderRegistry);
        var groupIcon = ResolveMemberTypeIcon(parent, GroupMemberType, context.IconProviderRegistry);
        var spIcon = ResolveMemberTypeIcon(parent, ServicePrincipalMemberType, context.IconProviderRegistry);
        var unknownIcon = ResolveMemberTypeIcon(parent, UnknownMemberType, context.IconProviderRegistry);

        var summaryCounts = BuildMemberCountSummary(
            userCount,
            userIcon,
            groupCount,
            groupIcon,
            spCount,
            spIcon,
            unknownCount,
            unknownIcon);

        // Extract the name summary portion (everything before the first | or —)
        var currentSummary = parent.SummaryHtml ?? string.Empty;
        var separatorIndex = currentSummary.IndexOf(" | ", StringComparison.Ordinal);
        if (separatorIndex == -1)
        {
            separatorIndex = currentSummary.IndexOf(" — ", StringComparison.Ordinal);
        }

        string nameSummary;
        string? childSummary = null;
        if (separatorIndex > 0)
        {
            nameSummary = currentSummary.Substring(0, separatorIndex);
            // Check if there's a child summary after the counts
            var secondSeparatorIndex = currentSummary.IndexOf(" | ", separatorIndex + 3, StringComparison.Ordinal);
            if (secondSeparatorIndex > 0)
            {
                childSummary = currentSummary.Substring(secondSeparatorIndex);
            }
        }
        else
        {
            nameSummary = currentSummary;
        }

        var updatedSummary = $"{nameSummary} | {FormatCodeSummary(summaryCounts)}";
        if (!string.IsNullOrWhiteSpace(childSummary))
        {
            updatedSummary = $"{updatedSummary}{childSummary}";
        }

        parent.SummaryHtml = updatedSummary;
    }

    /// <summary>
    /// Resolves the icon for a member type.
    /// </summary>
    private static string ResolveMemberTypeIcon(
        ResourceChangeModel model,
        string memberType,
        IconProviderRegistry? iconProviderRegistry)
    {
        if (iconProviderRegistry is null)
        {
            return string.Empty;
        }

        var context = new ServiceResolutionContext(model.ProviderName, "azuread_group_member", "member_type", memberType);
        return iconProviderRegistry.TryGetIcon(context) ?? string.Empty;
    }

    /// <summary>
    /// Builds a member count summary string with non-breaking spaces after icons.
    /// </summary>
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
    private static string FormatMemberCountSegment(int count, string icon)
    {
        return string.IsNullOrWhiteSpace(icon)
            ? count.ToString()
            : $"{count} {icon}{NonBreakingSpace}";
    }
}
