using Scriban.Runtime;

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
    /// Represents a single array item within an AzAPI body array section.
    /// </summary>
    /// <param name="Index">The array index.</param>
    /// <param name="IndexLabel">A display label for the index (e.g. <c>[0]</c>).</param>
    /// <param name="Entries">The extracted item entries.</param>
    /// <remarks>
    /// This model is purpose-built for rendering and stays internal to the Scriban helpers.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private sealed record AzApiArrayItem(int Index, string IndexLabel, IReadOnlyList<AzApiArrayItemEntry> Entries);

    /// <summary>
    /// Represents a single leaf entry within an array item.
    /// </summary>
    /// <param name="LocalPath">The local property path relative to the item.</param>
    /// <param name="Value">The create/delete mode value.</param>
    /// <param name="Before">The update mode "before" value.</param>
    /// <param name="After">The update mode "after" value.</param>
    /// <remarks>
    /// Only one of <paramref name="Value"/> or <paramref name="Before"/>/<paramref name="After"/> is expected
    /// to be used depending on rendering mode.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private sealed record AzApiArrayItemEntry(
        string LocalPath,
        object? Value,
        object? Before,
        object? After);

    /// <summary>
    /// Extracts array item data from a grouped property list.
    /// </summary>
    /// <param name="groupProps">The grouped flattened properties.</param>
    /// <param name="arrayPath">The normalized array path (without <c>properties.</c> prefix).</param>
    /// <param name="isUpdateMode">Whether the group is being rendered in update mode.</param>
    /// <returns>Ordered list of extracted array items.</returns>
    /// <remarks>
    /// This method supports arrays of objects (e.g. <c>items[0].name</c>) and arrays of primitives
    /// (e.g. <c>allowedOrigins[0]</c>). Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiArrayItem> ExtractArrayItems(
        ScriptArray groupProps,
        string arrayPath,
        bool isUpdateMode)
    {
        var itemOrder = new List<int>();
        var byIndex = new Dictionary<int, List<AzApiArrayItemEntry>>();

        foreach (var item in groupProps)
        {
            if (item is not ScriptObject prop)
            {
                continue;
            }

            var rawPath = prop["path"]?.ToString() ?? string.Empty;
            var normalizedPath = RemovePropertiesPrefix(rawPath);

            if (!TryParseArrayItemPath(normalizedPath, arrayPath, out var index, out var localPath))
            {
                continue;
            }

            if (!byIndex.TryGetValue(index, out var entries))
            {
                entries = new List<AzApiArrayItemEntry>();
                byIndex[index] = entries;
                itemOrder.Add(index);
            }

            if (isUpdateMode)
            {
                entries.Add(new AzApiArrayItemEntry(
                    LocalPath: localPath,
                    Value: null,
                    Before: prop["before"],
                    After: prop["after"]));
            }
            else
            {
                entries.Add(new AzApiArrayItemEntry(
                    LocalPath: localPath,
                    Value: prop["value"],
                    Before: null,
                    After: null));
            }
        }

        var result = new List<AzApiArrayItem>();
        foreach (var index in itemOrder)
        {
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
