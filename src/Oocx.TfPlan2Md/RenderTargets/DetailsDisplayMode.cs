namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Specifies how resource details blocks are rendered (open or collapsed) in the report.
/// </summary>
/// <remarks>
/// Controls whether the HTML <c>&lt;details&gt;</c> element for each resource change is expanded or
/// collapsed when the report is first viewed. Related feature: docs/features/092-details-display-mode/specification.md.
/// </remarks>
internal enum DetailsDisplayMode
{
    /// <summary>
    /// Resource details blocks are expanded when the resource has code analysis warnings,
    /// including warnings on merged child resources. This is the default behaviour.
    /// </summary>
    OpenOnWarnings,

    /// <summary>
    /// All resource details blocks are always expanded.
    /// </summary>
    Open,

    /// <summary>
    /// All resource details blocks are always collapsed.
    /// </summary>
    Closed
}
