namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Specifies which displayable resource drift entries are included in a report.
/// </summary>
internal enum DriftDisplayMode
{
    /// <summary>
    /// Includes every displayable drift entry.
    /// </summary>
    All,

    /// <summary>
    /// Includes drift only for resources with a displayable planned change.
    /// </summary>
    Relevant,

    /// <summary>
    /// Omits drift from the report.
    /// </summary>
    None
}
