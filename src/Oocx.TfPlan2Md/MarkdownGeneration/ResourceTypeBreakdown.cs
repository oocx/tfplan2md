namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Breakdown of resource types for a specific action.
/// </summary>
/// <param name="Type">The resource type name.</param>
/// <param name="Count">The count of resources for the type.</param>
internal record ResourceTypeBreakdown(string Type, int Count);
