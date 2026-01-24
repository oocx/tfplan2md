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
    /// Groups properties by their nested object parent if the parent has more than 3 attributes.
    /// </summary>
    /// <param name="properties">The list of properties to group.</param>
    /// <returns>A tuple with main properties and nested groups.</returns>
    private static (ScriptArray MainProps, Dictionary<string, ScriptArray> NestedGroups) GroupPropertiesByNestedObject(ScriptArray properties)
    {
        var nestedObjectCounts = CountNestedObjects(properties);
        var nestedObjectsToGroup = SelectGroupedParents(nestedObjectCounts);
        return AssignGroupedProperties(properties, nestedObjectsToGroup);
    }

    /// <summary>
    /// Counts first-level nested objects for grouping decisions.
    /// </summary>
    /// <param name="properties">The properties to analyze.</param>
    /// <returns>Dictionary of parent path counts.</returns>
    private static Dictionary<string, int> CountNestedObjects(ScriptArray properties)
    {
        var nestedObjectCounts = new Dictionary<string, int>();

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var segments = RemovePropertiesPrefix(path).Split('.');

                if (segments.Length < 2)
                {
                    continue;
                }

                var firstLevelParent = segments[0];
                if (firstLevelParent.Contains('['))
                {
                    continue;
                }

                if (!nestedObjectCounts.TryGetValue(firstLevelParent, out var count))
                {
                    count = 0;
                }

                nestedObjectCounts[firstLevelParent] = count + 1;
            }
        }

        return nestedObjectCounts;
    }

    /// <summary>
    /// Selects which parent paths should be grouped based on count thresholds.
    /// </summary>
    /// <param name="nestedObjectCounts">The parent path counts.</param>
    /// <returns>Set of parent paths to group.</returns>
    private static HashSet<string> SelectGroupedParents(Dictionary<string, int> nestedObjectCounts)
    {
        return nestedObjectCounts
            .Where(kvp => kvp.Value > 3)
            .Select(kvp => kvp.Key)
            .ToHashSet();
    }

    /// <summary>
    /// Assigns properties to either main or nested groups based on grouped parents.
    /// </summary>
    /// <param name="properties">The properties to assign.</param>
    /// <param name="nestedObjectsToGroup">The parent paths to group.</param>
    /// <returns>Tuple containing main properties and nested groups.</returns>
    private static (ScriptArray MainProps, Dictionary<string, ScriptArray> NestedGroups) AssignGroupedProperties(
        ScriptArray properties,
        HashSet<string> nestedObjectsToGroup)
    {
        var mainProps = new ScriptArray();
        var nestedGroups = new Dictionary<string, ScriptArray>();

        foreach (var item in properties)
        {
            if (item is ScriptObject prop)
            {
                var path = prop["path"]?.ToString() ?? string.Empty;
                var segments = RemovePropertiesPrefix(path).Split('.');

                if (segments.Length >= 2)
                {
                    var firstLevelParent = segments[0];
                    if (!firstLevelParent.Contains('[') && nestedObjectsToGroup.Contains(firstLevelParent))
                    {
                        if (!nestedGroups.TryGetValue(firstLevelParent, out var groupArray))
                        {
                            groupArray = new ScriptArray();
                            nestedGroups[firstLevelParent] = groupArray;
                        }

                        groupArray.Add(item);
                        continue;
                    }
                }

                mainProps.Add(item);
            }
        }

        return (mainProps, nestedGroups);
    }
}
