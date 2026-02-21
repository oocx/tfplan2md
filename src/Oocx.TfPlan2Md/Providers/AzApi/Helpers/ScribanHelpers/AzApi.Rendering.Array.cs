using Scriban.Runtime;

namespace Oocx.TfPlan2Md.Providers.AzApi;

/// <summary>
/// Scriban helper functions for azapi_resource template rendering.
/// Related feature: docs/features/028-azapi-resource-template/specification.md.
/// </summary>
/// <remarks>
/// These helpers transform JSON body content from azapi_resource resources into human-readable
/// markdown tables using dot-notation property paths. This makes Azure REST API resource
/// configurations easy to review in pull requests.
/// </remarks>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Represents a single array item within an AzAPI body array section.
    /// </summary>
    /// <param name="Index">The array index.</param>
    /// <param name="IndexLabel">A display label for the index (e.g. <c>[0]</c>).</param>
    /// <param name="Entries">The extracted item entries.</param>
    /// <remarks>
    /// This model is purpose-built for rendering and stays internal to the Scriban helpers.
    /// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private sealed record AzApiArrayItem(int Index, string IndexLabel, IReadOnlyList<AzApiArrayItemEntry> Entries);

    /// <summary>
    /// Represents a single leaf entry within an array item.
    /// </summary>
    /// <param name="LocalPath">The local property path relative to the item.</param>
    /// <param name="Value">The create/delete mode value.</param>
    /// <param name="Before">The update mode "before" value.</param>
    /// <param name="After">The update mode "after" value.</param>
    /// <param name="IsSensitive">Whether this entry is marked sensitive.</param>
    /// <remarks>
    /// Only one of <paramref name="Value"/> or <paramref name="Before"/>/<paramref name="After"/> is expected
    /// to be used depending on rendering mode.
    /// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </remarks>
    private sealed record AzApiArrayItemEntry(
        string LocalPath,
        object? Value,
        object? Before,
        object? After,
        bool IsSensitive = false);

    /// <summary>
    /// Extracts array item data from a grouped property list.
    /// </summary>
    /// <param name="groupProps">The grouped flattened properties.</param>
    /// <param name="arrayPath">The normalized array path (without <c>properties.</c> prefix).</param>
    /// <param name="isUpdateMode">Whether the group is being rendered in update mode.</param>
    /// <param name="changedIndexes">The set of changed property indexes (for update mode filtering).</param>
    /// <returns>Ordered list of extracted array items.</returns>
    /// <remarks>
    /// This method supports arrays of objects (e.g. <c>items[0].name</c>) and arrays of primitives
    /// (e.g. <c>allowedOrigins[0]</c>). In update mode, only array items containing at least one changed
    /// property are included. Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
    /// Related issue: docs/issues/089-nested-array-shows-all-items/analysis.md.
    /// </remarks>
    private static List<AzApiArrayItem> ExtractArrayItems(
        ScriptArray groupProps,
        string arrayPath,
        bool isUpdateMode,
        HashSet<int>? changedIndexes = null)
    {
        var itemOrder = new List<int>();
        var byIndex = new Dictionary<int, List<AzApiArrayItemEntry>>();
        var changedArrayItems = isUpdateMode && changedIndexes is not null
            ? new HashSet<int>()
            : null;

        var propIndex = 0;
        foreach (var item in groupProps)
        {
            if (item is not ScriptObject prop)
            {
                propIndex++;
                continue;
            }

            var rawPath = prop["path"]?.ToString() ?? string.Empty;
            var normalizedPath = RemovePropertiesPrefix(rawPath);

            if (!TryParseArrayItemPath(normalizedPath, arrayPath, out var index, out var localPath))
            {
                propIndex++;
                continue;
            }

            // Track if this property is changed in update mode
            if (changedArrayItems is not null && changedIndexes!.Contains(propIndex))
            {
                changedArrayItems.Add(index);
            }

            if (!byIndex.TryGetValue(index, out var entries))
            {
                entries = new List<AzApiArrayItemEntry>();
                byIndex[index] = entries;
                itemOrder.Add(index);
            }

            var entry = CreateArrayItemEntry(prop, isUpdateMode, localPath);
            entries.Add(entry);

            propIndex++;
        }

        return BuildArrayItemList(itemOrder, byIndex, changedArrayItems);
    }

    /// <summary>
    /// Creates an array item entry from a property object.
    /// </summary>
    /// <param name="prop">The property object.</param>
    /// <param name="isUpdateMode">Whether in update mode.</param>
    /// <param name="localPath">The local path within the array item.</param>
    /// <returns>The array item entry.</returns>
    /// <remarks>
    /// Reads the <c>is_sensitive</c> flag from the property object to propagate sensitivity
    /// into the extracted entry. Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </remarks>
    private static AzApiArrayItemEntry CreateArrayItemEntry(ScriptObject prop, bool isUpdateMode, string localPath)
    {
        var isSensitive = prop["is_sensitive"] is bool sensitive && sensitive;

        if (isUpdateMode)
        {
            return new AzApiArrayItemEntry(
                LocalPath: localPath,
                Value: null,
                Before: prop["before"],
                After: prop["after"],
                IsSensitive: isSensitive);
        }

        return new AzApiArrayItemEntry(
            LocalPath: localPath,
            Value: prop["value"],
            Before: null,
            After: null,
            IsSensitive: isSensitive);
    }

    /// <summary>
    /// Builds the final list of array items from the indexed data.
    /// </summary>
    /// <param name="itemOrder">The order of array item indices.</param>
    /// <param name="byIndex">Dictionary of entries by array index.</param>
    /// <param name="changedArrayItems">Set of changed array item indices (null to include all).</param>
    /// <returns>Ordered list of array items.</returns>
    private static List<AzApiArrayItem> BuildArrayItemList(
        List<int> itemOrder,
        Dictionary<int, List<AzApiArrayItemEntry>> byIndex,
        HashSet<int>? changedArrayItems)
    {
        var result = new List<AzApiArrayItem>();
        foreach (var index in itemOrder)
        {
            // Filter to only changed array items when filtering is enabled
            if (changedArrayItems?.Contains(index) == false)
            {
                continue;
            }

            if (!byIndex.TryGetValue(index, out var entries))
            {
                continue;
            }

            result.Add(new AzApiArrayItem(
                Index: index,
                IndexLabel: $"[{index}]",
                Entries: entries));
        }

        return result;
    }

    /// <summary>
    /// Parses an array item path into its item index and local property path.
    /// </summary>
    /// <param name="path">The normalized full path.</param>
    /// <param name="arrayPath">The normalized array path prefix.</param>
    /// <param name="index">The parsed array index.</param>
    /// <param name="localPath">The local path relative to the array item.</param>
    /// <returns><c>true</c> when parsing succeeds; otherwise <c>false</c>.</returns>
    private static bool TryParseArrayItemPath(
        string path,
        string arrayPath,
        out int index,
        out string localPath)
    {
        index = -1;
        localPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(arrayPath))
        {
            return false;
        }

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

        var indexText = path.Substring(indexStart, indexEnd - indexStart);
        if (!int.TryParse(indexText, out index))
        {
            return false;
        }

        if (indexEnd == path.Length - 1)
        {
            localPath = "(value)";
            return true;
        }

        var remainder = path.Substring(indexEnd + 1);
        if (remainder.Length > 0 && remainder[0] == '.')
        {
            remainder = remainder.Substring(1);
        }

        localPath = string.IsNullOrEmpty(remainder) ? "(value)" : remainder;
        return true;
    }
}
