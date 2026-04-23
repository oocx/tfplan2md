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

        var rows = new (string Label, ActionSummary Summary)[]
        {
            ("➕\u00A0Add", summary.ToAdd),
            ("🔄\u00A0Change", summary.ToChange),
            ("♻️\u00A0Replace", summary.ToReplace),
            ("❌\u00A0Destroy", summary.ToDestroy),
            ("📥\u00A0Import", summary.Imports),
            ("🔀\u00A0Move", summary.Moves),
        };

        var visibleRows = rows.Where(row => row.Summary.Count > 0).ToList();
        if (visibleRows.Count == 0)
        {
            writer.Paragraph("No changes");
            writer.BlankLine();
            return;
        }

        writer.Raw("| Action | Count | Resource Types |\n");
        writer.Raw("| -------- | ------- | ---------------- |\n");

        foreach (var (label, actionSummary) in visibleRows)
        {
            writer.TableRow([label, actionSummary.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(actionSummary.Breakdown)]);
        }

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
