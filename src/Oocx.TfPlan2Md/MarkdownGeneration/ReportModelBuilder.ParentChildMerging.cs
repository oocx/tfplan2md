using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Merges parent-child relationships by moving child rows into parent models.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    /// <param name="allChanges">The full list of resource change models to update.</param>
    private void MergeParentChildRelationships(List<ResourceChangeModel> allChanges)
    {
        if (_parentChildRelationshipRegistry.GetAllChildResourceTypes().Count == 0)
        {
            return;
        }

        var changesByType = allChanges
            .GroupBy(change => change.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var removedChildren = new HashSet<ResourceChangeModel>();

        foreach (var parent in allChanges)
        {
            var relationships = _parentChildRelationshipRegistry.GetRelationshipsForParent(parent.Type);
            if (relationships.Count == 0)
            {
                continue;
            }

            var groups = new List<ChildResourceGroup>();
            var inlineAttributeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var relationship in relationships)
            {
                var inlineRows = BuildInlineRows(parent, relationship);
                var separateRows = BuildSeparateRows(parent, relationship, changesByType, removedChildren);

                if (inlineRows.Count == 0 && separateRows.Count == 0)
                {
                    continue;
                }

                var rows = inlineRows.Concat(separateRows).ToList();
                var group = new ChildResourceGroup
                {
                    Label = relationship.ChildGroupLabel,
                    Columns = relationship.TableColumns,
                    Rows = rows,
                    HasMixedSources = inlineRows.Count > 0 && separateRows.Count > 0
                };

                groups.Add(group);

                if (!string.IsNullOrWhiteSpace(relationship.InlineAttributeName))
                {
                    inlineAttributeNames.Add(relationship.InlineAttributeName!);
                }
            }

            if (groups.Count == 0)
            {
                continue;
            }

            var usesDefaultSummaryHtml = string.Equals(parent.SummaryHtml, BuildSummaryHtml(parent), StringComparison.Ordinal);
            if (RemoveInlineAttributeChanges(parent, inlineAttributeNames))
            {
                parent.ChangedAttributesSummary = BuildChangedAttributesSummary(parent.AttributeChanges, parent.Action);
                if (usesDefaultSummaryHtml)
                {
                    parent.SummaryHtml = BuildSummaryHtml(parent);
                }
            }

            parent.ChildResourceGroups = groups;
            UpdateParentSummaryWithChildCounts(parent);
        }

        if (removedChildren.Count == 0)
        {
            return;
        }

        foreach (var child in removedChildren)
        {
            allChanges.Remove(child);
        }
    }

    /// <summary>
    /// Removes inline child attributes from the parent attribute change list when those children are rendered in tables.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    /// <param name="parent">The parent resource change model to update.</param>
    /// <param name="inlineAttributeNames">Inline attribute names to exclude from the attribute table.</param>
    /// <returns>True when attribute changes were removed; otherwise, false.</returns>
    private static bool RemoveInlineAttributeChanges(ResourceChangeModel parent, HashSet<string> inlineAttributeNames)
    {
        if (inlineAttributeNames.Count == 0 || parent.AttributeChanges.Count == 0)
        {
            return false;
        }

        if (parent.AttributeChanges is not List<AttributeChangeModel> changes)
        {
            return false;
        }

        var originalCount = changes.Count;
        changes.RemoveAll(attr => IsInlineAttributeName(attr.Name, inlineAttributeNames));
        return changes.Count != originalCount;
    }

    /// <summary>
    /// Determines whether an attribute name belongs to an inline child attribute.
    /// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
    /// </summary>
    /// <param name="attributeName">The flattened attribute name from the plan.</param>
    /// <param name="inlineAttributeNames">Inline attribute names to match against.</param>
    /// <returns>True when the attribute name should be excluded.</returns>
    private static bool IsInlineAttributeName(string attributeName, HashSet<string> inlineAttributeNames)
    {
        foreach (var inlineName in inlineAttributeNames)
        {
            if (attributeName.Equals(inlineName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (attributeName.StartsWith(inlineName + "[", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (attributeName.StartsWith(inlineName + ".", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds inline child rows from parent state attributes.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="relationship">The relationship definition to apply.</param>
    /// <returns>The inline child rows extracted from the parent state.</returns>
    private List<ChildResourceRow> BuildInlineRows(ResourceChangeModel parent, ParentChildRelationship relationship)
    {
        if (string.IsNullOrWhiteSpace(relationship.InlineAttributeName))
        {
            return [];
        }

        var attributeName = relationship.InlineAttributeName!;
        var rows = new List<ChildResourceRow>();
        var afterEntries = ExtractInlineEntries(parent.AfterJson, attributeName);
        var beforeEntries = ExtractInlineEntries(parent.BeforeJson, attributeName);

        if (parent.Action == CreateAction)
        {
            foreach (var entry in afterEntries)
            {
                if (TryBuildInlineRow(parent, relationship, entry.Element, ActionIcons.Add, attributeName, out var row))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        if (parent.Action == DeleteAction)
        {
            foreach (var entry in beforeEntries)
            {
                if (TryBuildInlineRow(parent, relationship, entry.Element, ActionIcons.Delete, attributeName, out var row))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        var beforeLookup = BuildInlineEntryLookup(beforeEntries);
        foreach (var entry in afterEntries)
        {
            if (TryConsumeInlineEntry(beforeLookup, entry.Key))
            {
                if (TryBuildInlineRow(parent, relationship, entry.Element, ActionIcons.Unchanged, attributeName, out var row))
                {
                    rows.Add(row);
                }
                continue;
            }

            if (TryBuildInlineRow(parent, relationship, entry.Element, ActionIcons.Add, attributeName, out var addedRow))
            {
                rows.Add(addedRow);
            }
        }

        foreach (var remaining in FlattenInlineEntries(beforeLookup))
        {
            if (TryBuildInlineRow(parent, relationship, remaining.Element, ActionIcons.Delete, attributeName, out var row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    /// <summary>
    /// Builds separate child rows by matching child resources to the parent.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="relationship">The relationship definition to apply.</param>
    /// <param name="changesByType">Lookup of changes by resource type.</param>
    /// <param name="removedChildren">Set of child resources already marked for removal.</param>
    /// <returns>The list of matched child rows.</returns>
    private List<ChildResourceRow> BuildSeparateRows(
        ResourceChangeModel parent,
        ParentChildRelationship relationship,
        Dictionary<string, List<ResourceChangeModel>> changesByType,
        HashSet<ResourceChangeModel> removedChildren)
    {
        if (string.IsNullOrWhiteSpace(relationship.ChildReferenceAttribute))
        {
            return [];
        }

        if (!changesByType.TryGetValue(relationship.ChildResourceType, out var candidates))
        {
            return [];
        }

        var parentState = ResolveStateForAction(parent);
        var parentId = GetFlatValue(parentState, relationship.ParentIdAttribute);
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return BuildSeparateRowsByReference(parent, relationship, candidates, removedChildren);
        }

        var rows = new List<ChildResourceRow>();
        foreach (var child in candidates)
        {
            if (ReferenceEquals(child, parent))
            {
                continue;
            }

            if (removedChildren.Contains(child))
            {
                continue;
            }

            var childState = ResolveStateForAction(child);
            var childReference = GetFlatValue(childState, relationship.ChildReferenceAttribute!);
            if (!string.Equals(childReference, parentId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryBuildSeparateRow(child, relationship, childState, out var row))
            {
                continue;
            }

            rows.Add(row);
            removedChildren.Add(child);
            MoveFindingsToParent(parent, child);
        }

        return rows;
    }

    /// <summary>
    /// Builds separate child rows by matching configuration references when parent IDs are unknown.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="relationship">The relationship definition to apply.</param>
    /// <param name="candidates">The candidate child resources to inspect.</param>
    /// <param name="removedChildren">Set of child resources already marked for removal.</param>
    /// <returns>The list of matched child rows.</returns>
    private List<ChildResourceRow> BuildSeparateRowsByReference(
        ResourceChangeModel parent,
        ParentChildRelationship relationship,
        List<ResourceChangeModel> candidates,
        HashSet<ResourceChangeModel> removedChildren)
    {
        if (_configurationReferenceIndex.Count == 0)
        {
            return [];
        }

        var rows = new List<ChildResourceRow>();
        var parentAddress = parent.Address;
        var parentIdReference = string.Concat(parentAddress, ".", relationship.ParentIdAttribute);

        foreach (var child in candidates)
        {
            if (ReferenceEquals(child, parent))
            {
                continue;
            }

            if (removedChildren.Contains(child))
            {
                continue;
            }

            var normalizedAddress = NormalizeResourceAddressForConfigurationLookup(child.Address);
            if (!_configurationReferenceIndex.TryGetValue((normalizedAddress, relationship.ChildReferenceAttribute!), out var references))
            {
                continue;
            }

            if (!ReferencesParent(references, parentAddress, parentIdReference))
            {
                continue;
            }

            var childState = ResolveStateForAction(child);
            if (!TryBuildSeparateRow(child, relationship, childState, out var row))
            {
                continue;
            }

            rows.Add(row);
            removedChildren.Add(child);
            MoveFindingsToParent(parent, child);
        }

        return rows;
    }

    /// <summary>
    /// Normalizes resource addresses for configuration lookups by removing instance keys.
    /// </summary>
    /// <param name="address">The resource address to normalize.</param>
    /// <returns>The normalized address without instance keys.</returns>
    private static string NormalizeResourceAddressForConfigurationLookup(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        if (!address.EndsWith(']'))
        {
            return address;
        }

        var bracketIndex = address.LastIndexOf('[');
        return bracketIndex < 0 ? address : address[..bracketIndex];
    }

    /// <summary>
    /// Determines whether reference values point to the expected parent resource.
    /// </summary>
    /// <param name="references">The reference values extracted from configuration.</param>
    /// <param name="parentAddress">The parent resource address.</param>
    /// <param name="parentIdReference">The parent address plus ID attribute.</param>
    /// <returns><c>true</c> when a reference matches the parent.</returns>
    private static bool ReferencesParent(
        IReadOnlyList<string> references,
        string parentAddress,
        string parentIdReference)
    {
        return references.Any(reference =>
            string.Equals(reference, parentAddress, StringComparison.OrdinalIgnoreCase)
            || string.Equals(reference, parentIdReference, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates the parent summary HTML with aggregated child change counts.
    /// </summary>
    /// <param name="parent">The parent resource change model to update.</param>
    private static void UpdateParentSummaryWithChildCounts(ResourceChangeModel parent)
    {
        if (parent.ChildResourceGroups.Count == 0)
        {
            return;
        }

        var childSummary = BuildChildSummaryText(parent.ChildResourceGroups);
        if (string.IsNullOrWhiteSpace(childSummary))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(parent.SummaryHtml))
        {
            parent.SummaryHtml = ResourceSummaryHtmlBuilder.BuildSummaryHtml(parent);
        }

        parent.SummaryHtml = parent.SummaryHtml!.Contains(" — ", StringComparison.Ordinal)
            ? $"{parent.SummaryHtml} | {childSummary}"
            : $"{parent.SummaryHtml} — {childSummary}";
    }

    /// <summary>
    /// Builds a concise summary suffix for child resource counts.
    /// </summary>
    /// <param name="groups">The child groups to summarize.</param>
    /// <returns>The summary suffix text, or empty when no rows exist.</returns>
    private static string BuildChildSummaryText(IReadOnlyList<ChildResourceGroup> groups)
    {
        var segments = new List<string>();

        foreach (var group in groups)
        {
            if (group.Rows.Count == 0)
            {
                continue;
            }

            var label = group.Label.ToLowerInvariant();
            var counts = group.Rows
                .GroupBy(row => row.ChangeIndicator)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.Count(), StringComparer.Ordinal);

            foreach (var indicator in GetSummaryIndicatorOrder())
            {
                if (!counts.TryGetValue(indicator, out var count))
                {
                    continue;
                }

                segments.Add($"{indicator}{NonBreakingSpace}{count} {label}");
            }
        }

        return string.Join(" | ", segments);
    }

    /// <summary>
    /// Defines the priority order for child summary indicators.
    /// </summary>
    /// <returns>Ordered list of change indicators.</returns>
    private static IReadOnlyList<string> GetSummaryIndicatorOrder()
    {
        return
        [
            ActionIcons.Add,
            ActionIcons.Update,
            ActionIcons.Replace,
            ActionIcons.Delete
        ];
    }

    /// <summary>
    /// Resolves the state object to use based on the resource action.
    /// </summary>
    /// <param name="change">The resource change model to evaluate.</param>
    /// <returns>The most relevant JSON state for the change action.</returns>
    private static object? ResolveStateForAction(ResourceChangeModel change)
    {
        return change.Action == DeleteAction ? change.BeforeJson : change.AfterJson ?? change.BeforeJson;
    }

    /// <summary>
    /// Builds a row for an inline child entry.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="relationship">The relationship definition.</param>
    /// <param name="childState">The inline child JSON state.</param>
    /// <param name="changeIndicator">The change indicator to use.</param>
    /// <param name="attributeName">The inline attribute name.</param>
    /// <returns>The constructed child row.</returns>
    private ChildResourceRow BuildInlineRow(
        ResourceChangeModel parent,
        ParentChildRelationship relationship,
        JsonElement childState,
        string changeIndicator,
        string attributeName)
    {
        var values = relationship.RowExtractor.ExtractRow(childState, parent.ProviderName, _valueFormatterRegistry, _iconProviderRegistry);

        return new ChildResourceRow
        {
            ChangeIndicator = changeIndicator,
            Values = values,
            TerraformResource = FormatInlineResourceLabel(attributeName)
        };
    }

    /// <summary>
    /// Attempts to build an inline child row while guarding against extractor failures.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="relationship">The relationship definition.</param>
    /// <param name="childState">The inline child JSON state.</param>
    /// <param name="changeIndicator">The change indicator to use.</param>
    /// <param name="attributeName">The inline attribute name.</param>
    /// <param name="row">The constructed row when successful.</param>
    /// <returns><c>true</c> when the row was created; otherwise <c>false</c>.</returns>
    private bool TryBuildInlineRow(
        ResourceChangeModel parent,
        ParentChildRelationship relationship,
        JsonElement childState,
        string changeIndicator,
        string attributeName,
        out ChildResourceRow row)
    {
        try
        {
            row = BuildInlineRow(parent, relationship, childState, changeIndicator, attributeName);
            return true;
        }
        catch (Exception)
        {
            row = null!;
            return false;
        }
    }

    /// <summary>
    /// Builds a row for a separate child resource.
    /// </summary>
    /// <param name="child">The child resource change model.</param>
    /// <param name="relationship">The relationship definition.</param>
    /// <param name="childState">The resolved child state.</param>
    /// <returns>The constructed child row.</returns>
    private ChildResourceRow BuildSeparateRow(
        ResourceChangeModel child,
        ParentChildRelationship relationship,
        object? childState)
    {
        var values = relationship.RowExtractor.ExtractRow(childState, child.ProviderName, _valueFormatterRegistry, _iconProviderRegistry);

        return new ChildResourceRow
        {
            ChangeIndicator = GetChildActionIndicator(child.Action),
            Values = values,
            TerraformResource = child.Address,
            OriginalResourceAddress = child.Address
        };
    }

    /// <summary>
    /// Attempts to build a separate child row while guarding against extractor failures.
    /// </summary>
    /// <param name="child">The child resource change model.</param>
    /// <param name="relationship">The relationship definition.</param>
    /// <param name="childState">The resolved child state.</param>
    /// <param name="row">The constructed row when successful.</param>
    /// <returns><c>true</c> when the row was created; otherwise <c>false</c>.</returns>
    private bool TryBuildSeparateRow(
        ResourceChangeModel child,
        ParentChildRelationship relationship,
        object? childState,
        out ChildResourceRow row)
    {
        try
        {
            row = BuildSeparateRow(child, relationship, childState);
            return true;
        }
        catch (Exception)
        {
            row = null!;
            return false;
        }
    }

    /// <summary>
    /// Moves child findings to the parent resource while preserving the original address.
    /// </summary>
    /// <param name="parent">The parent resource change model.</param>
    /// <param name="child">The child resource change model.</param>
    private static void MoveFindingsToParent(ResourceChangeModel parent, ResourceChangeModel child)
    {
        if (child.CodeAnalysisFindings.Count == 0)
        {
            return;
        }

        var updated = parent.CodeAnalysisFindings.ToList();
        updated.AddRange(child.CodeAnalysisFindings);
        parent.CodeAnalysisFindings = updated;
    }

    /// <summary>
    /// Formats the inline attribute label for display in the Terraform Resource column.
    /// </summary>
    /// <param name="attributeName">The inline attribute name.</param>
    /// <returns>The formatted inline resource label.</returns>
    private static string FormatInlineResourceLabel(string attributeName)
    {
        return string.IsNullOrWhiteSpace(attributeName) ? string.Empty : $"{attributeName} attribute";
    }

    /// <summary>
    /// Extracts a flat attribute value from the JSON state.
    /// </summary>
    /// <param name="state">The JSON state to inspect.</param>
    /// <param name="attributeName">The attribute name to resolve.</param>
    /// <returns>The attribute value, or null when missing.</returns>
    private static string? GetFlatValue(object? state, string attributeName)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            return null;
        }

        var flatState = JsonFlattener.ConvertToFlatDictionary(state);
        return flatState.TryGetValue(attributeName, out var value) ? value : null;
    }

    /// <summary>
    /// Extracts inline child entries from a parent JSON state.
    /// </summary>
    /// <param name="state">The parent JSON state to inspect.</param>
    /// <param name="attributeName">The inline attribute name.</param>
    /// <returns>The list of inline child entries.</returns>
    private static List<InlineChildEntry> ExtractInlineEntries(object? state, string attributeName)
    {
        if (state is not JsonElement element || element.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!element.TryGetProperty(attributeName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<InlineChildEntry>();
        foreach (var item in property.EnumerateArray())
        {
            var key = item.ValueKind == JsonValueKind.String
                ? item.GetString() ?? string.Empty
                : item.GetRawText();

            results.Add(new InlineChildEntry(key, item));
        }

        return results;
    }

    /// <summary>
    /// Builds a lookup for inline child entries to support set-based comparison.
    /// </summary>
    /// <param name="entries">The inline entries to index.</param>
    /// <returns>A lookup keyed by the entry signature.</returns>
    private static Dictionary<string, Queue<InlineChildEntry>> BuildInlineEntryLookup(
        IEnumerable<InlineChildEntry> entries)
    {
        var lookup = new Dictionary<string, Queue<InlineChildEntry>>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (!lookup.TryGetValue(entry.Key, out var queue))
            {
                queue = new Queue<InlineChildEntry>();
                lookup[entry.Key] = queue;
            }

            queue.Enqueue(entry);
        }

        return lookup;
    }

    /// <summary>
    /// Attempts to consume a matching inline entry from the lookup.
    /// </summary>
    /// <param name="lookup">The inline entry lookup.</param>
    /// <param name="key">The entry key to consume.</param>
    /// <returns><c>true</c> if an entry was consumed; otherwise <c>false</c>.</returns>
    private static bool TryConsumeInlineEntry(Dictionary<string, Queue<InlineChildEntry>> lookup, string key)
    {
        if (!lookup.TryGetValue(key, out var queue) || queue.Count == 0)
        {
            return false;
        }

        queue.Dequeue();
        if (queue.Count == 0)
        {
            lookup.Remove(key);
        }

        return true;
    }

    /// <summary>
    /// Flattens remaining inline entries in a lookup to a list.
    /// </summary>
    /// <param name="lookup">The inline entry lookup.</param>
    /// <returns>The remaining entries.</returns>
    private static IEnumerable<InlineChildEntry> FlattenInlineEntries(
        Dictionary<string, Queue<InlineChildEntry>> lookup)
    {
        foreach (var queue in lookup.Values)
        {
            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }
    }

    /// <summary>
    /// Maps a resource action to a child change indicator.
    /// </summary>
    /// <param name="action">The resource action string.</param>
    /// <returns>The change indicator icon.</returns>
    private static string GetChildActionIndicator(string action)
    {
        return action switch
        {
            CreateAction => ActionIcons.Add,
            DeleteAction => ActionIcons.Delete,
            UpdateAction => ActionIcons.Update,
            ReplaceAction => ActionIcons.Replace,
            _ => ActionIcons.Unchanged
        };
    }

    /// <summary>
    /// Represents a single inline child entry used for diffing.
    /// </summary>
    /// <param name="Key">The comparison key for the entry.</param>
    /// <param name="Element">The underlying JSON element.</param>
    private sealed record InlineChildEntry(string Key, JsonElement Element);

    /// <summary>
    /// Updates Azure AD group summaries after parent-child merging to include merged member counts.
    /// Related issue: docs/issues/070-parent-child-summary-member-counts/analysis.md.
    /// </summary>
    /// <param name="allChanges">The full list of resource change models.</param>
    private void UpdateAzureAdGroupSummaries(List<ResourceChangeModel> allChanges)
    {
        if (_principalMapper is null)
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
            var (userCount, groupCount, spCount, unknownCount) = CountMembersByType(memberIds);

            // Rebuild the icon count portion of the summary
            var newIconCounts = BuildAzureAdMemberCountSummary(userCount, groupCount, spCount, unknownCount);

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
    /// <returns>Tuple of counts (users, groups, service principals, unknown).</returns>
    private (int UserCount, int GroupCount, int SpCount, int UnknownCount) CountMembersByType(List<string> memberIds)
    {
        var userCount = 0;
        var groupCount = 0;
        var spCount = 0;
        var unknownCount = 0;

        foreach (var memberId in memberIds)
        {
            if (_principalMapper!.TryGetPrincipalType(memberId, out var principalType))
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
    private static string BuildAzureAdMemberCountSummary(int userCount, int groupCount, int spCount, int unknownCount)
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
