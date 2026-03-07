using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Helper routines for AzApi body render planning.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
internal static partial class AzApiBodyRenderPlanner
{
    private static AzApiCreateDeleteArrayGroupPlan BuildCreateDeleteArrayGroupPlan(
        string arrayPath,
        List<AzApiBodyProperty> properties,
        HashSet<string> sensitivePaths,
        bool showSensitive)
    {
        List<int> order = [];
        Dictionary<int, List<AzApiCreateDeleteArrayItemEntry>> byIndex = [];

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
            entries.Add(new AzApiCreateDeleteArrayItemEntry(
                localPath,
                property.Value,
                IsSensitive(fullPath, sensitivePaths, showSensitive)));
        }

        List<AzApiCreateDeleteArrayItem> items = [];
        foreach (var itemIndex in order)
        {
            if (byIndex.TryGetValue(itemIndex, out var entries))
            {
                items.Add(new AzApiCreateDeleteArrayItem(itemIndex, entries));
            }
        }

        return new AzApiCreateDeleteArrayGroupPlan(arrayPath, items);
    }

    private static AzApiUpdateArrayGroupPlan BuildUpdateArrayGroupPlan(
        string arrayPath,
        List<AzApiComparisonProperty> properties,
        HashSet<int> changedLocalIndexes)
    {
        List<int> order = [];
        Dictionary<int, List<AzApiUpdatePropertyPlan>> byIndex = [];
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

            entries.Add(new AzApiUpdatePropertyPlan(
                localPath,
                property.Before,
                property.After,
                property.IsSensitive,
                property.IsLarge,
                property.IsChanged));
        }

        List<AzApiUpdateArrayItem> items = [];
        foreach (var itemIndex in order)
        {
            if (changedItems.Contains(itemIndex) && byIndex.TryGetValue(itemIndex, out var entries))
            {
                items.Add(new AzApiUpdateArrayItem(itemIndex, entries));
            }
        }

        return new AzApiUpdateArrayGroupPlan(arrayPath, items);
    }

    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "Path-wise compare logic must preserve sensitivity and large-value semantics from baseline templates.")]
    private static List<AzApiComparisonProperty> Compare(
        object beforeJson,
        object afterJson,
        object? beforeSensitive,
        object? afterSensitive,
        bool showUnchanged,
        bool showSensitive,
        bool ignoreAzureIdCaseChanges = false)
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
            var isChanged = !AreEqual(before, after, ignoreAzureIdCaseChanges);
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

    private static bool AreEqual(object? before, object? after, bool ignoreAzureIdCaseChanges = false)
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

        if (ignoreAzureIdCaseChanges
            && before is string beforeStr && after is string afterStr
            && string.Equals(beforeStr, afterStr, StringComparison.OrdinalIgnoreCase)
            && (AzureScopeParser.IsAzureResourceId(beforeStr) || AzureScopeParser.IsAzureResourceId(afterStr)))
        {
            return true;
        }

        return before.Equals(after);
    }

    private static bool IsNumeric(object value)
    {
        return value is int or long or double or float or decimal;
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
}
