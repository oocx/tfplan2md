using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
public partial class ReportModelBuilder
{
    /// <summary>
    /// Builds a summary-safe HTML string for use inside summary elements, including action icon, type, name, location, address space, and changed attributes.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing the source data.</param>
    /// <returns>HTML string safe for use inside a summary element.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model)
    {
        var state = model.AfterJson ?? model.BeforeJson;
        var flatState = ConvertToFlatDictionary(state);

        flatState.TryGetValue("name", out var nameValue);
        flatState.TryGetValue("resource_group_name", out var resourceGroup);
        flatState.TryGetValue("location", out var location);
        flatState.TryGetValue("address_space[0]", out var addressSpace);

        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{ScribanHelpers.FormatCodeSummary(model.Name)}</b>";

        var detailParts = new List<string>();

        var primaryContext = !string.IsNullOrWhiteSpace(nameValue)
            ? ScribanHelpers.FormatAttributeValueSummary("name", nameValue!, null)
            : null;

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            var groupText = ScribanHelpers.FormatAttributeValueSummary("resource_group_name", resourceGroup!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} in {groupText}" : groupText;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationText = ScribanHelpers.FormatAttributeValueSummary("location", location!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {locationText}" : locationText;
        }

        if (primaryContext != null)
        {
            detailParts.Add(primaryContext);
        }

        if (!string.IsNullOrWhiteSpace(addressSpace))
        {
            detailParts.Add(ScribanHelpers.FormatAttributeValueSummary("address_space[0]", addressSpace!, null));
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
    /// <param name="attributeChanges">Attribute changes for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted summary or empty string when not applicable.</returns>
    private static string BuildChangedAttributesSummary(IReadOnlyList<AttributeChangeModel> attributeChanges, string action)
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
    /// <param name="after">After-state JSON for the resource.</param>
    /// <param name="before">Before-state JSON for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted tags badge string or null when no tags or not applicable.</returns>
    private static string? BuildTagsBadges(object? after, object? before, string action)
    {
        if (!string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var state = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase) ? before : after;
        var flat = ConvertToFlatDictionary(state);

        var tags = flat.Where(kvp => kvp.Key.StartsWith("tags.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new { Key = kvp.Key[5..], Value = kvp.Value })
            .ToList();

        if (tags.Count == 0)
        {
            return null;
        }

        var badges = tags.Select(tag => ScribanHelpers.FormatCodeTable($"{tag.Key}: {tag.Value}"));
        return $"**🏷️{NonBreakingSpace}Tags:** {string.Join(' ', badges)}";
    }
}
