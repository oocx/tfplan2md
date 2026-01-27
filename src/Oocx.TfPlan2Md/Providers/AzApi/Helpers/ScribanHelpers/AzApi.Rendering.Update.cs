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
    /// Provider name passed into value-formatting helpers for AzAPI rendering.
    /// </summary>
    private const string AzApiValueFormatProviderName = "azurerm";

    /// <summary>
    /// Header row for update property tables.
    /// </summary>
    private const string UpdateTableHeader = "| Property | Before | After |";

    /// <summary>
    /// Separator row for update property tables.
    /// </summary>
    private const string UpdateTableSeparator = "|----------|--------|-------|";

    /// <summary>
    /// Bundles parameters for update-mode AzAPI body rendering to keep helper signatures readable.
    /// </summary>
    /// <param name="BodyJson">The after-state JSON body.</param>
    /// <param name="BeforeJson">The before-state JSON body.</param>
    /// <param name="BeforeSensitive">The before_sensitive structure.</param>
    /// <param name="AfterSensitive">The after_sensitive structure.</param>
    /// <param name="ShowUnchanged">Whether to include unchanged properties.</param>
    /// <param name="LargeValueFormat">Format for rendering large values.</param>
    private sealed record UpdateBodyRenderInput(
        object BodyJson,
        object BeforeJson,
        object? BeforeSensitive,
        object? AfterSensitive,
        bool ShowUnchanged,
        string LargeValueFormat);

    /// <summary>
    /// Renders update-mode content by comparing before/after and grouping property changes.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="input">The update rendering inputs.</param>
    private static void RenderUpdateBody(
        StringBuilder sb,
        UpdateBodyRenderInput input)
    {
        var allComparisons = CompareJsonProperties(
            input.BeforeJson,
            input.BodyJson,
            input.BeforeSensitive,
            input.AfterSensitive,
            showUnchanged: true,
            showSensitive: false);

        var changedComparisons = CompareJsonProperties(
            input.BeforeJson,
            input.BodyJson,
            input.BeforeSensitive,
            input.AfterSensitive,
            showUnchanged: input.ShowUnchanged,
            showSensitive: false);

        var (smallAll, _) = SplitBySize(allComparisons);
        var (smallChanged, largeChanged) = SplitBySize(changedComparisons);

        var pathToAllIndex = BuildPathIndexLookup(smallAll);
        var changedIndexesInAll = BuildChangedIndexSet(pathToAllIndex, smallChanged);

        var groups = IdentifyGroupedPrefixes(smallAll);
        var (groupsToRender, mainProps) = SelectUpdateGroupsAndMainProps(smallAll, groups, changedIndexesInAll);

        RenderUpdateMainTable(sb, mainProps);
        RenderUpdateGroupedSections(sb, groupsToRender, smallAll);
        RenderLargeUpdateChanges(sb, largeChanged, input.LargeValueFormat);
        RenderNoChangesMessage(sb, smallChanged.Count, largeChanged.Count, "*No body changes detected*");
    }

    /// <summary>
    /// Builds a lookup of normalized property paths to their indices in the comparison array.
    /// </summary>
    /// <param name="properties">The properties to index.</param>
    /// <returns>Dictionary mapping path to index.</returns>
    private static Dictionary<string, int> BuildPathIndexLookup(ScriptArray properties)
    {
        var lookup = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < properties.Count; index++)
        {
            if (properties[index] is not ScriptObject prop)
            {
                continue;
            }

            var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
            var normalized = RemovePropertiesPrefix(path);

            if (!lookup.ContainsKey(normalized))
            {
                lookup[normalized] = index;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Builds a set of indices that correspond to changed properties.
    /// </summary>
    /// <param name="pathToAllIndex">Lookup from path to index in the full comparison set.</param>
    /// <param name="changed">The changed properties to map.</param>
    /// <returns>Set of indices in the full set that correspond to changes.</returns>
    private static HashSet<int> BuildChangedIndexSet(
        Dictionary<string, int> pathToAllIndex,
        ScriptArray changed)
    {
        var indexes = new HashSet<int>();

        foreach (var item in changed)
        {
            if (item is not ScriptObject prop)
            {
                continue;
            }

            var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
            var normalized = RemovePropertiesPrefix(path);
            if (pathToAllIndex.TryGetValue(normalized, out var index))
            {
                indexes.Add(index);
            }
        }

        return indexes;
    }

    /// <summary>
    /// Determines which groups should be rendered in update mode and extracts the remaining main properties.
    /// </summary>
    /// <param name="allProperties">All small properties (including unchanged).</param>
    /// <param name="allGroups">All potential groups.</param>
    /// <param name="changedIndexes">Indices (into <paramref name="allProperties"/>) that represent changed properties.</param>
    /// <returns>A tuple of groups to render and main properties to render.</returns>
    private static (IReadOnlyList<AzApiGroupedPrefix> GroupsToRender, ScriptArray MainProps) SelectUpdateGroupsAndMainProps(
        ScriptArray allProperties,
        IReadOnlyList<AzApiGroupedPrefix> allGroups,
        HashSet<int> changedIndexes)
    {
        var groupsToRender = new List<AzApiGroupedPrefix>();
        var groupedIndexes = new HashSet<int>();

        foreach (var group in allGroups)
        {
            var hasChange = group.MemberIndexes.Any(changedIndexes.Contains);
            if (!hasChange)
            {
                continue;
            }

            groupsToRender.Add(group);
            foreach (var index in group.MemberIndexes)
            {
                groupedIndexes.Add(index);
            }
        }

        var mainProps = new ScriptArray();
        for (var index = 0; index < allProperties.Count; index++)
        {
            if (!changedIndexes.Contains(index))
            {
                continue;
            }

            if (groupedIndexes.Contains(index))
            {
                continue;
            }

            mainProps.Add(allProperties[index]);
        }

        return (groupsToRender, mainProps);
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

        sb.AppendLine(UpdateTableHeader);
        sb.AppendLine(UpdateTableSeparator);

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
                var before = prop[AzApiBeforeKey];
                var after = prop[AzApiAfterKey];

                path = RemovePropertiesPrefix(path);

                var beforeFormatted = FormatAttributeValueTable(path, before?.ToString(), AzApiValueFormatProviderName);
                var afterFormatted = FormatAttributeValueTable(path, after?.ToString(), AzApiValueFormatProviderName);

                sb.AppendLine($"| {EscapeMarkdown(path)} | {beforeFormatted} | {afterFormatted} |");
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders grouped sections for update mode.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="groups">The groups that should be rendered.</param>
    /// <param name="allProperties">All small properties including unchanged.</param>
    private static void RenderUpdateGroupedSections(
        StringBuilder sb,
        IReadOnlyList<AzApiGroupedPrefix> groups,
        ScriptArray allProperties)
    {
        foreach (var group in groups)
        {
            var groupProps = new ScriptArray();
            foreach (var index in group.MemberIndexes)
            {
                if (index >= 0 && index < allProperties.Count)
                {
                    groupProps.Add(allProperties[index]);
                }
            }

            if (group.Kind == AzApiGroupedPrefixKind.Array)
            {
                RenderUpdateArrayGroup(sb, group.Path, groupProps);
            }
            else
            {
                RenderUpdatePrefixGroup(sb, group.Path, groupProps);
            }
        }
    }

    /// <summary>
    /// Renders a non-array prefix group section as a before/after table.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="groupPath">The prefix path for the section.</param>
    /// <param name="groupProps">The grouped properties.</param>
    private static void RenderUpdatePrefixGroup(StringBuilder sb, string groupPath, ScriptArray groupProps)
    {
        sb.AppendLine($"###### `{EscapeMarkdown(groupPath)}`");
        sb.AppendLine();
        sb.AppendLine(UpdateTableHeader);
        sb.AppendLine(UpdateTableSeparator);

        foreach (var item in groupProps)
        {
            if (item is not ScriptObject prop)
            {
                continue;
            }

            var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
            var before = prop[AzApiBeforeKey];
            var after = prop[AzApiAfterKey];

            var localPath = RemoveNestedPrefix(path, groupPath);

            var beforeFormatted = FormatAttributeValueTable(localPath, before?.ToString(), AzApiValueFormatProviderName);
            var afterFormatted = FormatAttributeValueTable(localPath, after?.ToString(), AzApiValueFormatProviderName);

            sb.AppendLine($"| {EscapeMarkdown(localPath)} | {beforeFormatted} | {afterFormatted} |");
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders an array group section in update mode using a matrix table when possible.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="arrayPath">The array path for the section.</param>
    /// <param name="groupProps">The grouped properties for the array.</param>
    private static void RenderUpdateArrayGroup(StringBuilder sb, string arrayPath, ScriptArray groupProps)
    {
        sb.AppendLine($"###### `{EscapeMarkdown(arrayPath)}` Array");
        sb.AppendLine();

        var items = ExtractArrayItems(groupProps, arrayPath, isUpdateMode: true);
        if (items.Count == 0)
        {
            return;
        }

        if (ShouldRenderMatrixTable(items, maxPropertiesPerItem: 8))
        {
            RenderUpdateArrayMatrixTable(sb, items);
        }
        else
        {
            RenderUpdateArrayPerItemTables(sb, items);
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Renders a matrix table for array items in update mode.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="items">The extracted array items.</param>
    private static void RenderUpdateArrayMatrixTable(StringBuilder sb, IReadOnlyList<AzApiArrayItem> items)
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
                var beforeFormatted = FormatAttributeValueTable(header, entry.Before?.ToString(), AzApiValueFormatProviderName);
                var afterFormatted = FormatAttributeValueTable(header, entry.After?.ToString(), AzApiValueFormatProviderName);
                sb.Append(FormatUpdateCell(beforeFormatted, afterFormatted));
                sb.Append(' ');
            }

            sb.AppendLine("|");
        }
    }

    /// <summary>
    /// Formats a before/after cell for a matrix table.
    /// </summary>
    /// <param name="beforeFormatted">The formatted before value.</param>
    /// <param name="afterFormatted">The formatted after value.</param>
    /// <returns>Cell content suitable for markdown table rendering.</returns>
    private static string FormatUpdateCell(string beforeFormatted, string afterFormatted)
    {
        if (string.Equals(beforeFormatted, afterFormatted, StringComparison.Ordinal))
        {
            return beforeFormatted;
        }

        return $"- {beforeFormatted}<br>+ {afterFormatted}";
    }

    /// <summary>
    /// Renders per-item tables for array items in update mode.
    /// </summary>
    /// <param name="sb">The string builder to append markdown to.</param>
    /// <param name="items">The extracted array items.</param>
    private static void RenderUpdateArrayPerItemTables(StringBuilder sb, IReadOnlyList<AzApiArrayItem> items)
    {
        foreach (var item in items)
        {
            sb.AppendLine($"**Item [{item.Index}]**");
            sb.AppendLine();
            sb.AppendLine("| Property | Before | After |");
            sb.AppendLine("|----------|--------|-------|");

            foreach (var entry in item.Entries)
            {
                var beforeFormatted = FormatAttributeValueTable(entry.LocalPath, entry.Before?.ToString(), AzApiValueFormatProviderName);
                var afterFormatted = FormatAttributeValueTable(entry.LocalPath, entry.After?.ToString(), AzApiValueFormatProviderName);
                sb.AppendLine($"| {EscapeMarkdown(entry.LocalPath)} | {beforeFormatted} | {afterFormatted} |");
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
                var path = prop[AzApiPathKey]?.ToString() ?? string.Empty;
                var before = prop[AzApiBeforeKey];
                var after = prop[AzApiAfterKey];

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
