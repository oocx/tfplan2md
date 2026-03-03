using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Grouping logic for flattened AzAPI body properties.
/// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Grouping logic requires path parsing and candidate selection helpers for parity with historical behavior.")]
internal static class AzApiGrouping
{
    private const int GroupThreshold = 3;

    /// <summary>
    /// Identifies grouped prefixes from flattened properties.
    /// </summary>
    /// <param name="properties">Flattened properties.</param>
    /// <returns>Groups ordered by first appearance.</returns>
    internal static IReadOnlyList<AzApiGroup> IdentifyGroups(IReadOnlyList<AzApiBodyProperty> properties)
    {
        var pathInfos = ExtractPathInfos(properties);
        var arrayCandidates = BuildArrayCandidates(pathInfos);
        var arrayGroups = SelectByThreshold(arrayCandidates, AzApiGroupKind.Array);

        var claimedIndexes = arrayGroups.SelectMany(group => group.MemberIndexes).ToHashSet();
        var prefixCandidates = BuildPrefixCandidates(pathInfos, claimedIndexes);
        var prefixGroups = SelectLongestPrefixes(prefixCandidates);

        return arrayGroups
            .Concat(prefixGroups)
            .OrderBy(group => group.FirstIndex)
            .ToList();
    }

    /// <summary>
    /// Removes <c>properties.</c> from a flattened path when present.
    /// </summary>
    /// <param name="path">Flattened path.</param>
    /// <returns>Normalized display path.</returns>
    internal static string RemovePropertiesPrefix(string path)
    {
        return path.StartsWith("properties.", StringComparison.Ordinal)
            ? path["properties.".Length..]
            : path;
    }

    /// <summary>
    /// Removes parent and <c>properties.</c> prefixes from a flattened path.
    /// </summary>
    /// <param name="path">Flattened path.</param>
    /// <param name="parentPath">Parent prefix.</param>
    /// <returns>Local path relative to the parent section.</returns>
    internal static string RemoveNestedPrefix(string path, string parentPath)
    {
        var normalizedPath = RemovePropertiesPrefix(path);
        var normalizedParent = RemovePropertiesPrefix(parentPath);

        if (normalizedPath.StartsWith(normalizedParent + ".", StringComparison.Ordinal))
        {
            return normalizedPath[(normalizedParent.Length + 1)..];
        }

        return normalizedPath;
    }

    private static List<PathInfo> ExtractPathInfos(IReadOnlyList<AzApiBodyProperty> properties)
    {
        List<PathInfo> result = [];

        for (var index = 0; index < properties.Count; index++)
        {
            var path = RemovePropertiesPrefix(properties[index].Path);
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
            result.Add(new PathInfo(index, path, segments));
        }

        return result;
    }

    private static List<GroupCandidate> BuildArrayCandidates(IReadOnlyList<PathInfo> pathInfos)
    {
        var candidates = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        foreach (var info in pathInfos)
        {
            var arrayPath = GetOutermostArrayPath(info.Segments);
            if (arrayPath is null)
            {
                continue;
            }

            if (!candidates.TryGetValue(arrayPath, out var indexes))
            {
                indexes = [];
                candidates[arrayPath] = indexes;
            }

            indexes.Add(info.Index);
        }

        return candidates
            .Select(candidate => new GroupCandidate(candidate.Key, candidate.Value))
            .ToList();
    }

    private static List<GroupCandidate> BuildPrefixCandidates(IReadOnlyList<PathInfo> pathInfos, HashSet<int> excluded)
    {
        var candidates = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        foreach (var info in pathInfos)
        {
            if (excluded.Contains(info.Index))
            {
                continue;
            }

            foreach (var prefix in GetNonArrayPrefixes(info.Segments))
            {
                if (!candidates.TryGetValue(prefix, out var indexes))
                {
                    indexes = [];
                    candidates[prefix] = indexes;
                }

                indexes.Add(info.Index);
            }
        }

        return candidates
            .Select(candidate => new GroupCandidate(candidate.Key, candidate.Value))
            .ToList();
    }

    private static List<AzApiGroup> SelectByThreshold(IReadOnlyList<GroupCandidate> candidates, AzApiGroupKind kind)
    {
        return candidates
            .Where(candidate => candidate.MemberIndexes.Count >= GroupThreshold)
            .Select(candidate => new AzApiGroup(candidate.Path, kind, candidate.MemberIndexes))
            .ToList();
    }

    private static List<AzApiGroup> SelectLongestPrefixes(List<GroupCandidate> candidates)
    {
        var thresholdGroups = SelectByThreshold(candidates, AzApiGroupKind.Prefix);
        var qualified = thresholdGroups
            .ConvertAll(group => new { Group = group, SegmentCount = group.Prefix.Split('.').Length });

        HashSet<string> suppressedParents = [];
        foreach (var group in qualified.OrderByDescending(item => item.SegmentCount))
        {
            foreach (var candidate in qualified)
            {
                if (group.SegmentCount <= candidate.SegmentCount)
                {
                    continue;
                }

                if (group.Group.Prefix.StartsWith(candidate.Group.Prefix + ".", StringComparison.Ordinal))
                {
                    suppressedParents.Add(candidate.Group.Prefix);
                }
            }
        }

        return qualified
            .Where(item => !suppressedParents.Contains(item.Group.Prefix))
            .Select(item => item.Group)
            .ToList();
    }

    private static string? GetOutermostArrayPath(IReadOnlyList<string> segments)
    {
        List<string> pathSegments = [];

        foreach (var segment in segments)
        {
            var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
            if (bracketIndex > 0)
            {
                pathSegments.Add(segment[..bracketIndex]);
                return string.Join('.', pathSegments);
            }

            pathSegments.Add(segment);
        }

        return null;
    }

    private static IEnumerable<string> GetNonArrayPrefixes(IReadOnlyList<string> segments)
    {
        var firstArrayIndex = FindFirstArrayIndex(segments);
        var limit = firstArrayIndex >= 0 ? firstArrayIndex : segments.Count - 1;

        if (limit <= 0)
        {
            yield break;
        }

        for (var length = 1; length <= limit; length++)
        {
            yield return string.Join('.', segments.Take(length));
        }
    }

    private static int FindFirstArrayIndex(IReadOnlyList<string> segments)
    {
        for (var index = 0; index < segments.Count; index++)
        {
            if (segments[index].Contains('[', StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private sealed record PathInfo(int Index, string Path, IReadOnlyList<string> Segments);

    private sealed record GroupCandidate(string Path, IReadOnlyList<int> MemberIndexes);
}
