using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Renders code-analysis sections for the report.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class CodeAnalysisSectionRenderer
{
    /// <summary>
    /// Renders the top-level code analysis summary section.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="codeAnalysis">Code analysis model.</param>
    public static void RenderSummary(MarkdownWriter writer, CodeAnalysisReportModel? codeAnalysis)
    {
        if (codeAnalysis is null)
        {
            return;
        }

        writer.Heading("Code Analysis Summary", 2);
        writer.BlankLine();

        if (codeAnalysis.Summary.TotalCount == 0)
        {
            writer.Paragraph("No code analysis findings were reported.");
            writer.BlankLine();
            return;
        }

        if (codeAnalysis.Summary.CriticalCount > 0)
        {
            writer.Paragraph($"**Status:** 🚨\u00A0{codeAnalysis.Summary.CriticalCount} critical findings require attention");
        }
        else if (codeAnalysis.Summary.HighCount > 0)
        {
            writer.Paragraph($"**Status:** ⚠️\u00A0{codeAnalysis.Summary.HighCount} high findings require attention");
        }
        else
        {
            writer.Paragraph("**Status:** ✅\u00A0No critical or high findings");
        }

        writer.BlankLine();
        writer.Raw("| Severity | Count | Resource Types |\n");
        writer.Raw("| -------- | ----- | -------------- |\n");
        writer.TableRow(["🚨\u00A0Critical", codeAnalysis.Summary.CriticalCount.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(codeAnalysis.Summary.CriticalResourceTypes)]);
        writer.TableRow(["⚠️\u00A0High", codeAnalysis.Summary.HighCount.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(codeAnalysis.Summary.HighResourceTypes)]);
        writer.TableRow(["⚠️\u00A0Medium", codeAnalysis.Summary.MediumCount.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(codeAnalysis.Summary.MediumResourceTypes)]);
        writer.TableRow(["ℹ️\u00A0Low", codeAnalysis.Summary.LowCount.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(codeAnalysis.Summary.LowResourceTypes)]);
        writer.TableRow(["ℹ️\u00A0Informational", codeAnalysis.Summary.InformationalCount.ToString(System.Globalization.CultureInfo.InvariantCulture), FormatBreakdown(codeAnalysis.Summary.InformationalResourceTypes)]);
        writer.BlankLine();

        if (codeAnalysis.Tools.Count > 0)
        {
            writer.Paragraph($"**Tools Used:** {string.Join(", ", codeAnalysis.Tools.Select(tool => ScribanHelpers.EscapeMarkdown(tool.DisplayName)))}");
            writer.BlankLine();
        }

        if (codeAnalysis.Warnings.Count == 0)
        {
            return;
        }

        writer.Heading("Code Analysis Warnings", 3);
        writer.BlankLine();

        foreach (var warning in codeAnalysis.Warnings)
        {
            writer.Paragraph($"⚠️\u00A0**Warning:** Unable to process code analysis file {ScribanHelpers.FormatCodeTable(warning.FilePath)}");
            writer.Paragraph($"- Error: {ScribanHelpers.EscapeMarkdown(warning.Message)}");
            writer.BlankLine();
        }
    }

    /// <summary>
    /// Renders module-level and unmatched code-analysis findings.
    /// </summary>
    /// <param name="writer">Markdown writer.</param>
    /// <param name="codeAnalysis">Code analysis model.</param>
    public static void RenderOtherFindings(MarkdownWriter writer, CodeAnalysisReportModel? codeAnalysis)
    {
        if (codeAnalysis is null || (codeAnalysis.ModuleFindings.Count == 0 && codeAnalysis.UnmatchedFindings.Count == 0))
        {
            return;
        }

        writer.Heading("Other Findings", 2);
        writer.BlankLine();

        foreach (var module in codeAnalysis.ModuleFindings)
        {
            var moduleText = string.IsNullOrWhiteSpace(module.ModuleAddress)
                ? "root"
                : ScribanHelpers.FormatCodeTable(module.ModuleAddress);

            writer.Heading($"📦\u00A0Module: {moduleText}", 3);
            writer.BlankLine();
            RenderOtherFindingsTable(writer, module.Findings);
        }

        if (codeAnalysis.UnmatchedFindings.Count == 0)
        {
            return;
        }

        writer.Heading("Unmatched Findings", 3);
        writer.BlankLine();
        RenderOtherFindingsTable(writer, codeAnalysis.UnmatchedFindings);
    }

    private static void RenderOtherFindingsTable(MarkdownWriter writer, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        writer.Raw("| Severity | Tool | Finding | Remediation |\n");
        writer.Raw("| -------- | ---- | ------- | ----------- |\n");

        foreach (var finding in findings)
        {
            var message = ScribanHelpers.EscapeMarkdownTableCell(finding.Message).Replace("\n", "<br/>", StringComparison.Ordinal);
            if (!string.IsNullOrWhiteSpace(finding.RuleId))
            {
                message += "<br/>Rule: " + ScribanHelpers.FormatCodeTable(finding.RuleId);
            }

            writer.TableRow([
                $"{finding.SeverityIcon}\u00A0{finding.Severity}",
                string.IsNullOrWhiteSpace(finding.ToolName) ? "-" : finding.ToolName,
                message,
                string.IsNullOrWhiteSpace(finding.HelpUri)
                    ? "-"
                    : $"[Details]({ScribanHelpers.EscapeMarkdown(finding.HelpUri)})"
            ]);
        }

        writer.BlankLine();
    }

    private static string FormatBreakdown(IReadOnlyList<ResourceTypeBreakdown> breakdown)
    {
        if (breakdown.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "<br/>",
            breakdown.Select(entry => $"{entry.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} {ScribanHelpers.EscapeMarkdown(entry.Type)}"));
    }
}
