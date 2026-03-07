using System.Diagnostics.CodeAnalysis;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Builds render-ready AzApi body plans before markdown emission begins.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
[SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Planner centralizes existing AzApi comparison, grouping, and sensitivity rules so renderers can stay emission-focused.")]
internal static partial class AzApiBodyRenderPlanner
{
    /// <summary>
    /// Builds a create/delete plan from the raw body payload.
    /// </summary>
    /// <param name="body">Body JSON value.</param>
    /// <param name="sensitivity">Sensitivity structure for body values.</param>
    /// <param name="showSensitive">Whether sensitive values may be shown.</param>
    /// <returns>A render-ready plan.</returns>
    internal static AzApiCreateDeleteRenderPlan BuildCreateDeletePlan(object body, object? sensitivity, bool showSensitive)
    {
        var flattened = AzApiBodyFlattener.Flatten(body).ToList();
        var sensitivePaths = AzApiSensitivityHelper.Flatten(sensitivity);
        if (!showSensitive)
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
        var groups = AzApiGrouping.IdentifyGroups(smallProperties);
        var groupedIndexes = groups.SelectMany(group => group.MemberIndexes).ToHashSet();

        var tableProperties = smallProperties
            .Where((_, index) => !groupedIndexes.Contains(index))
            .Select(property => CreatePropertyPlan(property, sensitivePaths, showSensitive))
            .ToList();

        List<AzApiCreateDeletePrefixGroupPlan> prefixGroups = [];
        List<AzApiCreateDeleteArrayGroupPlan> arrayGroups = [];

        foreach (var group in groups)
        {
            var groupProperties = group.MemberIndexes
                .Where(index => index >= 0 && index < smallProperties.Count)
                .Select(index => smallProperties[index])
                .ToList();

            if (group.Kind == AzApiGroupKind.Array)
            {
                arrayGroups.Add(BuildCreateDeleteArrayGroupPlan(group.Prefix, groupProperties, sensitivePaths, showSensitive));
                continue;
            }

            prefixGroups.Add(new AzApiCreateDeletePrefixGroupPlan(
                group.Prefix,
                groupProperties.ConvertAll(property => new AzApiCreateDeletePropertyPlan(
                    AzApiGrouping.RemoveNestedPrefix(property.Path, group.Prefix),
                    property.Value,
                    IsSensitive(property.Path, sensitivePaths, showSensitive),
                    property.IsLarge))));
        }

        var largePropertyPlans = largeProperties.ConvertAll(property => CreatePropertyPlan(property, sensitivePaths, showSensitive));

        return new AzApiCreateDeleteRenderPlan(tableProperties, prefixGroups, arrayGroups, largePropertyPlans);
    }

    /// <summary>
    /// Builds an update plan from raw before/after payloads.
    /// </summary>
    /// <param name="beforeBody">Before-state body value.</param>
    /// <param name="afterBody">After-state body value.</param>
    /// <param name="beforeSensitive">Before-state sensitivity structure.</param>
    /// <param name="afterSensitive">After-state sensitivity structure.</param>
    /// <param name="showSensitive">Whether sensitive values may be shown.</param>
    /// <param name="ignoreAzureIdCaseChanges">Whether casing-only Azure resource ID changes should be ignored.</param>
    /// <returns>A render-ready plan.</returns>
    [SuppressMessage("Maintainability", "CA1502:Avoid excessive complexity", Justification = "AzApi planning preserves historical comparison and grouping behavior while moving policy out of markdown emission.")]
    [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "AzApi planning still needs grouped and array-aware branching to preserve baseline output.")]
    [SuppressMessage("Design", "CA1506:Avoid excessive class coupling", Justification = "Update planning intentionally centralizes AzApi grouping and sensitivity rules in one place.")]
    internal static AzApiUpdateRenderPlan BuildUpdatePlan(
        object beforeBody,
        object afterBody,
        object? beforeSensitive,
        object? afterSensitive,
        bool showSensitive,
        bool ignoreAzureIdCaseChanges)
    {
        var allComparisons = Compare(beforeBody, afterBody, beforeSensitive, afterSensitive, showUnchanged: true, showSensitive, ignoreAzureIdCaseChanges);
        var changedComparisons = Compare(beforeBody, afterBody, beforeSensitive, afterSensitive, showUnchanged: false, showSensitive, ignoreAzureIdCaseChanges);

        var smallAll = allComparisons.Where(property => !property.IsLarge).ToList();
        var smallChanged = changedComparisons.Where(property => !property.IsLarge).ToList();
        var largeChanged = changedComparisons.Where(property => property.IsLarge).ToList();

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

        var tableProperties = smallAll
            .Where((_, index) => changedIndexesInAll.Contains(index) && !groupedIndexes.Contains(index))
            .Select(CreateUpdatePropertyPlan)
            .ToList();

        List<AzApiUpdatePrefixGroupPlan> prefixGroups = [];
        List<AzApiUpdateArrayGroupPlan> arrayGroups = [];

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

                arrayGroups.Add(BuildUpdateArrayGroupPlan(group.Prefix, groupProperties, localChanged));
                continue;
            }

            prefixGroups.Add(new AzApiUpdatePrefixGroupPlan(
                group.Prefix,
                groupProperties.ConvertAll(property => new AzApiUpdatePropertyPlan(
                    AzApiGrouping.RemoveNestedPrefix(property.Path, group.Prefix),
                    property.Before,
                    property.After,
                    property.IsSensitive,
                    property.IsLarge,
                    property.IsChanged))));
        }

        var largePropertyPlans = largeChanged.ConvertAll(CreateUpdatePropertyPlan);

        return new AzApiUpdateRenderPlan(tableProperties, prefixGroups, arrayGroups, largePropertyPlans);
    }

    /// <summary>
    /// Converts a flattened property into a render-ready property plan.
    /// </summary>
    /// <param name="property">Source property.</param>
    /// <param name="sensitivePaths">Sensitive path set.</param>
    /// <param name="showSensitive">Whether sensitive values may be shown.</param>
    /// <returns>A property plan.</returns>
    private static AzApiCreateDeletePropertyPlan CreatePropertyPlan(
        AzApiBodyProperty property,
        HashSet<string> sensitivePaths,
        bool showSensitive)
    {
        return new AzApiCreateDeletePropertyPlan(
            AzApiGrouping.RemovePropertiesPrefix(property.Path),
            property.Value,
            IsSensitive(property.Path, sensitivePaths, showSensitive),
            property.IsLarge);
    }

    /// <summary>
    /// Converts a comparison property into a render-ready update plan entry.
    /// </summary>
    /// <param name="property">Source comparison property.</param>
    /// <returns>A property plan.</returns>
    private static AzApiUpdatePropertyPlan CreateUpdatePropertyPlan(AzApiComparisonProperty property)
    {
        return new AzApiUpdatePropertyPlan(
            AzApiGrouping.RemovePropertiesPrefix(property.Path),
            property.Before,
            property.After,
            property.IsSensitive,
            property.IsLarge,
            property.IsChanged);
    }

    /// <summary>
    /// Determines whether a path should be masked in markdown output.
    /// </summary>
    /// <param name="path">Flattened path.</param>
    /// <param name="sensitivePaths">Sensitive path set.</param>
    /// <param name="showSensitive">Whether sensitive values may be shown.</param>
    /// <returns>True when the path should be masked.</returns>
    private static bool IsSensitive(string path, HashSet<string> sensitivePaths, bool showSensitive)
    {
        return !showSensitive && AzApiSensitivityHelper.IsPathSensitive(path, sensitivePaths);
    }
}
