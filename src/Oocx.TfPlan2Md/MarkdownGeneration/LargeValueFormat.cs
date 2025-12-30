namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Defines available rendering formats for large attribute values.
/// Related feature: docs/features/006-large-attribute-value-display/specification.md
/// </summary>
public enum LargeValueFormat
{
    /// <summary>
    /// Azure DevOps-optimized inline diff with HTML styling.
    /// </summary>
    InlineDiff,

    /// <summary>
    /// Simple diff fenced code block for GitHub and other platforms.
    /// </summary>
    SimpleDiff
}
