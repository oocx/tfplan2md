using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Providers.AzureAD.Models;

/// <summary>
/// Rebuilds Azure AD group summaries after parent-child merging to show correct member counts.
/// </summary>
/// <remarks>
/// This class contains the Azure-specific logic that was previously in ReportModelBuilder.
/// It runs as a post-merge callback to update group summaries with accurate member counts
/// after inline and separate members have been merged into child resource groups.
/// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
/// </remarks>
internal static class AzureAdGroupSummaryRebuilder
{
    /// <summary>
    /// Updates Azure AD group summaries after parent-child merging completes.
    /// </summary>
    /// <param name="allChanges">All resource changes after merging.</param>
    /// <param name="principalMapper">Principal mapper for resolving member types.</param>
    public static void UpdateGroupSummaries(
        List<ResourceChangeModel> allChanges,
        IPrincipalMapper? principalMapper)
    {
        if (principalMapper is null)
        {
            return;
        }

        foreach (var change in allChanges)
        {
            if (!string.Equals(change.Type, "azuread_group", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (change.ChildResourceGroups.Count == 0)
            {
                continue;
            }

            var membersGroup = change.ChildResourceGroups
                .FirstOrDefault(g => string.Equals(g.Label, "Members", StringComparison.OrdinalIgnoreCase));

            if (membersGroup == null)
            {
                continue;
            }

            // Extract member IDs from all child rows and count by type
            var memberIds = ExtractMemberIds(membersGroup.Rows);
            var (userCount, groupCount, spCount, unknownCount) = CountMembersByType(memberIds, principalMapper);

            // Rebuild the icon count portion of the summary
            var newIconCounts = BuildMemberCountSummary(userCount, groupCount, spCount, unknownCount);

            // Replace the old icon counts in the summary
            change.SummaryHtml = UpdateSummaryHtmlIconCounts(change.SummaryHtml, newIconCounts);
        }
    }

    /// <summary>
    /// Extracts member IDs from child resource rows.
    /// </summary>
    /// <param name="rows">The child rows containing member data.</param>
    /// <returns>List of member IDs.</returns>
    private static List<string> ExtractMemberIds(IReadOnlyList<ChildResourceRow> rows)
    {
        var memberIds = new List<string>();

        foreach (var row in rows)
        {
            // The "member" column contains the formatted member value
            // We need to extract the raw member ID from the row's source
            if (row.Values.TryGetValue("member", out var memberValue))
            {
                // Extract member ID from the formatted value
                // The format is typically "Name [member-id]" or just "member-id"
                var memberId = ExtractMemberIdFromFormattedValue(memberValue);
                if (!string.IsNullOrWhiteSpace(memberId))
                {
                    memberIds.Add(memberId);
                }
            }
        }

        return memberIds;
    }

    /// <summary>
    /// Extracts the member ID from a formatted member value.
    /// </summary>
    /// <param name="formattedValue">The formatted value (e.g., "Alice [user-1]" or "`user-1`").</param>
    /// <returns>The extracted member ID.</returns>
    private static string ExtractMemberIdFromFormattedValue(string formattedValue)
    {
        // Handle format: "Name [id]"
        var bracketStart = formattedValue.LastIndexOf('[');
        var bracketEnd = formattedValue.LastIndexOf(']');

        if (bracketStart >= 0 && bracketEnd > bracketStart)
        {
            return formattedValue.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
        }

        // If no brackets, the value might be just the ID or wrapped in backticks or HTML code tags
        // Remove HTML tags and backticks if present
        var cleaned = formattedValue
            .Replace("<code>", "")
            .Replace("</code>", "")
            .Replace("`", "")
            .Trim();

        return cleaned;
    }

    /// <summary>
    /// Counts members by type using the principal mapper.
    /// </summary>
    /// <param name="memberIds">List of member IDs to count.</param>
    /// <param name="principalMapper">Principal mapper for resolving member types.</param>
    /// <returns>Tuple of counts (users, groups, service principals, unknown).</returns>
    private static (int UserCount, int GroupCount, int SpCount, int UnknownCount) CountMembersByType(
        List<string> memberIds,
        IPrincipalMapper principalMapper)
    {
        var userCount = 0;
        var groupCount = 0;
        var spCount = 0;
        var unknownCount = 0;

        foreach (var memberId in memberIds)
        {
            if (principalMapper.TryGetPrincipalType(memberId, out var principalType))
            {
                if (string.Equals(principalType, "User", StringComparison.OrdinalIgnoreCase))
                {
                    userCount++;
                }
                else if (string.Equals(principalType, "Group", StringComparison.OrdinalIgnoreCase))
                {
                    groupCount++;
                }
                else if (string.Equals(principalType, "ServicePrincipal", StringComparison.OrdinalIgnoreCase))
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

        return (userCount, groupCount, spCount, unknownCount);
    }

    /// <summary>
    /// Builds the member count summary string for Azure AD groups.
    /// </summary>
    /// <param name="userCount">Number of user members.</param>
    /// <param name="groupCount">Number of group members.</param>
    /// <param name="spCount">Number of service principal members.</param>
    /// <param name="unknownCount">Number of unknown members.</param>
    /// <returns>Formatted member count summary.</returns>
    private static string BuildMemberCountSummary(int userCount, int groupCount, int spCount, int unknownCount)
    {
        const string nbsp = "\u00A0";
        var summary = $"{userCount} 👤{nbsp}{groupCount} 👥{nbsp}{spCount} 💻";

        if (unknownCount > 0)
        {
            summary = $"{summary}{nbsp}{unknownCount} ❓";
        }

        return summary.TrimEnd(nbsp[0]);
    }

    /// <summary>
    /// Updates the icon counts portion of a summary HTML string.
    /// </summary>
    /// <param name="summaryHtml">The original summary HTML.</param>
    /// <param name="newIconCounts">The new icon counts string.</param>
    /// <returns>Updated summary HTML.</returns>
    private static string? UpdateSummaryHtmlIconCounts(string? summaryHtml, string newIconCounts)
    {
        if (string.IsNullOrWhiteSpace(summaryHtml))
        {
            return summaryHtml;
        }

        // The icon counts are in a <code> tag after the display name
        // Pattern: ... | <code>0 👤 0 👥 0 💻</code> | ...
        // We need to replace the content between <code> and </code> that contains member icons

        // Find the section with member icons (contains 👤 or 👥 or 💻)
        var pattern = @"<code>[\d\s]*👤[^<]*</code>";
        var regex = new System.Text.RegularExpressions.Regex(
            pattern,
            System.Text.RegularExpressions.RegexOptions.None,
            System.TimeSpan.FromSeconds(1));
        var match = regex.Match(summaryHtml);

        if (match.Success)
        {
            var replacement = $"<code>{newIconCounts}</code>";
            return regex.Replace(summaryHtml, replacement, 1);
        }

        return summaryHtml;
    }
}
