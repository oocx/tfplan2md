using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Fallback renderer for resource types without a specialized provider renderer.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Default renderer composes shared markdown behaviors for multiple model types after Scriban removal.")]
internal sealed class DefaultResourceRenderer : IResourceRenderer
{
    private const string DetailsStyle = " style=\"margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;\"";

    /// <inheritdoc />
    public string ResourceType => "*";

    /// <inheritdoc />
    public void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(context);

        var detailsTag = context.DetailsDisplayMode switch
        {
            RenderTargets.DetailsDisplayMode.Open => "<details open",
            RenderTargets.DetailsDisplayMode.Closed => "<details",
            _ => change.CodeAnalysisFindings.Count > 0 ? "<details open" : "<details"
        };

        var summary = string.IsNullOrWhiteSpace(change.SummaryHtml)
            ? $"{change.ActionSymbol}\u00A0{ScribanHelpers.EscapeMarkdown(change.Type)} {ScribanHelpers.FormatCodeTable(change.Name)}"
            : change.SummaryHtml;

        writer.Raw(detailsTag + DetailsStyle + ">");
        writer.Raw("<summary>");
        writer.Raw(summary);
        writer.Raw("</summary>\n");
        writer.Raw("<br>\n");

        RenderCodeAnalysisMetadata(writer, change.CodeAnalysisFindings);

        var smallAttributes = change.AttributeChanges.Where(attribute => !attribute.IsLarge).ToArray();
        var largeAttributes = change.AttributeChanges.Where(attribute => attribute.IsLarge).ToArray();

        RenderAttributeTable(writer, change, smallAttributes);

