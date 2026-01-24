namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Specifies the target platform for markdown rendering.
/// </summary>
/// <remarks>
/// Different platforms (GitHub, Azure DevOps) have different markdown rendering capabilities
/// and optimal formatting strategies. This enum enables platform-specific rendering choices.
/// Related feature: docs/features/047-provider-code-separation/specification.md.
/// </remarks>
internal enum RenderTarget
{
    /// <summary>
    /// Target GitHub pull request rendering (simple diff format).
    /// </summary>
    GitHub,

    /// <summary>
    /// Target Azure DevOps pull request rendering (inline HTML diff format).
    /// </summary>
    AzureDevOps
}
