using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Renderers;

/// <summary>
/// Renders merged NSG security-rule child groups using the canonical parent-child report model.
/// Related issue: docs/issues/112-missing-nsg-rule-report/analysis.md.
/// </summary>
internal static class NsgMergedSecurityRulesRenderer
{
    /// <summary>
    /// Finds the merged security-rules child group when separate NSG rule resources were attached to the parent.
    /// </summary>
    /// <param name="change">The resource change being rendered.</param>
    /// <returns>The merged security-rules group when present; otherwise, <c>null</c>.</returns>
    public static ChildResourceGroup? GetMergedSecurityRulesGroup(ResourceChangeModel change)
    {
        return change.ChildResourceGroups.FirstOrDefault(group =>
            string.Equals(group.Label, "Security Rules", StringComparison.OrdinalIgnoreCase)
            && group.HasExternalResources);
    }

    /// <summary>
    /// Renders merged security-rule rows from the parent-child model.
    /// </summary>
    /// <param name="writer">The markdown writer.</param>
    /// <param name="change">The resource change being rendered.</param>
    /// <param name="context">The render context.</param>
    /// <param name="group">The merged security-rules child group.</param>
    public static void Render(MarkdownWriter writer, ResourceChangeModel change, IRenderContext context, ChildResourceGroup group)
    {
        AzureRmDelegatingRenderer.WriteDetailsOpen(writer, change, context);

        writer.Heading("Security Rules", 4);
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
                cells.Add(MarkdownHelpers.FormatChildValue(value));
            }

            if (group.HasExternalResources)
            {
                cells.Add(MarkdownHelpers.FormatChildValue(row.TerraformResource));
            }

            writer.TableRow(cells);
        }

        writer.BlankLine();
        writer.DetailsClose();
        writer.BlankLine();
    }
}
