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
        flatState.TryGetValue("subscription", out var subscriptionName);
        flatState.TryGetValue("subscription_id", out var subscriptionId);
        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{FormatCodeSummary(model.Name)}</b>";
        var detailParts = new List<string>();
        var refactoringContext = BuildRefactoringContext(model);

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

        if (!string.IsNullOrWhiteSpace(subscriptionName))
        {
            detailParts.Add(FormatAttributeValueSummary("subscription", subscriptionName!, null));
        }

        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            detailParts.Add(FormatAttributeValueSummary("subscription_id", subscriptionId!, null));
        }

        if (!string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            detailParts.Add($"| {model.ChangedAttributesSummary!}");
        }

        if (!string.IsNullOrWhiteSpace(refactoringContext))
        {
            if (detailParts.Count > 0 && !detailParts[0].StartsWith('|'))
            {
                detailParts[0] = $"| {detailParts[0]}";
            }

            detailParts.Insert(0, refactoringContext);
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }

    /// <summary>
    /// Builds the refactoring annotation for summary lines when import or moved metadata is present.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing refactoring metadata.</param>
    /// <returns>Formatted refactoring annotation or empty string when not applicable.</returns>
    private static string BuildRefactoringContext(ResourceChangeModel model)
    {
        if (model.ImportId is null && model.MovedFromAddress is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (model.ImportId is not null)
        {
            parts.Add($"📥{NonBreakingSpace}<i>Imported</i>{BuildAlreadyAppliedSuffix(model, "Import")}");
        }

        if (model.MovedFromAddress is not null)
        {
            var movedFrom = FormatCodeSummary(model.MovedFromAddress);
            parts.Add($"🔀{NonBreakingSpace}<i>Moved from</i> {movedFrom}{BuildAlreadyAppliedSuffix(model, "Move")}");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Builds the suffix for already-applied refactoring warnings.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="model">Resource change model indicating already-applied status.</param>
    /// <param name="operation">The refactoring operation name (Import or Move) used to pick consistent warning wording.</param>
    /// <returns>Warning suffix or empty string when not applicable.</returns>
    private static string BuildAlreadyAppliedSuffix(ResourceChangeModel model, string operation)
    {
        if (!model.IsRefactoringAlreadyApplied)
        {
            return string.Empty;
        }

        var warning = "already applied";
        if (operation.Equals("Import", StringComparison.OrdinalIgnoreCase))
        {
            warning = "already imported";
        }
        else if (operation.Equals("Move", StringComparison.OrdinalIgnoreCase))
        {
            warning = "already moved";
        }

        return $" (⚠️{NonBreakingSpace}<i>{warning}</i>)";
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
