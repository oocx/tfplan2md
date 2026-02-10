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
            }

            if (groups.Count == 0)
            {
                continue;
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
                rows.Add(BuildInlineRow(parent, relationship, entry.Element, ActionIcons.Add, attributeName));
            }

            return rows;
        }

        if (parent.Action == DeleteAction)
        {
            foreach (var entry in beforeEntries)
            {
                rows.Add(BuildInlineRow(parent, relationship, entry.Element, ActionIcons.Delete, attributeName));
            }

            return rows;
        }

        var beforeLookup = BuildInlineEntryLookup(beforeEntries);
        foreach (var entry in afterEntries)
        {
            if (TryConsumeInlineEntry(beforeLookup, entry.Key))
            {
                rows.Add(BuildInlineRow(parent, relationship, entry.Element, ActionIcons.Unchanged, attributeName));
                continue;
            }

            rows.Add(BuildInlineRow(parent, relationship, entry.Element, ActionIcons.Add, attributeName));
        }

        foreach (var remaining in FlattenInlineEntries(beforeLookup))
        {
            rows.Add(BuildInlineRow(parent, relationship, remaining.Element, ActionIcons.Delete, attributeName));
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
            return [];
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

            var row = BuildSeparateRow(child, relationship, childState);
            rows.Add(row);
            removedChildren.Add(child);
            MoveFindingsToParent(parent, child);
        }

        return rows;
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
}
