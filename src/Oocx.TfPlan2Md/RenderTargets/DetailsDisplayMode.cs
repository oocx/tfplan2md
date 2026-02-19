namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Specifies the display mode for resource details blocks in the markdown report.
/// </summary>
/// <remarks>
/// Controls whether resource &lt;details&gt; elements are rendered with the 'open' attribute.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </remarks>
internal enum DetailsDisplayMode
{
    /// <summary>
    /// Automatically expand only resources with code analysis findings.
    /// Resources without findings are collapsed.
    /// </summary>
    Auto,

    /// <summary>
    /// All resource details blocks are expanded by default (all have 'open' attribute).
    /// </summary>
    Open,

    /// <summary>
    /// All resource details blocks are collapsed by default (no 'open' attribute).
    /// </summary>
    Closed
}
