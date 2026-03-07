namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers;

/// <summary>
/// Render-ready create/delete body plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="TableProperties">Standalone table properties.</param>
/// <param name="PrefixGroups">Grouped prefix sections.</param>
/// <param name="ArrayGroups">Grouped array sections.</param>
/// <param name="LargeProperties">Large-value sections.</param>
internal sealed record AzApiCreateDeleteRenderPlan(
    IReadOnlyList<AzApiCreateDeletePropertyPlan> TableProperties,
    IReadOnlyList<AzApiCreateDeletePrefixGroupPlan> PrefixGroups,
    IReadOnlyList<AzApiCreateDeleteArrayGroupPlan> ArrayGroups,
    IReadOnlyList<AzApiCreateDeletePropertyPlan> LargeProperties)
{
    /// <summary>
    /// Gets a value indicating whether the main table should be rendered.
    /// </summary>
    internal bool ShouldRenderMainTable => TableProperties.Count > 0 || (PrefixGroups.Count == 0 && ArrayGroups.Count == 0);

    /// <summary>
    /// Gets a value indicating whether no properties remain to render.
    /// </summary>
    internal bool IsEmpty => TableProperties.Count == 0 && PrefixGroups.Count == 0 && ArrayGroups.Count == 0 && LargeProperties.Count == 0;
}

/// <summary>
/// Render-ready update body plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="TableProperties">Standalone changed properties.</param>
/// <param name="PrefixGroups">Grouped prefix sections with changes.</param>
/// <param name="ArrayGroups">Grouped array sections with changes.</param>
/// <param name="LargeProperties">Large-value change sections.</param>
internal sealed record AzApiUpdateRenderPlan(
    IReadOnlyList<AzApiUpdatePropertyPlan> TableProperties,
    IReadOnlyList<AzApiUpdatePrefixGroupPlan> PrefixGroups,
    IReadOnlyList<AzApiUpdateArrayGroupPlan> ArrayGroups,
    IReadOnlyList<AzApiUpdatePropertyPlan> LargeProperties)
{
    /// <summary>
    /// Gets a value indicating whether any visible changes remain.
    /// </summary>
    internal bool HasChanges => TableProperties.Count > 0 || PrefixGroups.Count > 0 || ArrayGroups.Count > 0 || LargeProperties.Count > 0;
}

/// <summary>
/// Render-ready create/delete property plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="DisplayPath">Display path for markdown output.</param>
/// <param name="Value">Rendered value.</param>
/// <param name="IsSensitive">Whether the value should be masked.</param>
/// <param name="IsLarge">Whether the value exceeds the large-value threshold.</param>
internal sealed record AzApiCreateDeletePropertyPlan(string DisplayPath, object? Value, bool IsSensitive, bool IsLarge);

/// <summary>
/// Render-ready update property plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="DisplayPath">Display path for markdown output.</param>
/// <param name="Before">Before-state value.</param>
/// <param name="After">After-state value.</param>
/// <param name="IsSensitive">Whether the value should be masked.</param>
/// <param name="IsLarge">Whether the value exceeds the large-value threshold.</param>
/// <param name="IsChanged">Whether the property is changed.</param>
internal sealed record AzApiUpdatePropertyPlan(string DisplayPath, object? Before, object? After, bool IsSensitive, bool IsLarge, bool IsChanged);

/// <summary>
/// Render-ready create/delete prefix-group plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="Prefix">Grouped prefix path.</param>
/// <param name="Properties">Properties in the group.</param>
internal sealed record AzApiCreateDeletePrefixGroupPlan(string Prefix, IReadOnlyList<AzApiCreateDeletePropertyPlan> Properties);

/// <summary>
/// Render-ready update prefix-group plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="Prefix">Grouped prefix path.</param>
/// <param name="Properties">Properties in the group.</param>
internal sealed record AzApiUpdatePrefixGroupPlan(string Prefix, IReadOnlyList<AzApiUpdatePropertyPlan> Properties);

/// <summary>
/// Render-ready create/delete array-group plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="ArrayPath">Grouped array path.</param>
/// <param name="Items">Array items in display order.</param>
internal sealed record AzApiCreateDeleteArrayGroupPlan(string ArrayPath, IReadOnlyList<AzApiCreateDeleteArrayItem> Items);

/// <summary>
/// Render-ready update array-group plan.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="ArrayPath">Grouped array path.</param>
/// <param name="Items">Array items in display order.</param>
internal sealed record AzApiUpdateArrayGroupPlan(string ArrayPath, IReadOnlyList<AzApiUpdateArrayItem> Items);

/// <summary>
/// Render-ready create/delete array item.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="Index">Array item index.</param>
/// <param name="Entries">Array item entries.</param>
internal sealed record AzApiCreateDeleteArrayItem(int Index, IReadOnlyList<AzApiCreateDeleteArrayItemEntry> Entries);

/// <summary>
/// Render-ready update array item.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="Index">Array item index.</param>
/// <param name="Entries">Array item entries.</param>
internal sealed record AzApiUpdateArrayItem(int Index, IReadOnlyList<AzApiUpdatePropertyPlan> Entries);

/// <summary>
/// Render-ready create/delete array item entry.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="DisplayPath">Display path for markdown output.</param>
/// <param name="Value">Rendered value.</param>
/// <param name="IsSensitive">Whether the value should be masked.</param>
internal sealed record AzApiCreateDeleteArrayItemEntry(string DisplayPath, object? Value, bool IsSensitive);

/// <summary>
/// Raw comparison property used while planning update rendering.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
/// <param name="Path">Flattened path.</param>
/// <param name="Before">Before-state value.</param>
/// <param name="After">After-state value.</param>
/// <param name="IsSensitive">Whether the property is sensitive.</param>
/// <param name="IsLarge">Whether the property exceeds the large-value threshold.</param>
/// <param name="IsChanged">Whether the property is changed.</param>
internal sealed record AzApiComparisonProperty(string Path, object? Before, object? After, bool IsSensitive, bool IsLarge, bool IsChanged);
