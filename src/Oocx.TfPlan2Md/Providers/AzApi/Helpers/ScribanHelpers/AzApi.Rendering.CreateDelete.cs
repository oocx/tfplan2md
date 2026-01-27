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
    /// Header row for create/delete property tables.
    /// </summary>
    private const string CreateDeleteTableHeader = "| Property | Value |";

    /// <summary>
    /// Separator row for create/delete property tables.
    /// </summary>
    private const string CreateDeleteTableSeparator = "|----------|-------|";

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

        var groups = IdentifyGroupedPrefixes(smallProps);
        var mainProps = ExtractNonGroupedProperties(smallProps, groups);

        RenderCreateDeleteMainTable(sb, mainProps, groups.Count == 0);
        RenderCreateDeleteGroupedSections(sb, groups, smallProps);
        RenderLargeCreateDeleteProps(sb, largeProps, largeValueFormat);
        RenderNoChangesMessage(sb, smallProps.Count, largeProps.Count, $"*{heading}: (empty)*");
    }

    /// <summary>
    /// Extracts the properties that are not covered by any grouping section.
    /// </summary>
    /// <param name="properties">The original flattened properties.</param>
    /// <param name="groups">The identified groups.</param>
    /// <returns>A script array containing only non-grouped properties in original order.</returns>
    private static ScriptArray ExtractNonGroupedProperties(
        ScriptArray properties,
        IReadOnlyList<AzApiGroupedPrefix> groups)
    {
        var groupedIndexes = groups.SelectMany(group => group.MemberIndexes).ToHashSet();
        var mainProps = new ScriptArray();

        for (var index = 0; index < properties.Count; index++)
        {
            if (groupedIndexes.Contains(index))
            {
                continue;
            }

            mainProps.Add(properties[index]);
        }

        return mainProps;
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

        sb.AppendLine(CreateDeleteTableHeader);
        sb.AppendLine(CreateDeleteTableSeparator);

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
                var value = prop[AzApiValueKey];

                path = RemovePropertiesPrefix(path);

                var valueFormatted = FormatAttributeValueTable(path, value?.ToString(), AzApiValueFormatProviderName);
                sb.AppendLine($"| {EscapeMarkdown(path)} | {valueFormatted} |");
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders grouped sections for prefix and array groupings.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="groups">The selected grouping sections.</param>
    /// <param name="properties">The original flattened properties (small values only).</param>
    private static void RenderCreateDeleteGroupedSections(
        StringBuilder sb,
        IReadOnlyList<AzApiGroupedPrefix> groups,
        ScriptArray properties)
    {
        foreach (var group in groups)
        {
            var groupProps = new ScriptArray();
            foreach (var index in group.MemberIndexes)
            {
                if (index >= 0 && index < properties.Count)
                {
                    groupProps.Add(properties[index]);
                }
            }

            if (group.Kind == AzApiGroupedPrefixKind.Array)
            {
                RenderCreateDeleteArrayGroup(sb, group.Path, groupProps);
            }
            else
            {
                RenderCreateDeletePrefixGroup(sb, group.Path, groupProps);
            }
        }
    }

    /// <summary>
    /// Renders a non-array prefix group section as a simple property/value table.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="groupPath">The prefix path for the section.</param>
    /// <param name="groupProps">The grouped properties.</param>
    private static void RenderCreateDeletePrefixGroup(StringBuilder sb, string groupPath, ScriptArray groupProps)
    {
        sb.AppendLine($"###### `{EscapeMarkdown(groupPath)}`");
        sb.AppendLine();
        sb.AppendLine(CreateDeleteTableHeader);
        sb.AppendLine(CreateDeleteTableSeparator);

        foreach (var item in groupProps)
        {
            if (item is not ScriptObject prop)
            {
                continue;
            }

            var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
            var value = prop[AzApiValueKey];

            var localPath = RemoveNestedPrefix(path, groupPath);
            var valueFormatted = FormatAttributeValueTable(localPath, value?.ToString(), AzApiValueFormatProviderName);
            sb.AppendLine($"| {EscapeMarkdown(localPath)} | {valueFormatted} |");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders an array group section using either a matrix table or per-item tables.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="arrayPath">The array path for the section.</param>
    /// <param name="groupProps">The grouped properties that belong to the array.</param>
    private static void RenderCreateDeleteArrayGroup(StringBuilder sb, string arrayPath, ScriptArray groupProps)
    {
        sb.AppendLine($"###### `{EscapeMarkdown(arrayPath)}` Array");
        sb.AppendLine();

        var items = ExtractArrayItems(groupProps, arrayPath, isUpdateMode: false);
        if (items.Count == 0)
        {
            return;
        }

        if (ShouldRenderMatrixTable(items, maxPropertiesPerItem: 8))
        {
            RenderCreateDeleteArrayMatrixTable(sb, items);
        }
        else
        {
            RenderCreateDeleteArrayPerItemTables(sb, items);
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Determines whether an array section should be rendered as a matrix table.
    /// </summary>
    /// <param name="items">The extracted array items.</param>
    /// <param name="maxPropertiesPerItem">The maximum number of properties per item for matrix rendering.</param>
    /// <returns><c>true</c> when the array is suitable for a matrix table; otherwise <c>false</c>.</returns>
    private static bool ShouldRenderMatrixTable(
        IReadOnlyList<AzApiArrayItem> items,
        int maxPropertiesPerItem)
    {
        if (items.Count == 0)
        {
            return false;
        }

        var firstKeys = items[0].Entries.Select(entry => entry.LocalPath).ToHashSet(StringComparer.Ordinal);

        return items.All(item =>
        {
            if (item.Entries.Count > maxPropertiesPerItem)
            {
                return false;
            }

            var keys = item.Entries.Select(entry => entry.LocalPath).ToHashSet(StringComparer.Ordinal);
            return keys.SetEquals(firstKeys);
        });
    }

    /// <summary>
    /// Renders a matrix table for array items in create/delete mode.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="items">The extracted array items.</param>
    private static void RenderCreateDeleteArrayMatrixTable(StringBuilder sb, IReadOnlyList<AzApiArrayItem> items)
    {
        var headers = items[0].Entries.Select(entry => entry.LocalPath).ToList();

        sb.Append('|');
        sb.Append(" Index ");
        foreach (var header in headers)
        {
            sb.Append('|');
            sb.Append(' ');
            sb.Append(EscapeMarkdown(header));
            sb.Append(' ');
        }

        sb.AppendLine("|");

        sb.Append('|');
        sb.Append("-------");
        foreach (var _ in headers)
        {
            sb.Append("|-------");
        }

        sb.AppendLine("|");

        foreach (var item in items)
        {
            var byKey = item.Entries
                .GroupBy(entry => entry.LocalPath, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            sb.Append('|');
            sb.Append(' ');
            sb.Append(item.IndexLabel);
            sb.Append(' ');

            foreach (var header in headers)
            {
                sb.Append('|');
                sb.Append(' ');

                var entry = byKey[header];
                var valueFormatted = FormatAttributeValueTable(header, entry.Value?.ToString(), AzApiValueFormatProviderName);
                sb.Append(valueFormatted);
                sb.Append(' ');
            }

            sb.AppendLine("|");
        }
    }

    /// <summary>
    /// Renders per-item tables for array items in create/delete mode.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="items">The extracted array items.</param>
    private static void RenderCreateDeleteArrayPerItemTables(StringBuilder sb, IReadOnlyList<AzApiArrayItem> items)
    {
        foreach (var item in items)
        {
            sb.AppendLine($"**Item [{item.Index}]**");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("|----------|-------|");

            foreach (var entry in item.Entries)
            {
                var valueFormatted = FormatAttributeValueTable(entry.LocalPath, entry.Value?.ToString(), AzApiValueFormatProviderName);
                sb.AppendLine($"| {EscapeMarkdown(entry.LocalPath)} | {valueFormatted} |");
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
                var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
                var value = prop[AzApiValueKey];

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
