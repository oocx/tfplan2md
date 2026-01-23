using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;

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
    private static string BuildSummaryHtml(ResourceChangeModel model) =>
        ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);

    /// <summary>
    /// Builds a concise changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2, +N more").
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeChanges">Attribute changes for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted summary or empty string when not applicable.</returns>
    private static string BuildChangedAttributesSummary(System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges, string action) =>
        ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(attributeChanges, action);

    /// <summary>
    /// Builds inline tag badges for create/delete operations, keeping templates free from tag formatting logic.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="after">After-state JSON for the resource.</param>
    /// <param name="before">Before-state JSON for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted tags badge string or null when no tags or not applicable.</returns>
    private static string? BuildTagsBadges(object? after, object? before, string action) =>
        ResourceSummaryHtmlBuilder.BuildTagsBadges(after, before, action);
}
