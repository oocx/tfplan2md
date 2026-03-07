using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Renders the summary section of the markdown report.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal static class SummaryRenderer
{
    /// <summary>
    /// Renders summary heading and summary table or no-change message.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="summary">Summary model to render.</param>
    /// <param name="boldTotal">Whether to bold the total row values.</param>
    public static void Render(MarkdownWriter writer, SummaryModel summary, bool boldTotal)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(summary);

        writer.Heading("Summary", 2);
        writer.BlankLine();

        if (summary.Total == 0)
        {
            writer.Paragraph("No changes");
            writer.BlankLine();
            return;
        }

        writer.Raw("| Action | Count | Resource Types |\n");
        writer.Raw("| -------- | ------- | ---------------- |\n");

        writer.TableRow(["➕\u00A0Add", summary.ToAdd.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(summary.ToAdd.Breakdown)]);
        writer.TableRow(["🔄\u00A0Change", summary.ToChange.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(summary.ToChange.Breakdown)]);
        writer.TableRow(["♻️\u00A0Replace", summary.ToReplace.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(summary.ToReplace.Breakdown)]);
        writer.TableRow(["❌\u00A0Destroy", summary.ToDestroy.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(summary.ToDestroy.Breakdown)]);

        var totalText = summary.Total.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (boldTotal)
        {
            writer.Raw($"| **Total** | **{totalText}** | |\n");
        }
        else
        {
            writer.Raw($"| Total | {totalText} | |\n");
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Formats resource type breakdown for summary tables.
    /// Used by both <see cref="SummaryRenderer"/> and <see cref="ReportRenderer"/>.
    /// Related feature: docs/features/111-code-simplification/specification.md (Finding 1.1).
    /// </summary>
    /// <param name="breakdown">Breakdown entries.</param>
    /// <returns>Markdown-safe table cell content.</returns>
    internal static string FormatBreakdown(IReadOnlyList<ResourceTypeBreakdown> breakdown)
    {
        if (breakdown.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "<br/>",
            breakdown.Select(entry => $"{entry.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} {MarkdownHelpers.EscapeMarkdown(entry.Type)}"));
    }
}
