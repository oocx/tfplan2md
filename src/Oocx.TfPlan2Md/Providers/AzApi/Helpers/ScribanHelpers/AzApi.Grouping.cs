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
    /// Represents the type of grouping applied to an AzAPI body prefix.
    /// </summary>
    /// <remarks>
    /// Array groups correspond to paths that include an array index, while prefix groups
    /// represent non-array shared prefixes. Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    internal enum AzApiGroupedPrefixKind
    {
        /// <summary>
        /// Indicates a grouping derived from an array path such as <c>items[0]</c>.
        /// </summary>
        Array,

        /// <summary>
        /// Indicates a grouping derived from a non-array shared prefix.
        /// </summary>
        Prefix
    }

    /// <summary>
    /// Describes a grouped prefix and the indices of its member properties.
    /// </summary>
    /// <param name="Path">The normalized prefix path for the group.</param>
    /// <param name="Kind">The kind of grouping represented by the prefix.</param>
    /// <param name="MemberIndexes">The ordered indices of properties that belong to the group.</param>
    /// <remarks>
    /// The indices map back to the original flattened property order, allowing renderers to
    /// preserve plan ordering. Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    internal sealed record AzApiGroupedPrefix(
        string Path,
        AzApiGroupedPrefixKind Kind,
        IReadOnlyList<int> MemberIndexes)
    {
        /// <summary>
        /// Gets the first occurrence index for ordering group sections.
        /// </summary>
        /// <value>
        /// The smallest index from <see cref="MemberIndexes"/> or <see cref="int.MaxValue"/> when empty.
        /// </value>
        internal int FirstIndex => MemberIndexes.Count == 0 ? int.MaxValue : MemberIndexes[0];
    }

    /// <summary>
    /// Removes the "properties." prefix from a property path if present.
    /// </summary>
    /// <param name="path">The property path.</param>
    /// <returns>Path with "properties." prefix removed.</returns>
    private static string RemovePropertiesPrefix(string path)
    {
        if (path.StartsWith("properties.", StringComparison.Ordinal))
        {
            return path.Substring("properties.".Length);
        }
        return path;
    }

    /// <summary>
    /// Removes the nested object prefix and "properties." prefix from a property path.
    /// </summary>
    /// <param name="path">The full property path.</param>
    /// <param name="parentPath">The parent path to remove.</param>
    /// <returns>Path with parent and "properties." prefixes removed.</returns>
    private static string RemoveNestedPrefix(string path, string parentPath)
    {
        // First remove "properties." if present
        path = RemovePropertiesPrefix(path);
        parentPath = RemovePropertiesPrefix(parentPath);

        // Then remove the parent path
        if (path.StartsWith(parentPath + ".", StringComparison.Ordinal))
        {
            return path.Substring(parentPath.Length + 1);
        }
        return path;
    }

    /// <summary>
    /// Identifies grouped prefixes for AzAPI body properties using the configured grouping rules.
    /// </summary>
    /// <param name="properties">Flattened property entries with a <c>path</c> field.</param>
    /// <returns>Ordered grouped prefix descriptors based on first appearance in the input.</returns>
    /// <remarks>
    /// This method applies the fixed threshold (≥3), distinguishes array groups from non-array
    /// prefix groups, and enforces the "longest common prefix wins" rule. Related feature:
    /// docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    internal static IReadOnlyList<AzApiGroupedPrefix> IdentifyGroupedPrefixes(ScriptArray properties)
    {
        var pathInfos = ExtractPathInfos(properties);
        var arrayCandidates = BuildArrayGroupCandidates(pathInfos);
        var arrayGroups = SelectGroupsByThreshold(arrayCandidates, AzApiGroupedPrefixKind.Array);

        var arrayMemberIndexes = arrayGroups
            .SelectMany(group => group.MemberIndexes)
            .ToHashSet();

        var prefixCandidates = BuildNonArrayPrefixCandidates(pathInfos, arrayMemberIndexes);
        var prefixGroups = SelectLongestPrefixGroups(prefixCandidates);

        return arrayGroups
            .Concat(prefixGroups)
            .OrderBy(group => group.FirstIndex)
            .ToList();
    }

    /// <summary>
    /// Represents a flattened property path with ordering metadata.
    /// </summary>
    /// <param name="Index">The original position of the property in the flattened list.</param>
    /// <param name="Path">The normalized property path without the <c>properties.</c> prefix.</param>
    /// <param name="Segments">The dot-separated segments that make up the path.</param>
    /// <remarks>
    /// The structure is used to preserve ordering when creating grouping sections.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private sealed record AzApiPathInfo(int Index, string Path, IReadOnlyList<string> Segments);

    /// <summary>
    /// Represents a grouping candidate before threshold and overlap filtering.
    /// </summary>
    /// <param name="Path">The prefix path represented by the candidate.</param>
    /// <param name="MemberIndexes">The property indices that contribute to the candidate.</param>
    /// <param name="SegmentCount">The number of dot segments in the prefix path.</param>
    /// <remarks>
    /// Candidates are filtered by threshold and overlap rules before becoming final groups.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private sealed record AzApiGroupingCandidate(string Path, IReadOnlyList<int> MemberIndexes, int SegmentCount);

    /// <summary>
    /// Extracts normalized path metadata for grouping calculations.
    /// </summary>
    /// <param name="properties">Flattened property entries containing <c>path</c> values.</param>
    /// <returns>Ordered list of path metadata entries.</returns>
    /// <remarks>
    /// Normalization removes the leading <c>properties.</c> prefix to align grouping with rendered keys.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiPathInfo> ExtractPathInfos(ScriptArray properties)
    {
        var result = new List<AzApiPathInfo>();

        for (var index = 0; index < properties.Count; index++)
        {
            if (properties[index] is not ScriptObject prop)
            {
                continue;
            }

            var rawPath = prop["path"]?.ToString() ?? string.Empty;
            var normalizedPath = RemovePropertiesPrefix(rawPath);
            var segments = SplitPathSegments(normalizedPath);

            result.Add(new AzApiPathInfo(index, normalizedPath, segments));
        }

        return result;
    }

    /// <summary>
    /// Builds array grouping candidates keyed by their outermost array path.
    /// </summary>
    /// <param name="pathInfos">The normalized path metadata.</param>
    /// <returns>Grouping candidates for array paths.</returns>
    /// <remarks>
    /// The outermost array dimension is used to keep grouping single-level for MVP behavior.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiGroupingCandidate> BuildArrayGroupCandidates(IReadOnlyList<AzApiPathInfo> pathInfos)
    {
        var candidates = new Dictionary<string, List<int>>();
        var segmentCounts = new Dictionary<string, int>();

        foreach (var info in pathInfos)
        {
            var arrayPath = GetOutermostArrayPath(info.Segments);
            if (arrayPath is null)
            {
                continue;
            }

            if (!candidates.TryGetValue(arrayPath, out var members))
            {
                members = new List<int>();
                candidates[arrayPath] = members;
                segmentCounts[arrayPath] = SplitPathSegments(arrayPath).Count;
            }

            members.Add(info.Index);
        }

        return candidates
            .Select(kvp => new AzApiGroupingCandidate(kvp.Key, kvp.Value, segmentCounts[kvp.Key]))
            .ToList();
    }

    /// <summary>
    /// Builds non-array prefix grouping candidates while excluding indices that belong to array groups.
    /// </summary>
    /// <param name="pathInfos">The normalized path metadata.</param>
    /// <param name="excludedIndexes">Property indices already claimed by array groups.</param>
    /// <returns>Grouping candidates for non-array prefixes.</returns>
    /// <remarks>
    /// Prefixes are derived only from non-array segments to avoid grouping within array items.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiGroupingCandidate> BuildNonArrayPrefixCandidates(
        IReadOnlyList<AzApiPathInfo> pathInfos,
        HashSet<int> excludedIndexes)
    {
        var candidates = new Dictionary<string, List<int>>();
        var segmentCounts = new Dictionary<string, int>();

        foreach (var info in pathInfos)
        {
            if (excludedIndexes.Contains(info.Index))
            {
                continue;
            }

            foreach (var prefix in GetNonArrayPrefixes(info.Segments))
            {
                if (!candidates.TryGetValue(prefix, out var members))
                {
                    members = new List<int>();
                    candidates[prefix] = members;
                    segmentCounts[prefix] = SplitPathSegments(prefix).Count;
                }

                members.Add(info.Index);
            }
        }

        return candidates
            .Select(kvp => new AzApiGroupingCandidate(kvp.Key, kvp.Value, segmentCounts[kvp.Key]))
            .ToList();
    }

    /// <summary>
    /// Filters candidates by the fixed threshold and maps them to grouped prefix descriptors.
    /// </summary>
    /// <param name="candidates">The candidate groups to evaluate.</param>
    /// <param name="kind">The grouping kind to apply to the selected groups.</param>
    /// <returns>Grouped prefix descriptors that meet the threshold.</returns>
    /// <remarks>
    /// The fixed threshold is three attributes per group as defined in the feature specification.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiGroupedPrefix> SelectGroupsByThreshold(
        IReadOnlyList<AzApiGroupingCandidate> candidates,
        AzApiGroupedPrefixKind kind)
    {
        const int threshold = 3;

        return candidates
            .Where(candidate => candidate.MemberIndexes.Count >= threshold)
            .Select(candidate => new AzApiGroupedPrefix(
                candidate.Path,
                kind,
                candidate.MemberIndexes))
            .ToList();
    }

    /// <summary>
    /// Selects non-array prefix groups while enforcing the "longest common prefix wins" rule.
    /// </summary>
    /// <param name="candidates">The candidate prefix groups.</param>
    /// <returns>Grouped prefix descriptors that remain after overlap filtering.</returns>
    /// <remarks>
    /// Any prefix that is a parent of another qualifying prefix is suppressed to avoid nested sections.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<AzApiGroupedPrefix> SelectLongestPrefixGroups(
        List<AzApiGroupingCandidate> candidates)
    {
        var thresholdGroups = SelectGroupsByThreshold(candidates, AzApiGroupedPrefixKind.Prefix);

        var qualified = thresholdGroups
            .ConvertAll(group => new
            {
                Group = group,
                SegmentCount = SplitPathSegments(group.Path).Count
            });

        var suppressedParents = new HashSet<string>();

        foreach (var group in qualified.OrderByDescending(item => item.SegmentCount))
        {
            foreach (var potentialParent in qualified)
            {
                if (group.SegmentCount <= potentialParent.SegmentCount)
                {
                    continue;
                }

                if (IsParentPrefix(potentialParent.Group.Path, group.Group.Path))
                {
                    suppressedParents.Add(potentialParent.Group.Path);
                }
            }
        }

        return qualified
            .Where(item => !suppressedParents.Contains(item.Group.Path))
            .Select(item => item.Group)
            .ToList();
    }

    /// <summary>
    /// Determines whether a potential parent prefix is a strict prefix of a child prefix path.
    /// </summary>
    /// <param name="parent">The parent prefix candidate.</param>
    /// <param name="child">The child prefix candidate.</param>
    /// <returns><c>true</c> if the parent is a strict prefix of the child; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// This comparison uses dot boundaries to avoid treating <c>foo</c> as a prefix of <c>foobar</c>.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static bool IsParentPrefix(string parent, string child)
    {
        if (child.Length <= parent.Length)
        {
            return false;
        }

        return child.StartsWith(parent + ".", StringComparison.Ordinal);
    }

    /// <summary>
    /// Splits a dot-separated property path into segments.
    /// </summary>
    /// <param name="path">The normalized property path.</param>
    /// <returns>Ordered list of segments for the path.</returns>
    /// <remarks>
    /// Empty segments are ignored to keep grouping deterministic with malformed input.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static List<string> SplitPathSegments(string path)
    {
        return path
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    /// <summary>
    /// Gets the outermost array path for a sequence of path segments.
    /// </summary>
    /// <param name="segments">The path segments to analyze.</param>
    /// <returns>The array prefix path without index notation, or <c>null</c> if none exists.</returns>
    /// <remarks>
    /// Only the first array segment is considered to enforce single-level array grouping.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static string? GetOutermostArrayPath(IReadOnlyList<string> segments)
    {
        var pathSegments = new List<string>();

        foreach (var segment in segments)
        {
            if (TryGetArraySegmentName(segment, out var arrayName))
            {
                pathSegments.Add(arrayName);
                return string.Join('.', pathSegments);
            }

            pathSegments.Add(segment);
        }

        return null;
    }

    /// <summary>
    /// Determines whether the segment represents an array index and returns the base name.
    /// </summary>
    /// <param name="segment">The path segment to inspect.</param>
    /// <param name="arrayName">The array name without index notation when detected.</param>
    /// <returns><c>true</c> if the segment represents an array; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Array segments are expected to be in the form <c>name[0]</c> as produced by JSON flattening.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static bool TryGetArraySegmentName(string segment, out string arrayName)
    {
        var indexStart = segment.IndexOf('[', StringComparison.Ordinal);
        if (indexStart <= 0)
        {
            arrayName = string.Empty;
            return false;
        }

        arrayName = segment.Substring(0, indexStart);
        return true;
    }

    /// <summary>
    /// Generates non-array prefix paths for grouping based on the provided segments.
    /// </summary>
    /// <param name="segments">The path segments to evaluate.</param>
    /// <returns>Enumerable of non-array prefix paths.</returns>
    /// <remarks>
    /// Prefix generation stops before the first array segment to avoid grouping within array items.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static IEnumerable<string> GetNonArrayPrefixes(IReadOnlyList<string> segments)
    {
        var firstArrayIndex = GetFirstArraySegmentIndex(segments);
        var prefixLimit = firstArrayIndex >= 0 ? firstArrayIndex : segments.Count - 1;

        if (prefixLimit <= 0)
        {
            yield break;
        }

        for (var length = 1; length <= prefixLimit; length++)
        {
            yield return string.Join('.', segments.Take(length));
        }
    }

    /// <summary>
    /// Finds the index of the first array segment in a path.
    /// </summary>
    /// <param name="segments">The path segments to inspect.</param>
    /// <returns>The index of the first array segment, or <c>-1</c> if none are found.</returns>
    /// <remarks>
    /// Detecting the first array segment ensures grouping respects the MVP single-level array rule.
    /// Related feature: docs/features/050-azapi-attribute-grouping/specification.md.
    /// </remarks>
    private static int GetFirstArraySegmentIndex(IReadOnlyList<string> segments)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (TryGetArraySegmentName(segments[i], out _))
            {
                return i;
            }
        }

        return -1;
    }
}
