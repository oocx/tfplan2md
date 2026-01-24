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
    /// Renders update-mode content by comparing before/after and grouping property changes.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="heading">The heading text to display.</param>
    /// <param name="bodyJson">The after-state JSON body.</param>
    /// <param name="beforeJson">The before-state JSON body.</param>
    /// <param name="beforeSensitive">The before_sensitive structure.</param>
    /// <param name="afterSensitive">The after_sensitive structure.</param>
    /// <param name="showUnchanged">Whether to include unchanged properties.</param>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    private static void RenderUpdateBody(
        StringBuilder sb,
        string heading,
        object bodyJson,
        object beforeJson,
        object? beforeSensitive,
        object? afterSensitive,
        bool showUnchanged,
        string largeValueFormat)
    {
        var comparisons = CompareJsonProperties(beforeJson, bodyJson, beforeSensitive, afterSensitive, showUnchanged, false);
        var (smallChanges, largeChanges) = SplitBySize(comparisons);

        var groupedProps = GroupPropertiesByNestedObject(smallChanges);
        RenderUpdateMainTable(sb, groupedProps.MainProps);
        RenderUpdateNestedTables(sb, heading, groupedProps.NestedGroups);
        RenderLargeUpdateChanges(sb, largeChanges, largeValueFormat);
        RenderNoChangesMessage(sb, smallChanges.Count, largeChanges.Count, "*No body changes detected*");
    }

    /// <summary>
    /// Renders the main update table containing root-level properties.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="properties">The properties to render.</param>
    private static void RenderUpdateMainTable(StringBuilder sb, ScriptArray properties)
    {
        if (properties.Count == 0)
        {
            return;
        }

        sb.AppendLine("| Property | Before | After |");
        sb.AppendLine("|----------|--------|-------|");

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var before = prop["before"];
                var after = prop["after"];

                path = RemovePropertiesPrefix(path);

                var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), "azurerm");
                var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), "azurerm");

                sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders nested update tables for grouped properties.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="heading">The heading text to display.</param>
    /// <param name="groups">The grouped properties keyed by parent.</param>
    private static void RenderUpdateNestedTables(
        StringBuilder sb,
        string heading,
        Dictionary<string, ScriptArray> groups)
    {
        foreach (var group in groups)
        {
            sb.AppendLine($"###### {heading} - `{group.Key}`");
            sb.AppendLine();
            sb.AppendLine("| Property | Before | After |");
            sb.AppendLine("|----------|--------|-------|");

            foreach (var item in group.Value)
            {
                if (item is ScriptObject prop)
                {
                    var path = prop["path"]?.ToString() ?? string.Empty;
                    var before = prop["before"];
                    var after = prop["after"];

                    path = RemoveNestedPrefix(path, group.Key);

                    var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), "azurerm");
                    var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), "azurerm");

                    sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
                }
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Renders large update-mode property changes in a details section.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="properties">The large properties to render.</param>
    /// <param name="largeValueFormat">Format for rendering large values.</param>
    private static void RenderLargeUpdateChanges(StringBuilder sb, ScriptArray properties, string largeValueFormat)
    {
        if (properties.Count == 0)
        {
            return;
        }

        sb.AppendLine("<details>");
        sb.AppendLine("<summary>Large body property changes</summary>");
        sb.AppendLine();

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var before = prop["before"];
                var after = prop["after"];

                path = RemovePropertiesPrefix(path);

                sb.AppendLine($"##### **{EscapeMarkdown(path)}:**");
                sb.AppendLine();

                var beforeStr = before?.ToString();
                var afterStr = after?.ToString();
                sb.AppendLine(FormatLargeValue(beforeStr, afterStr, largeValueFormat));
                sb.AppendLine();
            }
        }

        sb.AppendLine("</details>");
        sb.AppendLine();
    }
}
