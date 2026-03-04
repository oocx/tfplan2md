using System.Diagnostics.CodeAnalysis;
using System.Text;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Rendering;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Renders AzAPI body and output-values sections.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Body rendering combines grouping, diffing, and sensitivity handling to preserve historical azapi behavior.")]
internal static class AzApiBodyRenderer
{
    private const string ProviderForFormatting = "azurerm";

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
        var flattened = AzApiBodyFlattener.Flatten(body).ToList();
        var sensitivePaths = AzApiSensitivityHelper.Flatten(sensitivity);
        if (!context.ShowSensitive)
        {
            var existing = flattened.Select(property => property.Path).ToHashSet(StringComparer.Ordinal);
            var emptySensitiveContainerPaths = AzApiBodyFlattener.CollectEmptyContainerPaths(body)
                .Where(path => !existing.Contains(path) && AzApiSensitivityHelper.IsPathSensitive(path, sensitivePaths))
                .ToList();

            foreach (var path in emptySensitiveContainerPaths)
            {
                flattened.Add(new AzApiBodyProperty(path, null, IsLarge: false));
            }
        }

        var smallProperties = flattened.Where(property => !property.IsLarge).ToList();
        var largeProperties = flattened.Where(property => property.IsLarge).ToList();

        WriteHeading(writer, heading);

        var groups = AzApiGrouping.IdentifyGroups(smallProperties);
        var groupedIndexes = groups.SelectMany(group => group.MemberIndexes).ToHashSet();

        WriteCreateDeleteTable(
            writer,
            smallProperties.Where((_, index) => !groupedIndexes.Contains(index)).ToList(),
            sensitivePaths,
            context,
            renderWhenEmpty: groups.Count == 0);

        foreach (var group in groups)
        {
            var groupProperties = group.MemberIndexes
                .Where(index => index >= 0 && index < smallProperties.Count)
                .Select(index => smallProperties[index])
                .ToList();

            if (group.Kind == AzApiGroupKind.Array)
            {
                WriteCreateDeleteArrayGroup(writer, group.Prefix, groupProperties, sensitivePaths, context);
            }
            else
            {
                WriteCreateDeletePrefixGroup(writer, group.Prefix, groupProperties, sensitivePaths, context);
            }
        }

        WriteLargeCreateDeleteProperties(writer, largeProperties, sensitivePaths, context);

        if (smallProperties.Count == 0 && largeProperties.Count == 0)
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
        var allComparisons = Compare(beforeBody, afterBody, beforeSensitive, afterSensitive, showUnchanged: true, context.ShowSensitive);
        var changedComparisons = Compare(beforeBody, afterBody, beforeSensitive, afterSensitive, showUnchanged: false, context.ShowSensitive);

        var smallAll = allComparisons.Where(property => !property.IsLarge).ToList();
        var smallChanged = changedComparisons.Where(property => !property.IsLarge).ToList();
        var largeChanged = changedComparisons.Where(property => property.IsLarge).ToList();

        WriteHeading(writer, heading);

        var pathToIndex = smallAll
            .Select((property, index) => new { property.Path, index })
            .GroupBy(item => AzApiGrouping.RemovePropertiesPrefix(item.Path), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().index, StringComparer.Ordinal);

        HashSet<int> changedIndexesInAll = [];
        foreach (var property in smallChanged)
        {
            var normalized = AzApiGrouping.RemovePropertiesPrefix(property.Path);
            if (pathToIndex.TryGetValue(normalized, out var index))
            {
                changedIndexesInAll.Add(index);
            }
        }

        List<AzApiBodyProperty> groupingSource = [];
        foreach (var property in smallAll)
        {
            groupingSource.Add(new AzApiBodyProperty(property.Path, property.After, property.IsLarge));
        }

        var groups = AzApiGrouping.IdentifyGroups(groupingSource);
        List<AzApiGroup> groupsToRender = [];
        HashSet<int> groupedIndexes = [];

        foreach (var group in groups)
        {
            if (!group.MemberIndexes.Any(changedIndexesInAll.Contains))
            {
                continue;
            }

            groupsToRender.Add(group);
            foreach (var memberIndex in group.MemberIndexes)
            {
                groupedIndexes.Add(memberIndex);
            }
        }

        var mainProperties = smallAll
            .Where((_, index) => changedIndexesInAll.Contains(index) && !groupedIndexes.Contains(index))
            .ToList();

        WriteUpdateTable(writer, mainProperties, context);

