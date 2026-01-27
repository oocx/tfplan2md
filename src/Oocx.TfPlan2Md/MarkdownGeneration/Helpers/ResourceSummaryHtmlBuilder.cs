using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

/// <summary>
/// Builds summary HTML strings and badges for resource changes.
/// </summary>
/// <remarks>
/// Extracted from ReportModelBuilder to reduce class coupling.
/// Related feature: docs/features/024-visual-report-enhancements/specification.md.
/// </remarks>
internal static class ResourceSummaryHtmlBuilder
{
    /// <summary>
    /// Builds a summary-safe HTML string for use inside summary elements, including action icon, type, name, location, address space, and changed attributes.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing the source data.</param>
    /// <returns>HTML string safe for use inside a summary element.</returns>
    [SuppressMessage(
        "Maintainability",
        "CA1502:Avoid excessive complexity",
        Justification = "Feature formatting logic for docs/features/051-display-enhancements/specification.md.")]
    public static string BuildSummaryHtml(ResourceChangeModel model)
    {
        var state = model.AfterJson ?? model.BeforeJson;
        var flatState = JsonFlattener.ConvertToFlatDictionary(state);

        flatState.TryGetValue("name", out var nameValue);
        flatState.TryGetValue("resource_group_name", out var resourceGroup);
        flatState.TryGetValue("location", out var location);
        flatState.TryGetValue("address_space[0]", out var addressSpace);
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        var detailParts = new List<string>();

        var primaryContext = !string.IsNullOrWhiteSpace(nameValue)
            ? FormatAttributeValueSummary("name", nameValue!, null)
            : null;

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            var groupText = FormatAttributeValueSummary("resource_group_name", resourceGroup!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} in {groupText}" : groupText;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationText = FormatAttributeValueSummary("location", location!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {locationText}" : locationText;
        }

        if (primaryContext != null)
        {
            detailParts.Add(primaryContext);
        }

        if (!string.IsNullOrWhiteSpace(addressSpace))
        {
            detailParts.Add(FormatAttributeValueSummary("address_space[0]", addressSpace!, null));
        }

        if (!string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            detailParts.Add($"| {model.ChangedAttributesSummary!}");
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }

    /// <summary>
    /// Builds a concise changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2, +N more").
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeChanges">The list of attribute changes.</param>
    /// <param name="action">The resource action (create, update, delete, etc.).</param>
    /// <returns>Summary string for changed attributes, or empty if not an update.</returns>
    public static string BuildChangedAttributesSummary(IReadOnlyList<AttributeChangeModel> attributeChanges, string action)
    {
        if (!string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (attributeChanges.Count == 0)
        {
            return string.Empty;
        }

        var names = attributeChanges.Select(a => a.Name).ToList();
        var displayedNames = names.Take(3).ToList();
        var remaining = names.Count - displayedNames.Count;

        var nameList = string.Join(", ", displayedNames);
        if (remaining > 0)
        {
            nameList += $", +{remaining} more";
        }

        return $"{names.Count}🔧{NonBreakingSpace}{nameList}";
    }


    /// <summary>
    /// Builds inline tag badges for create/delete operations, keeping templates free from tag formatting logic.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="afterJson">The after state JSON.</param>
    /// <param name="beforeJson">The before state JSON.</param>
    /// <param name="action">The resource action (create, update, delete, etc.).</param>
    /// <returns>Tags badge string, or null if no tags or on updates.</returns>
    public static string? BuildTagsBadges(object? afterJson, object? beforeJson, string action)
    {
        if (!string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var state = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase) ? beforeJson : afterJson;
        var flat = JsonFlattener.ConvertToFlatDictionary(state);

        var tags = flat.Where(kvp => kvp.Key.StartsWith("tags.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new { Key = kvp.Key[5..], Value = kvp.Value })
            .ToList();

        if (tags.Count == 0)
        {
            return null;
        }

        var badges = tags.Select(tag => FormatCodeTable($"{tag.Key}: {tag.Value}"));
        return $"**🏷️{NonBreakingSpace}Tags:** {string.Join(' ', badges)}";
    }
}
