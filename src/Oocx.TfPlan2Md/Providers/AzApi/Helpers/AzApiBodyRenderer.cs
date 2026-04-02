using System.Diagnostics.CodeAnalysis;
using System.Text;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Renders AzAPI body and output-values sections.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Body rendering combines grouping, diffing, and sensitivity handling to preserve historical azapi behavior.")]
internal static partial class AzApiBodyRenderer
{
    /// <summary>
    /// Renders create/delete body content.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="heading">Section heading.</param>
    /// <param name="body">Body JSON value.</param>
    /// <param name="sensitivity">Sensitivity structure for body values.</param>
    /// <param name="context">Render context.</param>
    internal static void RenderCreateDeleteBody(
        MarkdownWriter writer,
        string heading,
        object body,
        object? sensitivity,
        IRenderContext context)
    {
        var plan = AzApiBodyRenderPlanner.BuildCreateDeletePlan(body, sensitivity, context.ShowSensitive);

        WriteHeading(writer, heading);

        WriteCreateDeleteTable(writer, plan.TableProperties, context, plan.ShouldRenderMainTable);

        foreach (var group in plan.PrefixGroups)
        {
            WriteCreateDeletePrefixGroup(writer, group, context);
        }

        foreach (var group in plan.ArrayGroups)
        {
            WriteCreateDeleteArrayGroup(writer, group, context);
        }

        WriteLargeCreateDeleteProperties(writer, plan.LargeProperties, context);

        if (plan.IsEmpty)
        {
            writer.Paragraph($"*{heading}: (empty)*");
            writer.BlankLine();
        }
    }

    /// <summary>
    /// Renders update body content.
    /// </summary>
    /// <param name="writer">Markdown writer target.</param>
    /// <param name="heading">Section heading.</param>
    /// <param name="beforeBody">Before-state body value.</param>
    /// <param name="afterBody">After-state body value.</param>
    /// <param name="beforeSensitive">Before-state sensitivity structure.</param>
    /// <param name="afterSensitive">After-state sensitivity structure.</param>
    /// <param name="context">Render context.</param>
    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Body update rendering requires grouped/array-aware branching for azapi parity.")]
    [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Legacy parity requires action and grouping branches in one place.")]
    internal static void RenderUpdateBody(
        MarkdownWriter writer,
        string heading,
        object beforeBody,
        object afterBody,
        object? beforeSensitive,
        object? afterSensitive,
        IRenderContext context)
    {
        var plan = AzApiBodyRenderPlanner.BuildUpdatePlan(
            beforeBody,
            afterBody,
            beforeSensitive,
            afterSensitive,
            context.ShowSensitive,
            context.IgnoreAzureIdCaseChanges);

        WriteHeading(writer, heading);

        WriteUpdateTable(writer, plan.TableProperties, context);

        foreach (var group in plan.PrefixGroups)
        {
            WriteUpdatePrefixGroup(writer, group, context);
        }

        foreach (var group in plan.ArrayGroups)
        {
            WriteUpdateArrayGroup(writer, group, context);
        }

        WriteLargeUpdateProperties(writer, plan.LargeProperties, context);

        if (!plan.HasChanges)
        {
            writer.Paragraph("*No body changes detected*");
            writer.BlankLine();
        }
    }

    private static void WriteHeading(MarkdownWriter writer, string heading)
    {
        writer.Raw($"\n#### {heading}\n\n");
    }

    private static void WriteCreateDeleteTable(
        MarkdownWriter writer,
        IReadOnlyList<AzApiCreateDeletePropertyPlan> properties,
        IRenderContext context,
        bool renderWhenEmpty)
    {
        if (properties.Count == 0 && !renderWhenEmpty)
        {
            return;
        }

        writer.Raw("| Property | Value |\n");
        writer.Raw("|----------|-------|\n");

        foreach (var property in properties)
        {
            if (property.IsSensitive)
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | (sensitive) |\n");
                continue;
            }

            var formatted = FormatValue(property.DisplayPath, property.Value?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | {formatted} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteCreateDeletePrefixGroup(
        MarkdownWriter writer,
        AzApiCreateDeletePrefixGroupPlan group,
        IRenderContext context)
    {
        var groupPath = group.Prefix;
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(groupPath)}`", 6);
        writer.BlankLine();
        writer.Raw("| Property | Value |\n");
        writer.Raw("|----------|-------|\n");

        foreach (var property in group.Properties)
        {
            if (property.IsSensitive)
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | (sensitive) |\n");
                continue;
            }

            var formatted = FormatValue(property.DisplayPath, property.Value?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | {formatted} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteCreateDeleteArrayGroup(
        MarkdownWriter writer,
        AzApiCreateDeleteArrayGroupPlan group,
        IRenderContext context)
    {
        var arrayPath = group.ArrayPath;
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(arrayPath)}` Array", 6);
        writer.BlankLine();

        var items = group.Items;
        if (items.Count == 0)
        {
            writer.BlankLine();
            return;
        }

        var columns = CollectArrayColumns(items);
        writer.Raw($"| Index | {string.Join(" | ", columns.Select(MarkdownHelpers.EscapeMarkdown))} |\n");
        writer.Raw($"|-------|{string.Join("|", columns.Select(_ => "-------"))}|\n");

        foreach (var item in items)
        {
            List<string> cells = [];
            foreach (var column in columns)
            {
                var entry = item.Entries.FirstOrDefault(candidate => string.Equals(candidate.DisplayPath, column, StringComparison.Ordinal));
                if (entry is null)
                {
                    cells.Add(string.Empty);
                    continue;
                }

                if (entry.IsSensitive)
                {
                    cells.Add("(sensitive)");
                    continue;
                }

                cells.Add(FormatValue(column, entry.Value?.ToString(), context));
            }

            writer.Raw($"| [{item.Index}] | {string.Join(" | ", cells)} |\n");
        }

        writer.BlankLine();
    }

    private static List<string> CollectArrayColumns(IReadOnlyList<AzApiCreateDeleteArrayItem> items)
    {
        List<string> columns = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (var item in items)
        {
            foreach (var localPath in item.Entries.Select(entry => entry.DisplayPath))
            {
                if (!seen.Add(localPath))
                {
                    continue;
                }

                columns.Add(localPath);
            }
        }

        return columns;
    }

    private static void WriteUpdateTable(MarkdownWriter writer, IReadOnlyList<AzApiUpdatePropertyPlan> properties, IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        writer.Raw("| Property | Before | After |\n");
        writer.Raw("|----------|--------|-------|\n");

        foreach (var property in properties)
        {
            if (property.IsSensitive && !context.ShowSensitive)
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | (sensitive) | (sensitive) |\n");
                continue;
            }

            var before = FormatValue(property.DisplayPath, property.Before?.ToString(), context);
            var after = FormatValue(property.DisplayPath, property.After?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(property.DisplayPath)} | {before} | {after} |\n");
        }

        writer.BlankLine();
    }

}