        foreach (var group in groupsToRender)
        {
            var groupProperties = group.MemberIndexes
                .Where(index => index >= 0 && index < smallAll.Count)
                .Select(index => smallAll[index])
                .ToList();

            if (group.Kind == AzApiGroupKind.Array)
            {
                HashSet<int> localChanged = [];
                for (var index = 0; index < group.MemberIndexes.Count; index++)
                {
                    if (changedIndexesInAll.Contains(group.MemberIndexes[index]))
                    {
                        localChanged.Add(index);
                    }
                }

                WriteUpdateArrayGroup(writer, group.Prefix, groupProperties, localChanged, context);
            }
            else
            {
                WriteUpdatePrefixGroup(writer, group.Prefix, groupProperties, context);
            }
        }

        WriteLargeUpdateProperties(writer, largeChanged, context);

        if (smallChanged.Count == 0 && largeChanged.Count == 0)
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
        List<AzApiBodyProperty> properties,
        HashSet<string> sensitivePaths,
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
            var displayPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);
            if (!context.ShowSensitive && AzApiSensitivityHelper.IsPathSensitive(property.Path, sensitivePaths))
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(displayPath)} | (sensitive) |\n");
                continue;
            }

            var formatted = FormatValue(displayPath, property.Value?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(displayPath)} | {formatted} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteCreateDeletePrefixGroup(
        MarkdownWriter writer,
        string groupPath,
        List<AzApiBodyProperty> properties,
        HashSet<string> sensitivePaths,
        IRenderContext context)
    {
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(groupPath)}`", 6);
        writer.BlankLine();
        writer.Raw("| Property | Value |\n");
        writer.Raw("|----------|-------|\n");

        foreach (var property in properties)
        {
            var localPath = AzApiGrouping.RemoveNestedPrefix(property.Path, groupPath);
            if (!context.ShowSensitive && AzApiSensitivityHelper.IsPathSensitive(property.Path, sensitivePaths))
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(localPath)} | (sensitive) |\n");
                continue;
            }

            var formatted = FormatValue(localPath, property.Value?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(localPath)} | {formatted} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteCreateDeleteArrayGroup(
        MarkdownWriter writer,
        string arrayPath,
        List<AzApiBodyProperty> properties,
        HashSet<string> sensitivePaths,
        IRenderContext context)
    {
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(arrayPath)}` Array", 6);
        writer.BlankLine();

        var items = ExtractArrayItemsForCreate(properties, arrayPath, sensitivePaths, context.ShowSensitive);
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
                var entry = item.Entries.FirstOrDefault(candidate => string.Equals(candidate.LocalPath, column, StringComparison.Ordinal));
                if (entry is null)
                {
                    cells.Add(string.Empty);
                    continue;
                }

                if (!context.ShowSensitive && entry.IsSensitive)
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

    private static List<string> CollectArrayColumns(IReadOnlyList<AzApiArrayItem> items)
    {
        List<string> columns = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (var item in items)
        {
            foreach (var localPath in item.Entries.Select(entry => entry.LocalPath))
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

    private static void WriteUpdateTable(MarkdownWriter writer, IReadOnlyList<AzApiComparisonProperty> properties, IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        writer.Raw("| Property | Before | After |\n");
        writer.Raw("|----------|--------|-------|\n");

        foreach (var property in properties)
        {
            var displayPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);
            if (property.IsSensitive && !context.ShowSensitive)
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(displayPath)} | (sensitive) | (sensitive) |\n");
                continue;
            }

            var before = FormatValue(displayPath, property.Before?.ToString(), context);
            var after = FormatValue(displayPath, property.After?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(displayPath)} | {before} | {after} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteUpdatePrefixGroup(MarkdownWriter writer, string groupPath, IReadOnlyList<AzApiComparisonProperty> properties, IRenderContext context)
    {
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(groupPath)}`", 6);
        writer.BlankLine();
        writer.Raw("| Property | Before | After |\n");
        writer.Raw("|----------|--------|-------|\n");

        foreach (var property in properties)
        {
            var localPath = AzApiGrouping.RemoveNestedPrefix(property.Path, groupPath);
            if (property.IsSensitive && !context.ShowSensitive)
            {
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(localPath)} | (sensitive) | (sensitive) |\n");
                continue;
            }

            var before = FormatValue(localPath, property.Before?.ToString(), context);
            var after = FormatValue(localPath, property.After?.ToString(), context);
            writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(localPath)} | {before} | {after} |\n");
        }

        writer.BlankLine();
    }

    private static void WriteUpdateArrayGroup(
        MarkdownWriter writer,
        string arrayPath,
        IReadOnlyList<AzApiComparisonProperty> properties,
        HashSet<int> changedLocalIndexes,
        IRenderContext context)
    {
        writer.Heading($"`{MarkdownHelpers.EscapeMarkdown(arrayPath)}` Array", 6);
        writer.BlankLine();

        var items = ExtractArrayItemsForUpdate(properties, arrayPath, changedLocalIndexes);
        foreach (var item in items)
        {
            writer.Paragraph($"**Item [{item.Index}]**");
            writer.BlankLine();
            writer.Raw("| Property | Before | After |\n");
            writer.Raw("|----------|--------|-------|\n");

            foreach (var entry in item.Entries)
            {
                if (entry.IsSensitive && !context.ShowSensitive)
                {
                    writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(entry.LocalPath)} | (sensitive) | (sensitive) |\n");
                    continue;
                }

                var beforeFormatted = FormatValue(entry.LocalPath, entry.Before?.ToString(), context);
                var afterFormatted = FormatValue(entry.LocalPath, entry.After?.ToString(), context);
                writer.Raw($"| {MarkdownHelpers.EscapeMarkdown(entry.LocalPath)} | {beforeFormatted} | {afterFormatted} |\n");
            }

            writer.BlankLine();
        }

        writer.BlankLine();
    }

    private static void WriteLargeCreateDeleteProperties(
        MarkdownWriter writer,
        List<AzApiBodyProperty> properties,
        HashSet<string> sensitivePaths,
        IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        writer.Raw("<details>\n<summary>Large body properties</summary>\n\n");

        foreach (var property in properties)
        {
            var displayPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(displayPath)}:**", 5);
            writer.BlankLine();

            if (!context.ShowSensitive && AzApiSensitivityHelper.IsPathSensitive(property.Path, sensitivePaths))
            {
                writer.Paragraph("(sensitive)");
            }
            else
            {
                writer.Raw(MarkdownHelpers.FormatLargeValue(null, property.Value?.ToString(), "inline-diff"));
                writer.Raw("\n");
            }

            writer.BlankLine();
        }

        writer.Raw("</details>\n\n");
    }

    private static void WriteLargeUpdateProperties(MarkdownWriter writer, IReadOnlyList<AzApiComparisonProperty> properties, IRenderContext context)
    {
        if (properties.Count == 0)
        {
            return;
        }

        writer.Raw("<details>\n<summary>Large body property changes</summary>\n\n");

        foreach (var property in properties)
        {
            var displayPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);
            writer.Heading($"**{MarkdownHelpers.EscapeMarkdown(displayPath)}:**", 5);
            writer.BlankLine();

            if (property.IsSensitive && !context.ShowSensitive)
            {
                writer.Paragraph("(sensitive)");
            }
            else
            {
                writer.Raw(MarkdownHelpers.FormatLargeValue(property.Before?.ToString(), property.After?.ToString(), "inline-diff"));
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

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Path-wise compare logic must preserve sensitivity and large-value semantics from baseline templates.")]
    private static List<AzApiComparisonProperty> Compare(
        object beforeJson,
        object afterJson,
        object? beforeSensitive,
        object? afterSensitive,
        bool showUnchanged,
        bool showSensitive)
    {
        var beforeFlattened = AzApiBodyFlattener.FlattenToDictionary(beforeJson);
        var afterFlattened = AzApiBodyFlattener.FlattenToDictionary(afterJson);

        var beforeSensitivePaths = AzApiSensitivityHelper.Flatten(beforeSensitive);
        var afterSensitivePaths = AzApiSensitivityHelper.Flatten(afterSensitive);

        var paths = beforeFlattened.Keys.Union(afterFlattened.Keys).OrderBy(path => path, StringComparer.Ordinal).ToList();

        List<AzApiComparisonProperty> result = [];
        foreach (var path in paths)
        {
            beforeFlattened.TryGetValue(path, out var before);
            afterFlattened.TryGetValue(path, out var after);

            var isSensitive = beforeSensitivePaths.Contains(path) || afterSensitivePaths.Contains(path);
            var isChanged = !AreEqual(before, after);
            if (!isChanged && isSensitive && !showSensitive && before is not null && after is not null)
            {
                isChanged = true;
            }

            if (!showUnchanged && !isChanged)
            {
                continue;
            }

            var isLarge = (before?.ToString()?.Length ?? 0) > 200 || (after?.ToString()?.Length ?? 0) > 200;
            result.Add(new AzApiComparisonProperty(path, before, after, isSensitive, isLarge, isChanged));
        }

        return result;
    }

    private static bool AreEqual(object? before, object? after)
    {
        if (before is null && after is null)
        {
            return true;
        }

        if (before is null || after is null)
        {
            return false;
        }

        if (IsNumeric(before) && IsNumeric(after))
        {
            return Math.Abs(Convert.ToDouble(before) - Convert.ToDouble(after)) < 0.0000001d;
        }

        return before.Equals(after);
    }

    private static bool IsNumeric(object value)
    {
        return value is int or long or double or float or decimal;
    }

    private static List<AzApiArrayItem> ExtractArrayItemsForCreate(
        List<AzApiBodyProperty> properties,
        string arrayPath,
        HashSet<string> sensitivePaths,
        bool showSensitive)
    {
        List<int> order = [];
        Dictionary<int, List<AzApiArrayItemEntry>> byIndex = [];

        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            var normalizedPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);

            if (!TryParseArrayItemPath(normalizedPath, arrayPath, out var itemIndex, out var localPath))
            {
                continue;
            }

            if (!byIndex.TryGetValue(itemIndex, out var entries))
            {
                entries = [];
                byIndex[itemIndex] = entries;
                order.Add(itemIndex);
            }

            var fullPath = $"properties.{arrayPath}[{itemIndex}]" + (localPath == "(value)" ? string.Empty : $".{localPath}");
            var isSensitive = !showSensitive && AzApiSensitivityHelper.IsPathSensitive(fullPath, sensitivePaths);
            entries.Add(new AzApiArrayItemEntry(localPath, property.Value, null, null, isSensitive));
        }

        List<AzApiArrayItem> result = [];
        foreach (var itemIndex in order)
        {
            if (!byIndex.TryGetValue(itemIndex, out var entries))
            {
                continue;
            }

            result.Add(new AzApiArrayItem(itemIndex, entries));
        }

        return result;
    }

    private static List<AzApiArrayItem> ExtractArrayItemsForUpdate(
        IReadOnlyList<AzApiComparisonProperty> properties,
        string arrayPath,
        HashSet<int> changedLocalIndexes)
    {
        List<int> order = [];
        Dictionary<int, List<AzApiArrayItemEntry>> byIndex = [];
        HashSet<int> changedItems = [];

        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            var normalizedPath = AzApiGrouping.RemovePropertiesPrefix(property.Path);

            if (!TryParseArrayItemPath(normalizedPath, arrayPath, out var itemIndex, out var localPath))
            {
                continue;
            }

            if (changedLocalIndexes.Contains(index))
            {
                changedItems.Add(itemIndex);
            }

            if (!byIndex.TryGetValue(itemIndex, out var entries))
            {
                entries = [];
                byIndex[itemIndex] = entries;
                order.Add(itemIndex);
            }

            entries.Add(new AzApiArrayItemEntry(localPath, null, property.Before, property.After, property.IsSensitive));
        }

        List<AzApiArrayItem> result = [];
        foreach (var itemIndex in order)
        {
            if (!changedItems.Contains(itemIndex))
            {
                continue;
            }

            if (byIndex.TryGetValue(itemIndex, out var entries))
            {
                result.Add(new AzApiArrayItem(itemIndex, entries));
            }
        }

        return result;
    }

    private static bool TryParseArrayItemPath(string path, string arrayPath, out int index, out string localPath)
    {
        index = -1;
        localPath = string.Empty;

        var prefix = arrayPath + "[";
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var indexStart = prefix.Length;
        var indexEnd = path.IndexOf(']', indexStart);
        if (indexEnd <= indexStart)
        {
            return false;
        }

        if (!int.TryParse(path[indexStart..indexEnd], out index))
        {
            return false;
        }

        if (indexEnd == path.Length - 1)
        {
            localPath = "(value)";
            return true;
        }

        var remainder = path[(indexEnd + 1)..];
        if (remainder.StartsWith('.'))
        {
            remainder = remainder[1..];
        }

        localPath = string.IsNullOrWhiteSpace(remainder) ? "(value)" : remainder;
        return true;
    }

    private sealed record AzApiComparisonProperty(
        string Path,
        object? Before,
        object? After,
        bool IsSensitive,
        bool IsLarge,
        bool IsChanged);

    private sealed record AzApiArrayItem(int Index, IReadOnlyList<AzApiArrayItemEntry> Entries);

    private sealed record AzApiArrayItemEntry(
        string LocalPath,
        object? Value,
        object? Before,
        object? After,
        bool IsSensitive = false);
}
