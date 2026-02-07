namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Centralizes action icon glyphs used across view models and summaries.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
internal static class ActionIcons
{
    /// <summary>
    /// Icon used for added resources or rows.
    /// </summary>
    internal const string Add = "➕";

    /// <summary>
    /// Icon used for updated resources or rows.
    /// </summary>
    internal const string Update = "🔄";

    /// <summary>
    /// Icon used for deleted resources or rows.
    /// </summary>
    internal const string Delete = "❌";

    /// <summary>
    /// Icon used for unchanged resources or rows.
    /// </summary>
    internal const string Unchanged = "⏺️";

    /// <summary>
    /// Icon used for replace actions.
    /// </summary>
    internal const string Replace = "♻️";

    /// <summary>
    /// Placeholder icon for no-op actions.
    /// </summary>
    internal const string NoOp = " ";
}
