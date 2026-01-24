using System.Text;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/040-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Renders create/delete-mode content by flattening and grouping body properties.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="heading">The heading text to display.</param>
    /// <param name="bodyJson">The JSON body to render.</param>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    private static void RenderCreateDeleteBody(
        StringBuilder sb,
        string heading,
        object bodyJson,
        string largeValueFormat)
    {
        var flattened = FlattenJson(bodyJson, string.Empty);
        var (smallProps, largeProps) = SplitBySize(flattened);

        var groupedProps = GroupPropertiesByNestedObject(smallProps);
        RenderCreateDeleteMainTable(sb, groupedProps.MainProps, groupedProps.NestedGroups.Count == 0);
        RenderCreateDeleteNestedTables(sb, heading, groupedProps.NestedGroups);
        RenderLargeCreateDeleteProps(sb, largeProps, largeValueFormat);
        RenderNoChangesMessage(sb, smallProps.Count, largeProps.Count, $"*{heading}: (empty)*");
    }

    /// <summary>
    /// Renders the main create/delete table containing root-level properties.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="properties">The properties to render.</param>
    /// <param name="renderWhenEmpty">Whether to render a table even when no main properties exist.</param>
    private static void RenderCreateDeleteMainTable(
        StringBuilder sb,
        ScriptArray properties,
        bool renderWhenEmpty)
    {
        if (properties.Count == 0 && !renderWhenEmpty)
        {
            return;
        }

        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|----------|-------|");

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var value = prop["value"];

                path = RemovePropertiesPrefix(path);

                var valueFormatted = FormatAttributeValueTable(path, value?.ToString(), "azurerm");
                sb.AppendLine($"| {EscapeMarkdown(path)} | {valueFormatted} |");
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders nested create/delete tables for grouped properties.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="heading">The heading text to display.</param>
    /// <param name="groups">The grouped properties keyed by parent.</param>
    private static void RenderCreateDeleteNestedTables(
        StringBuilder sb,
        string heading,
        Dictionary<string, ScriptArray> groups)
    {
        foreach (var group in groups)
        {
            sb.AppendLine($"###### {heading} - `{group.Key}`");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|----------|-------|");

            foreach (var item in group.Value)
            {
                if (item is ScriptObject prop)
                {
                    var path = prop["path"]?.ToString() ?? string.Empty;
                    var value = prop["value"];

                    path = RemoveNestedPrefix(path, group.Key);

                    var valueFormatted = FormatAttributeValueTable(path, value?.ToString(), "azurerm");
                    sb.AppendLine($"| {EscapeMarkdown(path)} | {valueFormatted} |");
                }
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Renders large create/delete properties in a details section.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="properties">The large properties to render.</param>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    private static void RenderLargeCreateDeleteProps(StringBuilder sb, ScriptArray properties, string largeValueFormat)
    {
        if (properties.Count == 0)
        {
            return;
        }

        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Large body properties</summary>");
        sb.AppendLine();

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var value = prop["value"];

                path = RemovePropertiesPrefix(path);

                sb.AppendLine($"##### **{EscapeMarkdown(path)}:**");
                sb.AppendLine();

                var valueStr = value?.ToString();
                sb.AppendLine(FormatLargeValue(null, valueStr, largeValueFormat));
                sb.AppendLine();
            }
        }

        sb.AppendLine("</details>");
        sb.AppendLine();
    }
}
