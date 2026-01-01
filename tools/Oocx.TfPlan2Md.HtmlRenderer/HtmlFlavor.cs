namespace Oocx.TfPlan2Md.HtmlRenderer;

/// <summary>
/// Enumerates supported HTML rendering flavors aligned with target platforms.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
internal enum HtmlFlavor
{
    /// <summary>
    /// Approximates GitHub pull request comment rendering.
    /// </summary>
    GitHub,

    /// <summary>
    /// Approximates Azure DevOps pull request and wiki rendering.
    /// </summary>
    AzureDevOps
}
