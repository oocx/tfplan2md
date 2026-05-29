using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Helper routines for the default resource renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed partial class DefaultResourceRenderer
{
    private static void RenderCodeAnalysisMetadata(MarkdownWriter writer, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        if (findings.Count == 0)
        {
            return;
        }

        var countBySeverity = findings
            .GroupBy(f => f.Severity, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key ?? string.Empty, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        string[] severityOrder = ["Critical", "High", "Medium", "Low", "Informational"];
        string[] severityIcons = ["🚨", "⚠️", "⚠️", "ℹ️", "ℹ️"];

        var parts = new List<string>();
        for (var i = 0; i < severityOrder.Length; i++)
        {
            if (countBySeverity.TryGetValue(severityOrder[i], out var count) && count > 0)
            {
                parts.Add($"{severityIcons[i]}\u00A0{count} {severityOrder[i]}");
            }
        }

        if (parts.Count == 0)
        {
            return;
        }

        writer.Paragraph($"🔒\u00A0**Security & Quality:** {string.Join(", ", parts)}");
        writer.BlankLine();
    }

    private static void RenderCodeAnalysisFindings(MarkdownWriter writer, ResourceChangeModel change)
    {
        if (change.CodeAnalysisFindings.Count == 0)
        {
            return;
        }

        writer.Heading($"🔒\u00A0Security & Quality Findings for {MarkdownHelpers.FormatCodeTable(change.Address)}", 4);
        writer.BlankLine();

        writer.Raw("| Severity | Tool | Attribute | Finding | Remediation |\n");
        writer.Raw("| -------- | ---- | --------- | ------- | ----------- |\n");

        foreach (var finding in change.CodeAnalysisFindings)
        {
            var message = MarkdownHelpers.EscapeMarkdownTableCell(finding.Message).Replace("\n", "<br/>", StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(finding.RuleId))
            {
                message += "<br/>Rule: " + MarkdownHelpers.FormatCodeTable(finding.RuleId);
            }

            if (!string.IsNullOrWhiteSpace(finding.ResourceAddress)
                && !string.Equals(finding.ResourceAddress, change.Address, StringComparison.Ordinal))
            {
                message += "<br/>Resource: " + MarkdownHelpers.FormatCodeTable(finding.ResourceAddress);
            }

            var remediation = string.IsNullOrWhiteSpace(finding.HelpUri)
                ? "-"
                : $"[Details](<{MarkdownHelpers.EscapeMarkdownLinkDestination(finding.HelpUri)}>)";

            writer.TableRow([
                $"{finding.SeverityIcon}\u00A0{finding.Severity}",
                string.IsNullOrWhiteSpace(finding.ToolName) ? "-" : finding.ToolName,
                string.IsNullOrWhiteSpace(finding.AttributePath) ? "-" : MarkdownWriter.InlineCode(MarkdownHelpers.EscapeMarkdown(finding.AttributePath)),
                message,
                remediation
            ]);
        }

        writer.BlankLine();
    }

    private static void RenderChildResources(
        MarkdownWriter writer,
        IReadOnlyList<ChildResourceGroup> childResourceGroups)
    {
        foreach (var group in childResourceGroups)
        {
            writer.Heading(group.Label, 4);
            writer.BlankLine();

            if (group.HasMixedSources)
            {
                writer.Paragraph("⚠️\u00A0**Warning:** This resource has children managed both inline and as separate resources. This configuration will cause conflicts.");
                writer.BlankLine();
            }

            var headers = new List<string> { "Change" };
            headers.AddRange(group.Columns.Select(column => column.Header));

            if (group.HasExternalResources)
            {
                headers.Add("Terraform Resource");
            }

            var separators = headers.Select(header =>
                string.Equals(header, "Terraform Resource", StringComparison.Ordinal) ? "--------------------" : "--------");
            writer.Raw($"| {string.Join(" | ", headers)} |\n");
            writer.Raw($"| {string.Join(" | ", separators)} |\n");

            foreach (var row in group.Rows)
            {
                var cells = new List<string> { row.ChangeIndicator };

                foreach (var column in group.Columns)
                {
                    row.Values.TryGetValue(column.PropertyName, out var value);
                    cells.Add(MarkdownHelpers.FormatChildValue(value));
                }

                if (group.HasExternalResources)
                {
                    cells.Add(MarkdownHelpers.FormatChildValue(row.TerraformResource));
                }

                writer.TableRow(cells);
            }

            writer.BlankLine();
        }
    }

    private static void RenderLargeAttributes(
        MarkdownWriter writer,
        AttributeChangeModel[] largeAttributes,
        bool hasSmallAttributesOrTags,
        IRenderContext context)
    {
        if (largeAttributes.Length == 0)
        {
            return;
        }

        var summary = MarkdownHelpers.LargeAttributesSummary(largeAttributes);
        if (hasSmallAttributesOrTags)
        {
            writer.Raw("<br/>\n");
            writer.Raw("<details>\n");
            writer.Raw($"<summary>{summary}</summary>\n");
            writer.BlankLine();
            RenderLargeAttributeBodies(writer, largeAttributes, context);
            writer.Raw("</details>\n");
            writer.BlankLine();
            return;
        }

        writer.Paragraph(summary);
        writer.BlankLine();
        RenderLargeAttributeBodies(writer, largeAttributes, context);
    }

    private static void RenderLargeAttributeBodies(MarkdownWriter writer, AttributeChangeModel[] largeAttributes, IRenderContext context)
    {
        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget) == LargeValueFormat.SimpleDiff
            ? "simple-diff"
            : "inline-diff";

        foreach (var attribute in largeAttributes)
        {
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(attribute.Name)}:**", 5);
            writer.BlankLine();
            writer.Raw(MarkdownHelpers.FormatLargeValue(attribute.Before, attribute.After, largeValueFormat));
            writer.BlankLine();
        }
    }

    private static string GetAttributeFindingIndicator(string attributeName, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        var highestRank = -1;
        string? highestIcon = null;

        foreach (var finding in findings)
        {
            if (!AttributeMatches(attributeName, finding.AttributePath))
            {
                continue;
            }

            if (finding.SeverityRank > highestRank)
            {
                highestRank = finding.SeverityRank;
                highestIcon = finding.SeverityIcon;
            }
        }

        return string.IsNullOrWhiteSpace(highestIcon) ? string.Empty : " " + highestIcon;
    }

    private static bool AttributeMatches(string attributeName, string? attributePath)
    {
        if (string.IsNullOrWhiteSpace(attributePath))
        {
            return false;
        }

        return string.Equals(attributePath, attributeName, StringComparison.OrdinalIgnoreCase)
            || attributePath.StartsWith(attributeName + ".", StringComparison.OrdinalIgnoreCase)
            || attributePath.StartsWith(attributeName + "[", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipTagAttribute(ResourceChangeModel change, string attributeName)
    {
        return !string.IsNullOrWhiteSpace(change.TagsBadges)
            && attributeName.StartsWith("tags.", StringComparison.Ordinal);
    }

    /// <summary>
    /// Renders inline forced-replacement callouts and depends-on line for a replaced or destroyed resource.
    /// Emits nothing when the resource has no correlated relevant attribute annotations.
    /// Each forced-replacement entry is rendered as a blockquote line:
    ///   <c>&gt; ⚠️\u00A0**Forced replacement** — `{local}` reads `{upstream}.{path}`{phrase}</c>
    /// A single depends-on line follows (if any), using <c>🔗\u00A0**Depends on:**</c>
    /// (or <c>🔗\u00A0**Also depends on:**</c> when forced-replacement entries are also present).
    /// A trailing blank line is appended when any annotations were rendered.
    /// Related feature: docs/features/660-inline-relevant-attributes/specification.md.
    /// </summary>
    /// <param name="writer">The markdown writer to write output to.</param>
    /// <param name="change">The resource change model that may carry annotation lists.</param>
    private static void RenderInlineRelevantAttributeAnnotations(MarkdownWriter writer, ResourceChangeModel change)
    {
        var forced = change.ForcedReplacementAnnotations;
        var dependsOn = change.DependsOnAnnotations;

        if (forced.Count == 0 && dependsOn.Count == 0)
        {
            return;
        }

        foreach (var ann in forced)
        {
            // Phrase appended to line: either ", which is **changing in this plan**." or "."
            var changingPhrase = ann.IsChangingInThisPlan ? ", which is **changing in this plan**." : ".";
            writer.Paragraph(
                $"> \u26a0\ufe0f\u00A0**Forced replacement** \u2014 `{ann.LocalAttribute}` reads " +
                $"`{ann.UpstreamResource}.{ann.UpstreamAttributePath}`{changingPhrase}");
        }

        if (dependsOn.Count > 0)
        {
            // Use "Also depends on:" label when forced-replacement entries are also present
            var label = forced.Count > 0 ? "Also depends on:" : "Depends on:";
            var entries = BuildDependsOnEntries(dependsOn);
            writer.Paragraph($"> \U0001f517\u00A0**{label}** {entries}");
        }

        writer.BlankLine();
    }

    private static string BuildDependsOnEntries(IReadOnlyList<Models.DependsOnAnnotation> dependsOn)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < dependsOn.Count; i++)
        {
            var ann = dependsOn[i];
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append($"`{ann.UpstreamResource}.{ann.UpstreamAttributePath}`");
            if (ann.IsChangingInThisPlan)
            {
                sb.Append(" \u26a0\ufe0f");
            }
        }

        return sb.ToString();
    }
}