        if (!string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.TagsBadges);
        }

        if (smallAttributes.Length == 0 && largeAttributes.Length == 0 && string.IsNullOrWhiteSpace(change.TagsBadges))
        {
            writer.Paragraph(change.HasWholeResourceUnknownAfterApply
                ? "_(all values known after apply)_"
                : "_No attribute changes._");
        }

        RenderChildResources(writer, change.ChildResourceGroups);
        RenderCodeAnalysisFindings(writer, change);
        RenderLargeAttributes(writer, largeAttributes, smallAttributes.Length > 0 || !string.IsNullOrWhiteSpace(change.TagsBadges), context);

        writer.DetailsClose();
        writer.BlankLine();
    }

    /// <summary>
    /// Renders attribute changes table according to action semantics.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    private static void RenderAttributeTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes)
    {
        if (smallAttributes.Length == 0)
        {
            return;
        }

        if (change.Action is "create" or "delete")
        {
            RenderSingleValueTable(writer, change, smallAttributes);
        }
        else
        {
            RenderBeforeAfterTable(writer, change, smallAttributes);
        }

        writer.BlankLine();
    }

    /// <summary>
    /// Renders a two-column attribute table for create/delete actions.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    private static void RenderSingleValueTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes)
    {
        writer.TableHeader("Attribute", "Value");

        foreach (var attribute in smallAttributes)
        {
            if (ShouldSkipTagAttribute(change, attribute.Name))
            {
                continue;
            }

            var raw = change.Action == "create" ? attribute.After : attribute.Before;
            var value = ScribanHelpers.FormatAttributeValueTable(attribute.Name, raw, change.ProviderName);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                ScribanHelpers.EscapeMarkdown(attribute.Name) + indicator,
                value
            ]);
        }
    }

    /// <summary>
    /// Renders a three-column before/after attribute table for update-like actions.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="change">Resource change model.</param>
    /// <param name="smallAttributes">Non-large attribute changes.</param>
    private static void RenderBeforeAfterTable(MarkdownWriter writer, ResourceChangeModel change, AttributeChangeModel[] smallAttributes)
    {
        writer.TableHeader("Attribute", "Before", "After");

        foreach (var attribute in smallAttributes)
        {
            var beforeValue = ScribanHelpers.FormatAttributeValueTable(attribute.Name, attribute.Before, change.ProviderName);
            var afterValue = ScribanHelpers.FormatAttributeValueTable(attribute.Name, attribute.After, change.ProviderName);
            var indicator = GetAttributeFindingIndicator(attribute.Name, change.CodeAnalysisFindings);

            writer.TableRow([
                ScribanHelpers.EscapeMarkdown(attribute.Name) + indicator,
                string.IsNullOrEmpty(beforeValue) ? "-" : beforeValue,
                string.IsNullOrEmpty(afterValue) ? "-" : afterValue
            ]);
        }
    }

    private static void RenderCodeAnalysisMetadata(MarkdownWriter writer, IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        if (findings.Count == 0)
        {
            return;
        }

        var criticalCount = findings.Count(finding => string.Equals(finding.Severity, "Critical", StringComparison.Ordinal));
        var highCount = findings.Count(finding => string.Equals(finding.Severity, "High", StringComparison.Ordinal));
        var mediumCount = findings.Count(finding => string.Equals(finding.Severity, "Medium", StringComparison.Ordinal));
        var lowCount = findings.Count(finding => string.Equals(finding.Severity, "Low", StringComparison.Ordinal));
        var informationalCount = findings.Count(finding => string.Equals(finding.Severity, "Informational", StringComparison.Ordinal));

        var parts = new List<string>();
        if (criticalCount > 0)
        {
            parts.Add($"🚨\u00A0{criticalCount} Critical");
        }

        if (highCount > 0)
        {
            parts.Add($"⚠️\u00A0{highCount} High");
        }

        if (mediumCount > 0)
        {
            parts.Add($"⚠️\u00A0{mediumCount} Medium");
        }

        if (lowCount > 0)
        {
            parts.Add($"ℹ️\u00A0{lowCount} Low");
        }

        if (informationalCount > 0)
        {
            parts.Add($"ℹ️\u00A0{informationalCount} Informational");
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

        writer.Heading($"🔒\u00A0Security & Quality Findings for {ScribanHelpers.FormatCodeTable(change.Address)}", 4);
        writer.BlankLine();

        writer.TableHeader("Severity", "Tool", "Attribute", "Finding", "Remediation");

        foreach (var finding in change.CodeAnalysisFindings)
        {
            var message = ScribanHelpers.EscapeMarkdownTableCell(finding.Message).Replace("\n", "<br/>", StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(finding.RuleId))
            {
                message += "<br/>Rule: " + ScribanHelpers.FormatCodeTable(finding.RuleId);
            }

            if (!string.IsNullOrWhiteSpace(finding.ResourceAddress)
                && !string.Equals(finding.ResourceAddress, change.Address, StringComparison.Ordinal))
            {
                message += "<br/>Resource: " + ScribanHelpers.FormatCodeTable(finding.ResourceAddress);
            }

            var remediation = string.IsNullOrWhiteSpace(finding.HelpUri)
                ? "-"
                : $"[Details](<{ScribanHelpers.EscapeMarkdownLinkDestination(finding.HelpUri)}>)";

            writer.TableRow([
                $"{finding.SeverityIcon}\u00A0{finding.Severity}",
                string.IsNullOrWhiteSpace(finding.ToolName) ? "-" : finding.ToolName,
                string.IsNullOrWhiteSpace(finding.AttributePath) ? "-" : MarkdownWriter.InlineCode(ScribanHelpers.EscapeMarkdown(finding.AttributePath)),
                message,
                remediation
            ]);
        }

        writer.BlankLine();
    }

    private static void RenderChildResources(MarkdownWriter writer, IReadOnlyList<ChildResourceGroup> childResourceGroups)
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

            writer.TableHeader(headers);

            foreach (var row in group.Rows)
            {
                var cells = new List<string> { row.ChangeIndicator };

                foreach (var column in group.Columns)
                {
                    row.Values.TryGetValue(column.PropertyName, out var value);
                    cells.Add(ScribanHelpers.FormatChildValue(value));
                }

                if (group.HasExternalResources)
                {
                    cells.Add(ScribanHelpers.FormatChildValue(row.TerraformResource));
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

        var summary = ScribanHelpers.LargeAttributesSummary(largeAttributes);
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
            writer.Heading($"**{ScribanHelpers.EscapeMarkdown(attribute.Name)}:**", 5);
            writer.BlankLine();
            writer.Raw(ScribanHelpers.FormatLargeValue(attribute.Before, attribute.After, largeValueFormat));
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
}
