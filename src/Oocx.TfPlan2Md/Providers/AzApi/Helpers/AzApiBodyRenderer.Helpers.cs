using System.Text;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Helper routines for AzApi body markdown emission.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal static partial class AzApiBodyRenderer
{
    private const string ProviderForFormatting = "azurerm";

    private static void WriteUpdatePrefixGroup(MarkdownWriter writer, AzApiUpdatePrefixGroupPlan group, IRenderContext context)
    {
        var groupPath = group.Prefix;
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(groupPath)}`", 6);
        writer.BlankLine();
        writer.Raw("| Property | Before | After |\n");
        writer.Raw("|----------|--------|-------|\n");

        foreach (var property in group.Properties)
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

    private static void WriteUpdateArrayGroup(
        MarkdownWriter writer,
        AzApiUpdateArrayGroupPlan group,
        IRenderContext context)
    {
        var arrayPath = group.ArrayPath;
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(arrayPath)}` Array", 6);
        writer.BlankLine();

        foreach (var item in group.Items)
        {
            writer.Paragraph($"**Item [{item.Index}]**");
            writer.BlankLine();
            writer.Raw("| Property | Before | After |\n");
            writer.Raw("|----------|--------|-------|\n");

            foreach (var entry in item.Entries)
            {
                if (entry.IsSensitive && !context.ShowSensitive)
                {
                    writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(entry.DisplayPath)} | (sensitive) | (sensitive) |\n");
                    continue;
                }

                var beforeFormatted = FormatValue(entry.DisplayPath, entry.Before?.ToString(), context);
                var afterFormatted = FormatValue(entry.DisplayPath, entry.After?.ToString(), context);
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(entry.DisplayPath)} | {beforeFormatted} | {afterFormatted} |\n");
            }

            writer.BlankLine();
        }

        writer.BlankLine();
    }

    private static void WriteLargeCreateDeleteProperties(
        MarkdownWriter writer,
        IReadOnlyList<AzApiCreateDeletePropertyPlan> properties,
        IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget) == LargeValueFormat.SimpleDiff
            ? "simple-diff"
            : "inline-diff";

        writer.Raw("<details>\n<summary>Large body properties</summary>\n\n");

        foreach (var property in properties)
        {
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(property.DisplayPath)}:**", 5);
            writer.BlankLine();

            if (property.IsSensitive)
            {
                writer.Paragraph("(sensitive)");
            }
            else
            {
                writer.Raw(MarkdownHelpers.FormatLargeValue(null, property.Value?.ToString(), largeValueFormat));
                writer.Raw("\n");
            }

            writer.BlankLine();
        }

        writer.Raw("</details>\n\n");
    }

    private static void WriteLargeUpdateProperties(MarkdownWriter writer, IReadOnlyList<AzApiUpdatePropertyPlan> properties, IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        var largeValueFormat = ReportModelBuilder.ConvertRenderTargetToLargeValueFormat(context.RenderTarget) == LargeValueFormat.SimpleDiff
            ? "simple-diff"
            : "inline-diff";

        writer.Raw("<details>\n<summary>Large body property changes</summary>\n\n");

        foreach (var property in properties)
        {
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(property.DisplayPath)}:**", 5);
            writer.BlankLine();

            if (property.IsSensitive && !context.ShowSensitive)
            {
                writer.Paragraph("(sensitive)");
            }
            else
            {
                writer.Raw(MarkdownHelpers.FormatLargeValue(property.Before?.ToString(), property.After?.ToString(), largeValueFormat));
                writer.Raw("\n");
            }

            writer.BlankLine();
        }

        writer.Raw("</details>\n\n");
    }

    private static string FormatValue(string? attributeName, string? value, IRenderContext context)
    {
        return MarkdownHelpers.FormatAttributeValueTableWithRegistryResource(
            attributeName,
            value,
            ProviderForFormatting,
            null,
            context.ValueFormatterRegistry,
            context.IconProviderRegistry);
    }
}
